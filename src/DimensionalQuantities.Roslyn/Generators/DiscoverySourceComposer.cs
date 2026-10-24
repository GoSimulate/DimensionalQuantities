using System.Collections.Immutable;
using DimensionalQuantities.Roslyn.Discovery;

namespace DimensionalQuantities.Roslyn.Generators;

internal static class DiscoverySourceComposer
{
	public static ImmutableArray<GeneratedSource> Compose(QuantityGenerationPlan plan,
		ImmutableArray<EnglishExponentAlias> aliases)
	{
		ImmutableArray<GeneratedSource>.Builder sources = ImmutableArray.CreateBuilder<GeneratedSource>();
		if (!aliases.IsEmpty)
		{
			sources.Add(new("EnglishExponents.g.cs", EnglishExponentAliasEmitter.Emit(aliases)));
		}

		if (!plan.AdditionalQuantities.IsEmpty)
		{
			sources.Add(new("DiscoveredQuantities.g.cs", QuantitySourceEmitter.Emit(plan.AdditionalQuantities)));
		}

		if (!plan.Operations.IsEmpty)
		{
			sources.Add(new("Arithmetic.g.cs", ArithmeticSourceEmitter.Emit(plan)));
		}

		if (!plan.PowerOperations.IsEmpty)
		{
			sources.Add(new("Powers.g.cs", PowerSourceEmitter.Emit(plan)));
		}

		if (!plan.PowerMethodOperations.IsEmpty)
		{
			sources.Add(new("PowerMethods.g.cs", PowerMethodSourceEmitter.Emit(plan)));
		}

		return sources.ToImmutable();
	}
}
