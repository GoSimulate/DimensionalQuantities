using System.Reflection;
using DimensionalQuantities.Extensions.Metric;
using Xunit;

namespace DimensionalQuantities.Extensions.Metric.Tests;

public sealed class MetricConversionsTests
{
	[Fact]
	public void CatalogueFactoriesConvertToExpectedSiValues()
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
		Assert.Equal(expected, result);
	}

	[Fact]
	public void CelsiusConvertsToAndFromAbsoluteKelvin()
	{
		Assert.Multiple(
			() => Assert.Equal(273.15, Temperature.FromCelsius(0).AsKelvin(), 10),
			() => Assert.Equal(0, Temperature.FromKelvin(273.15).AsCelsius(), 10));
	}

	[Fact]
	public void CatalogueAccessorsConvertFromKnownSiValues()
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
		Assert.Equal(expected, result);
	}

	[Fact]
	public void AliasesShareNamedConversionsWithoutDuplicateHelpers()
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
		Assert.Equal(Enumerable.Repeat(1d, result.Length), result);
	}

	[Fact]
	public void NamedConversionsApplyMetricScale()
	{
		Assert.Multiple(
			() => Assert.Equal(0.0025, Voltage.FromSiBaseUnit(2.5).AsKiloVolts()),
			() => Assert.Equal(1000, Density.FromGramsPerCubicCentiMetre(1).AsSiBaseUnit()));
	}

	[Fact]
	public void GenericPrefixConversionsApplyMetricScale()
	{
		Assert.Multiple(
			() => Assert.Equal(0.002, Voltage.FromPrefixedUnits(2, -3).AsSiBaseUnit()),
			() => Assert.Equal(1000000, CurrentDensity.FromPrefixedUnits(1, ampPrefixExponent: 0, metrePrefixExponent: -3).AsSiBaseUnit()),
			() => Assert.Equal(1000, SpecificPower.FromPrefixedUnits(1, wattPrefixExponent: 3, gramPrefixExponent: 3).AsSiBaseUnit()),
			() => Assert.Equal(1000, TemperatureCoefficient.FromPrefixedUnits(1, kelvinPrefixExponent: -3).AsSiBaseUnit()),
			() => Assert.Equal(2, Torque.FromPrefixedUnits(2).AsSiBaseUnit()),
			() => Assert.Equal(2, Voltage.FromSiBaseUnit(0.002).AsPrefixedUnits(-3)),
			() => Assert.Equal(1, CurrentDensity.FromSiBaseUnit(1000000).AsPrefixedUnits(ampPrefixExponent: 0, metrePrefixExponent: -3)),
			() => Assert.Equal(2, Torque.FromSiBaseUnit(2).AsPrefixedUnits()));
	}

	[Fact]
	public void PrefixesOnlyApplyToSupportedFactorPlacements()
	{
		string[] names = Assembly.Load("DimensionalQuantities.Extensions.Metric").GetExportedTypes()
			.Where(type => type.Namespace == "DimensionalQuantities.Extensions.Metric")
			.SelectMany(type => type.GetMethods()).Select(method => method.Name).ToArray();
		Assert.Multiple(
			() => Assert.DoesNotContain("FromInverseMilliKelvin", names),
			() => Assert.DoesNotContain("AsInverseMilliKelvin", names),
			() => Assert.DoesNotContain("FromMilliAmpsPerSquareMilliMetre", names),
			() => Assert.DoesNotContain("FromKiloGramsPerCubicCentiMetre", names));
	}
}
