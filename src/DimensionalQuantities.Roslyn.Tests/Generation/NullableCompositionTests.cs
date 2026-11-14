using DimensionalQuantities.Roslyn.Tests.Framework;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generation;

public sealed class NullableCompositionTests
{
	[Theory]
	[InlineData("Length.FromMetres(6)", 18)]
	[InlineData("null", -1)]
	public async Task NullableArithmeticUsesOperatorDiscoveredFromNonNullableOperands(string leftValue, double expectedValue)
	{
		string source = $$"""
			public static class Consumer
			{
				public static LengthCurrent? Calculate()
				{
					LengthCurrent seed = Length.FromMetres(1) * Current.FromAmps(1);
					Length? left = {{leftValue}};
					Current right = Current.FromAmps(3);
					return left * right;
				}
				public static double Execute() => Calculate()?.AsSiBaseUnit() ?? -1;
			}
			""";
		QuantityGeneratorTest test = new()
		{
			TestCode = source
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		double result = GeneratedCodeExecution.Execute(test.Compilation);

		Assert.Equal(expectedValue, result);
	}

	[Theory]
	[InlineData("Length.FromMetres(3)", 9)]
	[InlineData("null", -1)]
	public async Task BaseNullablePowerKeepsPregeneratedSquaredMethod(string value, double expectedValue)
	{
		string source = $$"""
			public static class Consumer
			{
				public static Area? Calculate()
				{
					Length? value = {{value}};
					return value?.Squared();
				}
				public static double Execute() => Calculate()?.AsSiBaseUnit() ?? -1;
			}
			""";
		QuantityGeneratorTest test = new()
		{
			TestCode = source
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		double result = GeneratedCodeExecution.Execute(test.Compilation);

		Assert.Equal(expectedValue, result);
	}

	[Theory]
	[InlineData("(value?.Squared())?.SquareRoot().AsSiBaseUnit()", 6)]
	public async Task NullablePowerDiscoveryHandlesConditionalAccessChains(string expression, double expectedValue)
	{
		string source = $$"""
			public static class Consumer
			{
				public static double Execute()
				{
					LengthCurrent? value = Length.FromMetres(2) * Current.FromAmps(3);
					return {{expression}} ?? -1;
				}
			}
			""";
		QuantityGeneratorTest test = new()
		{
			TestCode = source
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		double result = GeneratedCodeExecution.Execute(test.Compilation);

		Assert.Equal(expectedValue, result);
	}

	[Theory]
	[InlineData("Length?", "Current", "Length.FromMetres(6)", "Current.FromAmps(3)", 2)]
	[InlineData("Length", "Current?", "Length.FromMetres(6)", "Current.FromAmps(3)", 2)]
	[InlineData("Length?", "Current?", "Length.FromMetres(6)", "Current.FromAmps(3)", 2)]
	[InlineData("Length?", "Current", "null", "Current.FromAmps(3)", -1)]
	[InlineData("Length", "Current?", "Length.FromMetres(6)", "null", -1)]
	[InlineData("Length?", "Current?", "Length.FromMetres(6)", "null", -1)]
	public async Task NullableDivisionDiscoveryPreservesNullableResults(string leftType, string rightType, string leftValue, string rightValue, double expectedValue)
	{
		string source = $$"""
			public static class Consumer
			{
				public static LengthPerCurrent? Calculate()
				{
					{{leftType}} left = {{leftValue}};
					{{rightType}} right = {{rightValue}};
					return left / right;
				}
				public static double Execute() => Calculate()?.AsSiBaseUnit() ?? -1;
			}
			""";
		QuantityGeneratorTest test = new()
		{
			TestCode = source
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		double result = GeneratedCodeExecution.Execute(test.Compilation);

		Assert.Equal(expectedValue, result);
	}

	[Theory]
	[InlineData("Time?", "6d", "Time.FromSeconds(3)", 2)]
	[InlineData("Time?", "null", "Time.FromSeconds(3)", -1)]
	[InlineData("Time?", "6d", "null", -1)]
	[InlineData("Time?", "null", "null", -1)]
	[InlineData("Time", "6d", "Time.FromSeconds(3)", 2)]
	[InlineData("Time", "null", "Time.FromSeconds(3)", -1)]
	public async Task NullableScalarNumeratorDiscoveryPreservesNullableResults(string timeType, string scalarValue, string timeValue, double expectedValue)
	{
		string source = $$"""
			public static class Consumer
			{
				public static Frequency? Calculate()
				{
					double? scalar = {{scalarValue}};
					{{timeType}} time = {{timeValue}};
					return scalar / time;
				}
				public static double Execute() => Calculate()?.AsSiBaseUnit() ?? -1;
			}
			""";
		QuantityGeneratorTest test = new()
		{
			TestCode = source
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		double result = GeneratedCodeExecution.Execute(test.Compilation);

		Assert.Equal(expectedValue, result);
	}
}
