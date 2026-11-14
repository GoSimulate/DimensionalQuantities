using DimensionalQuantities.Roslyn.Tests.Framework;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generation;

public sealed class CompositionPatternTests
{
	[Fact]
	public async Task PropertyPatternVariablesComposeWithArithmetic()
	{
		const string source = """
			public sealed class Measurement
			{
				public LengthCurrent Value => LengthCurrent.FromSiBaseUnit(6);
			}
			public static class Consumer
			{
				private static LengthTimeCurrent Calculate(Measurement input)
				{
					if (input is { Value: var value })
					{
						return value * Time.FromSeconds(4);
					}
					throw new System.Exception();
				}
				public static double Execute() => Calculate(new Measurement()).AsSiBaseUnit();
			}
			""";
		QuantityGeneratorTest test = new()
		{
			TestCode = source
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		double result = GeneratedCodeExecution.Execute(test.Compilation);
		double expected = 24;

		Assert.Equal(expected, result);
	}

	[Fact]
	public async Task ListPatternVariablesComposeWithArithmetic()
	{
		const string source = """
			public static class Consumer
			{
				private static LengthTimeCurrent Calculate(LengthCurrent[] input)
				{
					if (input is [var value])
					{
						return value * Time.FromSeconds(4);
					}
					throw new System.Exception();
				}
				public static double Execute() => Calculate(new[] { LengthCurrent.FromSiBaseUnit(6) }).AsSiBaseUnit();
			}
			""";
		QuantityGeneratorTest test = new()
		{
			TestCode = source
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		double result = GeneratedCodeExecution.Execute(test.Compilation);
		double expected = 24;

		Assert.Equal(expected, result);
	}

	[Fact]
	public async Task CustomDeconstructionResultsComposeWithArithmetic()
	{
		const string source = """
			public sealed class Measurement
			{
				public void Deconstruct(out LengthCurrent value, out Time duration)
				{
					value = LengthCurrent.FromSiBaseUnit(6);
					duration = Time.FromSeconds(4);
				}
			}
			public static class Consumer
			{
				private static LengthTimeCurrent Calculate(Measurement input)
				{
					var (value, duration) = input;
					return value * duration;
				}
				public static double Execute() => Calculate(new Measurement()).AsSiBaseUnit();
			}
			""";
		QuantityGeneratorTest test = new()
		{
			TestCode = source
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		double result = GeneratedCodeExecution.Execute(test.Compilation);
		double expected = 24;

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("""
		if (input is LengthCurrent value)
		{
			return value * Time.FromSeconds(4);
		}
		throw new System.Exception();
		""")]
	[InlineData("""
		return input switch
		{
			LengthCurrent value => value * Time.FromSeconds(4),
			_ => throw new System.Exception()
		};
		""")]
	public async Task UnionCaseVariablesComposeWithArithmetic(string body)
	{
		string source = $$"""
			public union Measurement(LengthCurrent, Time);
			public static class Consumer
			{
				private static LengthTimeCurrent Calculate(Measurement input)
				{
					{{body}}
				}
				public static double Execute() => Calculate(Length.FromMetres(2) * Current.FromAmps(3)).AsSiBaseUnit();
			}
			""";
		QuantityGeneratorTest test = new()
		{
			TestCode = source
		};
		QuantityTestEnvironment.UseCurrentRuntimeReferences(test);
		await test.RunAsync(TestContext.Current.CancellationToken);
		double result = GeneratedCodeExecution.Execute(test.Compilation);
		double expected = 24;

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("""
		switch (input)
		{
			case LengthCurrent value:
				return value * Time.FromSeconds(4);
			default:
				throw new System.Exception();
		}
		""")]
	[InlineData("""
		return input switch
		{
			LengthCurrent value => value * Time.FromSeconds(4),
			_ => throw new System.Exception()
		};
		""")]
	public async Task PatternVariablesComposeWithArithmetic(string body)
	{
		string source = $$"""
			public static class Consumer
			{
				private static LengthTimeCurrent Calculate(object input)
				{
					{{body}}
				}
				public static double Execute() => Calculate(Length.FromMetres(2) * Current.FromAmps(3)).AsSiBaseUnit();
			}
			""";
		QuantityGeneratorTest test = new()
		{
			TestCode = source
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		double result = GeneratedCodeExecution.Execute(test.Compilation);
		double expected = 24;

		Assert.Equal(expected, result);
	}
}
