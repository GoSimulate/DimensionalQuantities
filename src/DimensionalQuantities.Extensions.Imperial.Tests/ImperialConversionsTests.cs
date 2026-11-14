using Assembly = System.Reflection.Assembly;
using DimensionalQuantities.Extensions.Imperial;

namespace DimensionalQuantities.Extensions.Imperial.Tests;

public sealed class ImperialConversionsTests
{
	[Test]
	public async Task LengthAndAreaUseInternationalFoot()
	{
		using (Assert.Multiple())
		{
			await Assert.That(Math.Round(Length.FromInches(1).AsSiBaseUnit(), 12)).IsEqualTo(Math.Round(0.0254, 12));
			await Assert.That(Math.Round(Length.FromFeet(1).AsSiBaseUnit(), 12)).IsEqualTo(Math.Round(0.3048, 12));
			await Assert.That(Math.Round(Length.FromYards(1).AsSiBaseUnit(), 12)).IsEqualTo(Math.Round(0.9144, 12));
			await Assert.That(Math.Round(Length.FromMiles(1).AsSiBaseUnit(), 9)).IsEqualTo(Math.Round(1609.344, 9));
			await Assert.That(Math.Round(Length.FromChains(1).AsSiBaseUnit(), 10)).IsEqualTo(Math.Round(20.1168, 10));
			await Assert.That(Math.Round(Length.FromFurlongs(1).AsSiBaseUnit(), 10)).IsEqualTo(Math.Round(201.168, 10));
			await Assert.That(Math.Round(Area.FromSquareFeet(1).AsSiBaseUnit(), 12)).IsEqualTo(Math.Round(0.09290304, 12));
			await Assert.That(Math.Round(Area.FromAcres(1).AsSiBaseUnit(), 8)).IsEqualTo(Math.Round(4046.8564224, 8));
			await Assert.That(Math.Round(Length.FromMiles(1).AsFeet(), 9)).IsEqualTo(Math.Round(5280d, 9));
		}
	}

	[Test]
	public async Task VolumeKeepsUsAndImperialLiquidMeasuresDistinct()
	{
		using (Assert.Multiple())
		{
			await Assert.That(Math.Round(Volume.FromCubicInches(1).AsSiBaseUnit(), 15)).IsEqualTo(Math.Round(0.000016387064, 15));
			await Assert.That(Math.Round(Volume.FromCubicFeet(1).AsSiBaseUnit(), 12)).IsEqualTo(Math.Round(0.028316846592, 12));
			await Assert.That(Math.Round(Volume.FromCubicYards(1).AsSiBaseUnit(), 12)).IsEqualTo(Math.Round(0.764554857984, 12));
			await Assert.That(Math.Round(Volume.FromUSGallons(1).AsSiBaseUnit(), 12)).IsEqualTo(Math.Round(0.003785411784, 12));
			await Assert.That(Math.Round(Volume.FromUSQuarts(1).AsSiBaseUnit(), 12)).IsEqualTo(Math.Round(0.000946352946, 12));
			await Assert.That(Math.Round(Volume.FromUSPints(1).AsSiBaseUnit(), 12)).IsEqualTo(Math.Round(0.000473176473, 12));
			await Assert.That(Math.Round(Volume.FromUSFluidOunces(1).AsSiBaseUnit(), 15)).IsEqualTo(Math.Round(0.0000295735295625, 15));
			await Assert.That(Math.Round(Volume.FromImperialGallons(1).AsSiBaseUnit(), 12)).IsEqualTo(Math.Round(0.00454609, 12));
			await Assert.That(Math.Round(Volume.FromImperialQuarts(1).AsSiBaseUnit(), 12)).IsEqualTo(Math.Round(0.0011365225, 12));
			await Assert.That(Math.Round(Volume.FromImperialPints(1).AsSiBaseUnit(), 12)).IsEqualTo(Math.Round(0.00056826125, 12));
			await Assert.That(Math.Round(Volume.FromImperialFluidOunces(1).AsSiBaseUnit(), 15)).IsEqualTo(Math.Round(0.0000284130625, 15));
			await Assert.That(Math.Round(Volume.FromUSGallons(1).AsUSFluidOunces(), 10)).IsEqualTo(Math.Round(128d, 10));
			await Assert.That(Math.Round(Volume.FromImperialGallons(1).AsImperialFluidOunces(), 10)).IsEqualTo(Math.Round(160d, 10));
		}
	}

