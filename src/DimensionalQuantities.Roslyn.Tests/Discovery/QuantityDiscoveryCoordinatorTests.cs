using System.Reflection;
using Assembly = System.Reflection.Assembly;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.Roslyn.Generators;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace DimensionalQuantities.Roslyn.Tests.Discovery;

[NotInParallel(nameof(QuantityDiscoveryCoordinatorTests))]
public sealed class QuantityDiscoveryCoordinatorTests
{
	private static readonly CSharpParseOptions _parseOptions = new(LanguageVersion.Preview);

	[Test]
	[Arguments("var make = () => length * current; return make() * time;")]
	[Arguments("var pair = (length * current, time); return pair.Item1 * pair.Item2;")]
	[Arguments("var (value, duration) = (length * current, time); return value * duration;")]
	[Arguments("return Identity(length * current) * time;")]
	[Arguments("LengthCurrent? value = null; var fallback = length * current; return (value ?? fallback) * time;")]
	[Arguments("object value = length * current; return ((LengthCurrent)value) * time;")]
	[Arguments("var values = new[] { length }.Select(item => item * current); return values.First() * time;")]
	[Arguments("var values = new[] { length * current }; LengthTimeCurrent result = default; foreach (var value in values) { result += value * time; } return result;")]
	public async Task IntermediateResultsComposeThroughOrdinaryCSharp(string body)
	{
		string source = $$"""
			using System.Linq;
			public static class Consumer
			{
				private static T Identity<T>(T value) => value;
				public static LengthTimeCurrent Calculate()
				{
					Length length = Length.FromMetres(2);
					Current current = Current.FromAmps(3);
					Time time = Time.FromSeconds(4);
					{{body}}
				}
				public static double Value() => Calculate().AsSiBaseUnit();
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);
		double expected = 24;

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
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
				public static double Value() => Calculate(new[] { LengthCurrent.FromSiBaseUnit(2), LengthCurrent.FromSiBaseUnit(3) }).AsSiBaseUnit();
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);

		await Assert.That(result).IsEqualTo(20);
	}

	[Test]
	public async Task ConstrainedProviderResultsComposeWithArithmetic()
	{
		const string source = """
			public interface IProvider<T> { T Get(); }
			public sealed class Provider : IProvider<LengthCurrent>
			{
				LengthCurrent IProvider<LengthCurrent>.Get() => Length.FromMetres(2) * Current.FromAmps(3);
			}
			public static class Consumer
			{
				private static LengthTimeCurrent Calculate<TProvider>(TProvider provider) where TProvider : IProvider<LengthCurrent>
				{
					var value = provider.Get();
					return value * Time.FromSeconds(4);
				}
				public static double Value() => Calculate(new Provider()).AsSiBaseUnit();
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);
		double expected = 24;

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task OutVariablesComposeWithArithmetic()
	{
		const string source = """
			public static class Consumer
			{
				private static bool TryGet(out LengthCurrent value)
				{
					value = LengthCurrent.FromSiBaseUnit(6);
					return true;
				}
				private static LengthTimeCurrent Calculate()
				{
					if (TryGet(out var value)) return value * Time.FromSeconds(4);
					throw new System.Exception();
				}
				public static double Value() => Calculate().AsSiBaseUnit();
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);
		double expected = 24;

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	[Arguments("ref", "return Get() * Time.FromSeconds(4);")]
	[Arguments("ref", "ref var value = ref Get(); return value * Time.FromSeconds(4);")]
	[Arguments("ref readonly", "ref readonly var value = ref Get(); return value * Time.FromSeconds(4);")]
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
				public static double Value() => Calculate().AsSiBaseUnit();
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);
		double expected = 24;

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	[Arguments("Span")]
	[Arguments("ReadOnlySpan")]
	public async Task SpanElementsComposeWithArithmetic(string spanType)
	{
		string source = $$"""
			using System;
			public static class Consumer
			{
				private static LengthTimeCurrent Calculate({{spanType}}<LengthCurrent> values) => values[0] * Time.FromSeconds(4);
				public static double Value() => Calculate(new[] { LengthCurrent.FromSiBaseUnit(6) }).AsSiBaseUnit();
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);
		double expected = 24;

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
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
					if (input is { Value: var value }) return value * Time.FromSeconds(4);
					throw new System.Exception();
				}
				public static double Value() => Calculate(new Measurement()).AsSiBaseUnit();
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);
		double expected = 24;

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task ListPatternVariablesComposeWithArithmetic()
	{
		const string source = """
			public static class Consumer
			{
				private static LengthTimeCurrent Calculate(LengthCurrent[] input)
				{
					if (input is [var value]) return value * Time.FromSeconds(4);
					throw new System.Exception();
				}
				public static double Value() => Calculate(new[] { LengthCurrent.FromSiBaseUnit(6) }).AsSiBaseUnit();
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);
		double expected = 24;

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
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
				public static double Value() => Calculate(new Measurement()).AsSiBaseUnit();
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);
		double expected = 24;

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	[Arguments("public LengthCurrent Get() => Length.FromMetres(2) * Current.FromAmps(3);")]
	[Arguments("LengthCurrent IProvider<LengthCurrent>.Get() => Length.FromMetres(2) * Current.FromAmps(3);")]
	public async Task GenericInterfaceResultsComposeWithArithmetic(string implementation)
	{
		string source = $$"""
			public interface IProvider<T> { T Get(); }
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
				public static double Value() => Calculate(new Provider()).AsSiBaseUnit();
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);
		double expected = 24;

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	[Arguments("if (input is LengthCurrent value) return value * Time.FromSeconds(4); throw new System.Exception();")]
	[Arguments("switch (input) { case LengthCurrent value: return value * Time.FromSeconds(4); default: throw new System.Exception(); }")]
	[Arguments("return input switch { LengthCurrent value => value * Time.FromSeconds(4), _ => throw new System.Exception() };")]
	public async Task PatternVariablesComposeWithArithmetic(string body)
	{
		string source = $$"""
			public static class Consumer
			{
				private static LengthTimeCurrent Calculate(object input)
				{
					{{body}}
				}
				public static double Value() => Calculate(Length.FromMetres(2) * Current.FromAmps(3)).AsSiBaseUnit();
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);
		double expected = 24;

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task IteratorResultsComposeWithArithmetic()
	{
		const string source = """
			using System.Collections.Generic;
			public static class Consumer
			{
				private static IEnumerable<LengthCurrent> Values()
				{
					yield return Length.FromMetres(2) * Current.FromAmps(3);
					yield return Length.FromMetres(3) * Current.FromAmps(3);
				}
				private static LengthTimeCurrent Calculate()
				{
					LengthTimeCurrent total = default;
					foreach (var value in Values())
					{
						total += value * Time.FromSeconds(4);
					}
					return total;
				}
				public static double Value() => Calculate().AsSiBaseUnit();
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);
		double expected = 60;

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	[Arguments("if (input is LengthCurrent value) return value * Time.FromSeconds(4); throw new System.Exception();")]
	[Arguments("return input switch { LengthCurrent value => value * Time.FromSeconds(4), _ => throw new System.Exception() };")]
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
				public static double Value() => Calculate(Length.FromMetres(2) * Current.FromAmps(3)).AsSiBaseUnit();
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);
		double expected = 24;

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task IncompatibleUnionCasesDoNotSupplyDimensionsToArithmetic()
	{
		const string source = """
			public union Measurement(LengthCurrent, Time);
			public static class Consumer
			{
				public static object Calculate(Measurement input, Current current)
				{
					var value = input switch
					{
						LengthCurrent product => product,
						Time duration => duration,
						_ => throw new System.Exception()
					};
					return value * current;
				}
			}
			""";
		QuantityDiscoveryResult result = Discover(source);
		Diagnostic[] errors = result.Compilation.GetDiagnostics(TestContext.Current!.Execution.CancellationToken)
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

		using (Assert.Multiple())
		{
			await Assert.That(errors.Any(diagnostic => diagnostic.Id == "CS8506")).IsTrue();
			await Assert.That(result.Sources.Any(source => source.HintName == "Arithmetic.g.cs")).IsFalse();
		}
	}

