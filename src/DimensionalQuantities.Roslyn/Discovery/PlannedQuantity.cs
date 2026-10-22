using System.Collections.Immutable;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Units;

namespace DimensionalQuantities.Roslyn.Discovery;

internal sealed record PlannedQuantity(Dimension Dimension, ImmutableArray<UnitDefinition> Units, bool IncludeNamedConversions)
{
	public string Name => Units[0].QuantityName;
}
