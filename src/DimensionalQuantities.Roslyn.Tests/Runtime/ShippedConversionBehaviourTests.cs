using DimensionalQuantities.Extensions.Metric;
using DimensionalQuantities.Extensions.Imperial;
using DimensionalQuantities.Extensions.Nautical;
using DimensionalQuantities.Extensions.CgsEmag;
using Xunit;
using Length = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>>;
using Area = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<DimensionalQuantities.TypeSystem.TerminalHex2, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>>;
using Volume = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<DimensionalQuantities.TypeSystem.TerminalHex3, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>>;
using Speed = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.Negative<DimensionalQuantities.TypeSystem.TerminalHex1>, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>>;
using Mass = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>>;
using Force = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.Negative<DimensionalQuantities.TypeSystem.TerminalHex2>, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>>;
using Pressure = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<DimensionalQuantities.TypeSystem.Negative<DimensionalQuantities.TypeSystem.TerminalHex1>, DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.Negative<DimensionalQuantities.TypeSystem.TerminalHex2>, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>>;
using Energy = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<DimensionalQuantities.TypeSystem.TerminalHex2, DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.Negative<DimensionalQuantities.TypeSystem.TerminalHex2>, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>>;
using Power = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<DimensionalQuantities.TypeSystem.TerminalHex2, DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.Negative<DimensionalQuantities.TypeSystem.TerminalHex3>, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>>;
using Temperature = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>>;
using Frequency = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.Negative<DimensionalQuantities.TypeSystem.TerminalHex1>, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>>;
using Density = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<DimensionalQuantities.TypeSystem.Negative<DimensionalQuantities.TypeSystem.TerminalHex3>, DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>>;
using CurrentDensity = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<DimensionalQuantities.TypeSystem.Negative<DimensionalQuantities.TypeSystem.TerminalHex2>, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>>;
using Current = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>>;
using Voltage = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<DimensionalQuantities.TypeSystem.TerminalHex2, DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.Negative<DimensionalQuantities.TypeSystem.TerminalHex3>, DimensionalQuantities.TypeSystem.Negative<DimensionalQuantities.TypeSystem.TerminalHex1>, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>>;
using Capacitance = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<DimensionalQuantities.TypeSystem.Negative<DimensionalQuantities.TypeSystem.TerminalHex2>, DimensionalQuantities.TypeSystem.Negative<DimensionalQuantities.TypeSystem.TerminalHex1>, DimensionalQuantities.TypeSystem.TerminalHex4, DimensionalQuantities.TypeSystem.TerminalHex2, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>>;
using MagneticFlux = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<DimensionalQuantities.TypeSystem.TerminalHex2, DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.Negative<DimensionalQuantities.TypeSystem.TerminalHex2>, DimensionalQuantities.TypeSystem.Negative<DimensionalQuantities.TypeSystem.TerminalHex1>, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>>;
using MagneticFluxDensity = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.Negative<DimensionalQuantities.TypeSystem.TerminalHex2>, DimensionalQuantities.TypeSystem.Negative<DimensionalQuantities.TypeSystem.TerminalHex1>, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>>;
using MagneticFieldStrength = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<DimensionalQuantities.TypeSystem.Negative<DimensionalQuantities.TypeSystem.TerminalHex1>, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>>;

namespace DimensionalQuantities.Roslyn.Tests.Runtime;

