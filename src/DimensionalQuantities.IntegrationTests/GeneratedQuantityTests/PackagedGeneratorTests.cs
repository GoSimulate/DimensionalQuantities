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
		using PackageRepository repository = PackageRepository.Create(_scratch);
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
			}
			""");
		ProjectCreator creator = ProjectCreator.Templates.SdkCsproj(path: consumerProject, targetFramework: "net10.0")
			.ItemPackageReference(package)
			.Save();

		creator.TryBuild(restore: true, out bool built, out BuildOutput output);
		using (output)
		{
			Assert.True(built, $"Consumer build failed.{Environment.NewLine}{output.GetConsoleLog()}");
		}

		string assemblyFile = Path.Combine(consumerDirectory, "bin", "Debug", "net10.0", "Consumer.dll");
		Assembly assembly = Assembly.LoadFrom(assemblyFile);
		Type consumerType = assembly.GetType("ConsumerProject.Consumer") ?? throw new InvalidOperationException("Consumer type was not built.");
		MethodInfo convertVoltage = consumerType.GetMethod("ConvertVoltage") ?? throw new InvalidOperationException("Consumer Voltage conversion was not built.");
		double result = Assert.IsType<double>(convertVoltage.Invoke(null, [12.5]));
		double expected = 12.5;
		Assert.Equal(expected, result);
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
			["Configuration"] = "Debug",
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
