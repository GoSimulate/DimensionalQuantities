using DimensionalQuantities.Extensions.Metric;
using Xunit;
using Voltage = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<DimensionalQuantities.TypeSystem.TerminalHex2, DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.Negative<DimensionalQuantities.TypeSystem.TerminalHex3>, DimensionalQuantities.TypeSystem.Negative<DimensionalQuantities.TypeSystem.TerminalHex1>, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>>;
using TemperatureCoefficient = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.Negative<DimensionalQuantities.TypeSystem.TerminalHex1>, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>>;

namespace DimensionalQuantities.Roslyn.Tests.Runtime;

public sealed class AdditionalShippedContractTests
{
	[Theory]
	[InlineData(2, -3, 2000)]
	[InlineData(-2, 3, -0.002)]
	[InlineData(0, 6, 0)]
	public void ParameterisedDeltaAccessorUsesRequestedPrefix(double siMagnitude, int prefix, double expected)
	{
		Assert.Equal(expected, Voltage.FromSiBaseUnit(siMagnitude).AsDelta().AsPrefixedUnits(voltPrefixExponent: prefix), 12);
	}

	[Fact]
	public void PrefixedDeltaAccessorUsesScaleOnly()
	{
		double actual = Voltage.FromSiBaseUnit(2).AsDelta().AsMilliVolts();
		Assert.Equal(2000, actual);
	}

	[Fact]
	public void ReciprocalOnlyUnitPreservesCoherentSiScale()
	{
		double actual = TemperatureCoefficient.FromInverseKelvin(2).AsSiBaseUnit();
		Assert.Equal(2, actual);
	}
}