public sealed class ShippedConversionBehaviourTests
{
	[Theory]
	[InlineData("feet", 1.8288)]
	[InlineData("squareFeet", 0.09290304)]
	[InlineData("cubicFeet", 0.028316846592)]
	[InlineData("milePerHour", 0.44704)]
	[InlineData("shortTon", 907.18474)]
	[InlineData("longTon", 1016.0469088)]
	[InlineData("usGallon", 0.003785411784)]
	[InlineData("imperialGallon", 0.00454609)]
	[InlineData("horsepower", 745.6998715822702)]
	[InlineData("btu", 1055.05585262)]
	[InlineData("psi", 6894.757293168361)]
	[InlineData("poundForce", 4.4482216152605)]
	[InlineData("celsius", 293.15)]
	[InlineData("fahrenheit", 293.15)]
	[InlineData("celsiusZero", 0)]
	[InlineData("fahrenheitZero", 0)]
	[InlineData("rankine", 273.15)]
	[InlineData("nauticalMile", 1852)]
	[InlineData("fathom", 1.8288)]
	[InlineData("nauticalSquare", 3429904)]
	[InlineData("knot", 0.5144444444444445)]
	[InlineData("gauss", 1)]
	[InlineData("oersted", 79.57747154594767)]
	[InlineData("abampere", 10)]
	[InlineData("biot", 10)]
	[InlineData("abvolt", 1e-08)]
	[InlineData("abfarad", 1000000000.0)]
	[InlineData("maxwell", 1e-08)]
	[InlineData("erg", 1)]
	[InlineData("volt", 1)]
	[InlineData("joule", 1)]
	[InlineData("pascal", 1)]
	[InlineData("genericKiloVolts", 1000)]
	[InlineData("namedKiloVolts", 1000)]
	[InlineData("gramPerCc", 1000)]
	[InlineData("ampPerMm2", 1000000.0)]
	public void ShippedFactoriesUseIndependentlyDefinedSiScales(string conversion, double expected)
	{
		double actual = conversion switch
		{
			"feet" => Length.FromFeet(6).AsSiBaseUnit(),
			"squareFeet" => Area.FromSquareFeet(1).AsSiBaseUnit(),
			"cubicFeet" => Volume.FromCubicFeet(1).AsSiBaseUnit(),
			"milePerHour" => Speed.FromMilesPerHour(1).AsSiBaseUnit(),
			"shortTon" => Mass.FromUSShortTons(1).AsSiBaseUnit(),
			"longTon" => Mass.FromImperialLongTons(1).AsSiBaseUnit(),
			"usGallon" => Volume.FromUSGallons(1).AsSiBaseUnit(),
			"imperialGallon" => Volume.FromImperialGallons(1).AsSiBaseUnit(),
			"horsepower" => Power.FromMechanicalHorsepower(1).AsSiBaseUnit(),
			"btu" => Energy.FromBritishThermalUnitsIT(1).AsSiBaseUnit(),
			"psi" => Pressure.FromPsi(1).AsSiBaseUnit(),
			"poundForce" => Force.FromPoundForces(1).AsSiBaseUnit(),
			"celsius" => Temperature.FromCelsius(20).AsSiBaseUnit(),
			"fahrenheit" => Temperature.FromFahrenheit(68).AsSiBaseUnit(),
			"celsiusZero" => Temperature.FromCelsius(-273.15).AsSiBaseUnit(),
			"fahrenheitZero" => Temperature.FromFahrenheit(-459.67).AsSiBaseUnit(),
			"rankine" => Temperature.FromRankine(491.67).AsSiBaseUnit(),
			"nauticalMile" => Length.FromNauticalMiles(1).AsSiBaseUnit(),
			"fathom" => Length.FromFathoms(1).AsSiBaseUnit(),
			"nauticalSquare" => Area.FromSquareNauticalMiles(1).AsSiBaseUnit(),
			"knot" => Speed.FromKnots(1).AsSiBaseUnit(),
			"gauss" => MagneticFluxDensity.FromGauss(10000).AsSiBaseUnit(),
			"oersted" => MagneticFieldStrength.FromOersteds(1).AsSiBaseUnit(),
			"abampere" => Current.FromAbamperes(1).AsSiBaseUnit(),
			"biot" => Current.FromBiots(1).AsSiBaseUnit(),
			"abvolt" => Voltage.FromAbvolts(1).AsSiBaseUnit(),
			"abfarad" => Capacitance.FromAbfarads(1).AsSiBaseUnit(),
			"maxwell" => MagneticFlux.FromMaxwells(1).AsSiBaseUnit(),
			"erg" => Energy.FromErgs(10000000).AsSiBaseUnit(),
			"volt" => Voltage.FromVolts(1).AsSiBaseUnit(),
			"joule" => Energy.FromJoules(1).AsSiBaseUnit(),
			"pascal" => Pressure.FromPascals(1).AsSiBaseUnit(),
			"genericKiloVolts" => Voltage.FromPrefixedUnits(1, voltPrefixExponent: 3).AsSiBaseUnit(),
			"namedKiloVolts" => Voltage.FromKiloVolts(1).AsSiBaseUnit(),
			"gramPerCc" => Density.FromGramsPerCubicCentiMetre(1).AsSiBaseUnit(),
			"ampPerMm2" => CurrentDensity.FromAmpsPerSquareMilliMetre(1).AsSiBaseUnit(),
			_ => throw new ArgumentOutOfRangeException(nameof(conversion))
		};
		Assert.Equal(expected, actual, Math.Abs(expected) * 1e-12 + 1e-12);
	}

	[Theory]
	[InlineData("feet", 1.8288, 6)]
	[InlineData("squareFeet", 0.09290304, 1)]
	[InlineData("cubicFeet", 0.028316846592, 1)]
	[InlineData("milePerHour", 0.44704, 1)]
	[InlineData("nauticalMile", 1852, 1)]
	[InlineData("knot", 0.5144444444444445, 1)]
	[InlineData("volt", 2, 2)]
	[InlineData("kiloVolt", 2000, 2)]
	[InlineData("gauss", 1, 10000)]
	[InlineData("erg", 1, 10000000)]
	[InlineData("gramPerCc", 1000, 1)]
	[InlineData("ampPerMm2", 1000000, 1)]
	public void ShippedAccessorsInvertIndependentSiScales(string conversion, double siMagnitude, double expected)
	{
		double actual = conversion switch
		{
			"feet" => Length.FromSiBaseUnit(siMagnitude).AsFeet(),
			"squareFeet" => Area.FromSiBaseUnit(siMagnitude).AsSquareFeet(),
			"cubicFeet" => Volume.FromSiBaseUnit(siMagnitude).AsCubicFeet(),
			"milePerHour" => Speed.FromSiBaseUnit(siMagnitude).AsMilesPerHour(),
			"nauticalMile" => Length.FromSiBaseUnit(siMagnitude).AsNauticalMiles(),
			"knot" => Speed.FromSiBaseUnit(siMagnitude).AsKnots(),
			"volt" => Voltage.FromSiBaseUnit(siMagnitude).AsVolts(),
			"kiloVolt" => Voltage.FromSiBaseUnit(siMagnitude).AsKiloVolts(),
			"gauss" => MagneticFluxDensity.FromSiBaseUnit(siMagnitude).AsGauss(),
			"erg" => Energy.FromSiBaseUnit(siMagnitude).AsErgs(),
			"gramPerCc" => Density.FromSiBaseUnit(siMagnitude).AsGramsPerCubicCentiMetre(),
			"ampPerMm2" => CurrentDensity.FromSiBaseUnit(siMagnitude).AsAmpsPerSquareMilliMetre(),
			_ => throw new ArgumentOutOfRangeException(nameof(conversion))
		};
		Assert.Equal(expected, actual, Math.Abs(expected) * 1e-12 + 1e-12);
	}

