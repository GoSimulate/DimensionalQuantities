using System.Reflection;
using Microsoft.Build.Utilities.ProjectCreation;
using Xunit;

namespace DimensionalQuantities.IntegrationTests.Packaging;

[Collection("MSBuild")]
public sealed class PackageInteroperabilityTests : IClassFixture<PackageInteroperabilityFixture>, IDisposable
{
	private readonly PackageInteroperabilityFixture _fixture;
	private readonly PackageConsumerWorkspace _workspace;

	public PackageInteroperabilityTests(PackageInteroperabilityFixture fixture)
	{
		_fixture = fixture;
		_workspace = fixture.CreateWorkspace();
	}

	[Fact]
	public void AffineTemperatureConversionsAndDeltasWorkAcrossPackages()
	{
		using PackageRepository repository = PackageRepository.Create(_workspace.Directory, new Uri("https://api.nuget.org/v3/index.json"));
		PackageTestSupport.AddPackedPackage(repository, _fixture.GetPackage("DimensionalQuantities"));
		Package metric = PackageTestSupport.AddPackedPackage(repository, _fixture.GetPackage("DimensionalQuantities.Extensions.Metric"));
		Package imperial = PackageTestSupport.AddPackedPackage(repository, _fixture.GetPackage("DimensionalQuantities.Extensions.Imperial"));

		ProjectCreator measurements = _workspace.CreateProject("TemperatureMeasurements", """
			using DimensionalQuantities.Extensions.Metric;
			using DimensionalQuantities.TypeSystem;

			namespace TemperatureMeasurements;
			public static class Readings
			{
				public static Delta<Temperature> Difference() =>
					(Temperature.FromCelsius(20) - Temperature.FromCelsius(10)).AsDelta();
			}
			""", metric);
		_workspace.Pack(measurements, "TemperatureMeasurements", repository, out Package measurementPackage);

		ProjectCreator consumer = _workspace.CreateProject("TemperatureConsumer", """
			using DimensionalQuantities.Extensions.Imperial;
			using DimensionalQuantities.Extensions.Metric;
			using TemperatureMeasurements;

			public static class Consumer
			{
				public static double[] DifferenceInEachUnit()
				{
					var delta = Readings.Difference();
					return [delta.AsCelsius(), delta.AsFahrenheit(), delta.AsKelvin(), delta.AsRankine()];
				}

				public static double[] AbsoluteRoundTrips() =>
				[
					Temperature.FromCelsius(20).AsCelsius(),
					Temperature.FromFahrenheit(68).AsFahrenheit(),
					Temperature.FromKelvin(293.15).AsCelsius(),
					Temperature.FromRankine(527.67).AsFahrenheit()
				];
			}
			""", metric, imperial, measurementPackage);
		_workspace.Build(consumer);

		string outputDirectory = _workspace.OutputDirectory("TemperatureConsumer");
		using PackageAssemblyContext context = new(outputDirectory);
		Type type = context.LoadFromAssemblyPath(Path.Combine(outputDirectory, "TemperatureConsumer.dll"))
			.GetType("Consumer", throwOnError: true)!;
		double[] difference = (double[])type.GetMethod("DifferenceInEachUnit")!.Invoke(null, null)!;
		double[] absolute = (double[])type.GetMethod("AbsoluteRoundTrips")!.Invoke(null, null)!;

		Assert.Multiple(
			() => Assert.Equal([10d, 18d, 10d, 18d], difference),
			() => Assert.Equal(20d, absolute[0], 12),
			() => Assert.Equal(68d, absolute[1], 12),
			() => Assert.Equal(20d, absolute[2], 12),
			() => Assert.Equal(68d, absolute[3], 12));
	}

