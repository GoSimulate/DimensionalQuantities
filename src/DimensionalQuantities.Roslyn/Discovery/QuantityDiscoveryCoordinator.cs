using System.Collections.Immutable;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Roslyn.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
		int maxRounds = GetRoundLimit(inputTrees, cancellationToken);
		for (int round = 0; round < maxRounds; round++)
		{
			cancellationToken.ThrowIfCancellationRequested();
			QuantityGenerationPlan plan = QuantityDiscovery.DiscoverWithPredefinedQuantities(defaults,
				arithmetic, powers, methods, requested);
			ImmutableArray<GeneratedSource> sources = DiscoverySourceComposer.Compose(plan, aliases);
			CSharpCompilation bound = Bind(compilation, sources);
			BoundOperationResult found = BoundOperationDiscovery.Discover(bound, inputTrees, cancellationToken);
			bool changed = AddNew(arithmetic, found.ArithmeticOperations);
			changed |= AddNew(powers, found.PowerOperations);
			changed |= AddNew(methods, found.PowerMethodOperations);
			changed |= AddNew(requested, DiscoverySeedCollector.FindRequestedDimensions(bound, inputTrees, cancellationToken));

			if (!changed)
			{
				return new(bound, sources);
			}
		}

		throw new InvalidOperationException($"Quantity discovery did not stabilize in {maxRounds} rounds.");
	}

	private static int GetRoundLimit(ImmutableArray<SyntaxTree> inputTrees, CancellationToken cancellationToken)
	{
		int operationCount = inputTrees.Sum(tree => tree.GetRoot(cancellationToken).DescendantNodes()
			.Count(node => node is BinaryExpressionSyntax or InvocationExpressionSyntax));
		return Math.Max(4, operationCount + 2);
	}

	private static bool AddNew<T>(HashSet<T> existing, IEnumerable<T> discovered)
	{
		bool changed = false;
		foreach (T item in discovered)
		{
			changed |= existing.Add(item);
		}

		return changed;
	}

	private static CSharpCompilation Bind(CSharpCompilation compilation, ImmutableArray<GeneratedSource> sources)
	{
		CSharpParseOptions options = compilation.SyntaxTrees.OfType<CSharpSyntaxTree>()
			.Select(tree => (CSharpParseOptions)tree.Options).FirstOrDefault() ?? CSharpParseOptions.Default;
		SyntaxTree[] trees = sources.Select(source => CSharpSyntaxTree.ParseText(source.Text, options, path: source.HintName)).ToArray();
		return compilation.AddSyntaxTrees(trees);
	}
}
