using System.IO.Compression;
using System.Reflection;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Utilities.ProjectCreation;

namespace DimensionalQuantities.IntegrationTests.Packaging;

internal static class PackageTestSupport
{
	public static Package AddPackedPackage(PackageRepository repository, FileInfo archive)
	{
		repository.Package(archive, out Package package);
		// ProjectCreation rewrites the nuspec without dependency groups; restore the packed manifest for transitive consumers.
		using ZipArchive zip = ZipFile.OpenRead(archive.FullName);
		ZipArchiveEntry manifest = zip.Entries.Single(entry => entry.Name.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
		using Stream source = manifest.Open();
		using Stream destination = File.Create(repository.GetManifestFilePath(package.Id, package.Version));
		source.CopyTo(destination);
		return package;
	}

	public static FileInfo PackLibrary(string sourceProject, string scratch)
	{
		string packageOutput = Path.Combine(scratch, "package");
		Directory.CreateDirectory(packageOutput);
		Dictionary<string, string> properties = new()
		{
			["Configuration"] = typeof(PackageTestSupport).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration,
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
