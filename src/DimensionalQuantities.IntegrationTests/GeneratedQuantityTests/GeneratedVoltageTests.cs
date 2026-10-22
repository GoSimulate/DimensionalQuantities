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

	[Fact]
	public void PrefixedFactoriesStoreSiMagnitude()
	{
		Voltage milliVolts = Voltage.FromMilliVolts(2500);
		Voltage kiloVolts = Voltage.FromKiloVolts(2.5);
		Voltage rontoVolts = Voltage.FromRontoVolts(1);
		Voltage quettaVolts = Voltage.FromQuettaVolts(1);

		Assert.Multiple(
			() => Assert.Equal(2.5, milliVolts.AsSiBaseUnit()),
			() => Assert.Equal(2500, kiloVolts.AsSiBaseUnit()),
			() => Assert.Equal(1e-27, rontoVolts.AsSiBaseUnit()),
			() => Assert.Equal(1e30, quettaVolts.AsSiBaseUnit()));
	}

	[Fact]
	public void PrefixedAccessorsConvertFromSiMagnitude()
	{
		Voltage original = Voltage.FromVolts(2.5);
		double milliVolts = original.AsMilliVolts();
		double kiloVolts = original.AsKiloVolts();
		double deciVolts = original.AsDeciVolts();
		double hectoVolts = original.AsHectoVolts();

		Assert.Multiple(
			() => Assert.Equal(2500, milliVolts),
			() => Assert.Equal(0.0025, kiloVolts),
			() => Assert.Equal(25, deciVolts),
			() => Assert.Equal(0.025, hectoVolts));
	}

	[Fact]
	public void NewRangePrefixesAreAvailableInBothDirections()
	{
		Voltage original = Voltage.FromQuectoVolts(1);
		double result = original.AsQuectoVolts();
		double expected = 1;

		Assert.Multiple(
			() => Assert.Equal(1e-30, original.AsVolts()),
			() => Assert.Equal(expected, result));
	}
}