	[Fact]
	public void IndependentPackagesShareQuantitiesAndConsumerCanExtendArithmetic()
	{
		using PackageRepository repository = PackageRepository.Create(_workspace.Directory, new Uri("https://api.nuget.org/v3/index.json"));
		Package core = PackageTestSupport.AddPackedPackage(repository, _fixture.GetPackage("DimensionalQuantities"));
		Package metric = PackageTestSupport.AddPackedPackage(repository, _fixture.GetPackage("DimensionalQuantities.Extensions.Metric"));

		ProjectCreator projectA = _workspace.CreateProject("ProjectA", """
			namespace PackageA;
			public static class Producer
			{
				public static Voltage Create() => Voltage.FromSiBaseUnit(6);
				public static LengthCurrent CreateIntermediate() => Length.FromMetres(2) * Current.FromAmps(3);
				public static System.Collections.Generic.List<Voltage> Values() => new() { Create(), Create() };
				public static Measurement Measure() => new();
			}
			public sealed class Measurement
			{
				public Voltage Value => Producer.Create();
			}
			""", core);
		_workspace.Pack(projectA, "ProjectA", repository, out Package packageA);

		ProjectCreator projectB = _workspace.CreateProject("ProjectB", """
			using DimensionalQuantities.Extensions.Metric;
			namespace PackageB;
			public static class Consumer
			{
				public static Voltage Create() => Voltage.FromVolts(10);
				public static LengthCurrent CreateIntermediate() => Length.FromMetres(5) * Current.FromAmps(2);
				public static double ReadIntermediate(LengthCurrent value) => value.AsSiBaseUnit();
				public static double Read(Voltage value) => value.AsVolts();
				public static double Sum(System.Collections.Generic.IEnumerable<Voltage> values)
				{
					double total = 0;
					foreach (Voltage value in values)
					{
						total += value.AsVolts();
					}
					return total;
				}
			}
			""", metric);
		_workspace.Pack(projectB, "ProjectB", repository, out Package packageB);

		ProjectCreator projectC = _workspace.CreateProject("ProjectC", """
			using DimensionalQuantities.Extensions.Metric;
			using PackageA;
			public static class Application
			{
				public static double Direct() => PackageB.Consumer.Read(Producer.Create());
				public static double Collection() => PackageB.Consumer.Sum(Producer.Values());
				public static double Contained() => PackageB.Consumer.Read(Producer.Measure().Value);
				public static double Prefixed() => Producer.Create().AsPrefixedUnits(3);
				public static double Product() => (Producer.Create() * Current.FromAmps(4)).AsWatts();
				public static double Intermediate() => PackageB.Consumer.ReadIntermediate(Producer.CreateIntermediate());
				public static double IntermediateProduct() => (Producer.CreateIntermediate() * Time.FromSeconds(4)).AsSiBaseUnit();
			}
			""", metric, packageA, packageB);
		_workspace.Build(projectC);

		string outputDirectory = _workspace.OutputDirectory("ProjectC");
		using PackageAssemblyContext context = new(outputDirectory);
		Assembly assemblyA = context.LoadFromAssemblyPath(Path.Combine(outputDirectory, "ProjectA.dll"));
		Assembly assemblyB = context.LoadFromAssemblyPath(Path.Combine(outputDirectory, "ProjectB.dll"));
		Assembly assemblyC = context.LoadFromAssemblyPath(Path.Combine(outputDirectory, "ProjectC.dll"));
		Type producer = assemblyA.GetType("PackageA.Producer", throwOnError: true)!;
		Type consumer = assemblyB.GetType("PackageB.Consumer", throwOnError: true)!;
		Type application = assemblyC.GetType("Application", throwOnError: true)!;
		Type producedType = producer.GetMethod("Create")!.ReturnType;
		Type independentlyProducedType = consumer.GetMethod("Create")!.ReturnType;
		Type acceptedType = consumer.GetMethod("Read")!.GetParameters().Single().ParameterType;
		Type intermediateFromA = producer.GetMethod("CreateIntermediate")!.ReturnType;
		Type intermediateFromB = consumer.GetMethod("CreateIntermediate")!.ReturnType;
		Type intermediateAccepted = consumer.GetMethod("ReadIntermediate")!.GetParameters().Single().ParameterType;

		Assert.Multiple(
			() => Assert.Equal(producedType, independentlyProducedType),
			() => Assert.Equal(producedType, acceptedType),
			() => Assert.Equal(intermediateFromA, intermediateFromB),
			() => Assert.Equal(intermediateFromA, intermediateAccepted),
			() => Assert.Equal(6, InvokeDouble(application, "Direct")),
			() => Assert.Equal(12, InvokeDouble(application, "Collection")),
			() => Assert.Equal(6, InvokeDouble(application, "Contained")),
			() => Assert.Equal(0.006, InvokeDouble(application, "Prefixed"), 12),
			() => Assert.Equal(24, InvokeDouble(application, "Product")),
			() => Assert.Equal(6, InvokeDouble(application, "Intermediate")),
			() => Assert.Equal(24, InvokeDouble(application, "IntermediateProduct")));
	}

