using DimensionalQuantities.Roslyn.Analyzers;
using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Diagnostics;

public sealed class QuantityEncodingTests
{
	private const string Imports = "using DimensionalQuantities.TypeSystem;\n";
	internal const string RawLength = "Quantity<Dimension<TerminalHex1, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0>>";

	[Theory]
	[InlineData("{0} value;")]
	[InlineData("System.Collections.Generic.List<{0}> value;")]
	[InlineData("global::DimensionalQuantities.TypeSystem.{0} value;")]
	public async Task CanonicalRawQuantitySuggestsReadableAliasAtGenericName(string member)
	{
		QuantityAnalyzerTest<QuantityEncodingAnalyzer> test = Case("class Measurement { " + string.Format(member, "{|#0:" + RawLength + "|}") + " }");
		test.ExpectedDiagnostics.Add(Alias());
		test.DiagnosticVerifier = (actual, expected, verifier) => Assert.Equal("Length", actual.Properties["AliasName"]);
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("Fraction<TerminalHex2, TerminalHex4>")]
	[InlineData("Fraction<TerminalHex1, TerminalHex1>")]
	[InlineData("Fraction<TerminalHex0, TerminalHex2>")]
	public async Task NoncanonicalQuantityWarnsWithoutAliasSuggestion(string exponent)
	{
		QuantityAnalyzerTest<QuantityEncodingAnalyzer> test = Case("class Measurement { {|#0:" + Quantity(exponent) + "|} value; }");
		test.ExpectedDiagnostics.Add(At("DQ0004", DiagnosticSeverity.Warning));
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("TerminalHex1")]
	[InlineData("Fraction<TerminalHex1, TerminalHex2>")]
	[InlineData("Negative<TerminalHex1>")]
	public async Task CanonicalPowerDeclarationNeedsNoInvocation(string exponent)
	{
		await Case($"class Calculation {{ Power<{exponent}> exponent; }}").RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("Fraction<TerminalHex2, TerminalHex4>", "DQ0004", DiagnosticSeverity.Warning)]
	[InlineData("int", "DQ0005", DiagnosticSeverity.Error)]
	[InlineData("UnknownNumber", "DQ0005", DiagnosticSeverity.Error)]
	[InlineData("Fraction<TerminalHex1, TerminalHex0>", "DQ0005", DiagnosticSeverity.Error)]
	[InlineData("Negative<TerminalHex0>", "DQ0005", DiagnosticSeverity.Error)]
	[InlineData("Hex<TerminalHex1, TerminalHex0>", "DQ0005", DiagnosticSeverity.Error)]
	public async Task BoundPowerEncodingIsValidatedInDeclarations(string exponent, string id, DiagnosticSeverity severity)
	{
		QuantityAnalyzerTest<QuantityEncodingAnalyzer> test = Case($"struct UnknownNumber : ITypeSystemNumber {{}} class Calculation {{ {{|#0:Power<{exponent}>|}} exponent; }}", compilerErrors: true);
		test.ExpectedDiagnostics.Add(At(id, severity));
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("Quantity<int>")]
	[InlineData("Quantity<Dimension<UnknownNumber, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0>>")]
	[InlineData("Quantity<Dimension<Fraction<TerminalHex1, TerminalHex1>, UnknownNumber, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0>>")]
	public async Task InvalidClosedEncodingPrecedesAliasAndNoncanonicalWarning(string type)
	{
		QuantityAnalyzerTest<QuantityEncodingAnalyzer> test = Case("struct UnknownNumber : ITypeSystemNumber {} class Measurement { {|#0:" + type + "|} value; }");
		test.ExpectedDiagnostics.Add(At("DQ0005", DiagnosticSeverity.Error));
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task IntentionalAliasSuppressesSuggestionOnly()
	{
		await Case("namespace Consumer { using Distance = " + RawLength + "; class Measurement { Distance value; } }").RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task InvalidAliasStillReportsEncodingError()
	{
		QuantityAnalyzerTest<QuantityEncodingAnalyzer> test = Case("namespace Consumer { using Invalid = {|#0:Quantity<int>|}; }");
		test.ExpectedDiagnostics.Add(At("DQ0005", DiagnosticSeverity.Error));
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task NoncanonicalAliasStillWarns()
	{
		QuantityAnalyzerTest<QuantityEncodingAnalyzer> test = Case("namespace Consumer { using Distance = {|#0:" + Quantity("Fraction<TerminalHex1, TerminalHex1>") + "|}; }");
		test.ExpectedDiagnostics.Add(At("DQ0004", DiagnosticSeverity.Warning));
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task DimensionlessQuantityHasNoReadableSuggestion()
	{
		await Case("class Measurement { " + Quantity("TerminalHex0") + " value; }").RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("Quantity<T> open;")]
	[InlineData("Quantity<Missing> unresolved;")]
	[InlineData("System.Type definition = typeof(Quantity<>);")]
	public async Task OpenAndUnresolvedEncodingsAreNotClosedInvalidEncodings(string member)
	{
		await Case($"class Measurement<T> {{ {member} }}", compilerErrors: true).RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData(RawLength + " length;")]
	[InlineData("Quantity<int> invalid;")]
	[InlineData("Power<Fraction<TerminalHex2, TerminalHex4>> exponent;")]
	public async Task RecognisedGeneratedHeaderExcludesEncodingDiagnostic(string member)
	{
		QuantityAnalyzerTest<QuantityEncodingAnalyzer> test = new() { TestCode = "// <auto-generated/>\n" + Imports + $"class Measurement {{ {member} }}" };
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task UnrelatedPowerIsOutsideLibraryEncodingRules()
	{
		await Case("namespace Consumer { class Power<T> {} class Calculation { Power<int> value; } }").RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task LibraryFreeCompilationRemainsUnaffected()
	{
		CSharpAnalyzerTest<QuantityEncodingAnalyzer, DefaultVerifier> test = new() { TestCode = "class Quantity<T> {} class Power<T> {} class Consumer { Quantity<int> q; Power<int> p; }", ReferenceAssemblies = ReferenceAssemblies.Net.Net100 };
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public void PublicDescriptorsHaveStableMetadata()
	{
		DiagnosticDescriptor[] descriptors = [.. new DimensionMismatchAnalyzer().SupportedDiagnostics, .. new QuantityEncodingAnalyzer().SupportedDiagnostics];
		Assert.Multiple(
			() => Assert.Equal(new[] { "DQ0001", "DQ0002", "DQ0003", "DQ0004", "DQ0005" }, descriptors.Select(descriptor => descriptor.Id)),
			() => Assert.Equal(new[] { DiagnosticSeverity.Error, DiagnosticSeverity.Error, DiagnosticSeverity.Info, DiagnosticSeverity.Warning, DiagnosticSeverity.Error }, descriptors.Select(descriptor => descriptor.DefaultSeverity)),
			() => Assert.All(descriptors, descriptor => Assert.Multiple(() => Assert.Equal("DimensionalQuantities", descriptor.Category), () => Assert.True(descriptor.IsEnabledByDefault))));
	}

	[Theory]
	[InlineData("TerminalHex1", "TerminalHex1", "Negative<TerminalHex2>", "Force")]
	[InlineData("TerminalHex4", "TerminalHex0", "TerminalHex0", "Length4")]
	public async Task PreferredAndCanonicalNamesAreCarriedInAliasProperty(string length, string mass, string time, string alias)
	{
		QuantityAnalyzerTest<QuantityEncodingAnalyzer> test = Case($"class Measurement {{ {{|#0:Quantity<Dimension<{length}, {mass}, {time}, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0>>|}} value; }}");
		test.ExpectedDiagnostics.Add(At("DQ0003", DiagnosticSeverity.Info).WithArguments(alias));
		test.DiagnosticVerifier = (actual, expected, verifier) => Assert.Equal(alias, actual.Properties["AliasName"]);
		await test.RunAsync(TestContext.Current.CancellationToken);
	}


	[Theory]
	[InlineData("class Consumer { Power<Missing> exponent; }")]
	[InlineData("class Consumer { Quantity<")]
	[InlineData("class Consumer { Power<")]
	public async Task UnresolvedPowerAndParserRecoveryDoNotInventInvalidClosedEncodings(string source)
	{
		await Case(source, compilerErrors: true).RunAsync(TestContext.Current.CancellationToken);
	}
	private static QuantityAnalyzerTest<QuantityEncodingAnalyzer> Case(string source, bool compilerErrors = false) => new() { TestCode = Imports + source, CompilerDiagnostics = compilerErrors ? CompilerDiagnostics.None : CompilerDiagnostics.Errors };
	private static DiagnosticResult At(string id, DiagnosticSeverity severity) => new DiagnosticResult(id, severity).WithLocation(0);
	private static DiagnosticResult Alias() => At("DQ0003", DiagnosticSeverity.Info).WithArguments("Length");
	private static string Quantity(string exponent) => $"Quantity<Dimension<{exponent}, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0>>";
}
