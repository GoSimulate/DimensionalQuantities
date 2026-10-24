using System.Collections.Immutable;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Roslyn.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DimensionalQuantities.Roslyn.Discovery;

internal static class QuantityDiscoveryCoordinator
{
	public static QuantityDiscoveryResult Discover(CSharpCompilation compilation, ImmutableArray<SyntaxTree> inputTrees,
		QuantityGenerationPlan defaults, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (inputTrees.IsEmpty)
		{
			return new(compilation, []);
		}

		ImmutableArray<EnglishExponentAlias> aliases = DiscoverySeedCollector.FindEnglishAliases(compilation, inputTrees, cancellationToken);
		CSharpCompilation seeded = aliases.IsEmpty ? compilation : Bind(compilation,
			[new("EnglishExponents.g.cs", EnglishExponentAliasEmitter.Emit(aliases))]);
		HashSet<Dimension> requested = [.. DiscoverySeedCollector.FindRequestedDimensions(seeded, inputTrees, cancellationToken)];
		HashSet<ArithmeticOperation> arithmetic = [];
		HashSet<PowerOperation> powers = [];
		HashSet<PowerMethodOperation> methods = [];
		int sites = inputTrees.Sum(tree => tree.GetRoot(cancellationToken).DescendantNodes()
			.Count(node => node is Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax or Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax));
		int maxRounds = Math.Max(4, sites + 2);
		for (int round = 0; round < maxRounds; round++)
		{
			cancellationToken.ThrowIfCancellationRequested();
			QuantityGenerationPlan plan = QuantityDiscovery.DiscoverWithPredefinedQuantities(defaults,
				arithmetic, powers, methods, requested);
			ImmutableArray<GeneratedSource> sources = DiscoverySourceComposer.Compose(plan, aliases);
			CSharpCompilation bound = Bind(compilation, sources);
			BoundOperationResult found = BoundOperationDiscovery.Discover(bound, inputTrees, cancellationToken);
			bool changed = false;
			foreach (ArithmeticOperation operation in found.ArithmeticOperations)
			{
				changed |= arithmetic.Add(operation);
			}

			foreach (PowerOperation operation in found.PowerOperations)
			{
				changed |= powers.Add(operation);
			}

			foreach (PowerMethodOperation operation in found.PowerMethodOperations)
			{
				changed |= methods.Add(operation);
			}

			foreach (Dimension dimension in DiscoverySeedCollector.FindRequestedDimensions(bound, inputTrees, cancellationToken))
			{
				changed |= requested.Add(dimension);
			}

			if (!changed)
			{
				return new(bound, sources);
			}
		}

		throw new InvalidOperationException($"Quantity discovery did not stabilize in {maxRounds} rounds.");
	}

	private static CSharpCompilation Bind(CSharpCompilation compilation, ImmutableArray<GeneratedSource> sources)
	{
		CSharpParseOptions options = compilation.SyntaxTrees.OfType<CSharpSyntaxTree>()
			.Select(tree => (CSharpParseOptions)tree.Options).FirstOrDefault() ?? CSharpParseOptions.Default;
		SyntaxTree[] trees = sources.Select(source => CSharpSyntaxTree.ParseText(source.Text, options, path: source.HintName)).ToArray();
		return compilation.AddSyntaxTrees(trees);
	}
}