	[Test]
	public async Task MassSpeedForceAndPressureUseTheirOwnDimensions()
	{
		using (Assert.Multiple())
		{
			await Assert.That(Math.Round(Mass.FromOunces(1).AsSiBaseUnit(), 12)).IsEqualTo(Math.Round(0.028349523125, 12));
			await Assert.That(Math.Round(Mass.FromPounds(1).AsSiBaseUnit(), 12)).IsEqualTo(Math.Round(0.45359237, 12));
			await Assert.That(Math.Round(Mass.FromStones(1).AsSiBaseUnit(), 10)).IsEqualTo(Math.Round(6.35029318, 10));
			await Assert.That(Math.Round(Mass.FromUSShortTons(1).AsSiBaseUnit(), 8)).IsEqualTo(Math.Round(907.18474, 8));
			await Assert.That(Math.Round(Mass.FromImperialLongTons(1).AsSiBaseUnit(), 8)).IsEqualTo(Math.Round(1016.0469088, 8));
			await Assert.That(Math.Round(Speed.FromMilesPerHour(1).AsSiBaseUnit(), 12)).IsEqualTo(Math.Round(0.44704, 12));
			await Assert.That(Math.Round(Speed.FromFeetPerSecond(1).AsSiBaseUnit(), 12)).IsEqualTo(Math.Round(0.3048, 12));
			await Assert.That(Math.Round(Force.FromPoundForces(1).AsSiBaseUnit(), 12)).IsEqualTo(Math.Round(4.4482216152605, 12));
			await Assert.That(Math.Round(Pressure.FromPsi(1).AsSiBaseUnit(), 9)).IsEqualTo(Math.Round(4.4482216152605 / (0.0254 * 0.0254), 9));
			await Assert.That(Math.Round(Stress.FromPsi(1).AsPsi(), 12)).IsEqualTo(Math.Round(1d, 12));
			await Assert.That(Math.Round(YoungsModulus.FromPsi(1).AsPsi(), 12)).IsEqualTo(Math.Round(1d, 12));
		}
	}

	[Test]
	public async Task EnergyAndPowerUseExplicitBritishThermalUnitDefinition()
	{
		using (Assert.Multiple())
		{
			await Assert.That(Math.Round(Energy.FromFootPoundForces(1).AsSiBaseUnit(), 12)).IsEqualTo(Math.Round(1.3558179483314004, 12));
			await Assert.That(Math.Round(Energy.FromBritishThermalUnitsIT(1).AsSiBaseUnit(), 9)).IsEqualTo(Math.Round(1055.05585262, 9));
			await Assert.That(Math.Round(Power.FromMechanicalHorsepower(1).AsSiBaseUnit(), 9)).IsEqualTo(Math.Round(745.69987158227022, 9));
			await Assert.That(Math.Round(Power.FromBritishThermalUnitsITPerHour(1).AsSiBaseUnit(), 12)).IsEqualTo(Math.Round(1055.05585262 / 3600, 12));
			await Assert.That(Math.Round(Energy.FromSiBaseUnit(1055.05585262).AsBritishThermalUnitsIT(), 12)).IsEqualTo(Math.Round(1d, 12));
			await Assert.That(Math.Round(Power.FromSiBaseUnit(1055.05585262 / 3600).AsBritishThermalUnitsITPerHour(), 12)).IsEqualTo(Math.Round(1d, 12));
		}
	}

	[Test]
	public async Task RankineUsesFiveNinthsKelvinPerDegree()
	{
		using (Assert.Multiple())
		{
			await Assert.That(Math.Round(Temperature.FromRankine(491.67).AsKelvin(), 10)).IsEqualTo(Math.Round(273.15, 10));
			await Assert.That(Math.Round(Temperature.FromKelvin(273.15).AsRankine(), 10)).IsEqualTo(Math.Round(491.67, 10));
		}
	}

	[Test]
	public async Task FahrenheitConvertsToAndFromAbsoluteKelvin()
	{
		using (Assert.Multiple())
		{
			await Assert.That(Math.Round(Temperature.FromFahrenheit(32).AsKelvin(), 10)).IsEqualTo(Math.Round(273.15, 10));
			await Assert.That(Math.Round(Temperature.FromKelvin(273.15).AsFahrenheit(), 10)).IsEqualTo(Math.Round(32d, 10));
		}
	}

	[Test]
	public async Task ProviderDoesNotGenerateSiPrefixHelpers()
	{
		string[] names = Assembly.Load("DimensionalQuantities.Extensions.Imperial").GetExportedTypes()
			.Where(type => type.Namespace == "DimensionalQuantities.Extensions.Imperial")
			.SelectMany(type => type.GetMethods()).Select(method => method.Name).ToArray();
		using (Assert.Multiple())
		{
			await Assert.That(names.Contains("FromKiloFeet")).IsFalse();
			await Assert.That(names.Contains("AsMilliPounds")).IsFalse();
			await Assert.That(names.Contains("FromPrefixedUnits")).IsFalse();
			await Assert.That(names.Contains("AsPrefixedUnits")).IsFalse();
		}
	}
}
