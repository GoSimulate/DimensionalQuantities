using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Naming;
using DimensionalQuantities.Units;

namespace DimensionalQuantities.Roslyn.Analyzers;

internal static class DimensionNames
{
	public static string Get(Dimension dimension) =>
		EngineeringQuantityAliases.Entries.TryGetValue(dimension, out string[]? aliases)
			? aliases[0]
			: dimension == default ? "Dimensionless"
				: dimension == new Dimension(Temperature: 1) ? "AbsoluteTemperature" : DimensionNameGenerator.GetQuantityName(dimension);
}
