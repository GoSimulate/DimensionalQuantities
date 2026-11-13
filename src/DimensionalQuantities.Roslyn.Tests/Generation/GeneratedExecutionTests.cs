using System.Reflection;
using System.Runtime.Loader;
using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generation;

public sealed class GeneratedExecutionTests
{
	[Theory]
	[InlineData("Length.FromSiBaseUnit(6) * Current.FromSiBaseUnit(2)", 12)]
	[InlineData("Length.FromSiBaseUnit(6) / Current.FromSiBaseUnit(2)", 3)]
	public async Task GeneratedArithmeticComputesSiMagnitude(string expression, double expected)
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
				public static class Consumer
				{
					public static double Execute()
					{
						return ({{expression}}).AsSiBaseUnit();
					}
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.Equal(expected, Execute(test.Compilation), 12);
	}

	[Theory]
	[InlineData("NumberHalf", "Area", "9", 3)]
	[InlineData("NumberTwoOverThree", "Volume", "8", 4)]
	[InlineData("NumberMinusOne", "Length", "4", 0.25)]
	public async Task OrdinaryRationalPowerComputesMagnitude(string exponent, string quantity, string value, double expected)
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
				using DimensionalQuantities.TypeSystem;

				public static class Consumer
				{
					public static double Execute()
					{
						return Power<{{exponent}}>.Of({{quantity}}.FromSiBaseUnit({{value}})).AsSiBaseUnit();
					}
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.Equal(expected, Execute(test.Compilation), 12);
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
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
				using DimensionalQuantities.TypeSystem;

				public static class Consumer
				{
					public static double Execute()
					{
						return Power<{{exponent}}>.Of(Length.FromSiBaseUnit({{value}})).AsSiBaseUnit();
					}
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		double actual = Execute(test.Compilation);
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
		QuantityGeneratorTest test = new()
		{
			TestCode = """
				using DimensionalQuantities.Units.Declarations;
				using DimensionalQuantities.Units.Symbols;
				using ConsumerUnits;

				public static class Consumer
				{
					public static double Execute()
					{
						var value = CurrentDensity.FromPrefixedUnits(2, ampPrefixExponent: 1, metrePrefixExponent: -4);
						return value.AsSiBaseUnit();
					}
				}

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
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.Equal(2e9, Execute(test.Compilation), 6);
	}

	private static double Execute(Compilation compilation)
	{
		using MemoryStream assemblyStream = new();
		EmitResult result = compilation.Emit(assemblyStream, cancellationToken: TestContext.Current.CancellationToken);
		if (!result.Success)
		{
			throw new InvalidOperationException(string.Join(Environment.NewLine, result.Diagnostics));
		}
		assemblyStream.Position = 0;

		// Dependencies fall back to the default context, preserving shared runtime type identity.
		AssemblyLoadContext loadContext = new(name: null, isCollectible: true);
		try
		{
			Assembly assembly = loadContext.LoadFromStream(assemblyStream);
			MethodInfo method = assembly.GetType("Consumer", throwOnError: true)!.GetMethod("Execute")!;
			return (double)method.Invoke(null, null)!;
		}
		finally
		{
			loadContext.Unload();
		}
	}
}
