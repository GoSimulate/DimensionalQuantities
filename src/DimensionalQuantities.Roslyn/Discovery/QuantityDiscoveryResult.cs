using System.Collections.Immutable;
using DimensionalQuantities.Roslyn.Generators;
using Microsoft.CodeAnalysis.CSharp;

namespace DimensionalQuantities.Roslyn.Discovery;

internal sealed record QuantityDiscoveryResult(CSharpCompilation Compilation, ImmutableArray<GeneratedSource> Sources,
	QuantityGenerationPlan Plan, ImmutableArray<EnglishExponentAlias> EnglishAliases);
