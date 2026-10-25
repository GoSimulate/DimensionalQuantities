using System.Reflection;
using Microsoft.Build.Locator;
using Microsoft.Build.Utilities.ProjectCreation;
using Xunit;

namespace DimensionalQuantities.IntegrationTests.Packaging;

[Collection("MSBuild")]
public sealed class PackagedGeneratorTests : IDisposable
{
	private readonly string _scratch = Directory.CreateTempSubdirectory("dimensional-quantities-").FullName;

	static PackagedGeneratorTests()
	{
		// MSBuild must be located before any Microsoft.Build type is loaded.
		if (!MSBuildLocator.IsRegistered)
		{
			MSBuildLocator.RegisterDefaults();
		}
	}

	[Fact]
	public void CorePackageProvidesBaseUnitsCatalogueAliasesAndArithmetic()
	{
		using PackageRepository repository = PackageRepository.Create(_scratch, new Uri("https://api.nuget.org/v3/index.json"));
		Package core = PackCore(repository);
		ProjectCreator consumer = CreateProject("CoreConsumer", """
			public static class Consumer
			{
				public static double ConvertLength() => Length.FromMetres(2.5).AsMetres();
				public static double ConvertCurrent() => Current.FromAmps(3).AsAmps();
				public static double CalculateArea() => (Length.FromMetres(2) * Length.FromMetres(3)).AsSiBaseUnit();
				public static double NamedArea() => Area.FromSiBaseUnit(6).AsSiBaseUnit();
			}
			""", core);
		Build(consumer);

		using PackageAssemblyContext context = new(OutputDirectory("CoreConsumer"));
		Type type = context.LoadFromAssemblyPath(Path.Combine(OutputDirectory("CoreConsumer"), "CoreConsumer.dll"))
			.GetType("Consumer", throwOnError: true)!;
		Assert.Multiple(
			() => Assert.Equal(2.5, InvokeDouble(type, "ConvertLength")),
			() => Assert.Equal(3, InvokeDouble(type, "ConvertCurrent")),
			() => Assert.Equal(6, InvokeDouble(type, "CalculateArea")),
			() => Assert.Equal(6, InvokeDouble(type, "NamedArea")));
	}

	[Fact]
	public void MetricPackageProvidesNamedConversionsAndArithmeticThroughCoreDependency()
	{
		using PackageRepository repository = PackageRepository.Create(_scratch, new Uri("https://api.nuget.org/v3/index.json"));
		PackCore(repository);
		Package metric = PackMetric(repository);
		ProjectCreator consumer = CreateProject("MetricConsumer", """
			using DimensionalQuantities.Extensions.Metric;

			public static class Consumer
			{
				public static double ConvertVoltage(double volts) => Voltage.FromVolts(volts).AsVolts();
				public static double MultiplyVoltageCurrent(double volts, double amps)
				{
					Power power = Voltage.FromVolts(volts) * Current.FromAmps(amps);
					return power.AsWatts();
				}
			}
			""", metric);
		Build(consumer);

		using PackageAssemblyContext context = new(OutputDirectory("MetricConsumer"));
		Type type = context.LoadFromAssemblyPath(Path.Combine(OutputDirectory("MetricConsumer"), "MetricConsumer.dll"))
			.GetType("Consumer", throwOnError: true)!;
		Assert.Multiple(
			() => Assert.Equal(12.5, InvokeDouble(type, "ConvertVoltage", 12.5)),
			() => Assert.Equal(25, InvokeDouble(type, "MultiplyVoltageCurrent", 12.5, 2.0)));
	}

	[Fact]
	public void ImperialPackageProvidesConversionsThroughCoreDependency()
	{
		using PackageRepository repository = PackageRepository.Create(_scratch, new Uri("https://api.nuget.org/v3/index.json"));
		PackCore(repository);
		Package imperial = PackSourceProject(repository, "DimensionalQuantities.Extensions.Imperial");
		ProjectCreator consumer = CreateProject("ImperialConsumer", """
			using DimensionalQuantities.Extensions.Imperial;

			public static class Consumer
			{
				public static double ConvertLength() => Length.FromFeet(6).AsSiBaseUnit();
				public static double ConvertTemperature() => Temperature.FromRankine(491.67).AsSiBaseUnit();
			}
			""", imperial);
		Build(consumer);

		using PackageAssemblyContext context = new(OutputDirectory("ImperialConsumer"));
		Type type = context.LoadFromAssemblyPath(Path.Combine(OutputDirectory("ImperialConsumer"), "ImperialConsumer.dll"))
			.GetType("Consumer", throwOnError: true)!;
		Assert.Multiple(
			() => Assert.Equal(1.8288, InvokeDouble(type, "ConvertLength"), 12),
			() => Assert.Equal(273.15, InvokeDouble(type, "ConvertTemperature"), 12));
	}

