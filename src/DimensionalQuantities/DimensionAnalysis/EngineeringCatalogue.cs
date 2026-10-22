using DimensionalQuantities.Units;

namespace DimensionalQuantities.DimensionAnalysis;

/// <summary>Recognised names and units for engineering dimensions.</summary>
internal static class EngineeringCatalogue
{
	private static readonly UnitExpression _density = new(new(AtomicUnits.Gram, PrefixExponent: SiPrefixes.Kilo), new(AtomicUnits.Metre, -3));
	private static readonly UnitExpression _specificPower = new(new(AtomicUnits.Watt), new(AtomicUnits.Gram, -1, SiPrefixes.Kilo));
	private static readonly UnitExpression _powerDensity = new(new(AtomicUnits.Watt), new(AtomicUnits.Metre, -3));
	private static readonly UnitExpression _magneticFieldStrength = new(new(AtomicUnits.Amp), new(AtomicUnits.Metre, -1));
	private static readonly UnitExpression _currentDensity = new(new(AtomicUnits.Amp), new(AtomicUnits.Metre, -2));

	public static readonly Dictionary<Dimension, UnitDefinition[]> Entries = new()
	{
		{ new(Length: 2), [new("Area", [new(AtomicUnits.Metre, 2)])] },
		{ new(Length: 3), [new("Volume", [new(AtomicUnits.Metre, 3)])] },
		{ new(Time: -1), [new("Frequency", [new(AtomicUnits.Hertz)]), new("AngularVelocity", [new(AtomicUnits.Radian), new(AtomicUnits.Second, -1)])] },
		{ new(Length: -3, Mass: 1), [new("Density", _density, [new(new(AtomicUnits.Gram), new(AtomicUnits.Metre, Power: -3, PrefixExponent: SiPrefixes.Centi))])] },
		{
			new(Length: -1, Mass: 1, Time: -2),
			[
				new("Pressure", [new(AtomicUnits.Pascal)]),
				new("Stress", [new(AtomicUnits.Pascal)]),
				new("YoungsModulus", [new(AtomicUnits.Pascal)])
			]
		},
		{ new(Length: 2, Mass: 1, Time: -2), [new("Energy", [new(AtomicUnits.Joule)]), new("Torque", [new(AtomicUnits.Newton), new(AtomicUnits.Metre)])] },
		{ new(Length: 2, Mass: 1, Time: -3), [new("Power", [new(AtomicUnits.Watt)])] },
		{ new(Length: 2, Time: -3), [new("SpecificPower", _specificPower), new("SpecificPowerLoss", _specificPower)] },
		{ new(Temperature: -1), [new("TemperatureCoefficient", [new(AtomicUnits.Kelvin, -1)])] },
		{ new(Length: -1, Mass: 1, Time: -3), [new("PowerDensity", _powerDensity), new("VolumetricPowerLoss", _powerDensity)] },
		{ new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -1), [new("Voltage", [new(AtomicUnits.Volt)])] },
		{ new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -2), [new("Resistance", [new(AtomicUnits.Ohm)])] },
		{ new(Length: 2, Mass: 1, Time: -2, ElectricCurrent: -2), [new("Inductance", [new(AtomicUnits.Henry)])] },
		{ new(Length: 2, Mass: 1, Time: -2, ElectricCurrent: -1), [new("MagneticFlux", [new(AtomicUnits.Weber)]), new("FluxLinkage", [new(AtomicUnits.Weber)])] },
		{
			new(Mass: 1, Time: -2, ElectricCurrent: -1),
			[
				new("MagneticFluxDensity", [new(AtomicUnits.Tesla)]),
				new("MagneticPolarisation", [new(AtomicUnits.Tesla)]),
				new("RemanentFluxDensity", [new(AtomicUnits.Tesla)])
			]
		},
		{ new(Length: -1, ElectricCurrent: 1), [new("MagneticFieldStrength", _magneticFieldStrength), new("CoerciveFieldStrength", _magneticFieldStrength)] },
		{ new(Length: -2, ElectricCurrent: 1), [new("CurrentDensity", _currentDensity, [new(new(AtomicUnits.Amp), new(AtomicUnits.Metre, Power: -2, PrefixExponent: SiPrefixes.Milli))])] },
		{ new(Length: 1, Mass: 1, Time: -2, ElectricCurrent: -2), [new("Permeability", [new(AtomicUnits.Henry), new(AtomicUnits.Metre, -1)])] },
		{ new(Length: 3, Mass: 1, Time: -3, ElectricCurrent: -2), [new("Resistivity", [new(AtomicUnits.Ohm), new(AtomicUnits.Metre)])] },
		{ new(Length: -3, Mass: -1, Time: 3, ElectricCurrent: 2), [new("Conductivity", [new(AtomicUnits.Siemens), new(AtomicUnits.Metre, -1)])] },
		{ new(Length: -1, Mass: -1, Time: 2, ElectricCurrent: 2), [new("MagneticReluctivity", [new(AtomicUnits.Metre), new(AtomicUnits.Henry, -1)])] },
		{ new(Length: 1, Mass: 1, Time: -2, ElectricCurrent: -1), [new("MagneticVectorPotential", [new(AtomicUnits.Weber), new(AtomicUnits.Metre, -1)])] },
	};
}
