using System.Globalization;
using DimensionalQuantities.IntegrationTests.Delivery;
using Xunit;

namespace DimensionalQuantities.IntegrationTests;

public sealed class GeneratedExecutionTests
{
	[Theory]
	[InlineData("Length.FromSiBaseUnit(6) * Current.FromSiBaseUnit(2)", 12)]
	[InlineData("Length.FromSiBaseUnit(6) / Current.FromSiBaseUnit(2)", 3)]
	public async Task GeneratedArithmeticComputesSiMagnitude(string expression, double expected)
	{
		using PackageSandbox sandbox = new();
		await sandbox.Pack();
		sandbox.WriteConsumer($"Console.WriteLine(({expression}).AsSiBaseUnit().ToString(\"R\", System.Globalization.CultureInfo.InvariantCulture));", "DimensionalQuantities");
		Assert.Equal(expected, double.Parse(await sandbox.ExecuteConsumer(), CultureInfo.InvariantCulture), 12);
	}

	[Theory]
	[InlineData("NumberHalf", "Area", "9", 3)]
	[InlineData("NumberTwoOverThree", "Volume", "8", 4)]
	[InlineData("NumberMinusOne", "Length", "4", 0.25)]
	public async Task OrdinaryRationalPowerComputesMagnitude(string exponent, string quantity, string value, double expected)
	{
		using PackageSandbox sandbox = new();
		await sandbox.Pack();
		sandbox.WriteConsumer($"using DimensionalQuantities.TypeSystem; Console.WriteLine(Power<{exponent}>.Of({quantity}.FromSiBaseUnit({value})).AsSiBaseUnit().ToString(\"R\", System.Globalization.CultureInfo.InvariantCulture));", "DimensionalQuantities");
		Assert.Equal(expected, double.Parse(await sandbox.ExecuteConsumer(), CultureInfo.InvariantCulture), 12);
	}

	[Theory]
	[InlineData("NumberOneOverThree", "-8", -2)]
	[InlineData("NumberTwoOverThree", "-8", 4)]
	[InlineData("NumberMinusOneOverThree", "-8", -0.5)]
	[InlineData("NumberMinusTwoOverThree", "-8", 0.25)]
	[InlineData("NumberHalf", "-8", double.NaN)]
	[InlineData("NumberOneOverThree", "0", 0)]
	[InlineData("NumberOneOverThree", "double.NegativeInfinity", double.NegativeInfinity)]
	public async Task OddDenominatorPowerHandlesSignAndReciprocal(string exponent, string value, double expected)
	{
		using PackageSandbox sandbox = new();
		await sandbox.Pack();
		sandbox.WriteConsumer($"using DimensionalQuantities.TypeSystem; Console.WriteLine(Power<{exponent}>.Of(Length.FromSiBaseUnit({value})).AsSiBaseUnit().ToString(\"R\", System.Globalization.CultureInfo.InvariantCulture));", "DimensionalQuantities");
		double actual = double.Parse(await sandbox.ExecuteConsumer(), CultureInfo.InvariantCulture);
		if (double.IsNaN(expected))
		{
			Assert.True(double.IsNaN(actual));
		}
		else if (double.IsInfinity(expected))
		{
			Assert.Equal(expected, actual);
		}
		else
		{
			Assert.Equal(expected, actual, 12);
		}
	}

	[Fact]
	public async Task GeneratedCompoundPrefixFormulaWeightsEachFactorPower()
	{
		using PackageSandbox sandbox = new();
		await sandbox.Pack();
		sandbox.WriteConsumer("""
            using DimensionalQuantities.Units.Declarations;
            using DimensionalQuantities.Units.Symbols;
            using ConsumerUnits;
            var value = CurrentDensity.FromPrefixedUnits(2, ampPrefixExponent: 1, metrePrefixExponent: -4);
            Console.WriteLine(value.AsSiBaseUnit().ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            namespace ConsumerUnits
            {
                [UnitProvider(GeneratePrefixConversions = true)]
                internal static class Units
                {
                    [QuantityUnit(typeof(CurrentDensity))]
                    [UnitFactor(typeof(Amp))]
                    [UnitFactor(typeof(Metre), Power = -2)]
                    private sealed class Density;
                }
            }
            """, "DimensionalQuantities");
		Assert.Equal(2e9, double.Parse(await sandbox.ExecuteConsumer(), CultureInfo.InvariantCulture), 6);
	}
}
