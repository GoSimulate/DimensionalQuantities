using System.Collections.Immutable;
using DimensionalQuantities.Roslyn.Discovery;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DimensionalQuantities.Roslyn.Generators;

internal static class AliasCollisionDiagnostics
{
	public static ImmutableArray<Diagnostic> Find(CSharpCompilation compilation, QuantityDiscoveryResult result,
		ImmutableArray<SyntaxTree> inputTrees, CancellationToken cancellationToken)
	{
		IEnumerable<string> names = result.Plan.KnownQuantities.Concat(result.Plan.AdditionalQuantities)
			.SelectMany(quantity => quantity.Aliases).Concat(result.EnglishAliases.Select(alias => alias.Name));
		HashSet<string> generatedNames = new(names, StringComparer.Ordinal);
		ImmutableArray<Diagnostic>.Builder diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
		CSharpCompilation prior = compilation.RemoveSyntaxTrees(compilation.SyntaxTrees.Except(inputTrees));
		foreach (SyntaxTree tree in inputTrees)
		{
			SyntaxNode root = tree.GetRoot(cancellationToken);
			HashSet<string> declaredAliases = FindExplicitAliases(root, generatedNames, diagnostics);
			FindGlobalTypes(root, generatedNames, diagnostics);
			FindDisplacedTypeUses(root, generatedNames, declaredAliases, prior.GetSemanticModel(tree),
				result.Compilation.GetSemanticModel(tree), diagnostics, cancellationToken);
		}

		return diagnostics.ToImmutable();
	}

	private static HashSet<string> FindExplicitAliases(SyntaxNode root, HashSet<string> generatedNames,
		ImmutableArray<Diagnostic>.Builder diagnostics)
	{
		HashSet<string> declaredAliases = [];
		foreach (UsingDirectiveSyntax directive in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
		{
			if (directive.Alias is { Name: IdentifierNameSyntax name }
				&& generatedNames.Contains(name.Identifier.ValueText))
			{
				declaredAliases.Add(name.Identifier.ValueText);
				Add(diagnostics, name.Identifier, name.GetLocation());
			}
		}

		return declaredAliases;
	}

	private static void FindGlobalTypes(SyntaxNode root, HashSet<string> generatedNames,
		ImmutableArray<Diagnostic>.Builder diagnostics)
	{
		foreach (BaseTypeDeclarationSyntax declaration in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
		{
			if (declaration.Parent is CompilationUnitSyntax
				&& declaration is not TypeDeclarationSyntax { TypeParameterList.Parameters.Count: > 0 }
				&& generatedNames.Contains(declaration.Identifier.ValueText))
			{
				Add(diagnostics, declaration.Identifier, declaration.Identifier.GetLocation());
			}
		}

		foreach (DelegateDeclarationSyntax declaration in root.DescendantNodes().OfType<DelegateDeclarationSyntax>())
		{
			if (declaration.Parent is CompilationUnitSyntax && declaration.TypeParameterList is null
				&& generatedNames.Contains(declaration.Identifier.ValueText))
			{
				Add(diagnostics, declaration.Identifier, declaration.Identifier.GetLocation());
			}
		}
	}

	private static void FindDisplacedTypeUses(SyntaxNode root, HashSet<string> generatedNames, HashSet<string> declaredAliases,
		SemanticModel prior, SemanticModel generated, ImmutableArray<Diagnostic>.Builder diagnostics, CancellationToken cancellationToken)
	{
		foreach (IdentifierNameSyntax name in root.DescendantNodes().OfType<IdentifierNameSyntax>())
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (!generatedNames.Contains(name.Identifier.ValueText) || declaredAliases.Contains(name.Identifier.ValueText)
				|| name.Parent is QualifiedNameSyntax or AliasQualifiedNameSyntax
				|| !IsTypeUse(name, prior, cancellationToken))
			{
				continue;
			}

			ITypeSymbol? before = prior.GetTypeInfo(name, cancellationToken).Type;
			ITypeSymbol? after = generated.GetTypeInfo(name, cancellationToken).Type;
			if (before is INamedTypeSymbol { Arity: 0 } and not IErrorTypeSymbol && after is not null
				&& !SymbolEqualityComparer.Default.Equals(before, after))
			{
				Add(diagnostics, name.Identifier, name.GetLocation());
			}
		}
	}

	private static bool IsTypeUse(IdentifierNameSyntax name, SemanticModel model, CancellationToken cancellationToken) =>
		SyntaxFacts.IsInTypeOnlyContext(name)
		|| name.Parent is MemberAccessExpressionSyntax access && ReferenceEquals(access.Expression, name)
			&& model.GetSymbolInfo(name, cancellationToken).Symbol is INamedTypeSymbol;

	private static void Add(ImmutableArray<Diagnostic>.Builder diagnostics, SyntaxToken identifier, Location location) =>
		diagnostics.Add(Diagnostic.Create(GenerationDiagnostics.AliasCollision, location, identifier.ValueText));
}
