using Microsoft.Build.Utilities.ProjectCreation;

namespace DimensionalQuantities.IntegrationTests.Packaging;

internal sealed class PackageConsumerWorkspace : IDisposable
{
	public string Directory { get; }

	public PackageConsumerWorkspace(string directory)
	{
		Directory = directory;
	}

	public void Build(ProjectCreator project)
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

	public void Pack(ProjectCreator project, string name, PackageRepository repository, out Package package)
	{
		project.TryBuild(restore: true, target: "Pack", out bool built, out BuildOutput output);
		using (output)
		{
			if (!built)
			{
				throw new InvalidOperationException(output.GetConsoleLog());
			}
		}
		package = PackageTestSupport.AddPackedPackage(repository, new FileInfo(Path.Combine(Directory, name, "packages", $"{name}.1.0.0.nupkg")));
	}

	public string OutputDirectory(string project) => Path.Combine(Directory, project, "bin", "Debug", "netstandard2.0");

	public ProjectCreator CreateProject(string name, string source, params Package[] packages)
	{
		string directory = Path.Combine(Directory, name);
		System.IO.Directory.CreateDirectory(directory);
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

	public void Dispose() => System.IO.Directory.Delete(Directory, recursive: true);
}
