using System.Collections.Immutable;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Naming;
using DimensionalQuantities.Units;

namespace DimensionalQuantities.Roslyn.Discovery;

internal sealed record PlannedQuantity(Dimension Dimension, ImmutableArray<string> Names, ImmutableArray<UnitDefinition> Units,
	bool IncludeNamedConversions)
{
	public PlannedQuantity(Dimension dimension, ImmutableArray<UnitDefinition> units, bool includeNamedConversions)
		: this(dimension, [.. units.Select(unit => unit.QuantityName)], units, includeNamedConversions) { }

	public string Name => Names[0];

	public ImmutableArray<string> Aliases =>
		[.. Names.Append(DimensionNameGenerator.GetQuantityName(Dimension)).Distinct(StringComparer.Ordinal)];
}
