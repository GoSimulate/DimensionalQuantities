using System.Text.Json;
using DimensionalQuantities.IntegrationTests.Fixtures;
using Xunit;

namespace DimensionalQuantities.IntegrationTests;

public sealed class PackageInteroperabilityTests : IClassFixture<PackageInteroperabilityFixture>
{
	private readonly PackageInteroperabilityFixture _fixture;

	public PackageInteroperabilityTests(PackageInteroperabilityFixture fixture)
	{
		_fixture = fixture;
	}

	[Fact]
	public async Task IndependentPackagesShareQuantitiesAndConsumerCanExtendArithmetic()
	{
		using JsonDocument result = JsonDocument.Parse(await _fixture.ExecuteConsumer());
		JsonElement sharedTypes = result.RootElement.GetProperty("SharedTypes");
		JsonElement values = result.RootElement.GetProperty("Values");
		Assert.Multiple(
			() => Assert.True(sharedTypes.GetProperty("NamedProducers").GetBoolean()),
			() => Assert.True(sharedTypes.GetProperty("NamedConsumer").GetBoolean()),
			() => Assert.True(sharedTypes.GetProperty("IntermediateProducers").GetBoolean()),
			() => Assert.True(sharedTypes.GetProperty("IntermediateConsumer").GetBoolean()),
			() => Assert.True(sharedTypes.GetProperty("IntermediateRuntime").GetBoolean()),
			() => Assert.True(sharedTypes.GetProperty("ExtendedRuntime").GetBoolean()),
			() => Assert.Equal(6d, values.GetProperty("Direct").GetDouble()),
			() => Assert.Equal(12d, values.GetProperty("Collection").GetDouble()),
			() => Assert.Equal(6d, values.GetProperty("Contained").GetDouble()),
			() => Assert.Equal(0.006d, values.GetProperty("Prefixed").GetDouble(), 12),
			() => Assert.Equal(24d, values.GetProperty("Product").GetDouble()),
			() => Assert.Equal(6d, values.GetProperty("Intermediate").GetDouble()),
			() => Assert.Equal(10d, values.GetProperty("IndependentIntermediate").GetDouble()),
			() => Assert.Equal(24d, values.GetProperty("IntermediateProduct").GetDouble()));
	}
}
