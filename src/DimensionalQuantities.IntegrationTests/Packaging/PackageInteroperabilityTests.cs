using System.Reflection;
using Microsoft.Build.Utilities.ProjectCreation;

namespace DimensionalQuantities.IntegrationTests.Packaging;

[NotInParallel]
[ClassDataSource<PackageInteroperabilityFixture>(Shared = SharedType.PerClass)]
public sealed class PackageInteroperabilityTests : IDisposable
{
	private readonly PackageInteroperabilityFixture _fixture;
	private readonly PackageConsumerWorkspace _workspace;

	public PackageInteroperabilityTests(PackageInteroperabilityFixture fixture)
	{
		_fixture = fixture;
		_workspace = fixture.CreateWorkspace();
	}

	[Test]
	public async Task AffineTemperatureConversionsAndDeltasWorkAcrossPackages()
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

		using (Assert.Multiple())
		{
			await Assert.That(difference).IsEquivalentTo((double[])[10d, 18d, 10d, 18d], TUnit.Assertions.Enums.CollectionOrdering.Matching);
			await Assert.That(Math.Round(absolute[0], 12)).IsEqualTo(Math.Round(20d, 12));
			await Assert.That(Math.Round(absolute[1], 12)).IsEqualTo(Math.Round(68d, 12));
			await Assert.That(Math.Round(absolute[2], 12)).IsEqualTo(Math.Round(20d, 12));
			await Assert.That(Math.Round(absolute[3], 12)).IsEqualTo(Math.Round(68d, 12));
		}
	}

	[Test]
	public async Task IndependentPackagesShareQuantitiesAndConsumerCanExtendArithmetic()
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

		using (Assert.Multiple())
		{
			await Assert.That(independentlyProducedType).IsEqualTo(producedType);
			await Assert.That(acceptedType).IsEqualTo(producedType);
			await Assert.That(intermediateFromB).IsEqualTo(intermediateFromA);
			await Assert.That(intermediateAccepted).IsEqualTo(intermediateFromA);
			await Assert.That(InvokeDouble(application, "Direct")).IsEqualTo(6);
			await Assert.That(InvokeDouble(application, "Collection")).IsEqualTo(12);
			await Assert.That(InvokeDouble(application, "Contained")).IsEqualTo(6);
			await Assert.That(Math.Round(InvokeDouble(application, "Prefixed"), 12)).IsEqualTo(Math.Round(0.006, 12));
			await Assert.That(InvokeDouble(application, "Product")).IsEqualTo(24);
			await Assert.That(InvokeDouble(application, "Intermediate")).IsEqualTo(6);
			await Assert.That(InvokeDouble(application, "IntermediateProduct")).IsEqualTo(24);
		}
	}

	[Test]
	public async Task AllUnitPackagesWorkInALibraryAndItsPackagedConsumer()
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

		using (Assert.Multiple())
		{
			await Assert.That(local).IsEquivalentTo((double[])[0.00000001d, 6d, 1d, 10_000d], TUnit.Assertions.Enums.CollectionOrdering.Matching);
			await Assert.That(converted.Length).IsEqualTo(5);
			await Assert.That(converted[0]).IsEqualTo(1d);
			await Assert.That(Math.Round(converted[1], 12)).IsEqualTo(Math.Round(1d, 12));
			await Assert.That(converted[2]).IsEqualTo(6d);
			await Assert.That(converted[3]).IsEqualTo(1d);
			await Assert.That(converted[4]).IsEqualTo(1d);
		}
	}

	private static double InvokeDouble(Type type, string method, params object[] arguments)
		=> (double)type.GetMethod(method)!.Invoke(null, arguments)!;

	public void Dispose() => _workspace.Dispose();
}
