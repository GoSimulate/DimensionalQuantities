using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generation;

public sealed class ComposedDiscoveryTests
{
	[Fact]
	public async Task ZerothPowerEmitsScalarMagnitudeWithZeroExponent()
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = """
				using DimensionalQuantities.TypeSystem;

				class Consumer
				{
					double M(Length value) => Power<NumberZero>.Of(value);
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.Contains("public static double Of(Length value) => global::System.Math.Pow(value.AsSiBaseUnit(), 0d);", string.Join("\n", test.GeneratedSources));
	}

	[Theory]
	[InlineData("NumberTwo", "TerminalHex2")]
	[InlineData("NumberHalf", "Fraction<TerminalHex1, TerminalHex2>")]
	public async Task ExplicitPowerRebindsGeneratedArgumentAndInferredDelegate(string exponent, string expectedExponent)
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
				using DimensionalQuantities.TypeSystem;
				class Consumer
				{
					void M(Length length, Current current, Time time)
					{
						var make = () => Power<{{exponent}}>.Of(length * current);
						var result = make() * time;
					}
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = test.Compilation.SyntaxTrees.First();
		SemanticModel model = test.Compilation.GetSemanticModel(tree);
		SyntaxNode root = tree.GetRoot(TestContext.Current.CancellationToken);
		InvocationExpressionSyntax power = root.DescendantNodes().OfType<InvocationExpressionSyntax>().Single(call => call.Expression is MemberAccessExpressionSyntax);
		IMethodSymbol selected = Assert.IsAssignableFrom<IMethodSymbol>(model.GetSymbolInfo(power, TestContext.Current.CancellationToken).Symbol);
		VariableDeclaratorSyntax result = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().Single(variable => variable.Identifier.ValueText == "result");
		ILocalSymbol final = Assert.IsAssignableFrom<ILocalSymbol>(model.GetDeclaredSymbol(result, TestContext.Current.CancellationToken));
		VariableDeclaratorSyntax make = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().Single(variable => variable.Identifier.ValueText == "make");
		INamedTypeSymbol delegateType = (INamedTypeSymbol)((ILocalSymbol)model.GetDeclaredSymbol(make, TestContext.Current.CancellationToken)!).Type;
		Assert.Multiple(
			() => Assert.Equal($"{expectedExponent},TerminalHex0,TerminalHex0,{expectedExponent},TerminalHex0,TerminalHex0,TerminalHex0", Encoding(selected.ReturnType)),
			() => Assert.Equal($"{expectedExponent},TerminalHex0,TerminalHex1,{expectedExponent},TerminalHex0,TerminalHex0,TerminalHex0", Encoding(final.Type)),
			() => Assert.True(SymbolEqualityComparer.Default.Equals(selected.ReturnType, delegateType.DelegateInvokeMethod!.ReturnType)),
			() => Assert.Equal("TerminalHex1,TerminalHex0,TerminalHex0,TerminalHex1,TerminalHex0,TerminalHex0,TerminalHex0", Encoding(selected.Parameters.Single().Type)),
			() => Assert.All(root.DescendantNodes().OfType<BinaryExpressionSyntax>(), binary => Assert.NotNull(model.GetSymbolInfo(binary, TestContext.Current.CancellationToken).Symbol)));
	}

	[Theory]
	[InlineData("*")]
	[InlineData("/")]
	public async Task ConsumerOperatorResultFeedsFurtherGeneratedArithmetic(string operation)
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
				static class Custom
				{
					extension(LengthCurrent)
					{
						public static Time operator {{operation}}(LengthCurrent left, Current right) => default;
					}
				}

				class Consumer
				{
					void M(LengthCurrent left, Current right)
					{
						var result = (left {{operation}} right) * left;
					}
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = test.Compilation.SyntaxTrees.First();
		SemanticModel model = test.Compilation.GetSemanticModel(tree);
		SyntaxNode root = tree.GetRoot(TestContext.Current.CancellationToken);
		BinaryExpressionSyntax consumerOperation = root.DescendantNodes().OfType<BinaryExpressionSyntax>().Single(binary => binary.Left is IdentifierNameSyntax);
		IMethodSymbol selected = Assert.IsAssignableFrom<IMethodSymbol>(model.GetSymbolInfo(consumerOperation, TestContext.Current.CancellationToken).Symbol);
		VariableDeclaratorSyntax result = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
		ILocalSymbol final = Assert.IsAssignableFrom<ILocalSymbol>(model.GetDeclaredSymbol(result, TestContext.Current.CancellationToken));
		Assert.Multiple(
			() => Assert.Contains(selected.DeclaringSyntaxReferences, reference => reference.SyntaxTree == tree),
			() => Assert.Equal("TerminalHex0,TerminalHex0,TerminalHex1,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0", Encoding(selected.ReturnType)),
			() => Assert.Equal("TerminalHex1,TerminalHex0,TerminalHex1,TerminalHex1,TerminalHex0,TerminalHex0,TerminalHex0", Encoding(final.Type)),
			() => Assert.NotNull(model.GetSymbolInfo(root.DescendantNodes().OfType<BinaryExpressionSyntax>().First(), TestContext.Current.CancellationToken).Symbol));
	}

	[Fact]
	public async Task ConsumerHelperOnGeneratedReceiverFeedsFurtherArithmetic()
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = """
				static class Custom
				{
					public static Time Squared(this LengthCurrent value) => default;
				}

