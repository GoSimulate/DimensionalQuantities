namespace DimensionalQuantities.DimensionAnalysis;

/// <summary>Recognised names and units for engineering dimensions.</summary>
internal static class EngineeringCatalogue
{
	public static readonly Dictionary<Dimension, UnitDefinition[]> Entries = new()
	{
		{ new(Length: 2), [new("Area", "SquareMetre", "SquareMetres", "m²")] },
		{ new(Length: 3), [new("Volume", "CubicMetre", "CubicMetres", "m³")] },
		{ new(Time: -1), [new("Frequency", "Hertz", "Hertz", "Hz"), new("AngularVelocity", "RadianPerSecond", "RadiansPerSecond", "rad/s")] },
		{ new(Length: -3, Mass: 1), [new("Density", "KilogramPerCubicMetre", "KilogramsPerCubicMetre", "kg/m³", "GramPerCubicMetre", "GramsPerCubicMetre", "g/m³", 3)] },
		{ new(Length: -1, Mass: 1, Time: -2),
			[
				new("Pressure", "Pascal", "Pascals", "Pa"),
				new("Stress", "Pascal", "Pascals", "Pa"),
				new("YoungsModulus", "Pascal", "Pascals", "Pa")
			] },
		{ new(Length: 2, Mass: 1, Time: -2), [new("Energy", "Joule", "Joules", "J"), new("Torque", "NewtonMetre", "NewtonMetres", "Nm")] },
		{ new(Length: 2, Mass: 1, Time: -3), [new("Power", "Watt", "Watts", "W")] },
		{ new(Length: 2, Time: -3),
			[
				new("SpecificPower", "WattPerKilogram", "WattsPerKilogram", "W/kg"),
				new("SpecificPowerLoss", "WattPerKilogram", "WattsPerKilogram", "W/kg")
			] },
		{ new(Temperature: -1), [new("TemperatureCoefficient", "InverseKelvin", "InverseKelvin", "K⁻¹")] },
		{ new(Length: -1, Mass: 1, Time: -3),
			[
				new("PowerDensity", "WattPerCubicMetre", "WattsPerCubicMetre", "W/m³"),
				new("VolumetricPowerLoss", "WattPerCubicMetre", "WattsPerCubicMetre", "W/m³")
			] },
		{ new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -1), [new("Voltage", "Volt", "Volts", "V")] },
		{ new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -2), [new("Resistance", "Ohm", "Ohms", "Ω")] },
		{ new(Length: 2, Mass: 1, Time: -2, ElectricCurrent: -2), [new("Inductance", "Henry", "Henries", "H")] },
		{ new(Length: 2, Mass: 1, Time: -2, ElectricCurrent: -1), [new("MagneticFlux", "Weber", "Webers", "Wb"), new("FluxLinkage", "Weber", "Webers", "Wb")] },
		{ new(Mass: 1, Time: -2, ElectricCurrent: -1),
			[
				new("MagneticFluxDensity", "Tesla", "Teslas", "T"),
				new("MagneticPolarisation", "Tesla", "Teslas", "T"),
				new("RemanentFluxDensity", "Tesla", "Teslas", "T")
			] },
		{ new(Length: -1, ElectricCurrent: 1),
			[
				new("MagneticFieldStrength", "AmpPerMetre", "AmpsPerMetre", "A/m"),
				new("CoerciveFieldStrength", "AmpPerMetre", "AmpsPerMetre", "A/m")
			] },
		{ new(Length: -2, ElectricCurrent: 1), [new("CurrentDensity", "AmpPerSquareMetre", "AmpsPerSquareMetre", "A/m²")] },
		{ new(Length: 1, Mass: 1, Time: -2, ElectricCurrent: -2), [new("Permeability", "HenryPerMetre", "HenriesPerMetre", "H/m")] },
		{ new(Length: 3, Mass: 1, Time: -3, ElectricCurrent: -2), [new("Resistivity", "OhmMetre", "OhmMetres", "Ωm")] },
		{ new(Length: -3, Mass: -1, Time: 3, ElectricCurrent: 2), [new("Conductivity", "SiemensPerMetre", "SiemensPerMetre", "S/m")] },
		{ new(Length: -1, Mass: -1, Time: 2, ElectricCurrent: 2), [new("MagneticReluctivity", "MetrePerHenry", "MetresPerHenry", "m/H")] },
		{ new(Length: 1, Mass: 1, Time: -2, ElectricCurrent: -1), [new("MagneticVectorPotential", "WeberPerMetre", "WebersPerMetre", "Wb/m")] },
	};
}
