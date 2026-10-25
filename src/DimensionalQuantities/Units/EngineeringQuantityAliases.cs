using DimensionalQuantities.Dimensions;

namespace DimensionalQuantities.Units;

internal static class EngineeringQuantityAliases
{
	public static readonly Dictionary<Dimension, string[]> Entries = new()
	{
		{ new(Length: 2), ["Area"] },
		{ new(Length: 3), ["Volume"] },
		{ new(Time: -1), ["Frequency", "AngularVelocity"] },
		{ new(Length: -3, Mass: 1), ["Density"] },
		{ new(Length: -1, Mass: 1, Time: -2), ["Pressure", "Stress", "YoungsModulus"] },
		{ new(Length: 2, Mass: 1, Time: -2), ["Energy", "Torque"] },
		{ new(Length: 2, Mass: 1, Time: -3), ["Power"] },
		{ new(Length: 2, Time: -3), ["SpecificPower", "SpecificPowerLoss"] },
		{ new(Temperature: -1), ["TemperatureCoefficient"] },
		{ new(Length: -1, Mass: 1, Time: -3), ["PowerDensity", "VolumetricPowerLoss"] },
		{ new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -1), ["Voltage"] },
		{ new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -2), ["Resistance"] },
		{ new(Length: 2, Mass: 1, Time: -2, ElectricCurrent: -2), ["Inductance"] },
		{ new(Length: 2, Mass: 1, Time: -2, ElectricCurrent: -1), ["MagneticFlux", "FluxLinkage"] },
		{ new(Mass: 1, Time: -2, ElectricCurrent: -1), ["MagneticFluxDensity", "MagneticPolarisation", "RemanentFluxDensity"] },
		{ new(Length: -1, ElectricCurrent: 1), ["MagneticFieldStrength", "CoerciveFieldStrength"] },
		{ new(Length: -2, ElectricCurrent: 1), ["CurrentDensity"] },
		{ new(Length: 1, Mass: 1, Time: -2, ElectricCurrent: -2), ["Permeability"] },
		{ new(Length: 3, Mass: 1, Time: -3, ElectricCurrent: -2), ["Resistivity"] },
		{ new(Length: -3, Mass: -1, Time: 3, ElectricCurrent: 2), ["Conductivity"] },
		{ new(Length: -1, Mass: -1, Time: 2, ElectricCurrent: 2), ["MagneticReluctivity"] },
		{ new(Length: 1, Mass: 1, Time: -2, ElectricCurrent: -1), ["MagneticVectorPotential"] }
	};
}
