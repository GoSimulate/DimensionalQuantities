using DimensionalQuantities.Roslyn.Tests.Framework;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Providers;

public sealed class CompoundFactorTests
{
	[Theory]
	[InlineData("Foot", "Metre", "FromFootMetres")]
	public async Task PositiveFactorsKeepDeclarationOrderAndPluraliseLastNumerator(string first, string second, string factory)
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
				using DimensionalQuantities.Units.Declarations;
				using DimensionalQuantities.Units.Symbols;

				namespace Custom;

				[UnitSymbol("Foot", "Feet", "ft", Scale = 0.3048)]
				class Foot
				{
				}

				[UnitProvider]
				class Provider
				{
					[QuantityUnit(typeof(Area))]
					[UnitFactor(typeof({{first}}))]
					[UnitFactor(typeof({{second}}))]
					class AreaUnit
					{
					}
				}

				class Consumer
				{
					Area M() => Area.{{factory}}(1);
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		string source = string.Join("\n", test.GeneratedSources.Where(tree => tree.ToString().Contains("namespace Custom;")));
		Assert.Multiple(() => Assert.Contains($"{factory}(double value)", source),
			() => Assert.Contains("value * 0.3048d *", source));
	}
}
