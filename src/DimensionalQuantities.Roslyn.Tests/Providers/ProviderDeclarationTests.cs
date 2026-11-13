using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Providers;

public sealed class ProviderDeclarationTests
{
	[Fact]
	public async Task InvalidAssociationDoesNotPublishConversions()
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = """
				using DimensionalQuantities.Units.Declarations;
				using DimensionalQuantities.Units.Symbols;

				namespace Custom
				{
					[UnitProvider]
					class Provider
					{
						[{|#0:QuantityUnit(typeof(string))|}]
						[UnitFactor(typeof(Metre))]
						class Distance
						{
						}
					}
				}
				"""
		};
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("DQ0008").WithLocation(0));
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.DoesNotContain(test.GeneratedSources, tree => tree.ToString().Contains("namespace Custom"));
	}

	[Fact]
	public async Task UnassociatedDeclarationDoesNotPublishConversions()
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = """
				using DimensionalQuantities.Units.Declarations;

				namespace Custom
				{
					[UnitSymbol("Widget", "Widgets", "w")]
					class Widget
					{
					}

					[UnitProvider]
					class Provider
					{
						[UnitFactor(typeof(Widget))]
						class Distance
						{
						}
					}
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.DoesNotContain("FromWidgets", string.Join("\n", test.GeneratedSources));
	}

	[Fact]
	public async Task AdditionalUnitMustBelongToTheProvider()
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = """
				using DimensionalQuantities.Units.Declarations;
				using DimensionalQuantities.Units.Symbols;

				namespace Custom
				{
					class Foreign
					{
					}

					[UnitProvider]
					class Provider
					{
						[QuantityUnit(typeof(Length))]
						[UnitFactor(typeof(Metre))]
						[{|#0:AdditionalUnit(typeof(Foreign))|}]
						class Distance
						{
						}
					}
				}
				"""
		};
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("DQ0008").WithLocation(0));
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.DoesNotContain("FromForeign", string.Join("\n", test.GeneratedSources));
	}

	[Theory]
	[InlineData(1, "FromFeet")]
	[InlineData(2, "FromSquareFeet")]
	[InlineData(3, "FromCubicFeet")]
	[InlineData(4, "FromPower4Feet")]
	[InlineData(-1, "FromInverseFoot")]
	public async Task OrderedPowerNamesUseIrregularPluralOnlyInNumerator(int power, string factory)
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
				using DimensionalQuantities.Units.Declarations;

				namespace Custom;

				[UnitSymbol("Foot", "Feet", "ft")]
				class Foot
				{
				}

				[UnitProvider]
				class Provider
				{
					[QuantityUnit(typeof(Length))]
					[UnitFactor(typeof(Foot), Power = {{power}})]
					class Distance
					{
					}
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.Single(test.GeneratedSources.SelectMany(tree => tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<MethodDeclarationSyntax>()), method => method.Identifier.ValueText == factory);
	}

	[Fact]
	public async Task FixedPrefixAndFactorOrderDetermineCompoundName()
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = """
				using DimensionalQuantities.Units.Declarations;
				using DimensionalQuantities.Units.Symbols;

				namespace Custom;

				[UnitProvider(GenerateSiPrefixes = true, GeneratePrefixConversions = true)]
				class Provider
				{
					[QuantityUnit(typeof(Density))]
					[UnitFactor(typeof(Gram))]
					[UnitFactor(typeof(Metre), Power = -3, PrefixExponent = -2)]
					class DensityUnit
					{
					}
				}

				class Consumer
				{
					Density M() => Density.FromGramsPerCubicCentimetre(1);
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		string source = string.Join("\n", test.GeneratedSources.Where(tree => tree.ToString().Contains("namespace Custom;")));
		Assert.Multiple(
			() => Assert.Contains("FromGramsPerCubicCentimetre(double value)", source),
			() => Assert.Contains("FromKiloGramsPerCubicCentimetre(double value)", source),
			() => Assert.DoesNotContain("FromGramsPerCubicMillimetre", source),
			() => Assert.Contains("int gramPrefixExponent = 0, int metrePrefixExponent = -2", source),
			() => Assert.Contains("((double)gramPrefixExponent - 3) + ((double)metrePrefixExponent) * -3", source),
			() => Assert.Contains("overallScaleExponent: 3d", source));
	}

	[Fact]
	public async Task IdenticalConversionNamesCoalesce()
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = """
				using DimensionalQuantities.Units.Declarations;

				namespace Custom;

				[UnitSymbol("Widget", "Widgets", "w", Scale = 4)]
				class Widget
				{
				}

				[UnitProvider]
				class Provider
				{
					[QuantityUnit(typeof(Length))]
					[UnitFactor(typeof(Widget))]
					[AdditionalUnit(typeof(Alternative))]
					class Distance
					{
					}

					[UnitFactor(typeof(Widget))]
					class Alternative
					{
					}
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.Single(test.GeneratedSources.SelectMany(tree => tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<MethodDeclarationSyntax>()), method => method.Identifier.ValueText == "FromWidgets");
	}

	[Fact]
	public async Task ConflictingSameNameConversionsReportDiagnostic()
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = """
				using DimensionalQuantities.Units.Declarations;

				namespace Custom;

				[UnitSymbol("Widget", "Widgets", "w", Scale = 4)]
				class First
				{
				}

				[UnitSymbol("Widget", "Widgets", "w", Scale = 5)]
				class Second
				{
				}

				[UnitProvider]
				class {|#0:Provider|}
				{
					[QuantityUnit(typeof(Length))]
					[UnitFactor(typeof(First))]
					[AdditionalUnit(typeof(Alternative))]
					class Distance
					{
					}

					[UnitFactor(typeof(Second))]
					class Alternative
					{
					}
				}
				"""
		};
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("DQ0008").WithLocation(0));
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.DoesNotContain("FromWidgets", string.Join("\n", test.GeneratedSources));
	}
}
