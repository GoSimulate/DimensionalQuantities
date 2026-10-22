using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;

namespace DimensionalQuantities.Roslyn.Discovery;

internal static class ArithmeticDiscovery
{
	public static bool IsRoot(BinaryExpressionSyntax binary)
	{
		if (!IsSupported(binary))
		{
			return false;
		}

		SyntaxNode node = binary;
		while (node.Parent is ParenthesizedExpressionSyntax parenthesized)
		{
			node = parenthesized;
		}

		return node.Parent is not BinaryExpressionSyntax parent || !IsSupported(parent);
	}

	public static ImmutableArray<ArithmeticOperation> Analyse(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
	{
		QuantityExpressionAnalyser analyser = new(semanticModel);
		return analyser.Analyse(expression, cancellationToken);
	}

	private static bool IsSupported(BinaryExpressionSyntax binary) => binary.IsKind(SyntaxKind.MultiplyExpression) || binary.IsKind(SyntaxKind.DivideExpression);
}
