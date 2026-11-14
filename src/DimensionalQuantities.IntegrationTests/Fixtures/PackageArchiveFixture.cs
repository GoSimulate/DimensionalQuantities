using DimensionalQuantities.IntegrationTests.Delivery;
using Xunit;

namespace DimensionalQuantities.IntegrationTests.Fixtures;

public sealed class PackageArchiveFixture : IAsyncLifetime
{
	internal PackageSandbox Packages { get; } = new();

	public async ValueTask InitializeAsync()
	{
		try
		{
			await Packages.Pack(
				"DimensionalQuantities.Extensions.Metric",
				"DimensionalQuantities.Extensions.Imperial",
				"DimensionalQuantities.Extensions.Nautical",
				"DimensionalQuantities.Extensions.CgsEmag");
		}
		catch
		{
			Packages.Dispose();
			throw;
		}
	}

	public ValueTask DisposeAsync()
	{
		Packages.Dispose();
		return ValueTask.CompletedTask;
	}
}
