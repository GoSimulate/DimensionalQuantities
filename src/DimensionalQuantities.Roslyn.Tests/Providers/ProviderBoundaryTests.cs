using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Providers;

public sealed class ProviderBoundaryTests
{
	[Theory]
	[InlineData("", "", "")]
	[InlineData("""
		namespace Custom
		{
			class Outer
			{
		""", "} }", """
		using Custom;
		""")]
	public async Task OrdinaryProviderShapesPublishPublicConversions(string opening, string closing, string import)
	{
		QuantityGeneratorTest test = new();
		test.TestState.Sources.Add(("Provider.cs", $$"""
			using DimensionalQuantities.Units.Declarations;
			{{opening}}
			[UnitSymbol("Widget", "Widgets", "w")]
			class Widget
			{
			}
			[UnitProvider]
			class Provider
			{
				[QuantityUnit(typeof(Length))]
				[UnitFactor(typeof(Widget))]
				private class Distance
				{
				}
			}
			{{closing}}
			"""));
		test.TestState.Sources.Add(("Consumer.cs", $$"""
			{{import}}
			class Consumer
			{
				Length M() => Length.FromWidgets(1);
			}
			"""));
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = test.Compilation.SyntaxTrees.Single(source => source.FilePath.EndsWith("Consumer.cs"));
		InvocationExpressionSyntax call = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
		IMethodSymbol method = Assert.IsAssignableFrom<IMethodSymbol>(test.Compilation.GetSemanticModel(tree).GetSymbolInfo(call, TestContext.Current.CancellationToken).Symbol);
		Assert.Equal(Accessibility.Public, method.DeclaredAccessibility);
	}

	[Fact]
	public async Task OneDeclarationCanAssociateDistinctQuantityReceivers()
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = """
				using DimensionalQuantities.Units.Declarations;
				namespace Custom;
				[UnitSymbol("Widget", "Widgets", "w")]
				class Widget
				{
				}
				[UnitProvider]
				class Provider
				{
					[QuantityUnit(typeof(Length))]
					[QuantityUnit(typeof(Time))]
					[UnitFactor(typeof(Widget))]
					class Reading
					{
					}
				}
				class Consumer
				{
					Length Distance() => Length.FromWidgets(1);
					Time Duration() => Time.FromWidgets(1);
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		MethodDeclarationSyntax[] factories = test.GeneratedSources.SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()).Where(method => method.Identifier.ValueText == "FromWidgets").ToArray();
		ITypeSymbol[] returns = factories.Select(method => ((IMethodSymbol)test.Compilation.GetSemanticModel(method.SyntaxTree).GetDeclaredSymbol(method)!).ReturnType).ToArray();
		ITypeSymbol[] expected = test.Compilation.GetTypeByMetadataName("Custom.Consumer")!.GetMembers().OfType<IMethodSymbol>().Where(method => method.Name is "Distance" or "Duration").Select(method => method.ReturnType).ToArray();
		Assert.Multiple(
			() => Assert.Equal(2, returns.Length),
			() => Assert.All(expected, type => Assert.Contains(returns, actual => SymbolEqualityComparer.Default.Equals(type, actual))));
	}

	[Fact]
	public async Task NumericallyEqualNoncanonicalReceiversRemainDistinct()
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = """
				using DimensionalQuantities.TypeSystem;
				using DimensionalQuantities.Units.Declarations;
				namespace Custom;
				[UnitSymbol("Widget", "Widgets", "w")]
				class Widget
				{
				}
				[UnitProvider]
				class Provider
				{
					[QuantityUnit(typeof(Length))]
					[UnitFactor(typeof(Widget))]
					class Distance
					{
					}
					[QuantityUnit(typeof(Quantity<Dimension<Fraction<TerminalHex2, TerminalHex2>, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0>>))]
					[UnitFactor(typeof(Widget))]
					class OtherDistance
					{
					}
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		MethodDeclarationSyntax[] factories = test.GeneratedSources.SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()).Where(method => method.Identifier.ValueText == "FromWidgets").ToArray();
		Assert.Equal(2, factories.Length);
		ITypeSymbol[] returns = factories.Select(method => ((IMethodSymbol)test.Compilation.GetSemanticModel(method.SyntaxTree).GetDeclaredSymbol(method)!).ReturnType).ToArray();
		Assert.False(SymbolEqualityComparer.Default.Equals(returns[0], returns[1]));
	}

	[Fact]
	public async Task PartialProviderEmitsEachConversionOnce()
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = """
				using DimensionalQuantities.Units.Declarations;
				namespace Custom;
				[UnitSymbol("Widget", "Widgets", "w")]
				class Widget
				{
				}
				[UnitProvider]
				partial class Provider
				{
					[QuantityUnit(typeof(Length))]
					[UnitFactor(typeof(Widget))]
					class Distance
					{
					}
				}
				partial class Provider
				{
				}
				class Consumer
				{
					Length M() => Length.FromWidgets(1);
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.Single(test.GeneratedSources.SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()), method => method.Identifier.ValueText == "FromWidgets");
	}

	[Fact]
	public async Task ReciprocalOnlyUnitDoesNotInventNumeratorPrefixes()
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = """
				using DimensionalQuantities.Units.Declarations;
				using DimensionalQuantities.Units.Symbols;
				namespace Custom;
				[UnitProvider(GenerateSiPrefixes = true)]
				class Provider
				{
					[QuantityUnit(typeof(Frequency))]
					[UnitFactor(typeof(Second), Power = -1)]
					class Rate
					{
					}
				}
				class Consumer
				{
					Frequency M() => Frequency.FromInverseSecond(1);
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		IEnumerable<string> factories = test.GeneratedSources.Where(tree => tree.ToString().Contains("namespace Custom;")).SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()).Select(method => method.Identifier.ValueText).Where(name => name.StartsWith("From"));
		Assert.Equal(new[] { "FromInverseSecond" }, factories);
	}
}