	[Test]
	public async Task AsyncIteratorResultsComposeWithArithmetic()
	{
		const string source = """
			using System.Collections.Generic;
			using System.Threading.Tasks;
			public static class Consumer
			{
				private static async IAsyncEnumerable<LengthCurrent> Values()
				{
					await Task.Yield();
					yield return Length.FromMetres(2) * Current.FromAmps(3);
					await Task.Yield();
					yield return Length.FromMetres(3) * Current.FromAmps(3);
				}
				private static async Task<LengthTimeCurrent> Calculate()
				{
					LengthTimeCurrent total = default;
					await foreach (var value in Values())
					{
						total += value * Time.FromSeconds(4);
					}
					return total;
				}
				public static double Value() => Calculate().GetAwaiter().GetResult().AsSiBaseUnit();
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);
		double expected = 60;

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task AwaitedIntermediateResultsComposeWithArithmetic()
	{
		const string source = """
			using System.Threading.Tasks;
			public static class Consumer
			{
				private static async Task<LengthCurrent> Make()
				{
					await Task.Yield();
					return Length.FromMetres(2) * Current.FromAmps(3);
				}
				private static async Task<LengthTimeCurrent> Calculate() => (await Make()) * Time.FromSeconds(4);
				public static double Value() => Calculate().GetAwaiter().GetResult().AsSiBaseUnit();
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);

		await Assert.That(result).IsEqualTo(24);
	}

