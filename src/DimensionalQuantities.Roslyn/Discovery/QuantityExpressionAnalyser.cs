using System.Collections.Immutable;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Naming;
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
		private readonly QuantityTypeReader _typeReader;
		private readonly PowerDiscovery _powerDiscovery;
		private readonly PowerMethodDiscovery _powerMethodDiscovery;
		private readonly CancellationToken _cancellationToken;
		private readonly ImmutableArray<ArithmeticOperation>.Builder _operations = ImmutableArray.CreateBuilder<ArithmeticOperation>();
		private readonly ImmutableArray<PowerOperation>.Builder _powerOperations = ImmutableArray.CreateBuilder<PowerOperation>();
		private readonly ImmutableArray<PowerMethodOperation>.Builder _powerMethodOperations = ImmutableArray.CreateBuilder<PowerMethodOperation>();
		private readonly Dictionary<ILocalSymbol, Operand> _locals = new(SymbolEqualityComparer.Default);
		private readonly HashSet<ILocalSymbol> _inProgress = new(SymbolEqualityComparer.Default);

		public Analysis(SemanticModel semanticModel, CancellationToken cancellationToken)
		{
			_semanticModel = semanticModel;
			_typeReader = new(semanticModel.Compilation);
			_powerDiscovery = new(semanticModel);
			_powerMethodDiscovery = new(semanticModel);
			_cancellationToken = cancellationToken;
		}

		public QuantityExpressionAnalysis Analyse(ExpressionSyntax expression)
		{
			AnalyseExpression(expression);
			return new(_operations.ToImmutable(), _powerOperations.ToImmutable(), _powerMethodOperations.ToImmutable());
		}

		private Operand AnalyseExpression(ExpressionSyntax expression)
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
				&& _powerDiscovery.TryRead(invocation, out Rational exponent, out string exponentTypeName, _cancellationToken))
			{
				Operand operand = AnalyseExpression(invocation.ArgumentList.Arguments[0].Expression);
				if (operand.Kind != OperandKind.Quantity)
				{
					return default;
				}

				Dimension result = operand.Dimension.Pow(exponent);
				_powerOperations.Add(new(operand.Dimension, exponent, result, exponentTypeName));
				return result == default ? new(OperandKind.Scalar) : new(OperandKind.Quantity, result);
			}

			if (expression is InvocationExpressionSyntax methodInvocation
				&& _powerMethodDiscovery.TryRead(methodInvocation, out Rational methodExponent, out string methodName, _cancellationToken))
			{
				MemberAccessExpressionSyntax access = (MemberAccessExpressionSyntax)methodInvocation.Expression;
				Operand operand = AnalyseExpression(access.Expression);
				if (operand.Kind != OperandKind.Quantity)
				{
					return default;
				}

				Dimension result = operand.Dimension.Pow(methodExponent);
				_powerMethodOperations.Add(new(operand.Dimension, methodExponent, result, methodName));
				return result == default ? new(OperandKind.Scalar) : new(OperandKind.Quantity, result);
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

		private Operand AnalyseBranches(ExpressionSyntax expression, IEnumerable<ExpressionSyntax> arms)
		{
			Operand common = default;
			bool hasValue = false;
			bool compatible = true;
			foreach (ExpressionSyntax arm in arms)
			{
				if (arm is ThrowExpressionSyntax)
				{
					continue;
				}

				Operand value = AnalyseExpression(arm);
				if (!hasValue)
				{
					common = value;
					hasValue = true;
				}
				else if (value.Kind != common.Kind || value.Kind == OperandKind.Quantity && value.Dimension != common.Dimension)
				{
					compatible = false;
				}
			}

			Operand bound = ClassifyLeaf(expression);
			if (bound.Kind != OperandKind.Unsupported)
			{
				return bound;
			}

			return hasValue && compatible ? common : default;
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
				Operand result = AnalyseLocalInitializer(local);
				_locals.Add(local, result);
				return result;
			}
			finally
			{
				_inProgress.Remove(local);
			}
		}

		private Operand AnalyseLocalInitializer(ILocalSymbol local)
		{
			if (local.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(_cancellationToken) is not VariableDeclaratorSyntax variable
				|| variable.Parent is not VariableDeclarationSyntax declaration
				|| variable.Initializer is not { Value: ExpressionSyntax initializer })
			{
				return default;
			}

			if (declaration.Type.IsVar && _semanticModel.GetSymbolInfo(declaration.Type, _cancellationToken).Symbol is null)
			{
				return AnalyseExpression(initializer);
			}

			if (declaration.Type is not IdentifierNameSyntax typeName
				|| _semanticModel.GetTypeInfo(typeName, _cancellationToken).Type is not IErrorTypeSymbol
				|| _semanticModel.GetSymbolInfo(typeName, _cancellationToken) is not { Symbol: null, CandidateSymbols.IsEmpty: true })
			{
				return default;
			}

			int arithmeticCount = _operations.Count;
			int powerCount = _powerOperations.Count;
			int methodCount = _powerMethodOperations.Count;
			Operand inferred = AnalyseExpression(initializer);
			if (inferred.Kind == OperandKind.Quantity && typeName.Identifier.ValueText == DimensionNameGenerator.GetQuantityName(inferred.Dimension))
			{
				return inferred;
			}

			_operations.Count = arithmeticCount;
			_powerOperations.Count = powerCount;
			_powerMethodOperations.Count = methodCount;
			return default;
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
				if (result == default && kind == ArithmeticOperationKind.Divide)
				{
					return new(OperandKind.Scalar);
				}

				_operations.Add(new(kind, left.Dimension, right.Dimension, result));
				if (result == default)
				{
					return new(OperandKind.Scalar);
				}

				return new(OperandKind.Quantity, result);
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
