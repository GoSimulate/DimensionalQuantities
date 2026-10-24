using System.Collections.Immutable;
using DimensionalQuantities.Roslyn.Analyzers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Analyzers;

public sealed class QuantityEncodingAnalyzerTests
{
	private const string LengthType = "Quantity<Dimension<TerminalHex1, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0>>";

	[Fact]
	public async Task RawQuantitySuggestsReadableAlias()
	{
		string source = $"using DimensionalQuantities.TypeSystem; public class Consumer {{ public {LengthType} Value {{ get; }} }}";
		Diagnostic result = Assert.Single(await AnalyzerTestCompilation.Analyze(source, new QuantityEncodingAnalyzer()));
		Assert.Multiple(
			() => Assert.Equal("DQ0003", result.Id),
			() => Assert.Contains("Length", result.GetMessage()),
			() => Assert.Equal(LengthType, source.Substring(result.Location.SourceSpan.Start, result.Location.SourceSpan.Length)));
	}

	[Theory]
	[InlineData("global::DimensionalQuantities.TypeSystem.Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex1>")]
	[InlineData("global::DimensionalQuantities.TypeSystem.Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex2, global::DimensionalQuantities.TypeSystem.TerminalHex2>")]
	public async Task NonCanonicalEncodingExplainsDistinctTypeIdentity(string exponent)
	{
		string type = $"global::DimensionalQuantities.TypeSystem.Quantity<global::DimensionalQuantities.TypeSystem.Dimension<{exponent}, "
			+ "global::DimensionalQuantities.TypeSystem.TerminalHex0, global::DimensionalQuantities.TypeSystem.TerminalHex0, global::DimensionalQuantities.TypeSystem.TerminalHex0, "
			+ "global::DimensionalQuantities.TypeSystem.TerminalHex0, global::DimensionalQuantities.TypeSystem.TerminalHex0, global::DimensionalQuantities.TypeSystem.TerminalHex0>>";
		string source = $"using MyLength = {type}; public class Consumer {{ }}";
		ImmutableArray<Diagnostic> results = await AnalyzerTestCompilation.Analyze(source, new QuantityEncodingAnalyzer());
		Diagnostic result = Assert.Single(results);
		Assert.Multiple(
			() => Assert.Equal("DQ0004", result.Id),
			() => Assert.Equal(DiagnosticSeverity.Warning, result.Severity));
	}

	[Theory]
	[InlineData("Quantity<string>")]
	[InlineData("Quantity<Dimension<Hex<TerminalHex1, TerminalHex0>, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0>>")]
	[InlineData("Quantity<Dimension<Fraction<TerminalHex1, TerminalHex0>, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0>>")]
	public async Task InvalidClosedEncodingIsDiagnosedWithoutAnalyzerCrash(string type)
	{
		string source = $"using DimensionalQuantities.TypeSystem; public class Consumer {{ public {type} Value {{ get; }} }}";
		Diagnostic result = Assert.Single(await AnalyzerTestCompilation.Analyze(source, new QuantityEncodingAnalyzer(), generate: false));
		Assert.Multiple(
			() => Assert.Equal("DQ0005", result.Id),
			() => Assert.Equal(DiagnosticSeverity.Error, result.Severity));
	}

	[Theory]
	[InlineData("Quantity<Missing>")]
	[InlineData("Power<Missing>")]
	public async Task UnresolvedArgumentsDoNotProduceEncodingDiagnostics(string type)
	{
		string source = $"using DimensionalQuantities.TypeSystem; public class Consumer {{ public {type} Value {{ get; }} }}";
		ImmutableArray<Diagnostic> result = await AnalyzerTestCompilation.Analyze(source, new QuantityEncodingAnalyzer(), generate: false);
		Assert.Empty(result);
	}

	[Theory]
	[InlineData("using MyLength = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>>; public class Consumer { public MyLength Value { get; } }")]
	[InlineData("public class Consumer { public Length Value { get; } }")]
	[InlineData("using DimensionalQuantities.TypeSystem; public class Consumer<T> { public Quantity<T> Value { get; } }")]
	[InlineData("public class Quantity<T> { } public class Consumer { public Quantity<string> Value { get; } }")]
	public async Task DeliberateAliasesOpenGenericsAndUnrelatedTypesAreAllowed(string source)
	{
		ImmutableArray<Diagnostic> result = await AnalyzerTestCompilation.Analyze(source, new QuantityEncodingAnalyzer());
		Assert.Empty(result);
	}
}
