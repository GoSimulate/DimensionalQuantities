using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generation;

public sealed class AdditionalDiscoveryTests
{
	[Fact]
	public async Task ExplicitConsumerAliasReservesUnresolvedName()
	{
		QuantityGeneratorTest test = new() { TestCode = "using LengthCurrent = Example.Custom; namespace Example { class Custom {} } class Consumer { LengthCurrent field; }" };
		await test.RunAsync(TestContext.Current.CancellationToken);
		IFieldSymbol field = test.Compilation.GetTypeByMetadataName("Consumer")!.GetMembers("field").OfType<IFieldSymbol>().Single();
		Assert.Multiple(
			() => Assert.Equal("Example.Custom", field.Type.ToDisplayString()),
			() => Assert.DoesNotContain("global using LengthCurrent =", string.Join("\n", test.GeneratedSources)));
	}

	[Theory]
	[InlineData("Length l, Mass x", "LengthMass")]
	[InlineData("Length l, Time x", "LengthTime")]
	[InlineData("Length l, Current x", "LengthCurrent")]
	[InlineData("Length l, Temperature x", "LengthTemperature")]
	[InlineData("Length l, AmountOfSubstance x", "LengthAmountOfSubstance")]
	[InlineData("Length l, LuminousIntensity x", "LengthLuminousIntensity")]
	public async Task MultiplicationRetainsEachEncodedAxis(string parameters, string result)
	{
		QuantityGeneratorTest test = new() { TestCode = $"class Consumer {{ {result} M({parameters}) => l*x; }}" };
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = test.Compilation.SyntaxTrees.First();
		BinaryExpressionSyntax binary = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<BinaryExpressionSyntax>().Single();
		IMethodSymbol selected = Assert.IsAssignableFrom<IMethodSymbol>(test.Compilation.GetSemanticModel(tree).GetSymbolInfo(binary, TestContext.Current.CancellationToken).Symbol);
		IMethodSymbol method = test.Compilation.GetTypeByMetadataName("Consumer")!.GetMembers("M").OfType<IMethodSymbol>().Single();
		Assert.Multiple(() => Assert.True(SymbolEqualityComparer.Default.Equals(method.ReturnType, selected.ReturnType)),
			() => Assert.True(SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, selected.Parameters[0].Type)),
			() => Assert.True(SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, selected.Parameters[1].Type)));
	}

	[Theory]
	[InlineData("(byte)2")]
	[InlineData("(short)2")]
	[InlineData("2")]
	[InlineData("2L")]
	[InlineData("2U")]
	[InlineData("2UL")]
	[InlineData("2f")]
	[InlineData("'a'")]
	public async Task ScalarInverseAcceptsStandardImplicitDoubleConversions(string scalar)
	{
		QuantityGeneratorTest test = new() { TestCode = $"class Consumer {{ Frequency M(Time time) => {scalar}/time; }}" };
		await test.RunAsync(TestContext.Current.CancellationToken);
		string generated = string.Join("\n", test.GeneratedSources);
		Assert.Multiple(
			() => Assert.Contains("operator /(double left, Time right)", generated),
			() => Assert.Contains(".FromSiBaseUnit(left / right.AsSiBaseUnit())", generated));
	}

	[Theory]
	[InlineData("1m")]
	[InlineData("new Scalar()")]
	public async Task UnsupportedScalarInverseDoesNotAcquireOperator(string scalar)
	{
		QuantityGeneratorTest test = new() { TestCode = $"struct Scalar {{ public static implicit operator double(Scalar x) => 1; }} class Consumer {{ object M(Time time) => {{|#0:{scalar}/time|}}; }}" };
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS0019").WithLocation(0));
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.DoesNotContain("operator /(double left, Time right)", string.Join("\n", test.GeneratedSources));
	}

	[Theory]
	[InlineData("*")]
	[InlineData("/")]
	public async Task BoundUserOperatorRetainsSelectedImplementation(string operation)
	{
		QuantityGeneratorTest test = new() { TestCode = $"static class Custom {{ extension(LengthCurrent) {{ public static Time operator {operation}(LengthCurrent left, Current right) => default; }} }} class Consumer {{ Time M(LengthCurrent left, Current right) => left{operation}right; }}" };
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = test.Compilation.SyntaxTrees.First();
		BinaryExpressionSyntax binary = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<BinaryExpressionSyntax>().Single();
		IMethodSymbol selected = Assert.IsAssignableFrom<IMethodSymbol>(test.Compilation.GetSemanticModel(tree).GetSymbolInfo(binary, TestContext.Current.CancellationToken).Symbol);
		Assert.Multiple(
			() => Assert.Contains(selected.DeclaringSyntaxReferences, reference => reference.SyntaxTree == tree),
			() => Assert.DoesNotContain($"operator {operation}(LengthCurrent left, Current right)", string.Join("\n", test.GeneratedSources)));
	}

	[Fact]
	public async Task StandaloneEnglishNameIsNotAnExponentRequest()
	{
		QuantityGeneratorTest test = new() { TestCode = "class Consumer { {|#0:NumberTwentyOne|} field; }" };
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS0246").WithLocation(0).WithArguments("NumberTwentyOne"));
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.DoesNotContain("global using NumberTwentyOne =", string.Join("\n", test.GeneratedSources));
	}

	[Fact]
	public async Task UnrelatedPowerApiKeepsItsChosenMethod()
	{
		QuantityGeneratorTest test = new() { TestCode = "class NumberTwentyOne {} static class Power<T> { public static int Of(Length l) => 21; } class Consumer { int M(Length l) => Power<NumberTwentyOne>.Of(l); }" };
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = test.Compilation.SyntaxTrees.First();
		InvocationExpressionSyntax call = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
		IMethodSymbol selected = Assert.IsAssignableFrom<IMethodSymbol>(test.Compilation.GetSemanticModel(tree).GetSymbolInfo(call, TestContext.Current.CancellationToken).Symbol);
		Assert.Multiple(() => Assert.Equal("Power", selected.ContainingType.Name),
			() => Assert.Equal(SpecialType.System_Int32, selected.ReturnType.SpecialType),
			() => Assert.DoesNotContain("global using NumberTwentyOne =", string.Join("\n", test.GeneratedSources)));
	}

	[Fact]
	public async Task IncompleteExpressionAllowsIndependentValidDiscovery()
	{
		QuantityGeneratorTest test = new() { TestCode = "class Consumer { void Broken(Length l) { var missing = l * {|#0:;|} } LengthCurrent Valid(Length l, Current i) => l*i; }" };
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS1525").WithLocation(0));
		await test.RunAsync(TestContext.Current.CancellationToken);
		OperatorDeclarationSyntax[] operators = test.GeneratedSources.SelectMany(tree => tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<OperatorDeclarationSyntax>()).ToArray();
		Assert.Multiple(
			() => Assert.Single(operators),
			() => Assert.Equal("LengthCurrent", operators[0].ReturnType.ToString()));
	}

	[Fact]
	public async Task RepeatedArithmeticAcrossFilesHasOneSignature()
	{
		QuantityGeneratorTest test = new();
		test.TestState.Sources.Add(("First.cs", "class First { LengthCurrent M(Length l, Current i) => l*i; }"));
		test.TestState.Sources.Add(("Second.cs", "class Second { LengthCurrent M(Length l, Current i) => l*i; }"));
		await test.RunAsync(TestContext.Current.CancellationToken);
		OperatorDeclarationSyntax[] operators = test.GeneratedSources.SelectMany(tree => tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<OperatorDeclarationSyntax>()).Where(op => op.ParameterList.ToString() == "(Length left, Current right)").ToArray();
		Assert.Single(operators);
	}

	[Theory]
	[InlineData("Length.Abs({|#0:Time.FromSiBaseUnit(2)|})")]
	[InlineData("Length.Min(Length.FromSiBaseUnit(1), {|#0:Time.FromSiBaseUnit(2)|})")]
	[InlineData("Length.Max(Length.FromSiBaseUnit(1), {|#0:Time.FromSiBaseUnit(2)|})")]
	[InlineData("Length.Clamp(Length.FromSiBaseUnit(1), Length.FromSiBaseUnit(0), {|#0:Time.FromSiBaseUnit(2)|})")]
	public async Task RuntimeHelpersRejectIncompatibleDimensions(string expression)
	{
		QuantityGeneratorTest test = new() { TestCode = $"class Consumer {{ object M() => {expression}; }}" };
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS1503").WithLocation(0));
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.DoesNotContain("implicit operator", string.Join("\n", test.GeneratedSources));
	}
}
