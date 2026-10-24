using DimensionalQuantities.Extensions.Metric;
using Xunit;

namespace DimensionalQuantities.Extensions.Metric.Tests;

public sealed class MetricConversionsTests
{
	[Fact]
	public void CatalogueFactoriesAndAccessorsCoverEngineeringDimensions()
	{
		double[] result =
		[
			Area.FromSquareMetres(1).AsSquareMetres(),
			Volume.FromCubicMetres(1).AsCubicMetres(),
			Frequency.FromHertz(1).AsHertz(),
			AngularVelocity.FromRadiansPerSecond(1).AsRadiansPerSecond(),
			Density.FromGramsPerCubicMetre(1).AsGramsPerCubicMetre(),
			Pressure.FromPascals(1).AsPascals(),
			Energy.FromJoules(1).AsJoules(),
			Torque.FromNewtonMetres(1).AsNewtonMetres(),
			Power.FromWatts(1).AsWatts(),
			SpecificPower.FromWattsPerKilogram(1).AsWattsPerKilogram(),
			TemperatureCoefficient.FromInverseKelvin(1).AsInverseKelvin(),
			PowerDensity.FromWattsPerCubicMetre(1).AsWattsPerCubicMetre(),
			Voltage.FromVolts(1).AsVolts(),
			Resistance.FromOhms(1).AsOhms(),
			Inductance.FromHenries(1).AsHenries(),
			MagneticFlux.FromWebers(1).AsWebers(),
			MagneticFluxDensity.FromTeslas(1).AsTeslas(),
			MagneticFieldStrength.FromAmpsPerMetre(1).AsAmpsPerMetre(),
			CurrentDensity.FromAmpsPerSquareMetre(1).AsAmpsPerSquareMetre(),
			Permeability.FromHenriesPerMetre(1).AsHenriesPerMetre(),
			Resistivity.FromOhmMetres(1).AsOhmMetres(),
			Conductivity.FromSiemensPerMetre(1).AsSiemensPerMetre(),
			MagneticReluctivity.FromMetresPerHenry(1).AsMetresPerHenry(),
			MagneticVectorPotential.FromWebersPerMetre(1).AsWebersPerMetre(),
		];

		Assert.Equal(Enumerable.Repeat(1d, 24), result);
	}

	[Fact]
	public void AliasesShareNamedConversionsWithoutDuplicateHelpers()
	{
		Pressure pressure = Pressure.FromPascals(1);
		Stress stress = Stress.FromPascals(1);
		YoungsModulus modulus = YoungsModulus.FromKiloPascals(1);
		double[] result = [pressure.AsPascals(), stress.AsPascals(), modulus.AsKiloPascals()];
		Assert.Equal([1d, 1d, 1d], result);
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
		string[] names = typeof(P11_MetricUnitsA0_TemperatureCoefficientExtensions).Assembly.GetExportedTypes()
			.Where(type => type.Namespace == "DimensionalQuantities.Extensions.Metric")
			.SelectMany(type => type.GetMethods()).Select(method => method.Name).ToArray();
		Assert.Multiple(
			() => Assert.DoesNotContain("FromInverseMilliKelvin", names),
			() => Assert.DoesNotContain("AsInverseMilliKelvin", names),
			() => Assert.DoesNotContain("FromMilliAmpsPerSquareMilliMetre", names),
			() => Assert.DoesNotContain("FromKiloGramsPerCubicCentiMetre", names));
	}
}
