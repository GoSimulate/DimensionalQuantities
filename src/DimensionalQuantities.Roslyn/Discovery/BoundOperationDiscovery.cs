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
			PowerDiscovery powerDiscovery = new(model);
			PowerMethodDiscovery methodDiscovery = new(model);
			foreach (BinaryExpressionSyntax binary in root.DescendantNodes().OfType<BinaryExpressionSyntax>())
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (ArithmeticDiscovery.TryRead(binary, model, reader, out ArithmeticOperation operation, cancellationToken))
				{
					arithmetic.Add(operation);
				}
			}

			foreach (InvocationExpressionSyntax invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (invocation.Expression is not MemberAccessExpressionSyntax access)
				{
					continue;
				}

				if (access.Name.Identifier.ValueText == "Of"
					&& powerDiscovery.TryRead(invocation, out Rational exponent, out string exponentTypeName, cancellationToken)
					&& reader.TryRead(model.GetTypeInfo(invocation.ArgumentList.Arguments[0].Expression, cancellationToken).Type,
						out Dimension operand, cancellationToken) && operand != default)
				{
					powers.Add(new(operand, exponent, operand.Pow(exponent), exponentTypeName));
				}
				else if (PowerMethodDiscovery.IsSupportedName(access.Name.Identifier.ValueText)
					&& methodDiscovery.TryRead(invocation, out Rational methodExponent, out string methodName, cancellationToken)
					&& reader.TryRead(model.GetTypeInfo(access.Expression, cancellationToken).Type,
						out Dimension methodOperand, cancellationToken) && methodOperand != default)
				{
					methods.Add(new(methodOperand, methodExponent, methodOperand.Pow(methodExponent), methodName));
				}
			}
		}

		return new(arithmetic.ToImmutable(), powers.ToImmutable(), methods.ToImmutable());
	}
}
