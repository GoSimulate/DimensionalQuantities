namespace DimensionalQuantities.DimensionAnalysis;

/// <summary>Recognised names and symbols for engineering dimensions.</summary>
internal static class EngineeringCatalogue
{
	public static readonly Dictionary<Dimension, AlternateUnit[]> Entries = new()
	{
		{ new(Length: 2), [new("Area", "m²")] },
		{ new(Length: 3), [new("Volume", "m³")] },
		{ new(Time: -1), [new("Frequency", "Hz"), new("AngularVelocity", "rad/s")] },
		{ new(Length: -3, Mass: 1), [new("Density", "kg/m³")] },
		{ new(Length: -1, Mass: 1, Time: -2), [new("Pressure", "Pa"), new("Stress", "Pa"), new("YoungsModulus", "Pa")] },
		{ new(Length: 2, Mass: 1, Time: -2), [new("Energy", "J"), new("Torque", "Nm")] },
		{ new(Length: 2, Mass: 1, Time: -3), [new("Power", "W")] },
		{ new(Length: 2, Time: -3), [new("SpecificPower", "W/kg"), new("SpecificPowerLoss", "W/kg")] },
		{ new(Temperature: -1), [new("TemperatureCoefficient", "K⁻¹")] },
		{ new(Length: -1, Mass: 1, Time: -3), [new("PowerDensity", "W/m³"), new("VolumetricPowerLoss", "W/m³")] },
		{ new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -1), [new("Voltage", "V")] },
		{ new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -2), [new("Resistance", "Ω")] },
		{ new(Length: 2, Mass: 1, Time: -2, ElectricCurrent: -2), [new("Inductance", "H")] },
		{ new(Length: 2, Mass: 1, Time: -2, ElectricCurrent: -1), [new("MagneticFlux", "Wb"), new("FluxLinkage", "Wb")] },
		{ new(Mass: 1, Time: -2, ElectricCurrent: -1), [new("MagneticFluxDensity", "T"), new("MagneticPolarisation", "T"), new("RemanentFluxDensity", "T")] },
		{ new(Length: -1, ElectricCurrent: 1), [new("MagneticFieldStrength", "A/m"), new("CoerciveFieldStrength", "A/m")] },
		{ new(Length: -2, ElectricCurrent: 1), [new("CurrentDensity", "A/m²")] },
		{ new(Length: 1, Mass: 1, Time: -2, ElectricCurrent: -2), [new("Permeability", "H/m")] },
		{ new(Length: 3, Mass: 1, Time: -3, ElectricCurrent: -2), [new("Resistivity", "Ωm")] },
		{ new(Length: -3, Mass: -1, Time: 3, ElectricCurrent: 2), [new("Conductivity", "S/m")] },
		{ new(Length: -1, Mass: -1, Time: 2, ElectricCurrent: 2), [new("MagneticReluctivity", "m/H")] },
		{ new(Length: 1, Mass: 1, Time: -2, ElectricCurrent: -1), [new("MagneticVectorPotential", "Wb/m")] },
	};
}
