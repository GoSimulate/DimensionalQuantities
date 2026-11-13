using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Providers;

public sealed class NamedPrefixTests
{
	[Theory]
	[InlineData("Quetta", 30)]
	[InlineData("Ronna", 27)]
	[InlineData("Yotta", 24)]
	[InlineData("Zetta", 21)]
	[InlineData("Exa", 18)]
	[InlineData("Peta", 15)]
	[InlineData("Tera", 12)]
	[InlineData("Giga", 9)]
	[InlineData("Mega", 6)]
	[InlineData("Kilo", 3)]
	[InlineData("Hecto", 2)]
	[InlineData("Deca", 1)]
	[InlineData("Deci", -1)]
	[InlineData("Centi", -2)]
	[InlineData("Milli", -3)]
	[InlineData("Micro", -6)]
	[InlineData("Nano", -9)]
	[InlineData("Pico", -12)]
	[InlineData("Femto", -15)]
	[InlineData("Atto", -18)]
	[InlineData("Zepto", -21)]
	[InlineData("Yocto", -24)]
	[InlineData("Ronto", -27)]
	[InlineData("Quecto", -30)]
	public async Task NamedPrefixesUseIndependentlySpecifiedDecimalExponents(string prefix, int exponent)
	{
		QuantityGeneratorTest test = new() { TestCode = $$"""
            using DimensionalQuantities.Units.Declarations;
            using DimensionalQuantities.Units.Symbols;
            namespace Custom;
            [UnitProvider(GenerateSiPrefixes = true)] class Provider { [QuantityUnit(typeof(Length))] [UnitFactor(typeof(Metre))] class Distance {} }
            class Consumer { Length M() => Length.From{{prefix}}Metres(1); }
            """ };
		await test.RunAsync(TestContext.Current.CancellationToken);
		string source = string.Join("\n", test.GeneratedSources.Where(tree => tree.ToString().Contains("namespace Custom;")));
		Assert.Contains($"From{prefix}Metres(double value)", source);
		MethodDeclarationSyntax method = test.GeneratedSources.SelectMany(tree => tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()).Single(method => method.Identifier.ValueText == $"From{prefix}Metres" && method.SyntaxTree.ToString().Contains("namespace Custom;"));
		Assert.Contains($"overallScaleExponent: {exponent}d", method.ToString());
	}
	[Theory]
	[InlineData("Quetta", 30)]
	[InlineData("Ronna", 27)]
	[InlineData("Yotta", 24)]
	[InlineData("Zetta", 21)]
	[InlineData("Exa", 18)]
	[InlineData("Peta", 15)]
	[InlineData("Tera", 12)]
	[InlineData("Giga", 9)]
	[InlineData("Mega", 6)]
	[InlineData("Kilo", 3)]
	[InlineData("Hecto", 2)]
	[InlineData("Deca", 1)]
	[InlineData("Deci", -1)]
	[InlineData("Centi", -2)]
	[InlineData("Milli", -3)]
	[InlineData("Micro", -6)]
	[InlineData("Nano", -9)]
	[InlineData("Pico", -12)]
	[InlineData("Femto", -15)]
	[InlineData("Atto", -18)]
	[InlineData("Zepto", -21)]
	[InlineData("Yocto", -24)]
	[InlineData("Ronto", -27)]
	[InlineData("Quecto", -30)]
	public async Task NamedPrefixAccessorBindsAndUsesIndependentExponent(string prefix, int exponent)
	{
		QuantityGeneratorTest test = new() { TestCode = $$"""
            using DimensionalQuantities.Units.Declarations;
            using DimensionalQuantities.Units.Symbols;
            namespace Custom;
            [UnitProvider(GenerateSiPrefixes = true)] class Provider { [QuantityUnit(typeof(Length))] [UnitFactor(typeof(Metre))] class Distance {} }
            class Consumer { double M(Length value) => value.As{{prefix}}Metres(); }
            """ };
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = test.Compilation.SyntaxTrees.First();
		InvocationExpressionSyntax call = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
		IMethodSymbol selected = Assert.IsAssignableFrom<IMethodSymbol>(test.Compilation.GetSemanticModel(tree).GetSymbolInfo(call, TestContext.Current.CancellationToken).Symbol);
		string declaration = selected.DeclaringSyntaxReferences.Single().GetSyntax(TestContext.Current.CancellationToken).ToString();
		Assert.Multiple(
			() => Assert.Equal("Custom", selected.ContainingNamespace.ToDisplayString()),
			() => Assert.Equal(SpecialType.System_Double, selected.ReturnType.SpecialType),
			() => Assert.Empty(selected.Parameters),
			() => Assert.Contains($"distance.AsArbitrarySiComponents(overallScaleExponent: {exponent}d)", declaration));
	}
}