	[Fact]
	public void NauticalPackageProvidesConversionsThroughCoreDependency()
	{
		using PackageRepository repository = PackageRepository.Create(_scratch, new Uri("https://api.nuget.org/v3/index.json"));
		PackCore(repository);
		Package nautical = PackSourceProject(repository, "DimensionalQuantities.Extensions.Nautical");
		ProjectCreator consumer = CreateProject("NauticalConsumer", """
			using DimensionalQuantities.Extensions.Nautical;

			public static class Consumer
			{
				public static double ConvertLength() => Length.FromNauticalMiles(1).AsSiBaseUnit();
				public static double ConvertSpeed() => Speed.FromKnots(1).AsSiBaseUnit();
			}
			""", nautical);
		Build(consumer);

		using PackageAssemblyContext context = new(OutputDirectory("NauticalConsumer"));
		Type type = context.LoadFromAssemblyPath(Path.Combine(OutputDirectory("NauticalConsumer"), "NauticalConsumer.dll"))
			.GetType("Consumer", throwOnError: true)!;
		Assert.Multiple(
			() => Assert.Equal(1852, InvokeDouble(type, "ConvertLength")),
			() => Assert.Equal(1852d / 3600d, InvokeDouble(type, "ConvertSpeed")));
	}

	[Fact]
	public void CgsEmagPackageProvidesConversionsThroughCoreDependency()
	{
		using PackageRepository repository = PackageRepository.Create(_scratch, new Uri("https://api.nuget.org/v3/index.json"));
		PackCore(repository);
		Package cgsEmag = PackSourceProject(repository, "DimensionalQuantities.Extensions.CgsEmag");
		ProjectCreator consumer = CreateProject("CgsEmagConsumer", """
			using DimensionalQuantities.Extensions.CgsEmag;

			public static class Consumer
			{
				public static double ConvertFluxDensity() => MagneticFluxDensity.FromGauss(10_000).AsSiBaseUnit();
				public static double ConvertCurrent() => Current.FromAbamperes(1).AsSiBaseUnit();
				public static double ConvertCharge() => ElectricCharge.FromAbcoulombs(1).AsSiBaseUnit();
			}
			""", cgsEmag);
		Build(consumer);

		using PackageAssemblyContext context = new(OutputDirectory("CgsEmagConsumer"));
		Type type = context.LoadFromAssemblyPath(Path.Combine(OutputDirectory("CgsEmagConsumer"), "CgsEmagConsumer.dll"))
			.GetType("Consumer", throwOnError: true)!;
		Assert.Multiple(
			() => Assert.Equal(1, InvokeDouble(type, "ConvertFluxDensity")),
			() => Assert.Equal(10, InvokeDouble(type, "ConvertCurrent")),
			() => Assert.Equal(10, InvokeDouble(type, "ConvertCharge")));
	}

	[Fact]
	public void MetricNamedConversionsRequireNamespaceImport()
	{
		using PackageRepository repository = PackageRepository.Create(_scratch, new Uri("https://api.nuget.org/v3/index.json"));
		PackCore(repository);
		Package metric = PackMetric(repository);
		ProjectCreator consumer = CreateProject("MissingMetricImport", """
			public static class Consumer
			{
				public static double Convert() => Voltage.FromVolts(12.5).AsVolts();
			}
			""", metric);
		consumer.TryBuild(restore: true, out bool built, out BuildOutput output);
		using (output)
		{
			string log = output.GetConsoleLog();
			Assert.Multiple(
				() => Assert.False(built, log),
				() => Assert.Contains(output.ErrorEvents, error => error.Code == "CS0117" && error.Message!.Contains("FromVolts", StringComparison.Ordinal)),
				() => Assert.DoesNotContain(output.ErrorEvents, error => error.Code is "CS8032" or "CS8785" or "AD0001"),
				() => Assert.DoesNotContain(output.WarningEvents, warning => warning.Code is "CS8032" or "CS8785" or "AD0001"));
		}
	}

