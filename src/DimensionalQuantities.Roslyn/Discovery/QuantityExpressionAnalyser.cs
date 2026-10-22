using System.Collections.Immutable;
using DimensionalQuantities.Dimensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace DimensionalQuantities.Roslyn.Discovery;

internal sealed class QuantityExpressionAnalyser
{
	private readonly SemanticModel _semanticModel;

	public QuantityExpressionAnalyser(SemanticModel semanticModel)
	{
		_semanticModel = semanticModel;
	}

	public ImmutableArray<ArithmeticOperation> Analyse(ExpressionSyntax expression, CancellationToken cancellationToken)
	{
		Analysis analysis = new(_semanticModel, cancellationToken);
		return analysis.Analyse(expression);
	}

	private sealed class Analysis
	{
		private readonly SemanticModel _semanticModel;
		private readonly QuantityTypeReader _typeReader;
		private readonly CancellationToken _cancellationToken;
		private readonly ImmutableArray<ArithmeticOperation>.Builder _operations = ImmutableArray.CreateBuilder<ArithmeticOperation>();
		private readonly Dictionary<ILocalSymbol, Operand> _locals = new(SymbolEqualityComparer.Default);
		private readonly HashSet<ILocalSymbol> _inProgress = new(SymbolEqualityComparer.Default);

		public Analysis(SemanticModel semanticModel, CancellationToken cancellationToken)
		{
			_semanticModel = semanticModel;
			_typeReader = new(semanticModel.Compilation);
			_cancellationToken = cancellationToken;
		}

		public ImmutableArray<ArithmeticOperation> Analyse(ExpressionSyntax expression)
		{
			AnalyseExpression(expression);
			return _operations.ToImmutable();
		}

		private Operand AnalyseExpression(ExpressionSyntax expression)
		{
			_cancellationToken.ThrowIfCancellationRequested();
			if (expression is ParenthesizedExpressionSyntax parenthesized)
			{
				return AnalyseExpression(parenthesized.Expression);
			}

			if (expression is BinaryExpressionSyntax binary && IsSupported(binary))
			{
				Operand left = AnalyseExpression(binary.Left);
				Operand right = AnalyseExpression(binary.Right);
				if (binary.IsKind(SyntaxKind.AddExpression) || binary.IsKind(SyntaxKind.SubtractExpression))
				{
					return AnalyseSum(binary, left, right);
				}

				return CombineProduct(binary, left, right);
			}

			if (expression is PrefixUnaryExpressionSyntax unary && unary.IsKind(SyntaxKind.UnaryMinusExpression))
			{
				Operand operand = AnalyseExpression(unary.Operand);
				Operand bound = ClassifyLeaf(unary);
				return bound.Kind != OperandKind.Unsupported ? bound : operand;
			}

			return ClassifyLeaf(expression);
		}

		private Operand ClassifyLeaf(ExpressionSyntax expression)
		{
			ITypeSymbol? type = _semanticModel.GetTypeInfo(expression, _cancellationToken).Type;
			if (_typeReader.TryRead(type, out Dimension dimension, _cancellationToken))
			{
				return dimension == default ? default : new(OperandKind.Quantity, dimension);
			}

			ITypeSymbol doubleType = _semanticModel.Compilation.GetSpecialType(SpecialType.System_Double);
			if (type is not null)
			{
				CommonConversion conversion = _semanticModel.Compilation.ClassifyCommonConversion(type, doubleType);
				if (conversion.IsImplicit && (conversion.IsIdentity || conversion.IsNumeric) && !conversion.IsUserDefined)
				{
					return new(OperandKind.Scalar);
				}
			}

			return expression is IdentifierNameSyntax identifier
				&& _semanticModel.GetSymbolInfo(identifier, _cancellationToken).Symbol is ILocalSymbol local
				? ResolveLocal(local) : default;
		}

		private Operand ResolveLocal(ILocalSymbol local)
		{
			if (_locals.TryGetValue(local, out Operand cached))
			{
				return cached;
			}

			if (!_inProgress.Add(local))
			{
				return default;
			}

			try
			{
				Operand result = default;
				if (local.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(_cancellationToken) is VariableDeclaratorSyntax variable
					&& variable.Parent is VariableDeclarationSyntax declaration && declaration.Type.IsVar
					&& _semanticModel.GetSymbolInfo(declaration.Type, _cancellationToken).Symbol is null
					&& variable.Initializer is { Value: ExpressionSyntax initializer })
				{
					result = AnalyseExpression(initializer);
				}

				_locals.Add(local, result);
				return result;
			}
			finally
			{
				_inProgress.Remove(local);
			}
		}

		private Operand AnalyseSum(BinaryExpressionSyntax binary, Operand left, Operand right)
		{
			Operand bound = ClassifyLeaf(binary);
			if (bound.Kind != OperandKind.Unsupported)
			{
				return bound;
			}

			if (left.Kind == OperandKind.Scalar && right.Kind == OperandKind.Scalar)
			{
				return new(OperandKind.Scalar);
			}

			return left.Kind == OperandKind.Quantity && right.Kind == OperandKind.Quantity && left.Dimension == right.Dimension
				? left : default;
		}

		private Operand CombineProduct(BinaryExpressionSyntax binary, Operand left, Operand right)
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
				_operations.Add(new(ArithmeticOperationKind.ScalarDivide, default, right.Dimension, inverse));
				return new(OperandKind.Quantity, inverse);
			}

			if (left.Kind == OperandKind.Quantity && right.Kind == OperandKind.Quantity)
			{
				ArithmeticOperationKind kind = binary.IsKind(SyntaxKind.MultiplyExpression) ? ArithmeticOperationKind.Multiply : ArithmeticOperationKind.Divide;
				Dimension result = kind == ArithmeticOperationKind.Multiply ? left.Dimension * right.Dimension : left.Dimension / right.Dimension;
				if (result != default)
				{
					_operations.Add(new(kind, left.Dimension, right.Dimension, result));
					return new(OperandKind.Quantity, result);
				}
			}

			return default;
		}

		private static bool IsSupported(BinaryExpressionSyntax binary) => binary.IsKind(SyntaxKind.MultiplyExpression) || binary.IsKind(SyntaxKind.DivideExpression)
			|| binary.IsKind(SyntaxKind.AddExpression) || binary.IsKind(SyntaxKind.SubtractExpression);

		private enum OperandKind
		{
			Unsupported,
			Scalar,
			Quantity
		}

		private readonly record struct Operand(OperandKind Kind, Dimension Dimension = default);
	}
}
