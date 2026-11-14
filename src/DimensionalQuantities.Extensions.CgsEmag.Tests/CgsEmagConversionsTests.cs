using Assembly = System.Reflection.Assembly;
using DimensionalQuantities.Extensions.CgsEmag;

namespace DimensionalQuantities.Extensions.CgsEmag.Tests;

public sealed class CgsEmagConversionsTests
{
	[Test]
	public async Task MechanicalUnitsConvertToCoherentSi()
	{
		using (Assert.Multiple())
		{
			await Assert.That(Force.FromDynes(1).AsSiBaseUnit()).IsEqualTo(1e-5);
			await Assert.That(Energy.FromErgs(1).AsSiBaseUnit()).IsEqualTo(1e-7);
			await Assert.That(Pressure.FromBaryes(1).AsSiBaseUnit()).IsEqualTo(0.1);
			await Assert.That(Acceleration.FromGals(1).AsSiBaseUnit()).IsEqualTo(0.01);
		}
	}

	[Test]
	public async Task ElectromagneticUnitsConvertToCoherentSi()
	{
		using (Assert.Multiple())
		{
			await Assert.That(Current.FromAbamperes(1).AsSiBaseUnit()).IsEqualTo(10);
			await Assert.That(Current.FromBiots(1).AsSiBaseUnit()).IsEqualTo(10);
			await Assert.That(ElectricCharge.FromAbcoulombs(1).AsSiBaseUnit()).IsEqualTo(10);
			await Assert.That(Voltage.FromAbvolts(1).AsSiBaseUnit()).IsEqualTo(1e-8);
			await Assert.That(Resistance.FromAbohms(1).AsSiBaseUnit()).IsEqualTo(1e-9);
			await Assert.That(Inductance.FromAbhenries(1).AsSiBaseUnit()).IsEqualTo(1e-9);
			await Assert.That(Capacitance.FromAbfarads(1).AsSiBaseUnit()).IsEqualTo(1e9);
			await Assert.That(MagneticFlux.FromMaxwells(1).AsSiBaseUnit()).IsEqualTo(1e-8);
			await Assert.That(MagneticFluxDensity.FromGauss(1).AsSiBaseUnit()).IsEqualTo(1e-4);
		}

		double oersted = MagneticFieldStrength.FromOersteds(1).AsSiBaseUnit();
		await Assert.That(oersted >= 1000 / (4 * Math.PI) - 1e-12 && oersted <= 1000 / (4 * Math.PI) + 1e-12).IsTrue();
	}

	[Test]
	public async Task AccessorsReturnOriginalCgsMagnitude()
	{
		using (Assert.Multiple())
		{
			await Assert.That(Force.FromSiBaseUnit(1e-5).AsDynes()).IsEqualTo(1);
			await Assert.That(Energy.FromSiBaseUnit(1e-7).AsErgs()).IsEqualTo(1);
			await Assert.That(Pressure.FromSiBaseUnit(0.1).AsBaryes()).IsEqualTo(1);
			await Assert.That(Acceleration.FromSiBaseUnit(0.01).AsGals()).IsEqualTo(1);
			await Assert.That(Current.FromSiBaseUnit(10).AsAbamperes()).IsEqualTo(1);
			await Assert.That(Current.FromSiBaseUnit(10).AsBiots()).IsEqualTo(1);
			await Assert.That(ElectricCharge.FromSiBaseUnit(10).AsAbcoulombs()).IsEqualTo(1);
			await Assert.That(Voltage.FromSiBaseUnit(1e-8).AsAbvolts()).IsEqualTo(1);
			await Assert.That(Resistance.FromSiBaseUnit(1e-9).AsAbohms()).IsEqualTo(1);
			await Assert.That(Inductance.FromSiBaseUnit(1e-9).AsAbhenries()).IsEqualTo(1);
			await Assert.That(Capacitance.FromSiBaseUnit(1e9).AsAbfarads()).IsEqualTo(1);
			await Assert.That(MagneticFlux.FromSiBaseUnit(1e-8).AsMaxwells()).IsEqualTo(1);
			await Assert.That(MagneticFluxDensity.FromSiBaseUnit(1e-4).AsGauss()).IsEqualTo(1);
		}

		double oersted = MagneticFieldStrength.FromSiBaseUnit(1000 / (4 * Math.PI)).AsOersteds();
		await Assert.That(oersted >= 1 - 1e-14 && oersted <= 1 + 1e-14).IsTrue();
	}

	[Test]
	public async Task SharedDimensionAliasesUseTheConversions()
	{
		using (Assert.Multiple())
		{
			await Assert.That(Torque.FromErgs(1).AsSiBaseUnit()).IsEqualTo(1e-7);
			await Assert.That(Stress.FromBaryes(1).AsSiBaseUnit()).IsEqualTo(0.1);
			await Assert.That(YoungsModulus.FromBaryes(1).AsSiBaseUnit()).IsEqualTo(0.1);
			await Assert.That(FluxLinkage.FromMaxwells(1).AsSiBaseUnit()).IsEqualTo(1e-8);
			await Assert.That(RemanentFluxDensity.FromGauss(1).AsSiBaseUnit()).IsEqualTo(1e-4);
		}
	}

	[Test]
	public async Task ProviderDoesNotGeneratePrefixConversions()
	{
		string[] names = Assembly.Load("DimensionalQuantities.Extensions.CgsEmag").GetExportedTypes()
			.Where(type => type.Namespace == "DimensionalQuantities.Extensions.CgsEmag")
			.SelectMany(type => type.GetMethods()).Select(method => method.Name).ToArray();

		using (Assert.Multiple())
		{
			await Assert.That(names.Contains("FromPrefixedUnits")).IsFalse();
			await Assert.That(names.Contains("AsPrefixedUnits")).IsFalse();
			await Assert.That(names.Contains("FromKiloGauss")).IsFalse();
			await Assert.That(names.Contains("AsKiloGauss")).IsFalse();
		}
	}
}
