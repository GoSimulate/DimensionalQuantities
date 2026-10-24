using System.Collections.Immutable;
using DimensionalQuantities.Units;

namespace DimensionalQuantities.Roslyn.Generators;

internal sealed record UnitConversionSpec(string Name, string ReceiverType, ImmutableArray<UnitDefinition> Units, bool IncludeNamed);
