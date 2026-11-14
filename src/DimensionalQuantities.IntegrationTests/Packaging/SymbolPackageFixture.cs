using Microsoft.Build.Locator;

namespace DimensionalQuantities.IntegrationTests.Packaging;

public sealed class SymbolPackageFixture : IDisposable
{
	private readonly string _scratch = Directory.CreateTempSubdirectory("dimensional-quantities-symbols-").FullName;
	private readonly Dictionary<string, FileInfo> _packages = new();

	static SymbolPackageFixture()
	{
		// MSBuild must be located before any Microsoft.Build type is loaded.
		if (!MSBuildLocator.IsRegistered)
		{
			MSBuildLocator.RegisterDefaults();
		}
	}

	public SymbolPackageFixture()
	{
		string sourceDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
		foreach (string projectName in new[]
		{
			"DimensionalQuantities",
			"DimensionalQuantities.Extensions.Metric",
			"DimensionalQuantities.Extensions.Imperial",
			"DimensionalQuantities.Extensions.Nautical",
			"DimensionalQuantities.Extensions.CgsEmag",
		})
		{
			_packages.Add(projectName, PackageTestSupport.PackLibrary(Path.Combine(sourceDirectory, projectName, projectName + ".csproj"), _scratch));
		}
	}

	internal FileInfo GetPackage(string projectName) => _packages[projectName];

	public void Dispose() => Directory.Delete(_scratch, recursive: true);
}
