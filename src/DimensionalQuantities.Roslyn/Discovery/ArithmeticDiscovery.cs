using DimensionalQuantities.Dimensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DimensionalQuantities.Roslyn.Discovery;

internal static class ArithmeticDiscovery
{
	public static bool TryRead(BinaryExpressionSyntax binary, SemanticModel semanticModel, QuantityTypeReader reader,
		out ArithmeticOperation operation, CancellationToken cancellationToken = default)
	{
		operation = default;
		bool multiply = binary.IsKind(SyntaxKind.MultiplyExpression);
		if (!multiply && !binary.IsKind(SyntaxKind.DivideExpression)
			|| semanticModel.GetSymbolInfo(binary, cancellationToken).Symbol is IMethodSymbol)
		{
			return false;
		}

		ITypeSymbol? leftType = semanticModel.GetTypeInfo(binary.Left, cancellationToken).Type;
		ITypeSymbol? rightType = semanticModel.GetTypeInfo(binary.Right, cancellationToken).Type;
		bool hasLeft = reader.TryRead(leftType, out Dimension left, cancellationToken) && left != default;
		bool hasRight = reader.TryRead(rightType, out Dimension right, cancellationToken) && right != default;
		if (hasLeft && hasRight)
		{
			if (!multiply && left == right)
			{
				return false;
			}

			operation = new(multiply ? ArithmeticOperationKind.Multiply : ArithmeticOperationKind.Divide,
				left, right, multiply ? left * right : left / right);
			return true;
		}

		if (!multiply && !hasLeft && hasRight && IsScalar(leftType, semanticModel.Compilation))
		{
			operation = new(ArithmeticOperationKind.ScalarDivide, default, right, right.Pow(-1));
			return true;
		}

		return false;
	}

	private static bool IsScalar(ITypeSymbol? type, Compilation compilation)
	{
		if (type is null or IErrorTypeSymbol)
		{
			return false;
		}

		Microsoft.CodeAnalysis.Operations.CommonConversion conversion = compilation.ClassifyCommonConversion(type, compilation.GetSpecialType(SpecialType.System_Double));
		return conversion.IsImplicit && (conversion.IsIdentity || conversion.IsNumeric) && !conversion.IsUserDefined;
	}
}
