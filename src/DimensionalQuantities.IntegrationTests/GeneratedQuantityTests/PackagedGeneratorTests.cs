using System.Reflection;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Locator;
using Microsoft.Build.Utilities.ProjectCreation;
using Xunit;

namespace DimensionalQuantities.IntegrationTests.GeneratedQuantityTests;

[Collection("MSBuild")]
public sealed class PackagedGeneratorTests : IDisposable
{
	private readonly string _scratch = Directory.CreateTempSubdirectory("dimensional-quantities-").FullName;

	static PackagedGeneratorTests()
	{
		// MSBuild must be located before any Microsoft.Build type is loaded.
		MSBuildLocator.RegisterDefaults();
	}

	[Fact]
	public void PackagedGeneratorConvertsVoltageInConsumer()
	{
		string sourceProject = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "DimensionalQuantities", "DimensionalQuantities.csproj"));
		FileInfo nupkg = PackLibrary(sourceProject, _scratch);
		using PackageRepository repository = PackageRepository.Create(_scratch, new Uri("https://api.nuget.org/v3/index.json"));
		repository.Package(nupkg, out Package package);

		string consumerDirectory = Path.Combine(_scratch, "consumer");
		Directory.CreateDirectory(consumerDirectory);
		string consumerProject = Path.Combine(consumerDirectory, "Consumer.csproj");
		File.WriteAllText(Path.Combine(consumerDirectory, "Consumer.cs"), """
			namespace ConsumerProject;

			public static class Consumer
			{
				public static double ConvertVoltage(double volts)
				{
					Voltage voltage = Voltage.FromVolts(volts);
					return voltage.AsVolts();
				}

				public static double MultiplyVoltageCurrent(double volts, double amps)
				{
					Voltage voltage = Voltage.FromVolts(volts);
					Current current = Current.FromAmps(amps);
					Power power = voltage * current;
					return power.AsWatts();
				}
			}
			""");
		ProjectCreator creator = ProjectCreator.Templates.SdkCsproj(path: consumerProject, targetFramework: "netstandard2.0")
			.Property("LangVersion", "preview")
			.Property("WarningsAsErrors", "CS8032;CS8785;AD0001")
			.ItemPackageReference(package)
			.Save();

		creator.TryBuild(restore: true, out bool built, out BuildOutput output);
		using (output)
		{
			Assert.True(built, $"Consumer build failed.{Environment.NewLine}{output.GetConsoleLog()}");
		}

		string assemblyFile = Path.Combine(consumerDirectory, "bin", "Debug", "netstandard2.0", "Consumer.dll");
		Assembly assembly = Assembly.LoadFrom(assemblyFile);
		Type consumerType = assembly.GetType("ConsumerProject.Consumer") ?? throw new InvalidOperationException("Consumer type was not built.");
		MethodInfo convertVoltage = consumerType.GetMethod("ConvertVoltage") ?? throw new InvalidOperationException("Consumer Voltage conversion was not built.");
		MethodInfo multiplyVoltageCurrent = consumerType.GetMethod("MultiplyVoltageCurrent") ?? throw new InvalidOperationException("Consumer arithmetic was not built.");
		double result = Assert.IsType<double>(convertVoltage.Invoke(null, [12.5]));
		double product = Assert.IsType<double>(multiplyVoltageCurrent.Invoke(null, [12.5, 2.0]));
		Assert.Multiple(
			() => Assert.Equal(12.5, result),
			() => Assert.Equal(25.0, product));
	}

	[Fact]
	public void IndependentPackagesShareQuantitiesAndConsumerCanExtendArithmetic()
	{
		string sourceProject = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "DimensionalQuantities", "DimensionalQuantities.csproj"));
		FileInfo nupkg = PackLibrary(sourceProject, _scratch);
		using PackageRepository repository = PackageRepository.Create(_scratch, new Uri("https://api.nuget.org/v3/index.json"));
		repository.Package(nupkg, out Package quantities);

		ProjectCreator projectA = CreateProject("ProjectA", """
			namespace PackageA;
			public static class Producer
			{
				public static LengthCurrent Create() => Length.FromMetres(2) * Current.FromAmps(3);
				public static System.Collections.Generic.List<LengthCurrent> Values() => new() { Create(), Create() };
				public static Measurement Measure() => new();
			}
			public sealed class Measurement
			{
				public LengthCurrent Value => Producer.Create();
			}
			""", quantities);
		projectA.TryBuild(restore: true, target: "Pack", out bool builtA, out BuildOutput outputA);
		using (outputA)
		{
			Assert.True(builtA, outputA.GetConsoleLog());
		}
		repository.Package(new FileInfo(Path.Combine(_scratch, "ProjectA", "packages", "ProjectA.1.0.0.nupkg")), out Package packageA);

		ProjectCreator projectB = CreateProject("ProjectB", """
			namespace PackageB;
			public static class Consumer
			{
				public static LengthCurrent Create() => Length.FromMetres(5) * Current.FromAmps(2);
				public static double Read(LengthCurrent value) => value.AsSiBaseUnit();
				public static double Sum(System.Collections.Generic.IEnumerable<LengthCurrent> values)
				{
					double total = 0;
					foreach (LengthCurrent value in values)
					{
						total += value.AsSiBaseUnit();
					}
					return total;
				}
			}
			""", quantities);
		projectB.TryBuild(restore: true, target: "Pack", out bool builtB, out BuildOutput outputB);
		using (outputB)
		{
			Assert.True(builtB, outputB.GetConsoleLog());
		}
		repository.Package(new FileInfo(Path.Combine(_scratch, "ProjectB", "packages", "ProjectB.1.0.0.nupkg")), out Package packageB);

		ProjectCreator projectC = CreateProject("ProjectC", """
			using PackageA;
			public static class Application
			{
				public static double Direct() => PackageB.Consumer.Read(Producer.Create());
				public static double Collection() => PackageB.Consumer.Sum(Producer.Values());
				public static double Contained() => PackageB.Consumer.Read(Producer.Measure().Value);
				public static double Prefixed() => Producer.Create().AsPrefixedUnits(3, 0);
				public static double Product() => (Producer.Create() * Time.FromSeconds(4)).AsSiBaseUnit();
			}
			""", quantities, packageA, packageB);
		projectC.TryBuild(restore: true, out bool builtC, out BuildOutput outputC);
		using (outputC)
		{
			Assert.True(builtC, outputC.GetConsoleLog());
		}

		string outputDirectory = Path.Combine(_scratch, "ProjectC", "bin", "Debug", "netstandard2.0");
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
		double direct = (double)application.GetMethod("Direct")!.Invoke(null, null)!;
		double collection = (double)application.GetMethod("Collection")!.Invoke(null, null)!;
		double contained = (double)application.GetMethod("Contained")!.Invoke(null, null)!;
		double prefixed = (double)application.GetMethod("Prefixed")!.Invoke(null, null)!;
		double product = (double)application.GetMethod("Product")!.Invoke(null, null)!;

		Assert.Multiple(
			() => Assert.Equal(producedType, independentlyProducedType),
			() => Assert.Equal(producedType, acceptedType),
			() => Assert.Equal(6, direct),
			() => Assert.Equal(12, collection),
			() => Assert.Equal(6, contained),
			() => Assert.Equal(0.006, prefixed, 12),
			() => Assert.Equal(24, product));
	}

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

	private static FileInfo PackLibrary(string sourceProject, string scratch)
	{
		string packageOutput = Path.Combine(scratch, "package");
		Directory.CreateDirectory(packageOutput);
		Dictionary<string, string> properties = new()
		{
			["Configuration"] = typeof(PackagedGeneratorTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration,
			["NoBuild"] = "true",
			["PackageOutputPath"] = packageOutput,
			["NuGetAudit"] = "false",
		};
		using ProjectCollection collection = new(properties);
		Project project = collection.LoadProject(sourceProject);
		string packageId = project.GetPropertyValue("PackageId");
		string packageVersion = project.GetPropertyValue("PackageVersion");
		using BuildOutput output = BuildOutput.Create();
		BuildParameters parameters = new(collection) { Loggers = [output] };
		BuildRequestData request = new(project.CreateProjectInstance(), ["Pack"]);
		BuildResult result = BuildManager.DefaultBuildManager.Build(parameters, request);
		if (result.OverallResult != BuildResultCode.Success)
		{
			throw new InvalidOperationException($"Pack failed.{Environment.NewLine}{output.GetConsoleLog()}");
		}
		FileInfo nupkg = new(Path.Combine(packageOutput, $"{packageId}.{packageVersion}.nupkg"));
		if (!nupkg.Exists)
		{
			throw new FileNotFoundException("Pack did not create the expected package", nupkg.FullName);
		}
		return nupkg;
	}
}
