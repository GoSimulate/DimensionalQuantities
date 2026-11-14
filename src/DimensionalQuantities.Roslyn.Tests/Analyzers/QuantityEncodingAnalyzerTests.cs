using DimensionalQuantities.Roslyn.Analyzers;
using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Analyzers;

public sealed class QuantityEncodingAnalyzerTests
{
	private const string LengthType = "Quantity<Dimension<TerminalHex1, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0>>";

	[Fact]
	public async Task RawQuantitySuggestsReadableAlias()
	{
		QuantityAnalyzerTest<QuantityEncodingAnalyzer> test = new()
		{
			TestCode = $$"""
				using DimensionalQuantities.TypeSystem;
				public class Consumer
				{
					public {|#0:{{LengthType}}|} Value { get; }
				}
				"""
		};
		test.ExpectedDiagnostics.Add(new DiagnosticResult("DQ0003", DiagnosticSeverity.Info).WithLocation(0).WithArguments("Length"));
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("global::DimensionalQuantities.TypeSystem.Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex1>")]
	[InlineData("global::DimensionalQuantities.TypeSystem.Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex2, global::DimensionalQuantities.TypeSystem.TerminalHex2>")]
	public async Task NonCanonicalEncodingExplainsDistinctTypeIdentity(string exponent)
	{
		string type = $"Quantity<global::DimensionalQuantities.TypeSystem.Dimension<{exponent}, "
			+ "global::DimensionalQuantities.TypeSystem.TerminalHex0, global::DimensionalQuantities.TypeSystem.TerminalHex0, global::DimensionalQuantities.TypeSystem.TerminalHex0, "
			+ "global::DimensionalQuantities.TypeSystem.TerminalHex0, global::DimensionalQuantities.TypeSystem.TerminalHex0, global::DimensionalQuantities.TypeSystem.TerminalHex0>>";
		QuantityAnalyzerTest<QuantityEncodingAnalyzer> test = new()
		{
			TestCode = $$"""
				using MyLength = global::DimensionalQuantities.TypeSystem.{|#0:{{type}}|};
				public class Consumer
				{
				}
				"""
		};
		test.ExpectedDiagnostics.Add(new DiagnosticResult("DQ0004", DiagnosticSeverity.Warning).WithLocation(0));
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("Quantity<string>", false)]
	[InlineData("Quantity<Dimension<Hex<TerminalHex1, TerminalHex0>, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0>>", true)]
	[InlineData("Quantity<Dimension<Fraction<TerminalHex1, TerminalHex0>, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0>>", true)]
	public async Task InvalidClosedEncodingIsDiagnosedWithoutAnalyzerCrash(string type, bool invalidConstraint)
	{
		QuantityAnalyzerTest<QuantityEncodingAnalyzer> test = new(generate: false)
		{
			TestCode = $$"""
				using DimensionalQuantities.TypeSystem;
				public class Consumer
				{
					public {|#0:{{type}}|} {|#1:Value|} { get; }
				}
				"""
		};
		test.ExpectedDiagnostics.Add(new DiagnosticResult("DQ0005", DiagnosticSeverity.Error).WithLocation(0));
		if (invalidConstraint)
		{
			test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS0315").WithLocation(1));
		}

		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("Quantity<{|CS0246:Missing|}>")]
	[InlineData("Power<{|CS0246:Missing|}>")]
	public async Task UnresolvedArgumentsDoNotProduceEncodingDiagnostics(string type)
	{
		QuantityAnalyzerTest<QuantityEncodingAnalyzer> test = new(generate: false)
		{
			TestCode = $$"""
				using DimensionalQuantities.TypeSystem;
				public class Consumer
				{
					public {{type}} Value { get; }
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("""
		using MyLength = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>>;
		public class Consumer
		{
			public MyLength Value { get; }
		}
		""")]
	[InlineData("""
		public class Consumer
		{
			public Length Value { get; }
		}
		""")]
	[InlineData("""
		using DimensionalQuantities.TypeSystem;
		public class Consumer<T>
		{
			public Quantity<T> Value { get; }
		}
		""")]
	[InlineData("""
		public class Quantity<T>
		{
		}
		public class Consumer
		{
			public Quantity<string> Value { get; }
		}
		""")]
	public async Task DeliberateAliasesOpenGenericsAndUnrelatedTypesAreAllowed(string source)
	{
		QuantityAnalyzerTest<QuantityEncodingAnalyzer> test = new() { TestCode = source };
		await test.RunAsync(TestContext.Current.CancellationToken);
	}
}
