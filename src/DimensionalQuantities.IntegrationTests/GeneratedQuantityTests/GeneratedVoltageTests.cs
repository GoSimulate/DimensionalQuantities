using Xunit;

namespace DimensionalQuantities.IntegrationTests.GeneratedQuantityTests;

public sealed class GeneratedVoltageTests
{
	[Fact]
	public void VoltageAndMechanicalAliasesResolveToSameQuantityType()
	{
		Type result = typeof(Voltage);
		Type expected = typeof(LengthSquaredMassPerTimeCubedCurrent);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void GeneratedVoltageFactoryAndAccessorRoundtrip()
	{
		Voltage original = Voltage.FromVolts(12.5);
		double result = original.AsVolts();
		double expected = 12.5;

		Assert.Equal(expected, result);
	}
}
