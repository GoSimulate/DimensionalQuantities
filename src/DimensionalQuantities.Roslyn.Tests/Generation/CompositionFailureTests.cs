using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generation;

public sealed class CompositionFailureTests
{
	[Fact]
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
		QuantityGeneratorTest test = new()
		{
			TestCode = source,
			CompilerDiagnostics = CompilerDiagnostics.None
		};
		QuantityTestEnvironment.UseCurrentRuntimeReferences(test);
		await test.RunAsync(TestContext.Current.CancellationToken);
		Diagnostic[] errors = test.Compilation.GetDiagnostics(TestContext.Current.CancellationToken)
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

		Assert.Multiple(
			() => Assert.Contains(errors, diagnostic => diagnostic.Id == "CS8506"),
			() => Assert.DoesNotContain(test.GeneratedSources, source => source.FilePath.EndsWith("/Arithmetic.g.cs", StringComparison.Ordinal)));
	}

	[Theory]
	[InlineData("choose ? length * current : length * time", "CS0173")]
	[InlineData("""
		choose switch
		{
			true => length * current,
			false => length * time
		}
		""", "CS8506")]
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
		QuantityGeneratorTest test = new()
		{
			TestCode = source,
			CompilerDiagnostics = CompilerDiagnostics.None
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		Diagnostic[] errors = test.Compilation.GetDiagnostics(TestContext.Current.CancellationToken)
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

		Assert.Multiple(
			() => Assert.Contains(errors, diagnostic => diagnostic.Id == expectedDiagnostic),
			() => Assert.Null(test.Compilation.Assembly.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentSquaredExtensions")),
			() => Assert.Null(test.Compilation.Assembly.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeCurrentExtensions")));
	}

	[Fact]
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
		QuantityGeneratorTest test = new()
		{
			TestCode = source,
			CompilerDiagnostics = CompilerDiagnostics.None
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		Diagnostic[] errors = test.Compilation.GetDiagnostics(TestContext.Current.CancellationToken)
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

		Assert.Multiple(
			() => Assert.Contains(errors, diagnostic => diagnostic.Id == "CS0121"),
			() => Assert.Null(test.Compilation.Assembly.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentSquaredExtensions")),
			() => Assert.Null(test.Compilation.Assembly.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeCurrentExtensions")));
	}

	[Theory]
	[InlineData("""
		var value = value * current;
		return value;
		""", "CS0841")]
	[InlineData("""
		return (length + current) * time;
		""", "CS0019")]
	[InlineData("""
		return LengthCurrent.FromBananas(6);
		""", "CS0103")]
	public async Task InvalidConsumerCodeRemainsACompilerError(string body, string expectedDiagnostic)
	{
		string source = $$"""
			public static class Consumer
			{
				public static object Value(Length length, Current current, Time time)
				{
					{{body}}
				}
			}
			""";
		QuantityGeneratorTest test = new()
		{
			TestCode = source,
			CompilerDiagnostics = CompilerDiagnostics.None
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		Diagnostic[] errors = test.Compilation.GetDiagnostics(TestContext.Current.CancellationToken)
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

		Assert.Contains(errors, diagnostic => diagnostic.Id == expectedDiagnostic);
	}

	[Fact]
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
		QuantityGeneratorTest test = new()
		{
			TestCode = source,
			CompilerDiagnostics = CompilerDiagnostics.None
		};
		await test.RunAsync(TestContext.Current.CancellationToken);

		Assert.Multiple(
			() => Assert.Contains(test.Compilation.GetDiagnostics(TestContext.Current.CancellationToken), diagnostic => diagnostic.Id == "CS0841"),
			() => Assert.Null(test.Compilation.GetTypeByMetadataName("DimensionalQuantities.Generated.ArithmeticExtensions")),
			() => Assert.DoesNotContain(test.GeneratedSources, generated => generated.FilePath.EndsWith("/Arithmetic.g.cs", StringComparison.Ordinal)));
	}

	[Fact]
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
		QuantityGeneratorTest test = new()
		{
			TestCode = source,
			CompilerDiagnostics = CompilerDiagnostics.None
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		Diagnostic[] errors = test.Compilation.GetDiagnostics(TestContext.Current.CancellationToken)
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

		Assert.Contains(errors, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
	}
}
