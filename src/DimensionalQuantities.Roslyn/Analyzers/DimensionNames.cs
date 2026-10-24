using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Naming;
using DimensionalQuantities.Units;

namespace DimensionalQuantities.Roslyn.Analyzers;

internal static class DimensionNames
{
	public static string Get(Dimension dimension) =>
		EngineeringCatalogue.Entries.TryGetValue(dimension, out UnitDefinition[]? units)
			? units[0].QuantityName
			: dimension == default ? "Dimensionless" : DimensionNameGenerator.GetQuantityName(dimension);
}
