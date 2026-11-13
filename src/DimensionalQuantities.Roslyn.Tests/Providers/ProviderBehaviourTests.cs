using System.Globalization;
using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Providers;

public sealed class ProviderBehaviourTests
{
	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task ProviderOptionsControlNamedAndParameterisedPrefixes(bool named, bool parameterised)
	{
		QuantityGeneratorTest test = new() { TestCode = $$"""
            using DimensionalQuantities.Units.Declarations;
            using DimensionalQuantities.Units.Symbols;
            namespace Custom;
            [UnitProvider(GenerateSiPrefixes = {{named.ToString().ToLowerInvariant()}}, GeneratePrefixConversions = {{parameterised.ToString().ToLowerInvariant()}})]
            internal static class Provider
            {
                [QuantityUnit(typeof(Length))] [UnitFactor(typeof(Metre))]
                private sealed class Distance {}
            }
            """ };
		await test.RunAsync(TestContext.Current.CancellationToken);
		string source = string.Join("\n", test.GeneratedSources.Where(tree => tree.ToString().Contains("namespace Custom;")));
		Assert.Multiple(
			() => Assert.Contains("public static class", source),
			() => Assert.Contains("FromMetres(double value)", source),
			() => Assert.Equal(named, source.Contains("FromKiloMetres(double value)")),
			() => Assert.Equal(parameterised, source.Contains("FromPrefixedUnits(double value, int metrePrefixExponent = 0)")));
		if (named)
		{
			Assert.Multiple(
				() => Assert.Contains("FromQuettaMetres", source),
				() => Assert.Contains("FromQuectoMetres", source),
				() => Assert.Contains("overallScaleExponent: 30", source),
				() => Assert.Contains("overallScaleExponent: -30", source));
		}
	}

	[Fact]
	public async Task UnmarkedProviderDoesNotPublishConversions()
	{
		QuantityGeneratorTest test = new() { TestCode = "using DimensionalQuantities.Units.Declarations; using DimensionalQuantities.Units.Symbols; namespace Custom { static class Provider { [QuantityUnit(typeof(Length))] [UnitFactor(typeof(Metre))] class Distance {} } }" };
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.DoesNotContain(test.GeneratedSources, tree => tree.ToString().Contains("namespace Custom"));
	}

	[Theory]
	[InlineData(1, 1, "FromWidgets", "4")]
	[InlineData(1, 2, "FromSquareRootWidgets", "2")]
	[InlineData(-1, 2, "FromInverseSquareRootWidget", "0.5")]
	public async Task CustomScaleAndRationalPowersEmitIndependentExpectedScale(int numerator, int denominator, string factory, string expectedScale)
	{
		QuantityGeneratorTest test = new() { TestCode = $$"""
            using DimensionalQuantities.Units.Declarations;
            namespace Custom;
            [UnitSymbol("Widget", "Widgets", "w", Scale = 4)] class Widget {}
            [UnitProvider] static class Provider
            {
                [QuantityUnit(typeof(Length))]
                [UnitFactor(typeof(Widget), Power = {{numerator}}, PowerDenominator = {{denominator}})]
                class Distance {}
            }
            """ };
		await test.RunAsync(TestContext.Current.CancellationToken);
		string source = string.Join("\n", test.GeneratedSources);
		Assert.Multiple(
			() => Assert.Contains($"{factory}(double value)", source),
			() => Assert.Contains($"value * {expectedScale}d * global::System.Math.Pow(10,", source));
	}

	[Fact]
	public async Task AlternativesShareTheAssociatedReceiver()
	{
		QuantityGeneratorTest test = new() { TestCode = """
            using DimensionalQuantities.Units.Declarations;
            namespace Custom;
            [UnitSymbol("Foot", "Feet", "ft", Scale = 0.3048)] class Foot {}
            [UnitSymbol("Inch", "Inches", "in", Scale = 0.0254)] class Inch {}
            [UnitProvider] static class Provider
            {
                [QuantityUnit(typeof(Length))] [UnitFactor(typeof(Foot))] [AdditionalUnit(typeof(Other))]
                class Distance {}
                [UnitFactor(typeof(Inch))] class Other {}
            }
            class Consumer { Length M() => Length.FromInches(6); }
            """ };
		await test.RunAsync(TestContext.Current.CancellationToken);
		string source = string.Join("\n", test.GeneratedSources);
		Assert.Multiple(
			() => Assert.Contains("FromFeet(double value)", source),
			() => Assert.Contains("FromInches(double value)", source),
			() => Assert.Contains("value * 0.0254d *", source),
			() => Assert.Contains("value * 0.3048d *", source));
	}

