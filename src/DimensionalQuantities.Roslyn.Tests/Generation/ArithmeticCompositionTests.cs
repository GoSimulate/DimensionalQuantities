using DimensionalQuantities.Roslyn.Tests.Framework;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generation;

public sealed class ArithmeticCompositionTests
{
	[Fact]
	public async Task ExistingMultiplicationOperatorKeepsItsBehaviour()
	{
		const string source = """
			public static class CustomOperators
			{
				extension(Length)
				{
					public static double operator *(Length length, Current current) => 42;
				}
			}
			public static class Consumer
			{
				public static Time Calculate() => (Length.FromMetres(2) * Current.FromAmps(3)) * Time.FromSeconds(4);
				public static double Execute() => Calculate().AsSiBaseUnit();
			}
			""";
		QuantityGeneratorTest test = new()
		{
			TestCode = source
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		double result = GeneratedCodeExecution.Execute(test.Compilation);

		Assert.Equal(168, result);
	}

	[Fact]
	public async Task LongDependentExpressionsCompleteWithoutAnArbitrarySmallPassLimit()
	{
		const string source = """
			public static class Consumer
			{
				public static LengthCurrent20 Calculate() =>
					Length.FromMetres(2)
					* Current.FromAmps(1)
					* Current.FromAmps(1)
					* Current.FromAmps(1)
					* Current.FromAmps(1)
					* Current.FromAmps(1)
					* Current.FromAmps(1)
					* Current.FromAmps(1)
					* Current.FromAmps(1)
					* Current.FromAmps(1)
					* Current.FromAmps(1)
					* Current.FromAmps(1)
					* Current.FromAmps(1)
					* Current.FromAmps(1)
					* Current.FromAmps(1)
					* Current.FromAmps(1)
					* Current.FromAmps(1)
					* Current.FromAmps(1)
					* Current.FromAmps(1)
					* Current.FromAmps(1)
					* Current.FromAmps(1);
				public static double Execute() => Calculate().AsSiBaseUnit();
			}
			""";
		QuantityGeneratorTest test = new()
		{
			TestCode = source
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		double result = GeneratedCodeExecution.Execute(test.Compilation);

		Assert.Equal(2, result);
	}

	[Theory]
	[InlineData("""
		var squared = current * current;
		var heating = squared * resistance;
		""", "Power", "heating", 36)]
	[InlineData("""
		var squared = current * current;
		var copy = squared;
		var heating = copy * resistance;
		var energy = heating * time;
		""", "Energy", "energy", 72)]
	[InlineData("""
		var squared = (2 * current) * current / 2;
		var heating = squared * resistance;
		""", "Power", "heating", 36)]
	[InlineData("""
		var squared = current * current;
		var fourth = squared * squared;
		""", "Current4", "fourth", 81)]
	public async Task ArithmeticThroughLocalInitialisersCompilesAndExecutes(string declarations, string resultType, string resultName, double expected)
	{
		string source = $$"""
			public static class Consumer
			{
				public static {{resultType}} Calculate()
				{
					Current current = Current.FromAmps(3);
					Resistance resistance = Resistance.FromSiBaseUnit(4);
					Time time = Time.FromSeconds(2);
					{{declarations}}
					return {{resultName}};
				}
				public static double Execute() => Calculate().AsSiBaseUnit();
			}
			""";
		QuantityGeneratorTest test = new()
		{
			TestCode = source
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		double result = GeneratedCodeExecution.Execute(test.Compilation);

		Assert.Equal(expected, result);
	}

	[Fact]
	public async Task IdenticallyNamedLocalsInSeparateScopesRetainTheirOwnDimensions()
	{
		const string source = """
			public static class Consumer
			{
				public static double Execute()
				{
					Current current = Current.FromAmps(3);
					Resistance resistance = Resistance.FromSiBaseUnit(4);
					Time time = Time.FromSeconds(2);
					double first;
					double second;
					{
						var intermediate = current * current;
						var result = intermediate * resistance;
						Power typedResult = result;
						first = typedResult.AsSiBaseUnit();
					}
					{
						var intermediate = time * time;
						var result = intermediate * current;
						TimeSquaredCurrent typedResult = result;
						second = typedResult.AsSiBaseUnit();
					}
					return first + second;
				}
			}
			""";
		QuantityGeneratorTest test = new()
		{
			TestCode = source
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		double result = GeneratedCodeExecution.Execute(test.Compilation);
		double expected = 48;

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("""
		LengthCurrent value = length * current;
		""")]
	[InlineData("""
		LengthCurrent first = length * current;
		LengthCurrent value = first;
		""")]
	[InlineData("""
		LengthCurrent first = length * current;
		var value = first;
		""")]
	public async Task ExplicitIntermediateLocalsComposeWithFurtherArithmetic(string declarations)
	{
		string source = $$"""
			public static class Consumer
			{
				public static LengthTimeCurrent Calculate()
				{
					Length length = Length.FromMetres(2);
					Current current = Current.FromAmps(3);
					Time time = Time.FromSeconds(4);
					{{declarations}}
					return value * time;
				}
				public static double Execute() => Calculate().AsSiBaseUnit();
			}
			""";
		QuantityGeneratorTest test = new()
		{
			TestCode = source
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		double result = GeneratedCodeExecution.Execute(test.Compilation);

		Assert.Equal(24, result);
	}

	[Theory]
	[InlineData("choose ? length * current : length * current")]
	[InlineData("choose ? length * current : throw new System.Exception()")]
	[InlineData("""
		choose switch
		{
			true => length * current,
			false => length * current
		}
		""")]
	[InlineData("""
		choose switch
		{
			true => length * current,
			_ => throw new System.Exception()
		}
		""")]
	[InlineData("""
		choose
			? (choose switch
			{
				true => length * current,
				_ => length * current
			})
			: length * current
		""")]
	public async Task ConditionalIntermediateResultsComposeWithArithmetic(string expression)
	{
		string source = $$"""
			public static class Consumer
			{
				public static LengthTimeCurrent Calculate()
				{
					bool choose = true;
					Length length = Length.FromMetres(2);
					Current current = Current.FromAmps(3);
					Time time = Time.FromSeconds(4);
					var product = {{expression}};
					return product * time;
				}
				public static double Execute() => Calculate().AsSiBaseUnit();
			}
			""";
		QuantityGeneratorTest test = new()
		{
			TestCode = source
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		double result = GeneratedCodeExecution.Execute(test.Compilation);

		Assert.Equal(24, result);
	}
}