	[Test]
	public async Task BlockGetterResultsComposeAcrossSourceFiles()
	{
		const string member = """
			public static class Measurements
			{
				public static LengthCurrent Value
				{
					get { return Length.FromMetres(2) * Current.FromAmps(3); }
				}
			}
			""";
		const string consumer = """
			public static class Consumer
			{
				public static LengthTimeCurrent Calculate() => Measurements.Value * Time.FromSeconds(4);
				public static double Value() => Calculate().AsSiBaseUnit();
			}
			""";
		QuantityDiscoveryResult discovery = Discover(consumer, member);
		double result = Evaluate(discovery.Compilation);

		await Assert.That(result).IsEqualTo(24);
	}

	[Test]
	[Arguments("new LengthCurrent()")]
	[Arguments("default(LengthCurrent)")]
	[Arguments("Make()")]
	public async Task ExplicitIntermediateTypesSupplyDimensionsWithoutInitialisers(string expression)
	{
		string source = $$"""
			public static class Consumer
			{
				private static LengthCurrent Make() => default;
				public static LengthTimeCurrent Calculate() => {{expression}} * Time.FromSeconds(4);
				public static double Value() => Calculate().AsSiBaseUnit();
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);

		await Assert.That(result).IsEqualTo(0);
	}

	[Test]
	[Arguments("FromSiBaseUnit(6000)")]
	[Arguments("FromPrefixedUnits(6, 3, 0)")]
	public async Task StandaloneCanonicalFactoryRequestsAQuantityAndItsConversions(string factory)
	{
		string source = $$"""
			public static class Consumer
			{
				public static double Value() => LengthCurrent.{{factory}}.AsPrefixedUnits(3, 0);
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);

		await Assert.That(result).IsEqualTo(6);
	}

