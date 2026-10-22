using System.Collections.Immutable;
using DimensionalQuantities.Dimensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

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
		QuantityTypeReader typeReader = new(semanticModel.Compilation);
		ImmutableArray<ArithmeticOperation>.Builder operations = ImmutableArray.CreateBuilder<ArithmeticOperation>();
		AnalyseExpression(expression, semanticModel, typeReader, operations, cancellationToken);
		return operations.ToImmutable();
	}

	private static Operand AnalyseExpression(ExpressionSyntax expression, SemanticModel semanticModel, QuantityTypeReader typeReader,
		ImmutableArray<ArithmeticOperation>.Builder operations, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (expression is ParenthesizedExpressionSyntax parenthesized)
		{
			return AnalyseExpression(parenthesized.Expression, semanticModel, typeReader, operations, cancellationToken);
		}

		if (expression is BinaryExpressionSyntax binary && IsSupported(binary))
		{
			Operand left = AnalyseExpression(binary.Left, semanticModel, typeReader, operations, cancellationToken);
			Operand right = AnalyseExpression(binary.Right, semanticModel, typeReader, operations, cancellationToken);
			return Combine(binary, left, right, operations);
		}

		return ClassifyLeaf(expression, semanticModel, typeReader, cancellationToken);
	}

	private static Operand Combine(BinaryExpressionSyntax binary, Operand left, Operand right, ImmutableArray<ArithmeticOperation>.Builder operations)
	{
		if (left.Kind == OperandKind.Scalar && right.Kind == OperandKind.Scalar)
		{
			return new(OperandKind.Scalar);
		}

		if (left.Kind == OperandKind.Quantity && right.Kind == OperandKind.Scalar)
		{
			return left;
		}

		if (left.Kind == OperandKind.Scalar && right.Kind == OperandKind.Quantity)
		{
			if (binary.IsKind(SyntaxKind.MultiplyExpression))
			{
				return right;
			}

			Dimension inverse = right.Dimension.Pow(-1);
			operations.Add(new(ArithmeticOperationKind.ScalarDivide, default, right.Dimension, inverse));
			return new(OperandKind.Quantity, inverse);
		}

		if (left.Kind == OperandKind.Quantity && right.Kind == OperandKind.Quantity)
		{
			ArithmeticOperationKind kind = binary.IsKind(SyntaxKind.MultiplyExpression) ? ArithmeticOperationKind.Multiply : ArithmeticOperationKind.Divide;
			Dimension result = kind == ArithmeticOperationKind.Multiply ? left.Dimension * right.Dimension : left.Dimension / right.Dimension;
			if (result != default)
			{
				operations.Add(new(kind, left.Dimension, right.Dimension, result));
				return new(OperandKind.Quantity, result);
			}
		}

		return default;
	}

	private static Operand ClassifyLeaf(ExpressionSyntax expression, SemanticModel semanticModel, QuantityTypeReader typeReader, CancellationToken cancellationToken)
	{
		ITypeSymbol? type = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
		if (typeReader.TryRead(type, out Dimension dimension, cancellationToken))
		{
			return dimension == default ? default : new(OperandKind.Quantity, dimension);
		}

		ITypeSymbol doubleType = semanticModel.Compilation.GetSpecialType(SpecialType.System_Double);
		if (type is not null)
		{
			CommonConversion conversion = semanticModel.Compilation.ClassifyCommonConversion(type, doubleType);
			if (conversion.IsImplicit && (conversion.IsIdentity || conversion.IsNumeric) && !conversion.IsUserDefined)
			{
				return new(OperandKind.Scalar);
			}
		}

		return default;
	}

	private static bool IsSupported(BinaryExpressionSyntax binary) => binary.IsKind(SyntaxKind.MultiplyExpression) || binary.IsKind(SyntaxKind.DivideExpression);

	private enum OperandKind
	{
		Unsupported,
		Scalar,
		Quantity
	}

	private readonly record struct Operand(OperandKind Kind, Dimension Dimension = default);
}