	[Fact]
	public void AllUnitPackagesWorkInALibraryAndItsPackagedConsumer()
	{
		using PackageRepository repository = PackageRepository.Create(_workspace.Directory, new Uri("https://api.nuget.org/v3/index.json"));
		PackageTestSupport.AddPackedPackage(repository, _fixture.GetPackage("DimensionalQuantities"));
		Package metric = PackageTestSupport.AddPackedPackage(repository, _fixture.GetPackage("DimensionalQuantities.Extensions.Metric"));
		Package imperial = PackageTestSupport.AddPackedPackage(repository, _fixture.GetPackage("DimensionalQuantities.Extensions.Imperial"));
		Package nautical = PackageTestSupport.AddPackedPackage(repository, _fixture.GetPackage("DimensionalQuantities.Extensions.Nautical"));
		Package cgsEmag = PackageTestSupport.AddPackedPackage(repository, _fixture.GetPackage("DimensionalQuantities.Extensions.CgsEmag"));

		ProjectCreator library = _workspace.CreateProject("AllUnitsLibrary", """
			using DimensionalQuantities.Extensions.Metric;
			using DimensionalQuantities.Extensions.Imperial;
			using DimensionalQuantities.Extensions.Nautical;
			using DimensionalQuantities.Extensions.CgsEmag;

			namespace AllUnitsLibrary;
			public static class Measurements
			{
				public static Voltage Supply() => Voltage.FromVolts(0.00000001);
				public static Length Depth() => Length.FromFeet(6);
				public static Speed Travel() => Speed.FromKnots(1);
				public static MagneticFluxDensity Field() => MagneticFluxDensity.FromGauss(10_000);
				public static double[] LocalValues() => [Supply().AsVolts(), Depth().AsFeet(), Travel().AsKnots(), Field().AsGauss()];
			}
			""", metric, imperial, nautical, cgsEmag);
		_workspace.Pack(library, "AllUnitsLibrary", repository, out Package libraryPackage);

		ProjectCreator downstream = _workspace.CreateProject("AllUnitsDownstream", """
			using DimensionalQuantities.Extensions.Metric;
			using DimensionalQuantities.Extensions.Imperial;
			using DimensionalQuantities.Extensions.Nautical;
			using DimensionalQuantities.Extensions.CgsEmag;
			using AllUnitsLibrary;

			namespace AllUnitsDownstream;
			public static class Reader
			{
				public static double[] LocalValues() => Measurements.LocalValues();
				public static double[] ConvertedValues() =>
				[
					Measurements.Supply().AsAbvolts(),
					Measurements.Depth().AsFathoms(),
					Measurements.Depth().AsFeet(),
					Measurements.Travel().AsKnots(),
					Measurements.Field().AsTeslas()
				];
			}
			""", libraryPackage);
		_workspace.Pack(downstream, "AllUnitsDownstream", repository, out Package downstreamPackage);

		ProjectCreator application = _workspace.CreateProject("AllUnitsApplication", """
			public static class Application
			{
				public static double[] LocalValues() => AllUnitsDownstream.Reader.LocalValues();
				public static double[] ConvertedValues() => AllUnitsDownstream.Reader.ConvertedValues();
			}
			""", downstreamPackage);
		_workspace.Build(application);

		string outputDirectory = _workspace.OutputDirectory("AllUnitsApplication");
		using PackageAssemblyContext context = new(outputDirectory);
		Type type = context.LoadFromAssemblyPath(Path.Combine(outputDirectory, "AllUnitsApplication.dll"))
			.GetType("Application", throwOnError: true)!;
		double[] local = (double[])type.GetMethod("LocalValues")!.Invoke(null, null)!;
		double[] converted = (double[])type.GetMethod("ConvertedValues")!.Invoke(null, null)!;

		Assert.Multiple(
			() => Assert.Equal([0.00000001d, 6d, 1d, 10_000d], local),
			() => Assert.Equal(5, converted.Length),
			() => Assert.Equal(1d, converted[0]),
			() => Assert.Equal(1d, converted[1], 12),
			() => Assert.Equal(6d, converted[2]),
			() => Assert.Equal(1d, converted[3]),
			() => Assert.Equal(1d, converted[4]));
	}

	private static double InvokeDouble(Type type, string method, params object[] arguments)
		=> (double)type.GetMethod(method)!.Invoke(null, arguments)!;

	public void Dispose() => _workspace.Dispose();
}