	[Fact]
	public async Task SynonymousAssociationsProduceOneFactory()
	{
		QuantityGeneratorTest test = new() { TestCode = """
            using DimensionalQuantities.Units.Declarations;
            namespace Custom;
            [UnitSymbol("Work", "Works", "w")] class Work {}
            [UnitProvider] static class Provider
            {
                [QuantityUnit(typeof(Energy))] [QuantityUnit(typeof(Torque))] [UnitFactor(typeof(Work))]
                class WorkUnit {}
            }
            class Consumer { Torque M() => Energy.FromWorks(2); }
            """ };
		await test.RunAsync(TestContext.Current.CancellationToken);
		MethodDeclarationSyntax[] factories = test.GeneratedSources.SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()).Where(method => method.Identifier.ValueText == "FromWorks").ToArray();
		Assert.Multiple(
			() => Assert.Single(factories),
			() => Assert.Contains("public static", factories[0].Modifiers.ToFullString()));
	}

	[Theory]
	[InlineData("FromSiBaseUnit((value + 10d) * (2d * global::System.Math.Pow(10, 0d)))")]
	[InlineData("reading.AsSiBaseUnit() / (2d * global::System.Math.Pow(10, 0d)) - 10d;")]
	[InlineData("delta.AsSiBaseUnit() / (2d * global::System.Math.Pow(10, 0d));")]
	public async Task AffineConversionEmitsExpectedFormula(string formula)
	{
		QuantityGeneratorTest test = new() { TestCode = AffineProviderSource };
		await test.RunAsync(TestContext.Current.CancellationToken);
		string source = string.Join("\n", test.GeneratedSources.Where(tree => tree.ToString().Contains("namespace Custom;")));
		Assert.Contains(formula, source);
	}

	[Theory]
	[InlineData("FromKiloDegrees")]
	[InlineData("FromPrefixedUnits")]
	public async Task AffineConversionDoesNotPublishPrefixFamily(string method)
	{
		QuantityGeneratorTest test = new() { TestCode = AffineProviderSource };
		await test.RunAsync(TestContext.Current.CancellationToken);
		string source = string.Join("\n", test.GeneratedSources.Where(tree => tree.ToString().Contains("namespace Custom;")));
		Assert.DoesNotContain(method, source);
	}

	private const string AffineProviderSource = """
        using DimensionalQuantities.Units.Declarations;
        namespace Custom;
        [UnitSymbol("Degree", "Degrees", "d", Scale = 2)] [AffineUnit(10)] class Degree {}
        [UnitProvider(GenerateSiPrefixes = true, GeneratePrefixConversions = true)] static class Provider
        {
            [QuantityUnit(typeof(Temperature))] [UnitFactor(typeof(Degree))] class Reading {}
        }
        """;

	[Theory]
	[InlineData("fr-FR", 0.00000000000000000004, "4E-20")]
	[InlineData("de-DE", 4e20, "4E+20")]
	public async Task NumericalLiteralsRemainInvariant(string culture, double scale, string literal)
	{
		CultureInfo previous = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
			QuantityGeneratorTest test = new() { TestCode = $$"""
                using DimensionalQuantities.Units.Declarations;
                namespace Custom;
                [UnitSymbol("Unit", "Units", "u", Scale = {{scale.ToString("R", CultureInfo.InvariantCulture)}})] class Symbol {}
                [UnitProvider] static class Provider { [QuantityUnit(typeof(Length))] [UnitFactor(typeof(Symbol))] class LengthUnit {} }
                """ };
			await test.RunAsync(TestContext.Current.CancellationToken);
			Assert.Multiple(
				() => Assert.Contains($"value * {literal}d *", string.Join("\n", test.GeneratedSources)),
				() => Assert.Contains(test.GeneratedSources.SelectMany(tree => tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<LiteralExpressionSyntax>()), expression => expression.Token.Value is double value && value == scale));
		}
		finally { CultureInfo.CurrentCulture = previous; }
	}
}
