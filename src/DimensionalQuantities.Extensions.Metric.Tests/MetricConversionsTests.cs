using Assembly = System.Reflection.Assembly;
using DimensionalQuantities.Extensions.Metric;

namespace DimensionalQuantities.Extensions.Metric.Tests;

public sealed class MetricConversionsTests
{
	[Test]
	public async Task CatalogueFactoriesConvertToExpectedSiValues()
	{
		double[] result =
		[
			Area.FromSquareMetres(1).AsSiBaseUnit(),
			Volume.FromCubicMetres(1).AsSiBaseUnit(),
			Frequency.FromHertz(1).AsSiBaseUnit(),
			AngularVelocity.FromRadiansPerSecond(1).AsSiBaseUnit(),
			Density.FromGramsPerCubicMetre(1).AsSiBaseUnit(),
			Pressure.FromPascals(1).AsSiBaseUnit(),
			Energy.FromJoules(1).AsSiBaseUnit(),
			Torque.FromNewtonMetres(1).AsSiBaseUnit(),
			Power.FromWatts(1).AsSiBaseUnit(),
			SpecificPower.FromWattsPerKilogram(1).AsSiBaseUnit(),
			TemperatureCoefficient.FromInverseKelvin(1).AsSiBaseUnit(),
			PowerDensity.FromWattsPerCubicMetre(1).AsSiBaseUnit(),
			Voltage.FromVolts(1).AsSiBaseUnit(),
			Resistance.FromOhms(1).AsSiBaseUnit(),
			Inductance.FromHenries(1).AsSiBaseUnit(),
			MagneticFlux.FromWebers(1).AsSiBaseUnit(),
			MagneticFluxDensity.FromTeslas(1).AsSiBaseUnit(),
			MagneticFieldStrength.FromAmpsPerMetre(1).AsSiBaseUnit(),
			CurrentDensity.FromAmpsPerSquareMetre(1).AsSiBaseUnit(),
			Permeability.FromHenriesPerMetre(1).AsSiBaseUnit(),
			Resistivity.FromOhmMetres(1).AsSiBaseUnit(),
			Conductivity.FromSiemensPerMetre(1).AsSiBaseUnit(),
			MagneticReluctivity.FromMetresPerHenry(1).AsSiBaseUnit(),
			MagneticVectorPotential.FromWebersPerMetre(1).AsSiBaseUnit(),
		];

		double[] expected = [1d, 1d, 1d, 1d, 0.001d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d];
		await Assert.That(result).IsEquivalentTo(expected, TUnit.Assertions.Enums.CollectionOrdering.Matching);
	}

	[Test]
	public async Task CelsiusConvertsToAndFromAbsoluteKelvin()
	{
		using (Assert.Multiple())
		{
			await Assert.That(Math.Round(Temperature.FromCelsius(0).AsKelvin(), 10)).IsEqualTo(Math.Round(273.15, 10));
			await Assert.That(Math.Round(Temperature.FromKelvin(273.15).AsCelsius(), 10)).IsEqualTo(Math.Round(0d, 10));
		}
	}

	[Test]
	[Arguments(0d, 0d)]
	[Arguments(60d, 2d)]
	[Arguments(-120d, -4d)]
	public async Task RpmConvertsToAndFromRadiansPerSecond(double rpm, double multiplesOfPi)
	{
		double radiansPerSecond = multiplesOfPi * Math.PI;
		using (Assert.Multiple())
		{
			await Assert.That(Math.Round(AngularVelocity.FromRpm(rpm).AsRadiansPerSecond(), 10)).IsEqualTo(Math.Round(radiansPerSecond, 10));
			await Assert.That(Math.Round(AngularVelocity.FromRadiansPerSecond(radiansPerSecond).ToRpm(), 10)).IsEqualTo(Math.Round(rpm, 10));
		}
	}

	[Test]
	public async Task CatalogueAccessorsConvertFromKnownSiValues()
	{
		double[] result =
		[
			Area.FromSiBaseUnit(1).AsSquareMetres(),
			Volume.FromSiBaseUnit(1).AsCubicMetres(),
			Frequency.FromSiBaseUnit(1).AsHertz(),
			AngularVelocity.FromSiBaseUnit(1).AsRadiansPerSecond(),
			Density.FromSiBaseUnit(1).AsGramsPerCubicMetre(),
			Pressure.FromSiBaseUnit(1).AsPascals(),
			Energy.FromSiBaseUnit(1).AsJoules(),
			Torque.FromSiBaseUnit(1).AsNewtonMetres(),
			Power.FromSiBaseUnit(1).AsWatts(),
			SpecificPower.FromSiBaseUnit(1).AsWattsPerKilogram(),
			TemperatureCoefficient.FromSiBaseUnit(1).AsInverseKelvin(),
			PowerDensity.FromSiBaseUnit(1).AsWattsPerCubicMetre(),
			Voltage.FromSiBaseUnit(1).AsVolts(),
			Resistance.FromSiBaseUnit(1).AsOhms(),
			Inductance.FromSiBaseUnit(1).AsHenries(),
			MagneticFlux.FromSiBaseUnit(1).AsWebers(),
			MagneticFluxDensity.FromSiBaseUnit(1).AsTeslas(),
			MagneticFieldStrength.FromSiBaseUnit(1).AsAmpsPerMetre(),
			CurrentDensity.FromSiBaseUnit(1).AsAmpsPerSquareMetre(),
			Permeability.FromSiBaseUnit(1).AsHenriesPerMetre(),
			Resistivity.FromSiBaseUnit(1).AsOhmMetres(),
			Conductivity.FromSiBaseUnit(1).AsSiemensPerMetre(),
			MagneticReluctivity.FromSiBaseUnit(1).AsMetresPerHenry(),
			MagneticVectorPotential.FromSiBaseUnit(1).AsWebersPerMetre(),
		];

		double[] expected = [1d, 1d, 1d, 1d, 1000d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d];
		await Assert.That(result).IsEquivalentTo(expected, TUnit.Assertions.Enums.CollectionOrdering.Matching);
	}

