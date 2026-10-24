using System.Collections.Immutable;
using DimensionalQuantities.Roslyn.Discovery;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DimensionalQuantities.Roslyn.Generators;

internal static class GenerationDiagnostics
{
	public static readonly DiagnosticDescriptor AliasCollision = new(
		"DQ0006", "Generated alias conflicts with a type name", "Generated alias '{0}' conflicts with a name in this source",
		"DimensionalQuantities", DiagnosticSeverity.Warning, isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor InvalidEnglishExponent = new(
		"DQ0007", "Invalid English exponent", "English exponent '{0}' cannot be resolved",
		"DimensionalQuantities", DiagnosticSeverity.Error, isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor InvalidUnitProvider = new(
		"DQ0008", "Invalid unit provider declaration", "{0}",
		"DimensionalQuantities", DiagnosticSeverity.Error, isEnabledByDefault: true);

	public static ImmutableArray<Diagnostic> FindInvalidEnglishExponents(CSharpCompilation compilation,
		ImmutableArray<SyntaxTree> inputTrees, CancellationToken cancellationToken)
	{
		ImmutableArray<Diagnostic>.Builder diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
		foreach (SyntaxTree tree in inputTrees)
		{
			EnglishExponentDiscovery discovery = new(compilation.GetSemanticModel(tree));
			foreach (InvocationExpressionSyntax invocation in tree.GetRoot(cancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>())
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (discovery.TryGetUnresolvedRequest(invocation, out IdentifierNameSyntax? name, cancellationToken)
					&& !EnglishExponentDiscovery.TryParseExponent(name, out _))
				{
					diagnostics.Add(Diagnostic.Create(InvalidEnglishExponent, name.GetLocation(), name.Identifier.ValueText));
				}
			}
		}

		return diagnostics.ToImmutable();
	}
}
