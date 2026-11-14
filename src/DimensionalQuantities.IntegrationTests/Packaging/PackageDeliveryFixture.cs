using Microsoft.Build.Locator;
using TUnit.Core.Interfaces;

namespace DimensionalQuantities.IntegrationTests.Packaging;

public sealed class PackageDeliveryFixture : IAsyncInitializer, IDisposable
{
	private string _scratch = null!;
	private readonly Dictionary<string, FileInfo> _packages = new();

	public Task InitializeAsync()
	{
		// MSBuild must be located before any Microsoft.Build type is loaded.
		if (!MSBuildLocator.IsRegistered)
		{
			MSBuildLocator.RegisterDefaults();
		}

		_scratch = Directory.CreateTempSubdirectory("dimensional-quantities-delivery-").FullName;
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

		return Task.CompletedTask;
	}

	internal FileInfo GetPackage(string projectName) => _packages[projectName];

	internal PackageConsumerWorkspace CreateWorkspace() => new(Directory.CreateDirectory(Path.Combine(_scratch, Guid.NewGuid().ToString("N"))).FullName);

	public void Dispose() => Directory.Delete(_scratch, recursive: true);
}
