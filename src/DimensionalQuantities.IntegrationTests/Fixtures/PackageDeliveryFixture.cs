using DimensionalQuantities.IntegrationTests.Delivery;
using Xunit;

namespace DimensionalQuantities.IntegrationTests.Fixtures;

public sealed class PackageDeliveryFixture : IAsyncLifetime
{
	private readonly PackageSandbox _packages = new();

	internal PackageSandbox CreateConsumer() => _packages.CreateConsumer();

	public async ValueTask InitializeAsync()
	{
		try
		{
			await _packages.Pack(
				"DimensionalQuantities.Extensions.Metric",
				"DimensionalQuantities.Extensions.Imperial",
				"DimensionalQuantities.Extensions.Nautical",
				"DimensionalQuantities.Extensions.CgsEmag");
		}
		catch
		{
			_packages.Dispose();
			throw;
		}
	}

	public ValueTask DisposeAsync()
	{
		_packages.Dispose();
		return ValueTask.CompletedTask;
	}
}
