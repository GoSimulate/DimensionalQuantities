using DimensionalQuantities.Units.Declarations;
using DimensionalQuantities.Units.Symbols;

namespace DimensionalQuantities.Extensions.Metric;

[UnitProvider(GenerateSiPrefixes = true, GeneratePrefixConversions = true)]
internal static class MetricUnits
{
	[QuantityUnit(typeof(Area))]
	[UnitFactor(typeof(Metre), Power = 2)]
	private sealed class AreaUnit { }
	[QuantityUnit(typeof(Volume))]
	[UnitFactor(typeof(Metre), Power = 3)]
	private sealed class VolumeUnit { }
	[QuantityUnit(typeof(Frequency))]
	[UnitFactor(typeof(MetricSymbols.Hertz))]
	private sealed class FrequencyUnit { }
	[QuantityUnit(typeof(AngularVelocity))]
	[UnitFactor(typeof(MetricSymbols.Radian))]
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
	[QuantityUnit(typeof(Stress))]
	[QuantityUnit(typeof(YoungsModulus))]
	[UnitFactor(typeof(MetricSymbols.Pascal))]
	private sealed class PascalUnit { }
	[QuantityUnit(typeof(Energy))]
	[UnitFactor(typeof(MetricSymbols.Joule))]
	private sealed class EnergyUnit { }
	[QuantityUnit(typeof(Torque))]
	[UnitFactor(typeof(MetricSymbols.Newton))]
	[UnitFactor(typeof(Metre))]
	private sealed class TorqueUnit { }
	[QuantityUnit(typeof(Power))]
	[UnitFactor(typeof(MetricSymbols.Watt))]
	private sealed class PowerUnit { }
	[QuantityUnit(typeof(SpecificPower))]
	[QuantityUnit(typeof(SpecificPowerLoss))]
	[UnitFactor(typeof(MetricSymbols.Watt))]
	[UnitFactor(typeof(Gram), Power = -1, PrefixExponent = 3)]
	private sealed class SpecificPowerUnit { }
	[QuantityUnit(typeof(TemperatureCoefficient))]
	[UnitFactor(typeof(Kelvin), Power = -1)]
	private sealed class TemperatureCoefficientUnit { }
	[QuantityUnit(typeof(PowerDensity))]
	[QuantityUnit(typeof(VolumetricPowerLoss))]
	[UnitFactor(typeof(MetricSymbols.Watt))]
	[UnitFactor(typeof(Metre), Power = -3)]
	private sealed class PowerDensityUnit { }
	[QuantityUnit(typeof(Voltage))]
	[UnitFactor(typeof(MetricSymbols.Volt))]
	private sealed class VoltageUnit { }
	[QuantityUnit(typeof(Resistance))]
	[UnitFactor(typeof(MetricSymbols.Ohm))]
	private sealed class ResistanceUnit { }
	[QuantityUnit(typeof(Inductance))]
	[UnitFactor(typeof(MetricSymbols.Henry))]
	private sealed class InductanceUnit { }
	[QuantityUnit(typeof(MagneticFlux))]
	[QuantityUnit(typeof(FluxLinkage))]
	[UnitFactor(typeof(MetricSymbols.Weber))]
	private sealed class MagneticFluxUnit { }
	[QuantityUnit(typeof(MagneticFluxDensity))]
	[QuantityUnit(typeof(MagneticPolarisation))]
	[QuantityUnit(typeof(RemanentFluxDensity))]
	[UnitFactor(typeof(MetricSymbols.Tesla))]
	private sealed class MagneticFluxDensityUnit { }
	[QuantityUnit(typeof(MagneticFieldStrength))]
	[QuantityUnit(typeof(CoerciveFieldStrength))]
	[UnitFactor(typeof(Amp))]
	[UnitFactor(typeof(Metre), Power = -1)]
	private sealed class MagneticFieldStrengthUnit { }
	[QuantityUnit(typeof(CurrentDensity))]
	[UnitFactor(typeof(Amp))]
	[UnitFactor(typeof(Metre), Power = -2)]
	[AdditionalUnit(typeof(CurrentDensityPerSquareMillimetre))]
	private sealed class CurrentDensityUnit { }
	[UnitFactor(typeof(Amp))]
	[UnitFactor(typeof(Metre), Power = -2, PrefixExponent = -3)]
	private sealed class CurrentDensityPerSquareMillimetre { }
	[QuantityUnit(typeof(Permeability))]
	[UnitFactor(typeof(MetricSymbols.Henry))]
	[UnitFactor(typeof(Metre), Power = -1)]
	private sealed class PermeabilityUnit { }
	[QuantityUnit(typeof(Resistivity))]
	[UnitFactor(typeof(MetricSymbols.Ohm))]
	[UnitFactor(typeof(Metre))]
	private sealed class ResistivityUnit { }
	[QuantityUnit(typeof(Conductivity))]
	[UnitFactor(typeof(MetricSymbols.Siemens))]
	[UnitFactor(typeof(Metre), Power = -1)]
	private sealed class ConductivityUnit { }
	[QuantityUnit(typeof(MagneticReluctivity))]
	[UnitFactor(typeof(Metre))]
	[UnitFactor(typeof(MetricSymbols.Henry), Power = -1)]
	private sealed class MagneticReluctivityUnit { }
	[QuantityUnit(typeof(MagneticVectorPotential))]
	[UnitFactor(typeof(MetricSymbols.Weber))]
	[UnitFactor(typeof(Metre), Power = -1)]
	private sealed class MagneticVectorPotentialUnit { }
}
