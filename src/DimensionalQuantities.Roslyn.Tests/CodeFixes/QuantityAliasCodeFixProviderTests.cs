using DimensionalQuantities.Roslyn.Analyzers;
using DimensionalQuantities.Roslyn.Tests.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.CodeFixes;

public sealed class QuantityAliasCodeFixProviderTests
{
	private const string LengthType = "global::DimensionalQuantities.TypeSystem.Quantity<global::DimensionalQuantities.TypeSystem.Dimension<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex0, "
		+ "global::DimensionalQuantities.TypeSystem.TerminalHex0, global::DimensionalQuantities.TypeSystem.TerminalHex0, global::DimensionalQuantities.TypeSystem.TerminalHex0, "
		+ "global::DimensionalQuantities.TypeSystem.TerminalHex0, global::DimensionalQuantities.TypeSystem.TerminalHex0>>";

	private const string LengthCurrentType = "global::DimensionalQuantities.TypeSystem.Quantity<global::DimensionalQuantities.TypeSystem.Dimension<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex0, "
		+ "global::DimensionalQuantities.TypeSystem.TerminalHex0, global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex0, "
		+ "global::DimensionalQuantities.TypeSystem.TerminalHex0, global::DimensionalQuantities.TypeSystem.TerminalHex0>>";

	[Theory]
	[InlineData("public class Consumer { public TYPE Value { get; } }", "public class Consumer { public Length Value { get; } }")]
	[InlineData("public class Consumer { public TYPE? Value { get; } }", "public class Consumer { public Length? Value { get; } }")]
	[InlineData("public class Consumer { public /* measurement */ TYPE Value { get; } }", "public class Consumer { public /* measurement */ Length Value { get; } }")]
	[InlineData("public class Consumer { public TYPE Value { get; } }", "public class Consumer { public LengthCurrent Value { get; } }", LengthCurrentType)]
	public async Task FixUsesAliasAndPreservesSurroundingSource(string template, string expected, string? rawType = null)
	{
		string marked = template.Replace("TYPE", (rawType ?? LengthType).Replace("Quantity<", "{|#0:Quantity<") + "|}");
		await new QuantityCodeFixTest
		{
			TestCode = marked,
			FixedCode = expected,
			ExpectedDiagnostics = { new DiagnosticResult(QuantityEncodingAnalyzer.ReadableAlias).WithLocation(0).WithArguments(rawType is null ? "Length" : "LengthCurrent") },
		}.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task NameOfDoesNotOfferFixThatChangesTheReturnedName()
	{
		string source = $"public class Consumer {{ public string Value() => nameof({LengthType}); }}";
		string marked = source.Replace(LengthType, LengthType.Replace("Quantity<", "{|#0:Quantity<") + "|}");
		await new QuantityCodeFixTest
		{
			TestCode = marked,
			FixedCode = source,
			FixedState =
			{
				ExpectedDiagnostics =
				{
					new DiagnosticResult(QuantityEncodingAnalyzer.ReadableAlias)
						.WithSpan(1, source.IndexOf("Quantity<", StringComparison.Ordinal) + 1,
							1, source.IndexOf(LengthType, StringComparison.Ordinal) + LengthType.Length + 1).WithArguments("Length"),
				},
			},
			ExpectedDiagnostics = { new DiagnosticResult(QuantityEncodingAnalyzer.ReadableAlias).WithLocation(0).WithArguments("Length") },
		}.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task ShadowedAliasDoesNotOfferUnsafeFix()
	{
		string source = $"namespace ConsumerNamespace {{ public class Length {{ }} public class Consumer {{ public {LengthType} Value {{ get; }} }} }}";
		string marked = source.Replace(LengthType, LengthType.Replace("Quantity<", "{|#0:Quantity<") + "|}");
		await new QuantityCodeFixTest
		{
			TestCode = marked,
			FixedCode = source,
			FixedState =
			{
				ExpectedDiagnostics =
				{
					new DiagnosticResult(QuantityEncodingAnalyzer.ReadableAlias)
						.WithSpan(1, source.IndexOf("Quantity<", StringComparison.Ordinal) + 1,
							1, source.IndexOf(LengthType, StringComparison.Ordinal) + LengthType.Length + 1).WithArguments("Length"),
				},
			},
			ExpectedDiagnostics = { new DiagnosticResult(QuantityEncodingAnalyzer.ReadableAlias).WithLocation(0).WithArguments("Length") },
		}.RunAsync(TestContext.Current.CancellationToken);
	}

}
