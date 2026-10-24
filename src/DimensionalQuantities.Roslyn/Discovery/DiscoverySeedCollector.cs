using System.Collections.Immutable;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Naming;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DimensionalQuantities.Roslyn.Discovery;

internal static class DiscoverySeedCollector
{
	public static ImmutableArray<EnglishExponentAlias> FindEnglishAliases(CSharpCompilation compilation,
		ImmutableArray<SyntaxTree> inputTrees, CancellationToken cancellationToken)
	{
		HashSet<EnglishExponentAlias> aliases = [];
		foreach (SyntaxTree tree in inputTrees)
		{
			SemanticModel model = compilation.GetSemanticModel(tree);
			EnglishExponentDiscovery discovery = new(model);
			foreach (InvocationExpressionSyntax invocation in tree.GetRoot(cancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>())
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (invocation.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Of" }
					&& discovery.TryRead(invocation, out EnglishExponentAlias alias, cancellationToken))
				{
					aliases.Add(alias);
				}
			}
		}

		return [.. aliases];
	}

	public static ImmutableArray<Dimension> FindRequestedDimensions(CSharpCompilation compilation,
		ImmutableArray<SyntaxTree> inputTrees, CancellationToken cancellationToken)
	{
		HashSet<Dimension> dimensions = [];
		foreach (SyntaxTree tree in inputTrees)
		{
			SemanticModel model = compilation.GetSemanticModel(tree);
			SyntaxNode root = tree.GetRoot(cancellationToken);
			dimensions.UnionWith(FindExplicitDimensions(root, model, cancellationToken));
			dimensions.UnionWith(FindMetadataDimensions(root, model, cancellationToken));
		}

		return [.. dimensions];
	}

	private static ImmutableArray<Dimension> FindExplicitDimensions(SyntaxNode root, SemanticModel model, CancellationToken cancellationToken)
	{
		ImmutableArray<Dimension>.Builder dimensions = ImmutableArray.CreateBuilder<Dimension>();
		foreach (IdentifierNameSyntax name in root.DescendantNodes().OfType<IdentifierNameSyntax>())
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (QuantityNameDiscovery.TryRead(name, model, out Dimension dimension, cancellationToken)
				|| TryReadUnresolvedFactory(name, model, out dimension, cancellationToken))
			{
				dimensions.Add(dimension);
			}
		}

		return dimensions.ToImmutable();
	}

	private static ImmutableArray<Dimension> FindMetadataDimensions(SyntaxNode root, SemanticModel model, CancellationToken cancellationToken)
	{
		ImmutableArray<Dimension>.Builder dimensions = ImmutableArray.CreateBuilder<Dimension>();
		MetadataQuantityDiscovery metadata = new(model);
		foreach (ExpressionSyntax expression in root.DescendantNodes().OfType<ExpressionSyntax>())
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (expression is InvocationExpressionSyntax or MemberAccessExpressionSyntax or ElementAccessExpressionSyntax or GenericNameSyntax
				|| expression is IdentifierNameSyntax name && !SyntaxFacts.IsInTypeOnlyContext(name))
			{
				dimensions.AddRange(metadata.Discover(expression, cancellationToken));
			}
		}

		return dimensions.ToImmutable();
	}

	private static bool TryReadUnresolvedFactory(IdentifierNameSyntax name, SemanticModel model,
		out Dimension dimension, CancellationToken cancellationToken)
	{
		dimension = default;
		if (name.Parent is not MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax receiver } member
			|| !ReferenceEquals(receiver, name)
			|| member.Name.Identifier.ValueText is not ("FromSiBaseUnit" or "FromPrefixedUnits"))
		{
			return false;
		}

		string typeName = name.Identifier.ValueText;
		SymbolInfo binding = model.GetSymbolInfo(name, cancellationToken);
		return binding.Symbol is null && binding.CandidateSymbols.IsEmpty
			&& model.GetAliasInfo(name, cancellationToken) is null
			&& !ExplicitAliasReservation.IsReserved(model.Compilation, typeName, cancellationToken)
			&& !model.Compilation.GlobalNamespace.GetMembers(typeName)
				.Any(candidate => candidate is INamespaceSymbol or INamedTypeSymbol { Arity: 0 })
			&& DimensionNameParser.TryParse(typeName, out dimension);
	}
}
