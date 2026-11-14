using DimensionalQuantities.Roslyn.Tests.Framework;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generation;

public sealed class BindingCompositionTests
{
	[Fact]
	public async Task ConstrainedProviderResultsComposeWithArithmetic()
	{
		const string source = """
			public interface IProvider<T>
			{
				T Get();
			}
			public sealed class Provider : IProvider<LengthCurrent>
			{
				LengthCurrent IProvider<LengthCurrent>.Get() => Length.FromMetres(2) * Current.FromAmps(3);
			}
			public static class Consumer
			{
				private static LengthTimeCurrent Calculate<TProvider>(TProvider provider)
					where TProvider : IProvider<LengthCurrent>
				{
					var value = provider.Get();
					return value * Time.FromSeconds(4);
				}
				public static double Execute() => Calculate(new Provider()).AsSiBaseUnit();
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
	[InlineData("ref", """
		ref var value = ref Get();
		return value * Time.FromSeconds(4);
		""")]
	[InlineData("ref readonly", """
		ref readonly var value = ref Get();
		return value * Time.FromSeconds(4);
		""")]
	public async Task RefReturnsComposeWithArithmetic(string returnModifier, string body)
	{
		string source = $$"""
			public static class Consumer
			{
				private static LengthCurrent stored = LengthCurrent.FromSiBaseUnit(6);
				private static {{returnModifier}} LengthCurrent Get() => ref stored;
				private static LengthTimeCurrent Calculate()
				{
					{{body}}
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
		double expected = 24;

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("ReadOnlySpan")]
	public async Task SpanElementsComposeWithArithmetic(string spanType)
	{
		string source = $$"""
			using System;
			public static class Consumer
			{
				private static LengthTimeCurrent Calculate({{spanType}}<LengthCurrent> values) => values[0] * Time.FromSeconds(4);
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
	public async Task BlockGetterResultsComposeAcrossSourceFiles()
	{
		const string member = """
			public static class Measurements
			{
				public static LengthCurrent Value
				{
					get
					{
						return Length.FromMetres(2) * Current.FromAmps(3);
					}
				}
			}
			""";
		const string consumer = """
			public static class Consumer
			{
				public static LengthTimeCurrent Calculate() => Measurements.Value * Time.FromSeconds(4);
				public static double Execute() => Calculate().AsSiBaseUnit();
			}
			""";
		QuantityGeneratorTest test = new();
		test.TestState.Sources.Add(("Consumer.cs", consumer));
		test.TestState.Sources.Add(("Measurements.cs", member));
		await test.RunAsync(TestContext.Current.CancellationToken);
		double result = GeneratedCodeExecution.Execute(test.Compilation);

		Assert.Equal(24, result);
	}

	[Theory]
	[InlineData("(Length.FromMetres(2) * Current.FromAmps(3)).Squared()", "LengthSquaredTimeCurrentSquared", 144)]
	public async Task PowerResultsComposeThroughInferredDelegates(string expression, string resultType, double expected)
	{
		string source = $$"""
			using DimensionalQuantities.TypeSystem;
			public static class Consumer
			{
				public static {{resultType}} Calculate()
				{
					var make = () => {{expression}};
					return make() * Time.FromSeconds(4);
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

	[Theory]
	[InlineData("""
		public LengthCurrent Make(Length length, Current current) => length * current;
		""")]
	[InlineData("""
		public LengthCurrent Make(Length length, Current current)
		{
			LengthCurrent product = length * current;
			return product;
		}
		""")]
	[InlineData("""
		public LengthCurrent Make(Length length, Current current)
		{
			if (length.AsSiBaseUnit() > 0)
			{
				return length * current;
			}
			return length * current * 2;
		}
		""")]
	[InlineData("""
		public LengthCurrent Make(Length length, Current current)
		{
			if (length.AsSiBaseUnit() < 0)
			{
				throw new System.Exception();
			}
			return length * current;
		}
		""")]
	[InlineData("""
		public LengthCurrent Make(Length length, Current current)
		{
			LengthCurrent Local(Length l, Current c) => l * c;
			return Local(length, current);
		}
		""")]
	[InlineData("""
		public LengthTime Make(Length length, Time time) => length * time;
		public LengthCurrent Make(Length length, Current current) => length * current;
		""")]
	public async Task IntermediateMethodResultsComposeWithArithmetic(string declaration)
	{
		string source = $$"""
			public class Inputs
			{
				{{declaration}}
			}
			public static class Consumer
			{
				public static LengthTimeCurrent Calculate() => new Inputs().Make(Length.FromMetres(2), Current.FromAmps(3)) * Time.FromSeconds(4);
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

	[Fact]
	public async Task ExtensionMethodIntermediateReturnComposesWithArithmetic()
	{
		const string source = """
			public static class Helpers
			{
				public static LengthCurrent Make(this Length length, Current current) => length * current;
			}
			public static class Consumer
			{
				public static LengthTimeCurrent Calculate() => Length.FromMetres(2).Make(Current.FromAmps(3)) * Time.FromSeconds(4);
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
	[InlineData("""
		public LengthCurrent Get() => Length.FromMetres(2) * Current.FromAmps(3);
		""")]
	[InlineData("""
		LengthCurrent IProvider<LengthCurrent>.Get() => Length.FromMetres(2) * Current.FromAmps(3);
		""")]
	public async Task GenericInterfaceResultsComposeWithArithmetic(string implementation)
	{
		string source = $$"""
			public interface IProvider<T>
			{
				T Get();
			}
			public sealed class Provider : IProvider<LengthCurrent>
			{
				{{implementation}}
			}
			public static class Consumer
			{
				private static LengthTimeCurrent Calculate(IProvider<LengthCurrent> provider)
				{
					var value = provider.Get();
					return value * Time.FromSeconds(4);
				}
				public static double Execute() => Calculate(new Provider()).AsSiBaseUnit();
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
	public async Task ForeachArithmeticOverTypedInputRetainsItsResultDimension()
	{
		const string source = """
			using System.Collections.Generic;
			public static class Consumer
			{
				public static LengthTimeCurrent Calculate(IEnumerable<LengthCurrent> values)
				{
					LengthTimeCurrent total = default;
					foreach (var value in values)
					{
						total = total + value * Time.FromSeconds(4);
					}
					return total;
				}
				public static double Execute() => Calculate(new[] { LengthCurrent.FromSiBaseUnit(2), LengthCurrent.FromSiBaseUnit(3) }).AsSiBaseUnit();
			}
			""";
		QuantityGeneratorTest test = new()
		{
			TestCode = source
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		double result = GeneratedCodeExecution.Execute(test.Compilation);

		Assert.Equal(20, result);
	}
}