	[Fact]
	public void IndependentPackagesShareQuantitiesAndConsumerCanExtendArithmetic()
	{
		using PackageRepository repository = PackageRepository.Create(_scratch, new Uri("https://api.nuget.org/v3/index.json"));
		Package core = PackCore(repository);
		Package metric = PackMetric(repository);

		ProjectCreator projectA = CreateProject("ProjectA", """
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
		Pack(projectA, "ProjectA", repository, out Package packageA);

		ProjectCreator projectB = CreateProject("ProjectB", """
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
		Pack(projectB, "ProjectB", repository, out Package packageB);

		ProjectCreator projectC = CreateProject("ProjectC", """
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
		Build(projectC);

		string outputDirectory = OutputDirectory("ProjectC");
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
		using PackageRepository repository = PackageRepository.Create(_scratch, new Uri("https://api.nuget.org/v3/index.json"));
		PackCore(repository);
		Package metric = PackMetric(repository);
		Package imperial = PackSourceProject(repository, "DimensionalQuantities.Extensions.Imperial");
		Package nautical = PackSourceProject(repository, "DimensionalQuantities.Extensions.Nautical");
		Package cgsEmag = PackSourceProject(repository, "DimensionalQuantities.Extensions.CgsEmag");

		ProjectCreator library = CreateProject("AllUnitsLibrary", """
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
		Pack(library, "AllUnitsLibrary", repository, out Package libraryPackage);

		ProjectCreator downstream = CreateProject("AllUnitsDownstream", """
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
		Pack(downstream, "AllUnitsDownstream", repository, out Package downstreamPackage);

		ProjectCreator application = CreateProject("AllUnitsApplication", """
			public static class Application
			{
				public static double[] LocalValues() => AllUnitsDownstream.Reader.LocalValues();
				public static double[] ConvertedValues() => AllUnitsDownstream.Reader.ConvertedValues();
			}
			""", downstreamPackage);
		Build(application);

		string outputDirectory = OutputDirectory("AllUnitsApplication");
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

	private Package PackCore(PackageRepository repository)
	{
		return PackSourceProject(repository, "DimensionalQuantities");
	}

	private Package PackMetric(PackageRepository repository)
	{
		return PackSourceProject(repository, "DimensionalQuantities.Extensions.Metric");
	}

	private Package PackSourceProject(PackageRepository repository, string projectName)
	{
		string sourceDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
		FileInfo packageFile = PackageTestSupport.PackLibrary(Path.Combine(sourceDirectory, projectName, projectName + ".csproj"), _scratch);
		return PackageTestSupport.AddPackedPackage(repository, packageFile);
	}

	private static void Build(ProjectCreator project)
	{
		project.TryBuild(restore: true, out bool built, out BuildOutput output);
		using (output)
		{
			if (!built)
			{
				throw new InvalidOperationException(output.GetConsoleLog());
			}
		}
	}

	private void Pack(ProjectCreator project, string name, PackageRepository repository, out Package package)
	{
		project.TryBuild(restore: true, target: "Pack", out bool built, out BuildOutput output);
		using (output)
		{
			if (!built)
			{
				throw new InvalidOperationException(output.GetConsoleLog());
			}
		}
		package = PackageTestSupport.AddPackedPackage(repository, new FileInfo(Path.Combine(_scratch, name, "packages", $"{name}.1.0.0.nupkg")));
	}

	private string OutputDirectory(string project) => Path.Combine(_scratch, project, "bin", "Debug", "netstandard2.0");

	private static double InvokeDouble(Type type, string method, params object[] arguments)
		=> (double)type.GetMethod(method)!.Invoke(null, arguments)!;

	private ProjectCreator CreateProject(string name, string source, params Package[] packages)
	{
		string directory = Path.Combine(_scratch, name);
		Directory.CreateDirectory(directory);
		File.WriteAllText(Path.Combine(directory, name + ".cs"), source);
		ProjectCreator creator = ProjectCreator.Templates.SdkCsproj(path: Path.Combine(directory, name + ".csproj"), targetFramework: "netstandard2.0")
			.Property("LangVersion", "preview")
			.Property("WarningsAsErrors", "CS8032;CS8785;AD0001")
			.Property("Version", "1.0.0")
			.Property("PackageOutputPath", Path.Combine(directory, "packages"))
			.Property("CopyLocalLockFileAssemblies", "true");
		foreach (Package package in packages)
		{
			creator.ItemPackageReference(package);
		}
		return creator.Save();
	}

	private sealed class PackageAssemblyContext : System.Runtime.Loader.AssemblyLoadContext, IDisposable
	{
		private readonly string _directory;

		public PackageAssemblyContext(string directory) : base(isCollectible: true)
		{
			_directory = directory;
		}

		protected override Assembly? Load(AssemblyName assemblyName)
		{
			string path = Path.Combine(_directory, assemblyName.Name + ".dll");
			return File.Exists(path) ? LoadFromAssemblyPath(path) : null;
		}

		public void Dispose() => Unload();
	}

	public void Dispose()
	{
		Directory.Delete(_scratch, recursive: true);
	}

}
