using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Roslyn.Discovery;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DimensionalQuantities.Roslyn.Analyzers;

internal static class BinaryDimensionAnalysis
{
	public static void AnalyzeCompoundAssignment(SyntaxNodeAnalysisContext context, QuantityTypeReader reader)
	{
		AssignmentExpressionSyntax assignment = (AssignmentExpressionSyntax)context.Node;
		if (context.SemanticModel.GetSymbolInfo(assignment, context.CancellationToken).Symbol is IMethodSymbol)
		{
			return;
		}

		ITypeSymbol? leftType = context.SemanticModel.GetTypeInfo(assignment.Left, context.CancellationToken).Type;
		ITypeSymbol? rightType = context.SemanticModel.GetTypeInfo(assignment.Right, context.CancellationToken).Type;
		if (reader.TryReadIncludingNullable(leftType, out Dimension left, context.CancellationToken)
			&& reader.TryReadIncludingNullable(rightType, out Dimension right, context.CancellationToken)
			&& left != right)
		{
			context.ReportDiagnostic(Diagnostic.Create(DimensionMismatchAnalyzer.IncompatibleOperands, assignment.GetLocation(),
				assignment.OperatorToken.Text, DimensionNames.Get(left), DimensionNames.Get(right)));
		}
	}

	public static void Analyze(SyntaxNodeAnalysisContext context, QuantityTypeReader reader)
	{
		BinaryExpressionSyntax binary = (BinaryExpressionSyntax)context.Node;
		if (context.SemanticModel.GetSymbolInfo(binary, context.CancellationToken).Symbol is IMethodSymbol)
		{
			return;
		}

		ITypeSymbol? leftType = context.SemanticModel.GetTypeInfo(binary.Left, context.CancellationToken).Type;
		ITypeSymbol? rightType = context.SemanticModel.GetTypeInfo(binary.Right, context.CancellationToken).Type;
		if (reader.TryReadIncludingNullable(leftType, out Dimension left, context.CancellationToken)
			&& reader.TryReadIncludingNullable(rightType, out Dimension right, context.CancellationToken)
			&& left != right)
		{
			context.ReportDiagnostic(Diagnostic.Create(DimensionMismatchAnalyzer.IncompatibleOperands, binary.GetLocation(),
				binary.OperatorToken.Text, DimensionNames.Get(left), DimensionNames.Get(right)));
		}
	}
}
