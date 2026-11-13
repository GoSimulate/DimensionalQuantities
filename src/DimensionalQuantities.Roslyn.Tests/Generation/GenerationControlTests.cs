using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generation;

public sealed class GenerationControlTests
{
	public static TheoryData<string, string> InvalidTypePositions
	{
		get
		{
			TheoryData<string, string> rows = new();
			foreach (string name in new[] { "CurrentLength", "Length1", "Root2Length", "LengthLength" })
			{
				foreach (string position in new[] { "TYPE Field;", "void M(TYPE input) {}", "TYPE M() => default;", "void M() { TYPE local = default; }", "System.Collections.Generic.List<TYPE> Field;", "TYPE[] Field;", "TYPE? Field;" })
				{
					rows.Add(name, position.Replace("TYPE", "{|#0:" + name + "|}"));
				}
			}
			return rows;
		}
	}

	[Theory]
	[MemberData(nameof(InvalidTypePositions))]
	public async Task NoncanonicalUnresolvedNamesRemainCompilerErrors(string spelling, string member)
	{
		QuantityGeneratorTest test = new() { TestCode = $"class Consumer {{ {member} }}" };
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS0246").WithLocation(0).WithArguments(spelling));
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.DoesNotContain($"global using {spelling} =", string.Join("\n", test.GeneratedSources));
	}

	[Fact]
	public async Task UnknownStaticMemberDoesNotRequestQuantity()
	{
		QuantityGeneratorTest test = new() { TestCode = "class Consumer { object M() => {|#0:LengthCurrent|}.Unknown(); }" };
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS0103").WithLocation(0).WithArguments("LengthCurrent"));
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.DoesNotContain("global using LengthCurrent =", string.Join("\n", test.GeneratedSources));
	}

