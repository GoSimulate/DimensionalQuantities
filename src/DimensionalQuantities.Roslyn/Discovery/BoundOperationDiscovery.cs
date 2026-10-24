using System.Collections.Immutable;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Numerics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DimensionalQuantities.Roslyn.Discovery;

internal static class BoundOperationDiscovery
{
	public static BoundOperationResult Discover(CSharpCompilation compilation, ImmutableArray<SyntaxTree> inputTrees,
		CancellationToken cancellationToken)
	{
		ImmutableArray<ArithmeticOperation>.Builder arithmetic = ImmutableArray.CreateBuilder<ArithmeticOperation>();
		ImmutableArray<PowerOperation>.Builder powers = ImmutableArray.CreateBuilder<PowerOperation>();
		ImmutableArray<PowerMethodOperation>.Builder methods = ImmutableArray.CreateBuilder<PowerMethodOperation>();
		QuantityTypeReader reader = new(compilation);
		foreach (SyntaxTree tree in inputTrees)
		{
			SemanticModel model = compilation.GetSemanticModel(tree);
			SyntaxNode root = tree.GetRoot(cancellationToken);
			arithmetic.AddRange(FindArithmeticOperations(root, model, reader, cancellationToken));
			powers.AddRange(FindPowerOperations(root, model, reader, cancellationToken));
			methods.AddRange(FindPowerMethodOperations(root, model, reader, cancellationToken));
		}

		return new(arithmetic.ToImmutable(), powers.ToImmutable(), methods.ToImmutable());
	}

	private static ImmutableArray<ArithmeticOperation> FindArithmeticOperations(SyntaxNode root, SemanticModel model,
		QuantityTypeReader reader, CancellationToken cancellationToken)
	{
		ImmutableArray<ArithmeticOperation>.Builder operations = ImmutableArray.CreateBuilder<ArithmeticOperation>();
		foreach (BinaryExpressionSyntax binary in root.DescendantNodes().OfType<BinaryExpressionSyntax>())
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (ArithmeticDiscovery.TryRead(binary, model, reader, out ArithmeticOperation operation, cancellationToken))
			{
				operations.Add(operation);
			}
		}

		return operations.ToImmutable();
	}

	private static ImmutableArray<PowerOperation> FindPowerOperations(SyntaxNode root, SemanticModel model,
		QuantityTypeReader reader, CancellationToken cancellationToken)
	{
		ImmutableArray<PowerOperation>.Builder operations = ImmutableArray.CreateBuilder<PowerOperation>();
		PowerDiscovery discovery = new(model);
		foreach (InvocationExpressionSyntax invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (invocation.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Of" }
				&& discovery.TryRead(invocation, out Rational exponent, out string exponentTypeName, cancellationToken)
				&& reader.TryRead(model.GetTypeInfo(invocation.ArgumentList.Arguments[0].Expression, cancellationToken).Type,
					out Dimension operand, cancellationToken) && operand != default)
			{
				operations.Add(new(operand, exponent, operand.Pow(exponent), exponentTypeName));
			}
		}

		return operations.ToImmutable();
	}

	private static ImmutableArray<PowerMethodOperation> FindPowerMethodOperations(SyntaxNode root, SemanticModel model,
		QuantityTypeReader reader, CancellationToken cancellationToken)
	{
		ImmutableArray<PowerMethodOperation>.Builder operations = ImmutableArray.CreateBuilder<PowerMethodOperation>();
		PowerMethodDiscovery discovery = new(model);
		foreach (InvocationExpressionSyntax invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (discovery.TryRead(invocation, out Rational exponent, out string methodName, cancellationToken)
				&& TryReadPowerMethodReceiver(invocation, model, reader, out Dimension operand, cancellationToken)
				&& operand != default)
			{
				operations.Add(new(operand, exponent, operand.Pow(exponent), methodName));
			}
		}

		return operations.ToImmutable();
	}

	private static bool TryReadPowerMethodReceiver(InvocationExpressionSyntax invocation, SemanticModel model,
		QuantityTypeReader reader, out Dimension operand, CancellationToken cancellationToken)
	{
		operand = default;
		if (invocation.Expression is MemberAccessExpressionSyntax access)
		{
			return reader.TryRead(model.GetTypeInfo(access.Expression, cancellationToken).Type, out operand, cancellationToken);
		}

		if (invocation.Expression is MemberBindingExpressionSyntax)
		{
			ConditionalAccessExpressionSyntax? conditional = invocation.Ancestors().OfType<ConditionalAccessExpressionSyntax>()
				.FirstOrDefault(access => access.WhenNotNull.Span.Contains(invocation.Span));
			if (conditional is not null)
			{
				return reader.TryReadIncludingNullable(model.GetTypeInfo(conditional.Expression, cancellationToken).Type,
					out operand, cancellationToken);
			}
		}

		return false;
	}
}
