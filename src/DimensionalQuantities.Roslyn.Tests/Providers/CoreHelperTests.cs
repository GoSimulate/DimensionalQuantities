using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Providers;

public sealed class CoreHelperTests
{
	[Theory]
	[InlineData("Length", "Metres")]
	[InlineData("Mass", "Grams")]
	[InlineData("Time", "Seconds")]
	[InlineData("Current", "Amps")]
	[InlineData("Temperature", "Kelvin")]
	[InlineData("AmountOfSubstance", "Moles")]
	[InlineData("LuminousIntensity", "Candelas")]
	public async Task CoreNamedSiFactoriesAreAvailableWithoutOptionalReferences(string quantity, string unit)
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
				class Consumer
				{
					{{quantity}} M() => {{quantity}}.From{{unit}}(1);
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = test.Compilation.SyntaxTrees.First();
		InvocationExpressionSyntax call = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
		IMethodSymbol selected = Assert.IsAssignableFrom<IMethodSymbol>(test.Compilation.GetSemanticModel(tree).GetSymbolInfo(call, TestContext.Current.CancellationToken).Symbol);
		ITypeSymbol expected = test.Compilation.GetTypeByMetadataName("Consumer")!.GetMembers("M").OfType<IMethodSymbol>().Single().ReturnType;
		Assert.Multiple(() => Assert.True(SymbolEqualityComparer.Default.Equals(expected, selected.ReturnType)),
			() => Assert.Equal(test.Compilation.AssemblyName, selected.ContainingAssembly.Name));
	}

	[Fact]
	public async Task AutomaticMassPrefixesApplyKilogramCorrection()
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = """
				class Consumer
				{
					Mass M() => Mass.FromKiloGrams(1);
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		string source = string.Join("\n", test.GeneratedSources);
		Assert.Multiple(() => Assert.Contains("FromGrams(double value) => Mass.FromArbitrarySiComponents(value, overallScaleExponent: -3d)", source),
			() => Assert.Contains("FromKiloGrams(double value) => Mass.FromArbitrarySiComponents(value, overallScaleExponent: 0d)", source));
	}

	[Fact]
	public async Task ExplicitKilogramsDeclarationPreservesItsSpelling()
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = """
				using DimensionalQuantities.Units.Declarations;
				using DimensionalQuantities.Units.Symbols;

				namespace Custom;

				[UnitProvider]
				class Provider
				{
					[QuantityUnit(typeof(Mass))]
					[UnitFactor(typeof(Gram), PrefixExponent = 3)]
					class MassUnit
					{
					}
				}

				class Consumer
				{
					Mass M() => Mass.FromKilograms(1);
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.Contains("FromKilograms(double value)", string.Join("\n", test.GeneratedSources));
	}
}