	[Test]
	[Arguments("(Length.FromMetres(2) * Current.FromAmps(3)).Squared()", "LengthSquaredTimeCurrentSquared", 144)]
	[Arguments("Power<NumberTwo>.Of(Length.FromMetres(2) * Current.FromAmps(3))", "LengthSquaredTimeCurrentSquared", 144)]
	[Arguments("Power<NumberHalf>.Of(Length.FromMetres(3) * Current.FromAmps(3))", "SquareRootLengthTimeSquareRootCurrent", 12)]
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
				public static double Value() => Calculate().AsSiBaseUnit();
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
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
				public static double Value() => Calculate().AsSiBaseUnit();
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);

		await Assert.That(result).IsEqualTo(168);
	}

	[Test]
	public async Task ExistingConvenienceMethodKeepsItsBehaviourAfterItsReceiverResolves()
	{
		const string source = """
			public static class CustomMethods
			{
				public static Time Squared(this LengthCurrent value) => Time.FromSeconds(7);
			}
			public static class Consumer
			{
				public static TimeCurrent Calculate() => (Length.FromMetres(2) * Current.FromAmps(3)).Squared() * Current.FromAmps(3);
				public static double Value() => Calculate().AsSiBaseUnit();
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);

		await Assert.That(result).IsEqualTo(21);
	}

	[Test]
	[Arguments("wrapper + wrapper")]
	[Arguments("wrapper - wrapper")]
	[Arguments("-wrapper")]
	public async Task BoundOperatorResultsComposeWithDimensionalArithmetic(string expression)
	{
		string source = $$"""
			public sealed class Wrapper
			{
				public static Current operator +(Wrapper left, Wrapper right) => Current.FromAmps(3);
				public static Current operator -(Wrapper left, Wrapper right) => Current.FromAmps(3);
				public static Current operator -(Wrapper value) => Current.FromAmps(3);
			}
			public static class Consumer
			{
				public static TimeCurrent Calculate()
				{
					Wrapper wrapper = new();
					return ({{expression}}) * Time.FromSeconds(4);
				}
				public static double Value() => Calculate().AsSiBaseUnit();
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);

		await Assert.That(result).IsEqualTo(12);
	}

	[Test]
	[Arguments("choose ? length * current : length * time", "CS0173")]
	[Arguments("choose switch { true => length * current, false => length * time }", "CS8506")]
	public async Task ConflictingBranchesDoNotSupplyDimensionsToFurtherArithmetic(string expression, string expectedDiagnostic)
	{
		string source = $$"""
			public static class Consumer
			{
				public static object Value(bool choose, Length length, Current current, Time time)
				{
					var value = {{expression}};
					return value * current;
				}
			}
			""";
		QuantityDiscoveryResult result = Discover(source);
		Diagnostic[] errors = result.Compilation.GetDiagnostics(TestContext.Current!.Execution.CancellationToken)
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

		using (Assert.Multiple())
		{
			await Assert.That(errors.Any(diagnostic => diagnostic.Id == expectedDiagnostic)).IsTrue();
			await Assert.That(result.Compilation.Assembly.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentSquaredExtensions")).IsNull();
			await Assert.That(result.Compilation.Assembly.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeCurrentExtensions")).IsNull();
		}
	}

	[Test]
	public async Task AmbiguousMethodCallsRemainCompilerErrors()
	{
		const string source = """
			public static class Consumer
			{
				private static LengthCurrent Make(int first, long second) => default;
				private static LengthTime Make(long first, int second) => default;
				public static object Value(Current current) => Make(1, 1) * current;
			}
			""";
		QuantityDiscoveryResult result = Discover(source);
		Diagnostic[] errors = result.Compilation.GetDiagnostics(TestContext.Current!.Execution.CancellationToken)
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

		using (Assert.Multiple())
		{
			await Assert.That(errors.Any(diagnostic => diagnostic.Id == "CS0121")).IsTrue();
			await Assert.That(result.Compilation.Assembly.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentSquaredExtensions")).IsNull();
			await Assert.That(result.Compilation.Assembly.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeCurrentExtensions")).IsNull();
		}
	}

	[Test]
	[Arguments("var value = value * current; return value;", "CS0841")]
	[Arguments("return (length + current) * time;", "CS0019")]
	[Arguments("return LengthCurrent.FromBananas(6);", "CS0103")]
	public async Task InvalidConsumerCodeRemainsACompilerError(string body, string expectedDiagnostic)
	{
		string source = $$"""
			public static class Consumer
			{
				public static object Value(Length length, Current current, Time time) { {{body}} }
			}
			""";
		QuantityDiscoveryResult result = Discover(source);
		Diagnostic[] errors = result.Compilation.GetDiagnostics(TestContext.Current!.Execution.CancellationToken)
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

		await Assert.That(errors.Any(diagnostic => diagnostic.Id == expectedDiagnostic)).IsTrue();
	}

	[Test]
	public async Task LongDependentExpressionsCompleteWithoutAnArbitrarySmallPassLimit()
	{
		string expression = string.Join(" * ", Enumerable.Repeat("Current.FromAmps(1)", 20));
		string source = $$"""
			public static class Consumer
			{
				public static LengthCurrent20 Calculate() => Length.FromMetres(2) * {{expression}};
				public static double Value() => Calculate().AsSiBaseUnit();
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);

		await Assert.That(result).IsEqualTo(2);
	}

	[Test]
	[Arguments("var squared = current * current; var heating = squared * resistance;", "Power", "heating", 36)]
	[Arguments("var squared = current * current; var copy = squared; var heating = copy * resistance; var energy = heating * time;", "Energy", "energy", 72)]
	[Arguments("var squared = (2 * current) * current / 2; var heating = squared * resistance;", "Power", "heating", 36)]
	[Arguments("var squared = current * current; var fourth = squared * squared;", "Current4", "fourth", 81)]
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
				public static double Value() => Calculate().AsSiBaseUnit();
			}
			""";
		double result = Evaluate(Discover(source).Compilation);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task IdenticallyNamedLocalsInSeparateScopesRetainTheirOwnDimensions()
	{
		const string source = """
			public static class Consumer
			{
				public static double Value()
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
		double result = Evaluate(Discover(source).Compilation);
		double expected = 48;

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task CyclicLocalInitialisersDoNotCrashDiscovery()
	{
		const string source = """
			public static class Consumer
			{
				public static object Value(Current current)
				{
					var first = second * current;
					var second = first * current;
					return second;
				}
			}
			""";
		QuantityDiscoveryResult result = Discover(source);

		using (Assert.Multiple())
		{
			await Assert.That(result.Compilation.GetTypeByMetadataName("DimensionalQuantities.Generated.ArithmeticExtensions")).IsNull();
			await Assert.That(result.Sources.Any(generated => generated.HintName == "ArithmeticExtensions.g.cs")).IsFalse();
		}
	}

	[Test]
	[Arguments("LengthCurrent value = length * current;")]
	[Arguments("LengthCurrent first = length * current; LengthCurrent value = first;")]
	[Arguments("LengthCurrent first = length * current; var value = first;")]
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
				public static double Value() => Calculate().AsSiBaseUnit();
			}
			""";
		double result = Evaluate(Discover(source).Compilation);

		await Assert.That(result).IsEqualTo(24);
	}

	[Test]
	public async Task IncorrectExplicitIntermediateTypeRemainsACompilerError()
	{
		const string source = """
			class Consumer
			{
				object Value(Length length, Current current, Time time)
				{
					LengthCurrent wrong = length * time;
					return wrong * current;
				}
			}
			""";
		QuantityDiscoveryResult result = Discover(source);
		Diagnostic[] errors = result.Compilation.GetDiagnostics(TestContext.Current!.Execution.CancellationToken)
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

		await Assert.That(errors.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)).IsTrue();
	}

	[Test]
	[Arguments("choose ? length * current : length * current")]
	[Arguments("choose ? length * current : throw new System.Exception()")]
	[Arguments("choose switch { true => length * current, false => length * current }")]
	[Arguments("choose switch { true => length * current, _ => throw new System.Exception() }")]
	[Arguments("choose ? (choose switch { true => length * current, _ => length * current }) : length * current")]
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
				public static double Value() => Calculate().AsSiBaseUnit();
			}
			""";
		double result = Evaluate(Discover(source).Compilation);

		await Assert.That(result).IsEqualTo(24);
	}

	[Test]
	[Arguments("public LengthCurrent Stored = Length.FromMetres(2) * Current.FromAmps(3);")]
	[Arguments("public LengthCurrent Stored => Length.FromMetres(2) * Current.FromAmps(3);")]
	[Arguments("public LengthCurrent Stored { get => Length.FromMetres(2) * Current.FromAmps(3); }")]
	[Arguments("public LengthCurrent Stored { get; } = Length.FromMetres(2) * Current.FromAmps(3);")]
	[Arguments("private LengthCurrent Seed = Length.FromMetres(2) * Current.FromAmps(3); public LengthCurrent Stored => Seed;")]
	public async Task IntermediateFieldsAndPropertiesComposeWithArithmetic(string declaration)
	{
		string source = $$"""
			public class Inputs { {{declaration}} }
			public static class Consumer
			{
				public static LengthTimeCurrent Calculate() => new Inputs().Stored * Time.FromSeconds(4);
				public static double Value() => Calculate().AsSiBaseUnit();
			}
			""";
		double result = Evaluate(Discover(source).Compilation);

		await Assert.That(result).IsEqualTo(24);
	}

	[Test]
	[Arguments("public LengthCurrent Make(Length length, Current current) => length * current;")]
	[Arguments("public LengthCurrent Make(Length length, Current current) { LengthCurrent product = length * current; return product; }")]
	[Arguments("public LengthCurrent Make(Length length, Current current) { if (length.AsSiBaseUnit() > 0) return length * current; return length * current * 2; }")]
	[Arguments("public LengthCurrent Make(Length length, Current current) { if (length.AsSiBaseUnit() < 0) throw new System.Exception(); return length * current; }")]
	[Arguments("public LengthCurrent Make(Length length, Current current) { LengthCurrent Local(Length l, Current c) => l * c; return Local(length, current); }")]
	[Arguments("public LengthTime Make(Length length, Time time) => length * time; public LengthCurrent Make(Length length, Current current) => length * current;")]
	public async Task IntermediateMethodResultsComposeWithArithmetic(string declaration)
	{
		string source = $$"""
			public class Inputs { {{declaration}} }
			public static class Consumer
			{
				public static LengthTimeCurrent Calculate() => new Inputs().Make(Length.FromMetres(2), Current.FromAmps(3)) * Time.FromSeconds(4);
				public static double Value() => Calculate().AsSiBaseUnit();
			}
			""";
		double result = Evaluate(Discover(source).Compilation);

		await Assert.That(result).IsEqualTo(24);
	}

	[Test]
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
				public static double Value() => Calculate().AsSiBaseUnit();
			}
			""";
		double result = Evaluate(Discover(source).Compilation);

		await Assert.That(result).IsEqualTo(24);
	}

	[Test]
	[Arguments("public static LengthCurrent Field;", "Field")]
	[Arguments("public static LengthCurrent Field = default;", "Field")]
	[Arguments("public static LengthCurrent Property { get; } = new();", "Property")]
	[Arguments("public static LengthCurrent Property { get; set; }", "Property")]
	[Arguments("public static LengthCurrent[] Values = new LengthCurrent[1];", "Values[0]")]
	[Arguments("public static System.Collections.Generic.List<LengthCurrent> Values = new() { default };", "Values[0]")]
	public async Task TypeOnlyMemberAndCollectionValuesParticipateInArithmetic(string declaration, string operand)
	{
		string source = $$"""
			public static class Consumer
			{
				{{declaration}}
				public static LengthTimeCurrent Calculate() => {{operand}} * Time.FromSeconds(4);
				public static double Value() => Calculate().AsSiBaseUnit();
			}
			""";
		double result = Evaluate(Discover(source).Compilation);

		await Assert.That(result).IsEqualTo(0);
	}

	[Test]
	[Arguments("new LengthCurrent[1]")]
	[Arguments("new System.Collections.Generic.List<LengthCurrent> { default }")]
	public async Task InferredCollectionsRetainTheirExplicitElementDimensions(string initializer)
	{
		string source = $$"""
			public static class Consumer
			{
				public static LengthTimeCurrent Calculate()
				{
					var values = {{initializer}};
					return values[0] * Time.FromSeconds(4);
				}
				public static double Value() => Calculate().AsSiBaseUnit();
			}
			""";
		double result = Evaluate(Discover(source).Compilation);

		await Assert.That(result).IsEqualTo(0);
	}

	[Test]
	public async Task SourceIndexerCanDeclareAnIntermediateQuantityResult()
	{
		const string source = """
			public sealed class Measurements
			{
				public LengthCurrent this[int index] => LengthCurrent.FromSiBaseUnit(6);
			}
			public static class Consumer
			{
				public static LengthTimeCurrent Calculate()
				{
					Measurements values = new();
					return values[0] * Time.FromSeconds(4);
				}
				public static double Value() => Calculate().AsSiBaseUnit();
			}
			""";
		double result = Evaluate(Discover(source).Compilation);

		await Assert.That(result).IsEqualTo(24);
	}

	[Test]
	public async Task ExplicitLocalCanBeAssignedAfterItsDeclaration()
	{
		const string source = """
			public static class Consumer
			{
				public static LengthTimeCurrent Calculate()
				{
					LengthCurrent value;
					value = LengthCurrent.FromSiBaseUnit(6);
					return value * Time.FromSeconds(4);
				}
				public static double Value() => Calculate().AsSiBaseUnit();
			}
			""";
		double result = Evaluate(Discover(source).Compilation);

		await Assert.That(result).IsEqualTo(24);
	}

	[Test]
	[Arguments("default")]
	[Arguments("new()")]
	public async Task ExplicitDefaultQuantityLocalParticipatesInArithmetic(string initializer)
	{
		string source = $$"""
			public static class Consumer
			{
				public static LengthTimeCurrent Calculate()
				{
					LengthCurrent value = {{initializer}};
					return value * Time.FromSeconds(4);
				}
				public static double Value() => Calculate().AsSiBaseUnit();
			}
			""";
		double result = Evaluate(Discover(source).Compilation);

		await Assert.That(result).IsEqualTo(0);
	}

	[Test]
	public async Task EmptyInputNeedsNoDynamicSource()
	{
		QuantityDiscoveryResult result = Discover();

		await Assert.That(result.Sources).IsEmpty();
	}

	[Test]
	public async Task CancellationStopsDiscovery()
	{
		using CancellationTokenSource cancellation = new();
		cancellation.Cancel();
		QuantityGenerationPlan defaults = DefaultQuantityDiscovery.Discover();
		CSharpCompilation compilation = CreateCompilation([]);

		await Assert.That(() => QuantityDiscoveryCoordinator.Discover(compilation, [], defaults, cancellation.Token)).ThrowsExactly<OperationCanceledException>();
	}

	[Test]
	[Arguments("Length?", "Current", "Length.FromMetres(6)", "Current.FromAmps(3)", 18)]
	[Arguments("Length", "Current?", "Length.FromMetres(6)", "Current.FromAmps(3)", 18)]
	[Arguments("Length?", "Current?", "Length.FromMetres(6)", "Current.FromAmps(3)", 18)]
	[Arguments("Length?", "Current", "null", "Current.FromAmps(3)", -1)]
	[Arguments("Length", "Current?", "Length.FromMetres(6)", "null", -1)]
	[Arguments("Length?", "Current?", "Length.FromMetres(6)", "null", -1)]
	public async Task NullableMultiplicationDiscoveryPreservesNullableResults(string leftType, string rightType, string leftValue, string rightValue, double expectedValue)
	{
		string source = $$"""
			public static class Consumer
			{
				public static LengthCurrent? Calculate()
				{
					{{leftType}} left = {{leftValue}};
					{{rightType}} right = {{rightValue}};
					return left * right;
				}
				public static double Value() => Calculate()?.AsSiBaseUnit() ?? -1;
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);

		await Assert.That(result).IsEqualTo(expectedValue);
	}

	[Test]
	[Arguments("Length?", "Current", "Length.FromMetres(6)", "Current.FromAmps(3)", 2)]
	[Arguments("Length", "Current?", "Length.FromMetres(6)", "Current.FromAmps(3)", 2)]
	[Arguments("Length?", "Current?", "Length.FromMetres(6)", "Current.FromAmps(3)", 2)]
	[Arguments("Length?", "Current", "null", "Current.FromAmps(3)", -1)]
	[Arguments("Length", "Current?", "Length.FromMetres(6)", "null", -1)]
	[Arguments("Length?", "Current?", "Length.FromMetres(6)", "null", -1)]
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
				public static double Value() => Calculate()?.AsSiBaseUnit() ?? -1;
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);

		await Assert.That(result).IsEqualTo(expectedValue);
	}

	[Test]
	[Arguments("Length.FromMetres(6)", 18)]
	[Arguments("null", -1)]
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
				public static double Value() => Calculate()?.AsSiBaseUnit() ?? -1;
			}
			""";
		double result = Evaluate(Discover(source).Compilation);

		await Assert.That(result).IsEqualTo(expectedValue);
	}

	[Test]
	[Arguments("Current.FromAmps(3)", 2)]
	[Arguments("null", -1)]
	public async Task NullableScalarReciprocalDiscoveryPreservesNullableResults(string value, double expectedValue)
	{
		string source = $$"""
			public static class Consumer
			{
				public static InverseCurrent? Calculate()
				{
					Current? current = {{value}};
					return 6d / current;
				}
				public static double Value() => Calculate()?.AsSiBaseUnit() ?? -1;
			}
			""";
		double result = Evaluate(Discover(source).Compilation);

		await Assert.That(result).IsEqualTo(expectedValue);
	}

	[Test]
	[Arguments("Time?", "6d", "Time.FromSeconds(3)", 2)]
	[Arguments("Time?", "null", "Time.FromSeconds(3)", -1)]
	[Arguments("Time?", "6d", "null", -1)]
	[Arguments("Time?", "null", "null", -1)]
	[Arguments("Time", "6d", "Time.FromSeconds(3)", 2)]
	[Arguments("Time", "null", "Time.FromSeconds(3)", -1)]
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
				public static double Value() => Calculate()?.AsSiBaseUnit() ?? -1;
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);

		await Assert.That(result).IsEqualTo(expectedValue);
	}

	[Test]
	[Arguments("LengthCurrent.FromSiBaseUnit(12)", 144)]
	[Arguments("null", -1)]
	public async Task NullablePowerDiscoveryAddsNullConditionalSquaredMethod(string value, double expectedValue)
	{
		string source = $$"""
			public static class Consumer
			{
				public static LengthSquaredCurrentSquared? Calculate()
				{
					LengthCurrent? value = {{value}};
					return value?.Squared();
				}
				public static double Value() => Calculate()?.AsSiBaseUnit() ?? -1;
			}
			""";
		double result = Evaluate(Discover(source).Compilation);

		await Assert.That(result).IsEqualTo(expectedValue);
	}

	[Test]
	[Arguments("LengthCurrent.FromSiBaseUnit(144)", 12)]
	[Arguments("null", -1)]
	public async Task NullablePowerDiscoveryAddsNullConditionalSquareRootMethod(string value, double expectedValue)
	{
		string source = $$"""
			public static class Consumer
			{
				public static SquareRootLengthSquareRootCurrent? Calculate()
				{
					LengthCurrent? value = {{value}};
					return value?.SquareRoot();
				}
				public static double Value() => Calculate()?.AsSiBaseUnit() ?? -1;
			}
			""";
		double result = Evaluate(Discover(source).Compilation);

		await Assert.That(result).IsEqualTo(expectedValue);
	}

	[Test]
	[Arguments("Length.FromMetres(3)", 9)]
	[Arguments("null", -1)]
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
				public static double Value() => Calculate()?.AsSiBaseUnit() ?? -1;
			}
			""";
		double result = Evaluate(Discover(source).Compilation);

		await Assert.That(result).IsEqualTo(expectedValue);
	}

	[Test]
	[Arguments("value?.Squared().AsSiBaseUnit()", 36)]
	[Arguments("value?.Squared().Squared().AsSiBaseUnit()", 1296)]
	[Arguments("(value?.Squared())?.SquareRoot().AsSiBaseUnit()", 6)]
	public async Task NullablePowerDiscoveryHandlesConditionalAccessChains(string expression, double expectedValue)
	{
		string source = $$"""
			public static class Consumer
			{
				public static double Value()
				{
					LengthCurrent? value = Length.FromMetres(2) * Current.FromAmps(3);
					return {{expression}} ?? -1;
				}
			}
			""";
		double result = Evaluate(Discover(source).Compilation);

		await Assert.That(result).IsEqualTo(expectedValue);
	}

	[Test]
	public async Task NullablePowerDiscoveryUsesTheNearestConditionalReceiver()
	{
		const string source = """
			public sealed class Holder
			{
				public LengthCurrent? Value { get; }
				public Holder(LengthCurrent? value) => Value = value;
			}
			public static class Consumer
			{
				public static double Value()
				{
					Holder? holder = new(Length.FromMetres(2) * Current.FromAmps(3));
					return holder?.Value?.Squared().AsSiBaseUnit() ?? -1;
				}
			}
			""";
		double result = Evaluate(Discover(source).Compilation);

		await Assert.That(result).IsEqualTo(36);
	}

	private static QuantityDiscoveryResult Discover(params string[] sources)
	{
		CSharpCompilation original = CreateCompilation(sources);
		QuantityGenerationPlan defaults = DefaultQuantityDiscovery.Discover();
		SyntaxTree[] defaultTrees =
		[
			CSharpSyntaxTree.ParseText(QuantitySourceEmitter.Emit([.. defaults.KnownQuantities, .. defaults.AdditionalQuantities]), _parseOptions, path: "Quantities.g.cs"),
			CSharpSyntaxTree.ParseText(PowerSourceEmitter.Emit(defaults, "DefaultPowerExtensions"), _parseOptions, path: "DefaultPowers.g.cs"),
			CSharpSyntaxTree.ParseText(PowerMethodSourceEmitter.Emit(defaults, "DefaultPowerMethodExtensions"), _parseOptions, path: "DefaultPowerMethods.g.cs")
		];
		return QuantityDiscoveryCoordinator.Discover(original.AddSyntaxTrees(defaultTrees), [.. original.SyntaxTrees], defaults, TestContext.Current!.Execution.CancellationToken);
	}

	private static CSharpCompilation CreateCompilation(string[] sources)
	{
		string[] platformAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = platformAssemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		return CSharpCompilation.Create("DiscoveryConsumer", sources.Select((source, index) => CSharpSyntaxTree.ParseText(source, _parseOptions, path: $"Consumer{index}.cs")),
			references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
	}

	private static double Evaluate(CSharpCompilation compilation)
	{
		using MemoryStream bytes = new();
		EmitResult emitted = compilation.Emit(bytes, cancellationToken: TestContext.Current!.Execution.CancellationToken);
		if (!emitted.Success)
		{
			throw new InvalidOperationException(string.Join(Environment.NewLine, emitted.Diagnostics));
		}
		Assembly assembly = Assembly.Load(bytes.ToArray());
		return (double)assembly.GetType("Consumer")!.GetMethod("Value")!.Invoke(null, null)!;
	}
}
