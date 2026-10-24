using System.Collections.Immutable;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Naming;
using DimensionalQuantities.Units;

namespace DimensionalQuantities.Roslyn.Discovery;

internal sealed record PlannedQuantity(Dimension Dimension, ImmutableArray<UnitDefinition> Units, bool IncludeNamedConversions)
{
	public string Name => Units[0].QuantityName;

	public ImmutableArray<string> Aliases =>
		[.. Units.Select(unit => unit.QuantityName).Append(DimensionNameGenerator.GetQuantityName(Dimension)).Distinct(StringComparer.Ordinal)];
}
