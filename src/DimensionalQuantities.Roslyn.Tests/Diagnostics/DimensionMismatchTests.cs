using DimensionalQuantities.Roslyn.Analyzers;
using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Diagnostics;

public sealed class DimensionMismatchTests
{
	[Theory]
	[InlineData("+")]
	[InlineData("-")]
	[InlineData("<")]
	[InlineData(">")]
	[InlineData("<=")]
	[InlineData(">=")]
	[InlineData("==")]
	[InlineData("!=")]
	public async Task IncompatibleBinaryOperandsReportWholeExpression(string operation)
	{
		QuantityAnalyzerTest<DimensionMismatchAnalyzer> test = Invalid($"class Calculation {{ object Apply(Length distance, Time duration) => {{|#0:distance {operation} duration|}}; }}");
		test.ExpectedDiagnostics.Add(new DiagnosticResult("DQ0001", DiagnosticSeverity.Error).WithLocation(0).WithArguments(operation, "Length", "Time"));
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("+=")]
	[InlineData("-=")]
	public async Task IncompatibleCompoundAssignmentReportsWholeAssignment(string operation)
	{
		QuantityAnalyzerTest<DimensionMismatchAnalyzer> test = Invalid($"class Calculation {{ void Apply(Length distance, Time duration) {{ {{|#0:distance {operation} duration|}}; }} }}");
		test.ExpectedDiagnostics.Add(new DiagnosticResult("DQ0001", DiagnosticSeverity.Error).WithLocation(0).WithArguments(operation, "Length", "Time"));
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("Length value = {|#0:Time.FromSiBaseUnit(2)|};")]
	[InlineData("Length Value { get; } = {|#0:Time.FromSiBaseUnit(2)|};")]
	[InlineData("void Apply(Time duration) { Length value = {|#0:duration|}; }")]
	[InlineData("void Apply(Length value, Time duration) { value = {|#0:duration|}; }")]
	[InlineData("void Apply(Time duration) { Length first = default, second = {|#0:duration|}; }")]
	public async Task DestinationMismatchReportsSourceExpression(string member)
	{
		QuantityAnalyzerTest<DimensionMismatchAnalyzer> test = Invalid("class Calculation { " + member + " }");
		test.ExpectedDiagnostics.Add(WrongDestination());
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("Length Apply(Time duration) { return {|#0:duration|}; }")]
	[InlineData("void Apply(Time duration) { Length Local() { return {|#0:duration|}; } }")]
	[InlineData("Time duration; Length Value { get { return {|#0:duration|}; } }")]
	[InlineData("Length Apply(Time duration) => {|#0:duration|};")]
	[InlineData("Time duration; Length Value => {|#0:duration|};")]
	[InlineData("Time duration; Length this[int index] => {|#0:duration|};")]
	public async Task ReturnMismatchUsesDeclaredDestination(string member)
	{
		QuantityAnalyzerTest<DimensionMismatchAnalyzer> test = Invalid("class Calculation { " + member + " }");
		test.ExpectedDiagnostics.Add(WrongDestination());
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("Accept({|#0:duration|});", "void Accept(Length value) {}")]
	[InlineData("Accept(other: 1, value: {|#0:duration|});", "void Accept(Length value, int other) {}")]
	[InlineData("System.Action<Length> accept = _ => {}; accept({|#0:duration|});", "")]
	[InlineData("_ = new Parcel({|#0:duration|});", "class Parcel { public Parcel(Length value) {} }")]
	[InlineData("Parcel parcel = new({|#0:duration|});", "class Parcel { public Parcel(Length value) {} }")]
	public async Task RecoverableArgumentMappingReportsExpectedDimension(string statement, string declaration)
	{
		QuantityAnalyzerTest<DimensionMismatchAnalyzer> test = Invalid($"class Calculation {{ void Apply(Time duration) {{ {statement} }} {declaration} }}");
		test.ExpectedDiagnostics.Add(WrongDestination());
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("Accept(duration);", "void Accept(Length value) {} void Accept(Mass value) {}")]
	[InlineData("Accept(missing: duration);", "void Accept(Length value) {}")]
	public async Task UnknownOrAmbiguousArgumentsDoNotGuessDestination(string call, string declarations)
	{
		await Invalid($"class Calculation {{ void Apply(Time duration) {{ {call} }} {declarations} }}").RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task NullableQuantitiesRetainTheirDimensions()
	{
		QuantityAnalyzerTest<DimensionMismatchAnalyzer> test = Invalid("class Calculation { Length? Apply(Time? duration) => {|#0:duration|}; }");
		test.ExpectedDiagnostics.Add(WrongDestination());
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("var sum = a + b;")]
	[InlineData("a += b;")]
	[InlineData("var same = a <= b;")]
	[InlineData("Length? lifted = a;")]
	[InlineData("var product = a * t;")]
	[InlineData("var ratio = a / t;")]
	[InlineData("var native = 1 + 2;")]
	[InlineData("object boxed = a;")]
	public async Task SameDimensionAndNativeBindingRemainAccepted(string statement)
	{
		QuantityAnalyzerTest<DimensionMismatchAnalyzer> test = new()
		{
			TestCode = $"class Calculation {{ void Apply(Length a, Length b, Time t) {{ {statement} }} }}"
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task SupportedUserOperatorKeepsItsOwnBinding()
	{
		QuantityAnalyzerTest<DimensionMismatchAnalyzer> test = new()
		{
			TestCode = "class Calculation { public static string operator +(Calculation value, Time duration) => \"supported\"; string Apply(Calculation value, Time duration) => value + duration; }"
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("System.Collections.Generic.List<Time>", "System.Collections.Generic.List<Length>")]
	[InlineData("DimensionalQuantities.TypeSystem.Delta<Time>", "DimensionalQuantities.TypeSystem.Delta<Length>")]
	[InlineData("Quantity<Time>", "Quantity<Length>")]
	public async Task WrappersAreNotGuessedAsDirectQuantities(string source, string target)
	{
		await Invalid($"using DimensionalQuantities.TypeSystem; class Calculation {{ {target} Apply({source} value) => value; }}").RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("Length Apply(Time duration) => duration;")]
	[InlineData("object Sum(Length l, Time t) => l + t;")]
	public async Task GeneratedSourceIsExcludedByRecognisedHeader(string member)
	{
		await Invalid($"// <auto-generated/>\nclass Calculation {{ {member} }}").RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("void Apply(Time duration) { Missing value = duration; }")]
	[InlineData("void Apply(Time duration) { var inferred = duration; }")]
	[InlineData("void Empty() { return; }")]
	public async Task IncompleteAndMissingDestinationsAreNotGuessed(string member)
	{
		await Invalid($"class Calculation {{ {member} }}").RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task SuccessfullyBoundCrossDimensionExtensionOperatorSuppressesMismatch()
	{
		QuantityAnalyzerTest<DimensionMismatchAnalyzer> test = new()
		{
			TestCode = "public static class Travel { extension(Length) { public static string operator +(Length distance, Time duration) => \"journey\"; } } class Calculation { string Apply(Length distance, Time duration) => distance + duration; }"
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("*=")]
	[InlineData("/=")]
	public async Task MultiplicativeAssignmentsAreNotEqualDimensionClaims(string operation)
	{
		await Invalid($"class Calculation {{ void Apply(Length length, Time time) {{ length {operation} time; }} }}").RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task NullableBinaryOperandsRetainDimensionIdentity()
	{
		QuantityAnalyzerTest<DimensionMismatchAnalyzer> test = Invalid("class Calculation { object Apply(Length? distance, Time? duration) => {|#0:distance + duration|}; }");
		test.ExpectedDiagnostics.Add(new DiagnosticResult("DQ0001", DiagnosticSeverity.Error).WithLocation(0).WithArguments("+", "Length", "Time"));
		await test.RunAsync(TestContext.Current.CancellationToken);
	}


	[Theory]
	[InlineData("object M(Length length) => length + ;")]
	[InlineData("void M() { Length value = ; }")]
	public async Task ParserRecoveryDoesNotGuessMissingOperandDimensions(string member)
	{
		await Invalid("class Consumer { " + member + " }").RunAsync(TestContext.Current.CancellationToken);
	}


	[Fact]
	public async Task AmbiguousOperatorCandidatesDoNotSuppressMismatch()
	{
		const string source = """
			using First;
			using Second;
			namespace First { public static class Operators { extension(Length) { public static string operator +(Length length, Time time) => "first"; } } }
			namespace Second { public static class Operators { extension(Length) { public static string operator +(Length length, Time time) => "second"; } } }
			class Consumer { object M(Length length, Time time) => {|#0:length {|#1:+|} time|}; }
			""";
		QuantityAnalyzerTest<DimensionMismatchAnalyzer> test = Invalid(source);
		test.ExpectedDiagnostics.Add(new DiagnosticResult("DQ0001", DiagnosticSeverity.Error).WithLocation(0).WithArguments("+", "Length", "Time"));
		QuantityGeneratorTest binding = new() { TestCode = source };
		binding.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS9342").WithLocation(1));
		await binding.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = binding.Compilation.SyntaxTrees.First();
		BinaryExpressionSyntax expression = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<BinaryExpressionSyntax>().Single();
		SymbolInfo candidates = binding.Compilation.GetSemanticModel(tree).GetSymbolInfo(expression, TestContext.Current.CancellationToken);
		Assert.Multiple(
			() => Assert.Null(candidates.Symbol),
			() => Assert.Equal(new[] { "First", "Second" }, candidates.CandidateSymbols.Select(symbol => symbol.ContainingNamespace.ToDisplayString()).Order()));
		await test.RunAsync(TestContext.Current.CancellationToken);
	}
	private static QuantityAnalyzerTest<DimensionMismatchAnalyzer> Invalid(string source) => new() { TestCode = source, CompilerDiagnostics = CompilerDiagnostics.None };
	private static DiagnosticResult WrongDestination() => new DiagnosticResult("DQ0002", DiagnosticSeverity.Error).WithLocation(0).WithArguments("Length", "Time");
}
