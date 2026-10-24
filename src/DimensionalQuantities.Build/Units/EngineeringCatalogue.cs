using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Units;

namespace DimensionalQuantities.Build.Units;

/// <summary>Recognised names and units for engineering dimensions.</summary>
internal static class EngineeringCatalogue
{
	private static readonly UnitExpression _density = new(new(AtomicUnits.Gram, prefixExponent: SiPrefixes.Kilo), new(AtomicUnits.Metre, -3));
	private static readonly UnitExpression _specificPower = new(new(EngineeringAtomicUnits.Watt), new(AtomicUnits.Gram, -1, SiPrefixes.Kilo));
	private static readonly UnitExpression _powerDensity = new(new(EngineeringAtomicUnits.Watt), new(AtomicUnits.Metre, -3));
	private static readonly UnitExpression _magneticFieldStrength = new(new(AtomicUnits.Amp), new(AtomicUnits.Metre, -1));
	private static readonly UnitExpression _currentDensity = new(new(AtomicUnits.Amp), new(AtomicUnits.Metre, -2));

	public static readonly Dictionary<Dimension, UnitDefinition[]> Entries = new()
	{
		{ new(Length: 2), [new("Area", [new(AtomicUnits.Metre, 2)])] },
		{ new(Length: 3), [new("Volume", [new(AtomicUnits.Metre, 3)])] },
		{ new(Time: -1), [new("Frequency", [new(EngineeringAtomicUnits.Hertz)]), new("AngularVelocity", [new(EngineeringAtomicUnits.Radian), new(AtomicUnits.Second, -1)])] },
		{ new(Length: -3, Mass: 1), [new("Density", _density, [new(new(AtomicUnits.Gram), new(AtomicUnits.Metre, power: -3, prefixExponent: SiPrefixes.Centi))])] },
		{
			new(Length: -1, Mass: 1, Time: -2),
			[
				new("Pressure", [new(EngineeringAtomicUnits.Pascal)]),
				new("Stress", [new(EngineeringAtomicUnits.Pascal)]),
				new("YoungsModulus", [new(EngineeringAtomicUnits.Pascal)])
			]
		},
		{ new(Length: 2, Mass: 1, Time: -2), [new("Energy", [new(EngineeringAtomicUnits.Joule)]), new("Torque", [new(EngineeringAtomicUnits.Newton), new(AtomicUnits.Metre)])] },
		{ new(Length: 2, Mass: 1, Time: -3), [new("Power", [new(EngineeringAtomicUnits.Watt)])] },
		{ new(Length: 2, Time: -3), [new("SpecificPower", _specificPower), new("SpecificPowerLoss", _specificPower)] },
		{ new(Temperature: -1), [new("TemperatureCoefficient", [new(AtomicUnits.Kelvin, -1)])] },
		{ new(Length: -1, Mass: 1, Time: -3), [new("PowerDensity", _powerDensity), new("VolumetricPowerLoss", _powerDensity)] },
		{ new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -1), [new("Voltage", [new(EngineeringAtomicUnits.Volt)])] },
		{ new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -2), [new("Resistance", [new(EngineeringAtomicUnits.Ohm)])] },
		{ new(Length: 2, Mass: 1, Time: -2, ElectricCurrent: -2), [new("Inductance", [new(EngineeringAtomicUnits.Henry)])] },
		{ new(Length: 2, Mass: 1, Time: -2, ElectricCurrent: -1), [new("MagneticFlux", [new(EngineeringAtomicUnits.Weber)]), new("FluxLinkage", [new(EngineeringAtomicUnits.Weber)])] },
		{
			new(Mass: 1, Time: -2, ElectricCurrent: -1),
			[
				new("MagneticFluxDensity", [new(EngineeringAtomicUnits.Tesla)]),
				new("MagneticPolarisation", [new(EngineeringAtomicUnits.Tesla)]),
				new("RemanentFluxDensity", [new(EngineeringAtomicUnits.Tesla)])
			]
		},
		{ new(Length: -1, ElectricCurrent: 1), [new("MagneticFieldStrength", _magneticFieldStrength), new("CoerciveFieldStrength", _magneticFieldStrength)] },
		{ new(Length: -2, ElectricCurrent: 1), [new("CurrentDensity", _currentDensity, [new(new(AtomicUnits.Amp), new(AtomicUnits.Metre, power: -2, prefixExponent: SiPrefixes.Milli))])] },
		{ new(Length: 1, Mass: 1, Time: -2, ElectricCurrent: -2), [new("Permeability", [new(EngineeringAtomicUnits.Henry), new(AtomicUnits.Metre, -1)])] },
		{ new(Length: 3, Mass: 1, Time: -3, ElectricCurrent: -2), [new("Resistivity", [new(EngineeringAtomicUnits.Ohm), new(AtomicUnits.Metre)])] },
		{ new(Length: -3, Mass: -1, Time: 3, ElectricCurrent: 2), [new("Conductivity", [new(EngineeringAtomicUnits.Siemens), new(AtomicUnits.Metre, -1)])] },
		{ new(Length: -1, Mass: -1, Time: 2, ElectricCurrent: 2), [new("MagneticReluctivity", [new(AtomicUnits.Metre), new(EngineeringAtomicUnits.Henry, -1)])] },
		{ new(Length: 1, Mass: 1, Time: -2, ElectricCurrent: -1), [new("MagneticVectorPotential", [new(EngineeringAtomicUnits.Weber), new(AtomicUnits.Metre, -1)])] },
	};
}
