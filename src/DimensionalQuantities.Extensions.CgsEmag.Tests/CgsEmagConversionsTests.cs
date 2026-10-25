using System.Reflection;
using DimensionalQuantities.Extensions.CgsEmag;
using Xunit;

namespace DimensionalQuantities.Extensions.CgsEmag.Tests;

public sealed class CgsEmagConversionsTests
{
	[Fact]
	public void MechanicalUnitsConvertToCoherentSi()
	{
		Assert.Multiple(
			() => Assert.Equal(1e-5, Force.FromDynes(1).AsSiBaseUnit()),
			() => Assert.Equal(1e-7, Energy.FromErgs(1).AsSiBaseUnit()),
			() => Assert.Equal(0.1, Pressure.FromBaryes(1).AsSiBaseUnit()),
			() => Assert.Equal(0.01, Acceleration.FromGals(1).AsSiBaseUnit()));
	}

	[Fact]
	public void ElectromagneticUnitsConvertToCoherentSi()
	{
		Assert.Multiple(
			() => Assert.Equal(10, Current.FromAbamperes(1).AsSiBaseUnit()),
			() => Assert.Equal(10, Current.FromBiots(1).AsSiBaseUnit()),
			() => Assert.Equal(10, ElectricCharge.FromAbcoulombs(1).AsSiBaseUnit()),
			() => Assert.Equal(1e-8, Voltage.FromAbvolts(1).AsSiBaseUnit()),
			() => Assert.Equal(1e-9, Resistance.FromAbohms(1).AsSiBaseUnit()),
			() => Assert.Equal(1e-9, Inductance.FromAbhenries(1).AsSiBaseUnit()),
			() => Assert.Equal(1e9, Capacitance.FromAbfarads(1).AsSiBaseUnit()),
			() => Assert.Equal(1e-8, MagneticFlux.FromMaxwells(1).AsSiBaseUnit()),
			() => Assert.Equal(1e-4, MagneticFluxDensity.FromGauss(1).AsSiBaseUnit()));

		double oersted = MagneticFieldStrength.FromOersteds(1).AsSiBaseUnit();
		Assert.InRange(oersted, 1000 / (4 * Math.PI) - 1e-12, 1000 / (4 * Math.PI) + 1e-12);
	}

	[Fact]
	public void AccessorsReturnOriginalCgsMagnitude()
	{
		Assert.Multiple(
			() => Assert.Equal(1, Force.FromSiBaseUnit(1e-5).AsDynes()),
			() => Assert.Equal(1, Energy.FromSiBaseUnit(1e-7).AsErgs()),
			() => Assert.Equal(1, Pressure.FromSiBaseUnit(0.1).AsBaryes()),
			() => Assert.Equal(1, Acceleration.FromSiBaseUnit(0.01).AsGals()),
			() => Assert.Equal(1, Current.FromSiBaseUnit(10).AsAbamperes()),
			() => Assert.Equal(1, Current.FromSiBaseUnit(10).AsBiots()),
			() => Assert.Equal(1, ElectricCharge.FromSiBaseUnit(10).AsAbcoulombs()),
			() => Assert.Equal(1, Voltage.FromSiBaseUnit(1e-8).AsAbvolts()),
			() => Assert.Equal(1, Resistance.FromSiBaseUnit(1e-9).AsAbohms()),
			() => Assert.Equal(1, Inductance.FromSiBaseUnit(1e-9).AsAbhenries()),
			() => Assert.Equal(1, Capacitance.FromSiBaseUnit(1e9).AsAbfarads()),
			() => Assert.Equal(1, MagneticFlux.FromSiBaseUnit(1e-8).AsMaxwells()),
			() => Assert.Equal(1, MagneticFluxDensity.FromSiBaseUnit(1e-4).AsGauss()));

		double oersted = MagneticFieldStrength.FromSiBaseUnit(1000 / (4 * Math.PI)).AsOersteds();
		Assert.InRange(oersted, 1 - 1e-14, 1 + 1e-14);
	}

	[Fact]
	public void SharedDimensionAliasesUseTheConversions()
	{
		Assert.Multiple(
			() => Assert.Equal(1e-7, Torque.FromErgs(1).AsSiBaseUnit()),
			() => Assert.Equal(0.1, Stress.FromBaryes(1).AsSiBaseUnit()),
			() => Assert.Equal(0.1, YoungsModulus.FromBaryes(1).AsSiBaseUnit()),
			() => Assert.Equal(1e-8, FluxLinkage.FromMaxwells(1).AsSiBaseUnit()),
			() => Assert.Equal(1e-4, RemanentFluxDensity.FromGauss(1).AsSiBaseUnit()));
	}

	[Fact]
	public void ProviderDoesNotGeneratePrefixConversions()
	{
		string[] names = Assembly.Load("DimensionalQuantities.Extensions.CgsEmag").GetExportedTypes()
			.Where(type => type.Namespace == "DimensionalQuantities.Extensions.CgsEmag")
			.SelectMany(type => type.GetMethods()).Select(method => method.Name).ToArray();

		Assert.Multiple(
			() => Assert.DoesNotContain("FromPrefixedUnits", names),
			() => Assert.DoesNotContain("AsPrefixedUnits", names),
			() => Assert.DoesNotContain("FromKiloGauss", names),
			() => Assert.DoesNotContain("AsKiloGauss", names));
	}
}
