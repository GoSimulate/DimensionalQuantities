using DimensionalQuantities.Generated;
using Xunit;

namespace DimensionalQuantities.IntegrationTests.AnalyserPackageSmokeTests;

public sealed class GeneratorLoadingTests
{
	[Fact]
	public void GeneratedTypeCanBeConstructed()
	{
		GeneratedSmokeTest result = new();

		Assert.NotNull(result);
	}
}
