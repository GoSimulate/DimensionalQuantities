using DimensionalQuantities.Units.Declarations;

namespace DimensionalQuantities.Extensions.Metric;

[UnitProvider(GenerateSiPrefixes = true, GeneratePrefixConversions = true)]
internal static class MetricUnits
{
	[UnitSymbol("Metre", "Metres", "m")]
	private sealed class Metre { }
	[UnitSymbol("Gram", "Grams", "g", SiBasePrefixExponent = 3)]
	private sealed class Gram { }
	[UnitSymbol("Second", "Seconds", "s")]
	private sealed class Second { }
	[UnitSymbol("Amp", "Amps", "A")]
	private sealed class Amp { }
	[UnitSymbol("Kelvin", "Kelvin", "K")]
	private sealed class Kelvin { }
	[UnitSymbol("Radian", "Radians", "rad")]
	private sealed class Radian { }
	[UnitSymbol("Hertz", "Hertz", "Hz")]
	private sealed class Hertz { }
	[UnitSymbol("Pascal", "Pascals", "Pa")]
	private sealed class Pascal { }
	[UnitSymbol("Joule", "Joules", "J")]
	private sealed class Joule { }
	[UnitSymbol("Newton", "Newtons", "N")]
	private sealed class Newton { }
	[UnitSymbol("Watt", "Watts", "W")]
	private sealed class Watt { }
	[UnitSymbol("Volt", "Volts", "V")]
	private sealed class Volt { }
	[UnitSymbol("Ohm", "Ohms", "Ω")]
	private sealed class Ohm { }
	[UnitSymbol("Henry", "Henries", "H")]
	private sealed class Henry { }
	[UnitSymbol("Weber", "Webers", "Wb")]
	private sealed class Weber { }
	[UnitSymbol("Tesla", "Teslas", "T")]
	private sealed class Tesla { }
	[UnitSymbol("Siemens", "Siemens", "S")]
	private sealed class Siemens { }

