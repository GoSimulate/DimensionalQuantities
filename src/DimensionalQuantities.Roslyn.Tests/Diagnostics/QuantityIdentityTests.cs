using DimensionalQuantities.Roslyn.Analyzers;
using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Diagnostics;

public sealed class QuantityIdentityTests
{
	[Fact]
	public async Task MetadataOriginQuantityRetainsDimensionInMismatchMessage()
	{
		QuantityGeneratorTest producer = new()
		{
			TestCode = """
				public class Clock
				{
					public static Time Elapsed() => Time.FromSiBaseUnit(3);
				}
				"""
		};
		await producer.RunAsync(TestContext.Current.CancellationToken);
		using MemoryStream assembly = new();
		Microsoft.CodeAnalysis.Emit.EmitResult emitted = producer.Compilation.Emit(assembly, cancellationToken: TestContext.Current.CancellationToken);
		if (!emitted.Success)
		{
			throw new InvalidOperationException(string.Join("\n", emitted.Diagnostics));
		}

		QuantityAnalyzerTest<DimensionMismatchAnalyzer> consumer = new()
		{
			TestCode = """
				class Route
				{
					Length Distance() => {|#0:Clock.Elapsed()|};
				}
				""",
			CompilerDiagnostics = CompilerDiagnostics.None
		};
		consumer.TestState.AdditionalReferences.Add(MetadataReference.CreateFromImage(assembly.ToArray()));
		consumer.ExpectedDiagnostics.Add(new DiagnosticResult("DQ0002", DiagnosticSeverity.Error).WithLocation(0).WithArguments("Length", "Time"));
		await consumer.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("TerminalHex0", "TerminalHex0", "TerminalHex0", "Dimensionless")]
	[InlineData("TerminalHex1", "TerminalHex1", "Negative<TerminalHex2>", "Force")]
	[InlineData("TerminalHex4", "TerminalHex0", "TerminalHex0", "Length4")]
	public async Task DestinationMessagesUseReadableDimensionNames(string length, string mass, string time, string expected)
	{
		QuantityAnalyzerTest<DimensionMismatchAnalyzer> test = new()
		{
			TestCode = $$"""
				using DimensionalQuantities.TypeSystem;

				class Measurement
				{
					Quantity<Dimension<{{length}}, {{mass}}, {{time}}, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0>> Apply(Time value) => {|#0:value|};
				}
				""",
			CompilerDiagnostics = CompilerDiagnostics.None
		};
		test.ExpectedDiagnostics.Add(new DiagnosticResult("DQ0002", DiagnosticSeverity.Error).WithLocation(0).WithArguments(expected, "Time"));
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task MathematicallyEqualDimensionsDoNotBecomeMismatchFromDifferentEncoding()
	{
		QuantityAnalyzerTest<DimensionMismatchAnalyzer> test = new()
		{
			TestCode = """
				using DimensionalQuantities.TypeSystem;

				class Measurement
				{
					Length Apply(Quantity<Dimension<Fraction<TerminalHex2, TerminalHex2>, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0>> value) => value;
				}
				""",
			CompilerDiagnostics = CompilerDiagnostics.None
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("""
		struct Quantity<T>
		{
		}
		class Length
		{
		}
		class Time
		{
		}
		class Consumer
		{
			Quantity<Length> Apply(Quantity<Time> value) => value;
		}
		""")]
	[InlineData("""
		using DimensionalQuantities.TypeSystem;

		class Consumer
		{
			Quantity<int> Apply(Quantity<string> value) => value;
		}
		""")]
	public async Task LookalikeAndUnreadableQuantitiesAreNotGuessed(string source)
	{
		QuantityAnalyzerTest<DimensionMismatchAnalyzer> test = new()
		{
			TestCode = source,
			CompilerDiagnostics = CompilerDiagnostics.None
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task LibraryFreeMismatchAnalyzerDoesNotAffectOrdinaryCode()
	{
		CSharpAnalyzerTest<DimensionMismatchAnalyzer, DefaultVerifier> test = new()
		{
			TestCode = """
				class Calculation
				{
					int Add(int a, int b) => a + b;
				}
				""",
			ReferenceAssemblies = ReferenceAssemblies.Net.Net100
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
	}
}
