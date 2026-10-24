using System.Collections.Immutable;
using DimensionalQuantities.Units;

namespace DimensionalQuantities.Roslyn.Generators;

internal sealed record UnitConversionSpec(string ExtensionName, string ReceiverType, ImmutableArray<UnitDefinition> Units, bool IncludeNamed,
	bool GenerateSiPrefixes = true, bool GeneratePrefixConversions = true, string? ExtensionClassName = null);