	[QuantityUnit(typeof(Area))]
	[UnitFactor(typeof(Metre), Power = 2)]
	private sealed class AreaUnit { }
	[QuantityUnit(typeof(Volume))]
	[UnitFactor(typeof(Metre), Power = 3)]
	private sealed class VolumeUnit { }
	[QuantityUnit(typeof(Frequency))]
	[UnitFactor(typeof(Hertz))]
	private sealed class FrequencyUnit { }
	[QuantityUnit(typeof(AngularVelocity))]
	[UnitFactor(typeof(Radian))]
	[UnitFactor(typeof(Second), Power = -1)]
	private sealed class AngularVelocityUnit { }
	[QuantityUnit(typeof(Density))]
	[UnitFactor(typeof(Gram), PrefixExponent = 3)]
	[UnitFactor(typeof(Metre), Power = -3)]
	[AdditionalUnit(typeof(DensityCgs))]
	private sealed class DensityUnit { }
	[UnitFactor(typeof(Gram))]
	[UnitFactor(typeof(Metre), Power = -3, PrefixExponent = -2)]
	private sealed class DensityCgs { }
	[QuantityUnit(typeof(Pressure))]
	[UnitFactor(typeof(Pascal))]
	private sealed class PressureUnit { }
	[QuantityUnit(typeof(Stress))]
	[UnitFactor(typeof(Pascal))]
	private sealed class StressUnit { }
	[QuantityUnit(typeof(YoungsModulus))]
	[UnitFactor(typeof(Pascal))]
	private sealed class YoungsModulusUnit { }
	[QuantityUnit(typeof(Energy))]
	[UnitFactor(typeof(Joule))]
	private sealed class EnergyUnit { }
	[QuantityUnit(typeof(Torque))]
	[UnitFactor(typeof(Newton))]
	[UnitFactor(typeof(Metre))]
	private sealed class TorqueUnit { }
	[QuantityUnit(typeof(Power))]
	[UnitFactor(typeof(Watt))]
	private sealed class PowerUnit { }
	[QuantityUnit(typeof(SpecificPower))]
	[UnitFactor(typeof(Watt))]
	[UnitFactor(typeof(Gram), Power = -1, PrefixExponent = 3)]
	private sealed class SpecificPowerUnit { }
	[QuantityUnit(typeof(SpecificPowerLoss))]
	[UnitFactor(typeof(Watt))]
	[UnitFactor(typeof(Gram), Power = -1, PrefixExponent = 3)]
	private sealed class SpecificPowerLossUnit { }
	[QuantityUnit(typeof(TemperatureCoefficient))]
	[UnitFactor(typeof(Kelvin), Power = -1)]
	private sealed class TemperatureCoefficientUnit { }
	[QuantityUnit(typeof(PowerDensity))]
	[UnitFactor(typeof(Watt))]
	[UnitFactor(typeof(Metre), Power = -3)]
	private sealed class PowerDensityUnit { }
	[QuantityUnit(typeof(VolumetricPowerLoss))]
	[UnitFactor(typeof(Watt))]
	[UnitFactor(typeof(Metre), Power = -3)]
	private sealed class VolumetricPowerLossUnit { }
	[QuantityUnit(typeof(Voltage))]
	[UnitFactor(typeof(Volt))]
	private sealed class VoltageUnit { }
	[QuantityUnit(typeof(Resistance))]
	[UnitFactor(typeof(Ohm))]
	private sealed class ResistanceUnit { }
	[QuantityUnit(typeof(Inductance))]
	[UnitFactor(typeof(Henry))]
	private sealed class InductanceUnit { }
	[QuantityUnit(typeof(MagneticFlux))]
	[UnitFactor(typeof(Weber))]
	private sealed class MagneticFluxUnit { }
	[QuantityUnit(typeof(FluxLinkage))]
	[UnitFactor(typeof(Weber))]
	private sealed class FluxLinkageUnit { }
	[QuantityUnit(typeof(MagneticFluxDensity))]
	[UnitFactor(typeof(Tesla))]
	private sealed class MagneticFluxDensityUnit { }
	[QuantityUnit(typeof(MagneticPolarisation))]
	[UnitFactor(typeof(Tesla))]
	private sealed class MagneticPolarisationUnit { }
	[QuantityUnit(typeof(RemanentFluxDensity))]
	[UnitFactor(typeof(Tesla))]
	private sealed class RemanentFluxDensityUnit { }
	[QuantityUnit(typeof(MagneticFieldStrength))]
	[UnitFactor(typeof(Amp))]
	[UnitFactor(typeof(Metre), Power = -1)]
	private sealed class MagneticFieldStrengthUnit { }
	[QuantityUnit(typeof(CoerciveFieldStrength))]
	[UnitFactor(typeof(Amp))]
	[UnitFactor(typeof(Metre), Power = -1)]
	private sealed class CoerciveFieldStrengthUnit { }
	[QuantityUnit(typeof(CurrentDensity))]
	[UnitFactor(typeof(Amp))]
	[UnitFactor(typeof(Metre), Power = -2)]
	[AdditionalUnit(typeof(CurrentDensityPerSquareMillimetre))]
	private sealed class CurrentDensityUnit { }
	[UnitFactor(typeof(Amp))]
	[UnitFactor(typeof(Metre), Power = -2, PrefixExponent = -3)]
	private sealed class CurrentDensityPerSquareMillimetre { }
	[QuantityUnit(typeof(Permeability))]
	[UnitFactor(typeof(Henry))]
	[UnitFactor(typeof(Metre), Power = -1)]
	private sealed class PermeabilityUnit { }
	[QuantityUnit(typeof(Resistivity))]
	[UnitFactor(typeof(Ohm))]
	[UnitFactor(typeof(Metre))]
	private sealed class ResistivityUnit { }
	[QuantityUnit(typeof(Conductivity))]
	[UnitFactor(typeof(Siemens))]
	[UnitFactor(typeof(Metre), Power = -1)]
	private sealed class ConductivityUnit { }
	[QuantityUnit(typeof(MagneticReluctivity))]
	[UnitFactor(typeof(Metre))]
	[UnitFactor(typeof(Henry), Power = -1)]
	private sealed class MagneticReluctivityUnit { }
	[QuantityUnit(typeof(MagneticVectorPotential))]
	[UnitFactor(typeof(Weber))]
	[UnitFactor(typeof(Metre), Power = -1)]
	private sealed class MagneticVectorPotentialUnit { }
}
