using Xunit;
using Current = DimensionalQuantities.Quantity<DimensionalQuantities.TypeSystem.Dimension<
	DimensionalQuantities.TypeSystem.TerminalHex0,
	DimensionalQuantities.TypeSystem.TerminalHex0,
	DimensionalQuantities.TypeSystem.TerminalHex0,
	DimensionalQuantities.TypeSystem.TerminalHex1,
	DimensionalQuantities.TypeSystem.TerminalHex0,
	DimensionalQuantities.TypeSystem.TerminalHex0,
	DimensionalQuantities.TypeSystem.TerminalHex0>>;

namespace DimensionalQuantities.Tests;

public sealed class QuantityTests
{
	[Theory]
	[InlineData(0)]
	[InlineData(-0.125)]
	[InlineData(1234.5)]
	public void ConstructionPreservesSiMagnitude(double expected)
	{
		Current original = new(expected);
		double result = original.Si;

		Assert.Equal(expected, result);
	}

	[Fact]
	public void RawSiMagnitudeIsNotPartOfPublicApi()
	{
		Type quantityType = typeof(Current);

		Assert.Multiple(
			() => Assert.Empty(quantityType.GetProperties()),
			() => Assert.Empty(quantityType.GetFields()),
			() => Assert.Null(quantityType.GetConstructor([typeof(double)])));
	}
}
