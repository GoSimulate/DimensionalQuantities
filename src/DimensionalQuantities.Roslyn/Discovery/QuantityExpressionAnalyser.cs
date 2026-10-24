using System.Collections.Immutable;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Numerics;
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

	public QuantityExpressionAnalysis Analyse(ExpressionSyntax expression, CancellationToken cancellationToken)
	{
		Analysis analysis = new(_semanticModel, cancellationToken);
		return analysis.Analyse(expression);
	}

	private sealed class Analysis
	{
		private readonly SemanticModel _semanticModel;
		private readonly Dictionary<SyntaxTree, SemanticModel> _semanticModels = [];
		private readonly QuantityTypeReader _typeReader;
		private readonly QuantityDeclarationResolver _declarations;
		private readonly CancellationToken _cancellationToken;
		private readonly ImmutableArray<ArithmeticOperation>.Builder _operations = ImmutableArray.CreateBuilder<ArithmeticOperation>();
		private readonly ImmutableArray<PowerOperation>.Builder _powerOperations = ImmutableArray.CreateBuilder<PowerOperation>();
		private readonly ImmutableArray<PowerMethodOperation>.Builder _powerMethodOperations = ImmutableArray.CreateBuilder<PowerMethodOperation>();

		public Analysis(SemanticModel semanticModel, CancellationToken cancellationToken)
		{
			_semanticModel = semanticModel;
			_semanticModels.Add(semanticModel.SyntaxTree, semanticModel);
			_typeReader = new(semanticModel.Compilation);
			_declarations = new(semanticModel.Compilation, AnalyseExpression, AnalyseNamedInitializer, cancellationToken);
			_cancellationToken = cancellationToken;
		}

		public QuantityExpressionAnalysis Analyse(ExpressionSyntax expression)
		{
			AnalyseExpression(expression);
			return new(_operations.ToImmutable(), _powerOperations.ToImmutable(), _powerMethodOperations.ToImmutable());
		}

		private QuantityOperand AnalyseExpression(ExpressionSyntax expression)
		{
			_cancellationToken.ThrowIfCancellationRequested();
			if (expression is ParenthesizedExpressionSyntax parenthesized)
			{
				return AnalyseExpression(parenthesized.Expression);
			}

			if (expression is ConditionalExpressionSyntax conditional)
			{
				return AnalyseBranches(conditional, [conditional.WhenTrue, conditional.WhenFalse]);
			}

			if (expression is SwitchExpressionSyntax switchExpression)
			{
				return AnalyseBranches(switchExpression, switchExpression.Arms.Select(arm => arm.Expression));
			}

			if (expression is InvocationExpressionSyntax invocation
				&& new PowerDiscovery(GetSemanticModel(invocation)).TryRead(invocation, out Rational exponent, out string exponentTypeName, _cancellationToken))
			{
				QuantityOperand operand = AnalyseExpression(invocation.ArgumentList.Arguments[0].Expression);
				if (operand.Kind != QuantityOperandKind.Quantity)
				{
					return default;
				}

				Dimension result = operand.Dimension.Pow(exponent);
				_powerOperations.Add(new(operand.Dimension, exponent, result, exponentTypeName));
				return result == default ? new(QuantityOperandKind.Scalar) : new(QuantityOperandKind.Quantity, result);
			}

			if (expression is InvocationExpressionSyntax methodInvocation
				&& new PowerMethodDiscovery(GetSemanticModel(methodInvocation)).TryRead(methodInvocation, out Rational methodExponent, out string methodName, _cancellationToken))
			{
				MemberAccessExpressionSyntax access = (MemberAccessExpressionSyntax)methodInvocation.Expression;
				QuantityOperand operand = AnalyseExpression(access.Expression);
				if (operand.Kind != QuantityOperandKind.Quantity)
				{
					return default;
				}

				Dimension result = operand.Dimension.Pow(methodExponent);
				_powerMethodOperations.Add(new(operand.Dimension, methodExponent, result, methodName));
				return result == default ? new(QuantityOperandKind.Scalar) : new(QuantityOperandKind.Quantity, result);
			}

			if (expression is BinaryExpressionSyntax binary && IsSupported(binary))
			{
				QuantityOperand left = AnalyseExpression(binary.Left);
				QuantityOperand right = AnalyseExpression(binary.Right);
				if (binary.IsKind(SyntaxKind.AddExpression) || binary.IsKind(SyntaxKind.SubtractExpression))
				{
					return AnalyseSum(binary, left, right);
				}

				return CombineProduct(binary, left, right);
			}

			if (expression is PrefixUnaryExpressionSyntax unary && unary.IsKind(SyntaxKind.UnaryMinusExpression))
			{
				QuantityOperand operand = AnalyseExpression(unary.Operand);
				QuantityOperand bound = ClassifyLeaf(unary);
				return bound.Kind != QuantityOperandKind.Unsupported ? bound : operand;
			}

			return ClassifyLeaf(expression);
		}

		private QuantityOperand AnalyseBranches(ExpressionSyntax expression, IEnumerable<ExpressionSyntax> arms)
		{
			QuantityOperand common = default;
			bool hasValue = false;
			bool compatible = true;
			foreach (ExpressionSyntax arm in arms)
			{
				if (arm is ThrowExpressionSyntax)
				{
					continue;
				}

				QuantityOperand value = AnalyseExpression(arm);
				if (!hasValue)
				{
					common = value;
					hasValue = true;
				}
				else if (value.Kind != common.Kind || value.Kind == QuantityOperandKind.Quantity && value.Dimension != common.Dimension)
				{
					compatible = false;
				}
			}

			QuantityOperand bound = ClassifyLeaf(expression);
			if (bound.Kind != QuantityOperandKind.Unsupported)
			{
				return bound;
			}

			return hasValue && compatible ? common : default;
		}

		private QuantityOperand ClassifyLeaf(ExpressionSyntax expression)
		{
			SemanticModel model = GetSemanticModel(expression);
			ITypeSymbol? type = model.GetTypeInfo(expression, _cancellationToken).Type;
			if (_typeReader.TryRead(type, out Dimension dimension, _cancellationToken))
			{
				return dimension == default ? default : new(QuantityOperandKind.Quantity, dimension);
			}

			ITypeSymbol doubleType = _semanticModel.Compilation.GetSpecialType(SpecialType.System_Double);
			if (type is not null)
			{
				CommonConversion conversion = _semanticModel.Compilation.ClassifyCommonConversion(type, doubleType);
				if (conversion.IsImplicit && (conversion.IsIdentity || conversion.IsNumeric) && !conversion.IsUserDefined)
				{
					return new(QuantityOperandKind.Scalar);
				}
			}

			if (expression is ElementAccessExpressionSyntax elementAccess
				&& TryReadUnresolvedElement(elementAccess, model, type, out Dimension elementDimension))
			{
				return new(QuantityOperandKind.Quantity, elementDimension);
			}

			return expression is IdentifierNameSyntax or MemberAccessExpressionSyntax or InvocationExpressionSyntax
				&& model.GetSymbolInfo(expression, _cancellationToken).Symbol is ISymbol symbol
				? _declarations.Resolve(symbol) : default;
		}

		private bool TryReadUnresolvedElement(ElementAccessExpressionSyntax access, SemanticModel model, ITypeSymbol? type, out Dimension dimension)
		{
			dimension = default;
			if (type is not IErrorTypeSymbol error)
			{
				return false;
			}

			ITypeSymbol? receiverType = model.GetTypeInfo(access.Expression, _cancellationToken).Type;
			IPropertySymbol? indexer = model.GetSymbolInfo(access, _cancellationToken).Symbol as IPropertySymbol;
			ITypeSymbol? elementType = receiverType is IArrayTypeSymbol array
				? array.ElementType
				: indexer is { IsIndexer: true }
					? indexer.Type : null;
			return elementType is IErrorTypeSymbol elementError && SymbolEqualityComparer.Default.Equals(elementError, error)
				&& (_declarations.TryReadCollectionElementType(indexer, elementError, out dimension)
					|| _declarations.TryReadCollectionElementType(model.GetSymbolInfo(access.Expression, _cancellationToken).Symbol,
						elementError, out dimension));
		}

		private SemanticModel GetSemanticModel(SyntaxNode node)
		{
			if (!_semanticModels.TryGetValue(node.SyntaxTree, out SemanticModel? model))
			{
				model = _semanticModel.Compilation.GetSemanticModel(node.SyntaxTree);
				_semanticModels.Add(node.SyntaxTree, model);
			}

			return model;
		}

		private QuantityOperand AnalyseNamedInitializer(IReadOnlyList<ExpressionSyntax?> values, string expectedName)
		{
			int arithmeticCount = _operations.Count;
			int powerCount = _powerOperations.Count;
			int methodCount = _powerMethodOperations.Count;
			QuantityOperand common = default;
			bool hasValue = false;
			bool compatible = true;
			foreach (ExpressionSyntax? expression in values)
			{
				if (expression is ThrowExpressionSyntax)
				{
					continue;
				}

				QuantityOperand inferred = expression is null ? default : AnalyseExpression(expression);
				if (!hasValue)
				{
					common = inferred;
					hasValue = true;
				}
				else if (inferred.Kind != common.Kind || inferred.Kind == QuantityOperandKind.Quantity && inferred.Dimension != common.Dimension)
				{
					compatible = false;
				}
			}

			if (hasValue && compatible && QuantityDeclarationResolver.MatchesExpectedName(expectedName, common))
			{
				return common;
			}

			_operations.Count = arithmeticCount;
			_powerOperations.Count = powerCount;
			_powerMethodOperations.Count = methodCount;
			return default;
		}

		private QuantityOperand AnalyseSum(BinaryExpressionSyntax binary, QuantityOperand left, QuantityOperand right)
		{
			QuantityOperand bound = ClassifyLeaf(binary);
			if (bound.Kind != QuantityOperandKind.Unsupported)
			{
				return bound;
			}

			if (left.Kind == QuantityOperandKind.Scalar && right.Kind == QuantityOperandKind.Scalar)
			{
				return new(QuantityOperandKind.Scalar);
			}

			return left.Kind == QuantityOperandKind.Quantity && right.Kind == QuantityOperandKind.Quantity && left.Dimension == right.Dimension
				? left : default;
		}

		private QuantityOperand CombineProduct(BinaryExpressionSyntax binary, QuantityOperand left, QuantityOperand right)
		{
			if (left.Kind == QuantityOperandKind.Scalar && right.Kind == QuantityOperandKind.Scalar)
			{
				return new(QuantityOperandKind.Scalar);
			}

			if (left.Kind == QuantityOperandKind.Quantity && right.Kind == QuantityOperandKind.Scalar)
			{
				return left;
			}

			if (left.Kind == QuantityOperandKind.Scalar && right.Kind == QuantityOperandKind.Quantity)
			{
				if (binary.IsKind(SyntaxKind.MultiplyExpression))
				{
					return right;
				}

				Dimension inverse = right.Dimension.Pow(-1);
				_operations.Add(new(ArithmeticOperationKind.ScalarDivide, default, right.Dimension, inverse));
				return new(QuantityOperandKind.Quantity, inverse);
			}

			if (left.Kind == QuantityOperandKind.Quantity && right.Kind == QuantityOperandKind.Quantity)
			{
				ArithmeticOperationKind kind = binary.IsKind(SyntaxKind.MultiplyExpression) ? ArithmeticOperationKind.Multiply : ArithmeticOperationKind.Divide;
				Dimension result = kind == ArithmeticOperationKind.Multiply ? left.Dimension * right.Dimension : left.Dimension / right.Dimension;
				if (result == default && kind == ArithmeticOperationKind.Divide)
				{
					return new(QuantityOperandKind.Scalar);
				}

				_operations.Add(new(kind, left.Dimension, right.Dimension, result));
				if (result == default)
				{
					return new(QuantityOperandKind.Scalar);
				}

				return new(QuantityOperandKind.Quantity, result);
			}

			return default;
		}

		private static bool IsSupported(BinaryExpressionSyntax binary) => binary.IsKind(SyntaxKind.MultiplyExpression) || binary.IsKind(SyntaxKind.DivideExpression)
			|| binary.IsKind(SyntaxKind.AddExpression) || binary.IsKind(SyntaxKind.SubtractExpression);

	}
}