	[Theory]
	[InlineData(0, 0)]
	[InlineData(-6, -1.8288)]
	public void LinearFactoryPreservesZeroAndNegativeReadings(double feet, double expectedSi)
	{
		Assert.Equal(expectedSi, Length.FromFeet(feet).AsSiBaseUnit(), 12);
	}

	[Theory]
	[InlineData(0, 0)]
	[InlineData(-1.8288, -6)]
	public void LinearAccessorPreservesZeroAndNegativeReadings(double siMagnitude, double expectedFeet)
	{
		Assert.Equal(expectedFeet, Length.FromSiBaseUnit(siMagnitude).AsFeet(), 12);
	}

	[Theory]
	[InlineData(2000, 3, 2)]
	[InlineData(-0.002, -3, -2)]
	[InlineData(0, 6, 0)]
	public void ParameterisedAccessorUsesRequestedPrefix(double siMagnitude, int prefix, double expected)
	{
		Assert.Equal(expected, Voltage.FromSiBaseUnit(siMagnitude).AsPrefixedUnits(voltPrefixExponent: prefix), 12);
	}

	[Theory]
	[InlineData("celsius", 293.15, 20)]
	[InlineData("celsius", 253.15, -20)]
	[InlineData("fahrenheit", 293.15, 68)]
	[InlineData("fahrenheit", 0, -459.67)]
	[InlineData("rankine", 273.15, 491.67)]
	[InlineData("rankine", 0, 0)]
	public void AbsoluteAffineAccessorUsesScaleAndOffset(string conversion, double siMagnitude, double expected)
	{
		Temperature value = Temperature.FromSiBaseUnit(siMagnitude);
		double actual = conversion switch
		{
			"celsius" => value.AsCelsius(),
			"fahrenheit" => value.AsFahrenheit(),
			"rankine" => value.AsRankine(),
			_ => throw new ArgumentOutOfRangeException(nameof(conversion))
		};
		Assert.Equal(expected, actual, 10);
	}

	[Theory]
	[InlineData(0, 0)]
	[InlineData(60, 6.283185307179586)]
	[InlineData(-60, -6.283185307179586)]
	public void RpmFactoryPreservesSign(double rpm, double expected)
	{
		Assert.Equal(expected, Frequency.FromRpm(rpm).AsSiBaseUnit(), 12);
	}

	[Fact]
	public void RpmAccessorUsesActualToRpmSpelling() => Assert.Equal(30, Frequency.FromSiBaseUnit(Math.PI).ToRpm(), 12);

	[Theory]
	[InlineData(-10, -18)]
	[InlineData(0, 0)]
	[InlineData(10, 18)]
	public void DeltaFahrenheitOmitsAbsoluteOffset(double kelvin, double expected)
	{
		Assert.Equal(expected, Temperature.FromSiBaseUnit(kelvin).AsDelta().AsFahrenheit(), 12);
	}

	[Theory]
	[InlineData(-10, -10)]
	[InlineData(0, 0)]
	[InlineData(10, 10)]
	public void DeltaCelsiusOmitsAbsoluteOffset(double kelvin, double expected) => Assert.Equal(expected, Temperature.FromSiBaseUnit(kelvin).AsDelta().AsCelsius(), 12);

	[Theory]
	[InlineData(-10, -18)]
	[InlineData(0, 0)]
	[InlineData(10, 18)]
	public void DeltaRankineUsesScaleOnly(double kelvin, double expected) => Assert.Equal(expected, Temperature.FromSiBaseUnit(kelvin).AsDelta().AsRankine(), 12);

	[Fact]
	public void DefaultDeltaRepresentsZero() => Assert.Equal(0, default(DimensionalQuantities.TypeSystem.Delta<Temperature>).AsFahrenheit());

	[Fact]
	public void ReinterpretedAbsoluteTemperatureRetainsMagnitude() => Assert.Equal(293.15, Temperature.FromCelsius(20).AsDelta().AsCelsius(), 12);
}
