using DimensionalQuantities.Roslyn.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using DimensionalQuantities.Roslyn.Tests.Testing;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Analyzers;

public sealed class QuantityEncodingAnalyzerTests
{
	private const string LengthType = "Quantity<Dimension<TerminalHex1, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0>>";

	[Fact]
	public async Task RawQuantitySuggestsReadableAlias()
	{
		string source = $"using DimensionalQuantities.TypeSystem; public class Consumer {{ public {{|#0:{LengthType}|}} Value {{ get; }} }}";
		await new QuantityAnalyzerTest<QuantityEncodingAnalyzer>
		{
			TestCode = source,
			ExpectedDiagnostics = { new DiagnosticResult(QuantityEncodingAnalyzer.ReadableAlias).WithLocation(0).WithArguments("Length") },
		}.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("global::DimensionalQuantities.TypeSystem.Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex1>")]
	[InlineData("global::DimensionalQuantities.TypeSystem.Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex2, global::DimensionalQuantities.TypeSystem.TerminalHex2>")]
	public async Task NonCanonicalEncodingExplainsDistinctTypeIdentity(string exponent)
	{
		string type = $"global::DimensionalQuantities.TypeSystem.Quantity<global::DimensionalQuantities.TypeSystem.Dimension<{exponent}, "
			+ "global::DimensionalQuantities.TypeSystem.TerminalHex0, global::DimensionalQuantities.TypeSystem.TerminalHex0, global::DimensionalQuantities.TypeSystem.TerminalHex0, "
			+ "global::DimensionalQuantities.TypeSystem.TerminalHex0, global::DimensionalQuantities.TypeSystem.TerminalHex0, global::DimensionalQuantities.TypeSystem.TerminalHex0>>";
		string markedType = type.Replace("Quantity<", "{|#0:Quantity<") + "|}";
		string source = $"using MyLength = {markedType}; public class Consumer {{ }}";
		await new QuantityAnalyzerTest<QuantityEncodingAnalyzer>
		{
			TestCode = source,
			ExpectedDiagnostics = { new DiagnosticResult(QuantityEncodingAnalyzer.NonCanonical).WithLocation(0) },
		}.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("Quantity<string>")]
	[InlineData("Quantity<Dimension<Hex<TerminalHex1, TerminalHex0>, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0>>")]
	[InlineData("Quantity<Dimension<Fraction<TerminalHex1, TerminalHex0>, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0>>")]
	public async Task InvalidClosedEncodingIsDiagnosedWithoutAnalyzerCrash(string type)
	{
		string source = $"using DimensionalQuantities.TypeSystem; public class Consumer {{ public {{|#0:{type}|}} Value {{ get; }} }}";
		await new QuantityAnalyzerTest<QuantityEncodingAnalyzer>
		{
			TestCode = source,
			Generate = false,
			CompilerDiagnostics = CompilerDiagnostics.None,
			ExpectedDiagnostics = { new DiagnosticResult(QuantityEncodingAnalyzer.Invalid).WithLocation(0) },
		}.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("Quantity<Missing>")]
	[InlineData("Power<Missing>")]
	public async Task UnresolvedArgumentsDoNotProduceEncodingDiagnostics(string type)
	{
		string source = $"using DimensionalQuantities.TypeSystem; public class Consumer {{ public {type} Value {{ get; }} }}";
		await new QuantityAnalyzerTest<QuantityEncodingAnalyzer>
		{
			TestCode = source,
			Generate = false,
			CompilerDiagnostics = CompilerDiagnostics.None,
		}.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("using MyLength = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>>; public class Consumer { public MyLength Value { get; } }")]
	[InlineData("public class Consumer { public Length Value { get; } }")]
	[InlineData("using DimensionalQuantities.TypeSystem; public class Consumer<T> { public Quantity<T> Value { get; } }")]
	[InlineData("public class Quantity<T> { } public class Consumer { public Quantity<string> Value { get; } }")]
	public async Task DeliberateAliasesOpenGenericsAndUnrelatedTypesAreAllowed(string source)
	{
		await new QuantityAnalyzerTest<QuantityEncodingAnalyzer>
		{
			TestCode = source,
		}.RunAsync(TestContext.Current.CancellationToken);
	}
}
