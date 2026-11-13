using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Providers;

public sealed class ProviderCoverageTests
{
	[Fact]
	public async Task NamedPrefixTargetsFirstPositiveFactorAfterDenominator()
	{
		QuantityGeneratorTest test = new() { TestCode = """
			using DimensionalQuantities.Units.Declarations;
			using DimensionalQuantities.Units.Symbols;
			namespace Custom;
			[UnitProvider(GenerateSiPrefixes = true)] class Provider {
			    [QuantityUnit(typeof(CurrentDensity))] [UnitFactor(typeof(Metre), Power = -2)] [UnitFactor(typeof(Amp))] class Reading {}
			}
			class Consumer { CurrentDensity M() => CurrentDensity.FromKiloAmpsPerSquareMetre(1); }
			""" };
		await test.RunAsync(TestContext.Current.CancellationToken);
		MethodDeclarationSyntax factory = test.GeneratedSources.SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()).Single(method => method.Identifier.ValueText == "FromKiloAmpsPerSquareMetre");
		Assert.Multiple(
			() => Assert.Contains("overallScaleExponent: 3d", factory.ToString()),
			() => Assert.DoesNotContain("FromAmpsPerSquareKiloMetre", string.Join("\n", test.GeneratedSources)));
	}

	[Fact]
	public async Task ParameterisedDeltaAccessorUsesItsOwnDefaultsAndWeightedFormula()
	{
		QuantityGeneratorTest test = new() { TestCode = """
			using DimensionalQuantities.Units.Declarations;
			using DimensionalQuantities.Units.Symbols;
			namespace Custom;
			[UnitProvider(GeneratePrefixConversions = true)] class Provider {
			    [QuantityUnit(typeof(CurrentDensity))] [UnitFactor(typeof(Metre), Power = -2, PrefixExponent = -2)] [UnitFactor(typeof(Amp), PrefixExponent = 3)] class Reading {}
			}
			class Consumer { double M(CurrentDensity value) => value.AsDelta().AsPrefixedUnits(-4, 1); }
			""" };
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = test.Compilation.SyntaxTrees.First();
		InvocationExpressionSyntax call = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single(invocation => invocation.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "AsPrefixedUnits" });
		IMethodSymbol selected = Assert.IsAssignableFrom<IMethodSymbol>(test.Compilation.GetSemanticModel(tree).GetSymbolInfo(call, TestContext.Current.CancellationToken).Symbol);
		string declaration = selected.DeclaringSyntaxReferences.Single().GetSyntax(TestContext.Current.CancellationToken).ToString();
		Assert.Multiple(
			() => Assert.Equal("Custom", selected.ContainingNamespace.ToDisplayString()),
			() => Assert.Equal(new[] { "metrePrefixExponent", "ampPrefixExponent" }, selected.Parameters.Select(parameter => parameter.Name)),
			() => Assert.Equal(new object?[] { -2, 3 }, selected.Parameters.Select(parameter => parameter.ExplicitDefaultValue)),
			() => Assert.Contains("delta.AsSiBaseUnit() / (1d * global::System.Math.Pow(10, ((double)metrePrefixExponent) * -2 + ((double)ampPrefixExponent)))", declaration));
	}

	[Theory]
	[InlineData("[UnitSymbol(\"Widget\", \"Widgets\", \"w\", SiBasePrefixExponent = 0)]", "[UnitSymbol(\"Widget\", \"Widgets\", \"w\", SiBasePrefixExponent = 3)]")]
	[InlineData("[UnitSymbol(\"Widget\", \"Widgets\", \"w\")] [AffineUnit(10)]", "[UnitSymbol(\"Widget\", \"Widgets\", \"w\")] [AffineUnit(20)]")]
	public async Task SameNameWithDifferentExponentOrOffsetIsRejected(string first, string second)
	{
		QuantityGeneratorTest test = new() { TestCode = $$"""
			using DimensionalQuantities.Units.Declarations;
			namespace Custom;
			{{first}} class First {}
			{{second}} class Second {}
			[UnitProvider] class {|#0:Provider|} {
			    [QuantityUnit(typeof(Temperature))] [UnitFactor(typeof(First))] [AdditionalUnit(typeof(Alternative))] class Reading {}
			    [UnitFactor(typeof(Second))] class Alternative {}
			}
			""" };
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("DQ0008").WithLocation(0));
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.DoesNotContain("FromWidgets", string.Join("\n", test.GeneratedSources));
	}

	[Theory]
	[InlineData("namespace First {", "}", "namespace Second {", "}", "First", "Second")]
	[InlineData("namespace Custom { class First {", "} }", "namespace Custom { class Second {", "} }", "Custom", "Custom")]
	public async Task SameShortProviderNamesKeepIndependentPublicSurfaces(string firstStart, string firstEnd, string secondStart, string secondEnd, string firstNamespace, string secondNamespace)
	{
		QuantityGeneratorTest test = new() { TestCode = $$"""
			using DimensionalQuantities.Units.Declarations;
			{{firstStart}}
			[UnitSymbol("Widget", "Widgets", "w")] class Widget {}
			[UnitProvider] class Provider { [QuantityUnit(typeof(Length))] [UnitFactor(typeof(Widget))] class Distance {} }
			{{firstEnd}}
			{{secondStart}}
			[UnitSymbol("Step", "Steps", "s")] class Step {}
			[UnitProvider] class Provider { [QuantityUnit(typeof(Length))] [UnitFactor(typeof(Step))] class Distance {} }
			{{secondEnd}}
			namespace Consumer {
			    using {{firstNamespace}};
			    using {{secondNamespace}};
			    class Reader { Length First() => Length.FromWidgets(1); Length Second() => Length.FromSteps(1); }
			}
			""" };
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = test.Compilation.SyntaxTrees.First();
		IMethodSymbol[] methods = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Select(call => (IMethodSymbol)test.Compilation.GetSemanticModel(tree).GetSymbolInfo(call, TestContext.Current.CancellationToken).Symbol!).ToArray();
		Assert.Multiple(
			() => Assert.Equal(new[] { "FromWidgets", "FromSteps" }, methods.Select(method => method.Name)),
			() => Assert.Equal(new[] { firstNamespace, secondNamespace }, methods.Select(method => method.ContainingNamespace.ToDisplayString())),
			() => Assert.All(methods, method => Assert.Equal(Accessibility.Public, method.DeclaredAccessibility)));
	}
}