	[Theory]
	[InlineData("class Consumer { Length M(Length l, Current i) => {|#0:l*i|}; }", "CS0029")]
	[InlineData("class Consumer { void M(Length l, Current i) { Length result = {|#0:l*i|}; } }", "CS0029")]
	[InlineData("class Consumer { void Accept(Time t) {} void M(Length l, Current i) => Accept({|#0:l*i|}); }", "CS1503")]
	[InlineData("class Consumer { object M(Length l, Current i) => new Length[] { {|#0:l*i|} }; }", "CS0029")]
	[InlineData("class Consumer { void M(Length l, Current i, bool b) { var value = {|#0:b ? l : i|}; } }", "CS0173")]
	public async Task DimensionalTypeSafetyPreservesCompilerErrors(string source, string diagnostic)
	{
		QuantityGeneratorTest test = new() { TestCode = source };
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError(diagnostic).WithLocation(0));
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.DoesNotContain("implicit operator", string.Join("\n", test.GeneratedSources));
	}

	[Theory]
	[InlineData("+", "CS0019")]
	[InlineData("<", "CS0019")]
	public async Task UnsupportedOperationsDoNotFabricateOperators(string operation, string diagnostic)
	{
		QuantityGeneratorTest test = new() { TestCode = $"class Consumer {{ object M(Length l, Current i) => {{|#0:l {operation} i|}}; }}" };
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError(diagnostic).WithLocation(0));
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.DoesNotContain($"operator {operation}(Length left, Current right)", string.Join("\n", test.GeneratedSources));
	}

	[Fact]
	public async Task UnrelatedQuantityLookalikeDoesNotAcquireDimensionalOperators()
	{
		QuantityGeneratorTest test = new() { TestCode = "namespace Custom { struct Quantity<T> {} class Dimension {} class Consumer { object M(Quantity<Dimension> a, Quantity<Dimension> b) => {|#0:a*b|}; } }" };
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS0019").WithLocation(0));
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.DoesNotContain("Custom.Quantity", string.Join("\n", test.GeneratedSources));
	}

	[Fact]
	public async Task ExistingConsumerHelperRetainsSelectedSymbol()
	{
		QuantityGeneratorTest test = new() { TestCode = "static class Custom { public static Length Squared(this LengthCurrent value) => default; } class Consumer { Length M(LengthCurrent value) => value.Squared(); }" };
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = test.Compilation.SyntaxTrees.First();
		InvocationExpressionSyntax call = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
		IMethodSymbol selected = Assert.IsAssignableFrom<IMethodSymbol>(test.Compilation.GetSemanticModel(tree).GetSymbolInfo(call, TestContext.Current.CancellationToken).Symbol);
		Assert.Multiple(
			() => Assert.Equal("Custom", selected.ContainingType.Name),
			() => Assert.DoesNotContain("public LengthSquaredCurrentSquared Squared()", string.Join("\n", test.GeneratedSources)));
	}

	[Theory]
	[InlineData("TerminalHex2", "Area")]
	[InlineData("Hex<TerminalHex0, TerminalHex1>", "Length16")]
	[InlineData("Negative<TerminalHex1>", "InverseLength")]
	[InlineData("Fraction<TerminalHex3, TerminalHex2>", "SquareRootLengthCubed")]
	[InlineData("Fraction<TerminalHex4, TerminalHex2>", "Area")]
	public async Task ExplicitPowerEncodingsPreserveClosedReceiver(string exponent, string result)
	{
		QuantityGeneratorTest test = new() { TestCode = $"using DimensionalQuantities.TypeSystem; using P = DimensionalQuantities.TypeSystem.Power<{exponent.Replace("TerminalHex", "DimensionalQuantities.TypeSystem.TerminalHex").Replace("Hex<", "DimensionalQuantities.TypeSystem.Hex<").Replace("Negative<", "DimensionalQuantities.TypeSystem.Negative<").Replace("Fraction<", "DimensionalQuantities.TypeSystem.Fraction<")}>; class Consumer {{ {result} M(Length value) => P.Of(value); }}" };
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = test.Compilation.SyntaxTrees.First();
		InvocationExpressionSyntax call = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
		IMethodSymbol selected = Assert.IsAssignableFrom<IMethodSymbol>(test.Compilation.GetSemanticModel(tree).GetSymbolInfo(call, TestContext.Current.CancellationToken).Symbol);
		IMethodSymbol declared = test.Compilation.GetTypeByMetadataName("Consumer")!.GetMembers("M").OfType<IMethodSymbol>().Single();
		Assert.Multiple(
			() => Assert.True(SymbolEqualityComparer.Default.Equals(declared.ReturnType, selected.ReturnType)),
			() => Assert.Contains($"Power<{exponent.Replace("TerminalHex", "global::DimensionalQuantities.TypeSystem.TerminalHex").Replace("Hex<", "global::DimensionalQuantities.TypeSystem.Hex<").Replace("Negative<", "global::DimensionalQuantities.TypeSystem.Negative<").Replace("Fraction<", "global::DimensionalQuantities.TypeSystem.Fraction<")}>", string.Join("\n", test.GeneratedSources)));
	}

	[Fact]
	public async Task EquivalentEnglishRequestsAcrossFilesCoalescePowerSignatures()
	{
		QuantityGeneratorTest test = new();
		test.TestState.Sources.Add(("First.cs", "using DimensionalQuantities.TypeSystem; class First { Length M(Area value) => Power<NumberHalf>.Of(value); }"));
		test.TestState.Sources.Add(("Second.cs", "using DimensionalQuantities.TypeSystem; class Second { Length M(Area value) => Power<NumberOneOverTwo>.Of(value); NumberHalf field; }"));
		await test.RunAsync(TestContext.Current.CancellationToken);
		UsingDirectiveSyntax[] aliases = test.GeneratedSources.SelectMany(tree => tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<UsingDirectiveSyntax>()).Where(alias => alias.Alias?.Name.Identifier.ValueText is "NumberHalf" or "NumberOneOverTwo").ToArray();
		ISymbol?[] calls = test.Compilation.SyntaxTrees.Where(tree => tree.FilePath.EndsWith("First.cs") || tree.FilePath.EndsWith("Second.cs")).Select(tree => test.Compilation.GetSemanticModel(tree).GetSymbolInfo(tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single(), TestContext.Current.CancellationToken).Symbol).ToArray();
		Assert.Multiple(
			() => Assert.Equal(2, aliases.Length),
			() => Assert.Equal(aliases[0].Name!.ToString(), aliases[1].Name!.ToString()),
			() => Assert.True(SymbolEqualityComparer.Default.Equals(calls[0], calls[1])));
	}
}
