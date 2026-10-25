using DimensionalQuantities.Units.Declarations;

namespace DimensionalQuantities.Extensions.CgsEmag;

[UnitProvider]
internal static class CgsEmagUnits
{
	[QuantityUnit(typeof(Force))]
	[UnitFactor(typeof(CgsEmagSymbols.Dyne))]
	private sealed class ForceUnit { }

	[QuantityUnit(typeof(Energy))]
	[QuantityUnit(typeof(Torque))]
	[UnitFactor(typeof(CgsEmagSymbols.Erg))]
	private sealed class EnergyUnit { }

	[QuantityUnit(typeof(Pressure))]
	[QuantityUnit(typeof(Stress))]
	[QuantityUnit(typeof(YoungsModulus))]
	[UnitFactor(typeof(CgsEmagSymbols.Barye))]
	private sealed class PressureUnit { }

	[QuantityUnit(typeof(Acceleration))]
	[UnitFactor(typeof(CgsEmagSymbols.Gal))]
	private sealed class AccelerationUnit { }

	[QuantityUnit(typeof(Current))]
	[UnitFactor(typeof(CgsEmagSymbols.Abampere))]
	[AdditionalUnit(typeof(BiotUnit))]
	private sealed class CurrentUnit { }

	[UnitFactor(typeof(CgsEmagSymbols.Biot))]
	private sealed class BiotUnit { }

	[QuantityUnit(typeof(ElectricCharge))]
	[UnitFactor(typeof(CgsEmagSymbols.Abcoulomb))]
	private sealed class ChargeUnit { }

	[QuantityUnit(typeof(Voltage))]
	[UnitFactor(typeof(CgsEmagSymbols.Abvolt))]
	private sealed class VoltageUnit { }

	[QuantityUnit(typeof(Resistance))]
	[UnitFactor(typeof(CgsEmagSymbols.Abohm))]
	private sealed class ResistanceUnit { }

	[QuantityUnit(typeof(Inductance))]
	[UnitFactor(typeof(CgsEmagSymbols.Abhenry))]
	private sealed class InductanceUnit { }

	[QuantityUnit(typeof(Capacitance))]
	[UnitFactor(typeof(CgsEmagSymbols.Abfarad))]
	private sealed class CapacitanceUnit { }

	[QuantityUnit(typeof(MagneticFlux))]
	[QuantityUnit(typeof(FluxLinkage))]
	[UnitFactor(typeof(CgsEmagSymbols.Maxwell))]
	private sealed class MagneticFluxUnit { }

	[QuantityUnit(typeof(MagneticFluxDensity))]
	[QuantityUnit(typeof(RemanentFluxDensity))]
	[UnitFactor(typeof(CgsEmagSymbols.Gauss))]
	private sealed class MagneticFluxDensityUnit { }

	[QuantityUnit(typeof(MagneticFieldStrength))]
	[QuantityUnit(typeof(CoerciveFieldStrength))]
	[UnitFactor(typeof(CgsEmagSymbols.Oersted))]
	private sealed class MagneticFieldStrengthUnit { }
}