	[Test]
	public async Task AliasesShareNamedConversionsWithoutDuplicateHelpers()
	{
		Pressure pressure = Pressure.FromPascals(1);
		Stress stress = Stress.FromPascals(1);
		YoungsModulus modulus = YoungsModulus.FromKiloPascals(1);
		double[] result =
		[
			pressure.AsPascals(), stress.AsPascals(), modulus.AsKiloPascals(),
			SpecificPower.FromWattsPerKilogram(1).AsWattsPerKilogram(),
			SpecificPowerLoss.FromWattsPerKilogram(1).AsWattsPerKilogram(),
			PowerDensity.FromWattsPerCubicMetre(1).AsWattsPerCubicMetre(),
			VolumetricPowerLoss.FromWattsPerCubicMetre(1).AsWattsPerCubicMetre(),
			MagneticFlux.FromWebers(1).AsWebers(), FluxLinkage.FromWebers(1).AsWebers(),
			MagneticFluxDensity.FromTeslas(1).AsTeslas(), MagneticPolarisation.FromTeslas(1).AsTeslas(),
			RemanentFluxDensity.FromTeslas(1).AsTeslas(),
			MagneticFieldStrength.FromAmpsPerMetre(1).AsAmpsPerMetre(),
			CoerciveFieldStrength.FromAmpsPerMetre(1).AsAmpsPerMetre()
		];
		await Assert.That(result).IsEquivalentTo(Enumerable.Repeat(1d, result.Length), TUnit.Assertions.Enums.CollectionOrdering.Matching);
	}

	[Test]
	public async Task NamedConversionsApplyMetricScale()
	{
		using (Assert.Multiple())
		{
			await Assert.That(Voltage.FromSiBaseUnit(2.5).AsKiloVolts()).IsEqualTo(0.0025);
			await Assert.That(Density.FromGramsPerCubicCentiMetre(1).AsSiBaseUnit()).IsEqualTo(1000);
		}
	}

	[Test]
	public async Task GenericPrefixConversionsApplyMetricScale()
	{
		using (Assert.Multiple())
		{
			await Assert.That(Voltage.FromPrefixedUnits(2, -3).AsSiBaseUnit()).IsEqualTo(0.002);
			await Assert.That(CurrentDensity.FromPrefixedUnits(1, ampPrefixExponent: 0, metrePrefixExponent: -3).AsSiBaseUnit()).IsEqualTo(1000000);
			await Assert.That(SpecificPower.FromPrefixedUnits(1, wattPrefixExponent: 3, gramPrefixExponent: 3).AsSiBaseUnit()).IsEqualTo(1000);
			await Assert.That(TemperatureCoefficient.FromPrefixedUnits(1, kelvinPrefixExponent: -3).AsSiBaseUnit()).IsEqualTo(1000);
			await Assert.That(Torque.FromPrefixedUnits(2).AsSiBaseUnit()).IsEqualTo(2);
			await Assert.That(Voltage.FromSiBaseUnit(0.002).AsPrefixedUnits(-3)).IsEqualTo(2);
			await Assert.That(CurrentDensity.FromSiBaseUnit(1000000).AsPrefixedUnits(ampPrefixExponent: 0, metrePrefixExponent: -3)).IsEqualTo(1);
			await Assert.That(Torque.FromSiBaseUnit(2).AsPrefixedUnits()).IsEqualTo(2);
		}
	}

	[Test]
	public async Task PrefixesOnlyApplyToSupportedFactorPlacements()
	{
		string[] names = Assembly.Load("DimensionalQuantities.Extensions.Metric").GetExportedTypes()
			.Where(type => type.Namespace == "DimensionalQuantities.Extensions.Metric")
			.SelectMany(type => type.GetMethods()).Select(method => method.Name).ToArray();
		using (Assert.Multiple())
		{
			await Assert.That(names.Contains("FromInverseMilliKelvin")).IsFalse();
			await Assert.That(names.Contains("AsInverseMilliKelvin")).IsFalse();
			await Assert.That(names.Contains("FromMilliAmpsPerSquareMilliMetre")).IsFalse();
			await Assert.That(names.Contains("FromKiloGramsPerCubicCentiMetre")).IsFalse();
		}
	}
}