				class Consumer
				{
					void M(Length length, Current current)
					{
						var result = (length * current).Squared() * current;
					}
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = test.Compilation.SyntaxTrees.First();
		SemanticModel model = test.Compilation.GetSemanticModel(tree);
		SyntaxNode root = tree.GetRoot(TestContext.Current.CancellationToken);
		IMethodSymbol selected = Assert.IsAssignableFrom<IMethodSymbol>(model.GetSymbolInfo(root.DescendantNodes().OfType<InvocationExpressionSyntax>().Single(), TestContext.Current.CancellationToken).Symbol);
		ILocalSymbol result = (ILocalSymbol)model.GetDeclaredSymbol(root.DescendantNodes().OfType<VariableDeclaratorSyntax>().Single(), TestContext.Current.CancellationToken)!;
		Assert.Multiple(
			() => Assert.Equal("Custom", selected.ContainingType.Name),
			() => Assert.Equal("TerminalHex0,TerminalHex0,TerminalHex1,TerminalHex1,TerminalHex0,TerminalHex0,TerminalHex0", Encoding(result.Type)),
			() => Assert.All(root.DescendantNodes().OfType<BinaryExpressionSyntax>(), binary => Assert.NotNull(model.GetSymbolInfo(binary, TestContext.Current.CancellationToken).Symbol)),
			() => Assert.DoesNotContain("public LengthSquaredCurrentSquared Squared()", string.Join("\n", test.GeneratedSources)));
	}

	[Theory]
	[InlineData("wrapper + wrapper")]
	[InlineData("wrapper - wrapper")]
	[InlineData("-wrapper")]
	public async Task BoundWrapperOperatorResultFeedsGeneratedArithmetic(string expression)
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
				class Wrapper
				{
					public static Current operator +(Wrapper left, Wrapper right) => default;
					public static Current operator -(Wrapper left, Wrapper right) => default;
					public static Current operator -(Wrapper value) => default;
				}

				class Consumer
				{
					void M(Wrapper wrapper, LengthCurrent quantity)
					{
						var result = ({{expression}}) * quantity;
					}
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = test.Compilation.SyntaxTrees.First();
		SemanticModel model = test.Compilation.GetSemanticModel(tree);
		SyntaxNode root = tree.GetRoot(TestContext.Current.CancellationToken);
		ParenthesizedExpressionSyntax operand = root.DescendantNodes().OfType<ParenthesizedExpressionSyntax>().Single();
		IMethodSymbol selected = Assert.IsAssignableFrom<IMethodSymbol>(model.GetSymbolInfo(operand.Expression, TestContext.Current.CancellationToken).Symbol);
		ILocalSymbol result = (ILocalSymbol)model.GetDeclaredSymbol(root.DescendantNodes().OfType<VariableDeclaratorSyntax>().Single(), TestContext.Current.CancellationToken)!;
		Assert.Multiple(
			() => Assert.Equal("Wrapper", selected.ContainingType.Name),
			() => Assert.Equal("TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex1,TerminalHex0,TerminalHex0,TerminalHex0", Encoding(selected.ReturnType)),
			() => Assert.Equal("TerminalHex1,TerminalHex0,TerminalHex0,TerminalHex2,TerminalHex0,TerminalHex0,TerminalHex0", Encoding(result.Type)),
			() => Assert.NotNull(model.GetSymbolInfo(operand.Parent!, TestContext.Current.CancellationToken).Symbol));
	}

	private static string Encoding(ITypeSymbol type)
	{
		INamedTypeSymbol dimension = (INamedTypeSymbol)((INamedTypeSymbol)type).TypeArguments.Single();
		return string.Join(",", dimension.TypeArguments.Select(argument => argument.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
	}
}
