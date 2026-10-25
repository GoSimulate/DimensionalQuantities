using System.Reflection;
using DimensionalQuantities.Extensions.Imperial;
using Xunit;

namespace DimensionalQuantities.Extensions.Imperial.Tests;

public sealed class ImperialConversionsTests
{
	[Fact]
	public void LengthAndAreaUseInternationalFoot()
	{
		Assert.Multiple(
			() => Assert.Equal(0.0254, Length.FromInches(1).AsSiBaseUnit(), 12),
			() => Assert.Equal(0.3048, Length.FromFeet(1).AsSiBaseUnit(), 12),
			() => Assert.Equal(0.9144, Length.FromYards(1).AsSiBaseUnit(), 12),
			() => Assert.Equal(1609.344, Length.FromMiles(1).AsSiBaseUnit(), 9),
			() => Assert.Equal(20.1168, Length.FromChains(1).AsSiBaseUnit(), 10),
			() => Assert.Equal(201.168, Length.FromFurlongs(1).AsSiBaseUnit(), 10),
			() => Assert.Equal(0.09290304, Area.FromSquareFeet(1).AsSiBaseUnit(), 12),
			() => Assert.Equal(4046.8564224, Area.FromAcres(1).AsSiBaseUnit(), 8),
			() => Assert.Equal(5280, Length.FromMiles(1).AsFeet(), 9));
	}

	[Fact]
	public void VolumeKeepsUsAndImperialLiquidMeasuresDistinct()
	{
		Assert.Multiple(
			() => Assert.Equal(0.000016387064, Volume.FromCubicInches(1).AsSiBaseUnit(), 15),
			() => Assert.Equal(0.028316846592, Volume.FromCubicFeet(1).AsSiBaseUnit(), 12),
			() => Assert.Equal(0.764554857984, Volume.FromCubicYards(1).AsSiBaseUnit(), 12),
			() => Assert.Equal(0.003785411784, Volume.FromUSGallons(1).AsSiBaseUnit(), 12),
			() => Assert.Equal(0.000946352946, Volume.FromUSQuarts(1).AsSiBaseUnit(), 12),
			() => Assert.Equal(0.000473176473, Volume.FromUSPints(1).AsSiBaseUnit(), 12),
			() => Assert.Equal(0.0000295735295625, Volume.FromUSFluidOunces(1).AsSiBaseUnit(), 15),
			() => Assert.Equal(0.00454609, Volume.FromImperialGallons(1).AsSiBaseUnit(), 12),
			() => Assert.Equal(0.0011365225, Volume.FromImperialQuarts(1).AsSiBaseUnit(), 12),
			() => Assert.Equal(0.00056826125, Volume.FromImperialPints(1).AsSiBaseUnit(), 12),
			() => Assert.Equal(0.0000284130625, Volume.FromImperialFluidOunces(1).AsSiBaseUnit(), 15),
			() => Assert.Equal(128, Volume.FromUSGallons(1).AsUSFluidOunces(), 10),
			() => Assert.Equal(160, Volume.FromImperialGallons(1).AsImperialFluidOunces(), 10));
	}

	[Fact]
	public void MassSpeedForceAndPressureUseTheirOwnDimensions()
	{
		Assert.Multiple(
			() => Assert.Equal(0.028349523125, Mass.FromOunces(1).AsSiBaseUnit(), 12),
			() => Assert.Equal(0.45359237, Mass.FromPounds(1).AsSiBaseUnit(), 12),
			() => Assert.Equal(6.35029318, Mass.FromStones(1).AsSiBaseUnit(), 10),
			() => Assert.Equal(907.18474, Mass.FromUSShortTons(1).AsSiBaseUnit(), 8),
			() => Assert.Equal(1016.0469088, Mass.FromImperialLongTons(1).AsSiBaseUnit(), 8),
			() => Assert.Equal(0.44704, Speed.FromMilesPerHour(1).AsSiBaseUnit(), 12),
			() => Assert.Equal(0.3048, Speed.FromFeetPerSecond(1).AsSiBaseUnit(), 12),
			() => Assert.Equal(4.4482216152605, Force.FromPoundForces(1).AsSiBaseUnit(), 12),
			() => Assert.Equal(4.4482216152605 / (0.0254 * 0.0254), Pressure.FromPsi(1).AsSiBaseUnit(), 9),
			() => Assert.Equal(1, Stress.FromPsi(1).AsPsi(), 12),
			() => Assert.Equal(1, YoungsModulus.FromPsi(1).AsPsi(), 12));
	}

	[Fact]
	public void EnergyAndPowerUseExplicitBritishThermalUnitDefinition()
	{
		Assert.Multiple(
			() => Assert.Equal(1.3558179483314004, Energy.FromFootPoundForces(1).AsSiBaseUnit(), 12),
			() => Assert.Equal(1055.05585262, Energy.FromBritishThermalUnitsIT(1).AsSiBaseUnit(), 9),
			() => Assert.Equal(745.69987158227022, Power.FromMechanicalHorsepower(1).AsSiBaseUnit(), 9),
			() => Assert.Equal(1055.05585262 / 3600, Power.FromBritishThermalUnitsITPerHour(1).AsSiBaseUnit(), 12),
			() => Assert.Equal(1, Energy.FromSiBaseUnit(1055.05585262).AsBritishThermalUnitsIT(), 12),
			() => Assert.Equal(1, Power.FromSiBaseUnit(1055.05585262 / 3600).AsBritishThermalUnitsITPerHour(), 12));
	}

	[Fact]
	public void RankineUsesFiveNinthsKelvinPerDegree()
	{
		Assert.Multiple(
			() => Assert.Equal(273.15, AbsoluteTemperature.FromRankine(491.67).AsKelvin(), 10),
			() => Assert.Equal(491.67, AbsoluteTemperature.FromKelvin(273.15).AsRankine(), 10));
	}

	[Fact]
	public void FahrenheitConvertsToAndFromAbsoluteKelvin()
	{
		Assert.Multiple(
			() => Assert.Equal(273.15, AbsoluteTemperature.FromFahrenheit(32).AsKelvin(), 10),
			() => Assert.Equal(32, AbsoluteTemperature.FromKelvin(273.15).AsFahrenheit(), 10));
	}

	[Fact]
	public void ProviderDoesNotGenerateSiPrefixHelpers()
	{
		string[] names = Assembly.Load("DimensionalQuantities.Extensions.Imperial").GetExportedTypes()
			.Where(type => type.Namespace == "DimensionalQuantities.Extensions.Imperial")
			.SelectMany(type => type.GetMethods()).Select(method => method.Name).ToArray();
		Assert.Multiple(
			() => Assert.DoesNotContain("FromKiloFeet", names),
			() => Assert.DoesNotContain("AsMilliPounds", names),
			() => Assert.DoesNotContain("FromPrefixedUnits", names),
			() => Assert.DoesNotContain("AsPrefixedUnits", names));
	}
}
