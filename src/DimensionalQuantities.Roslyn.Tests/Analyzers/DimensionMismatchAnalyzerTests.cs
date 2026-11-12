using DimensionalQuantities.Roslyn.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using DimensionalQuantities.Roslyn.Tests.Testing;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Analyzers;

public sealed class DimensionMismatchAnalyzerTests
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
	public async Task IncompatibleOperandsExplainTheirDimensions(string operation)
	{
		string source = $$"""
			public class Consumer
			{
				public object Calculate(Length length, Current current) => {|#0:length {{operation}} current|};
			}
			""";
		await new QuantityAnalyzerTest<DimensionMismatchAnalyzer>
		{
			TestCode = source,
			CompilerDiagnostics = CompilerDiagnostics.None,
			ExpectedDiagnostics = { new DiagnosticResult(DimensionMismatchAnalyzer.IncompatibleOperands).WithLocation(0).WithArguments(operation, "Length", "Current") },
		}.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("public void Calculate(Length length) { Current current = {|#0:length|}; }")]
	[InlineData("public void Calculate(Length length, Current current) { current = {|#0:length|}; }")]
	[InlineData("public Current Calculate(Length length) => {|#0:length|};")]
	[InlineData("public Current Calculate(Length length) { return {|#0:length|}; }")]
	[InlineData("public void Calculate(Length length) { Accept({|#0:length|}); } private void Accept(Current current) { }")]
	[InlineData("public void Calculate(Length length) { Accept(current: {|#0:length|}); } private void Accept(Current current) { }")]
	[InlineData("public object Calculate(Length length, Current current) => Current.Min(current, {|#0:length|});")]
	[InlineData("public object Calculate(Length length, Current current) => Current.Clamp(current, current, {|#0:length|});")]
	[InlineData("public Current Value { get; } = {|#0:Length.FromMetres(1)|};")]
	[InlineData("public Current Value => {|#0:Length.FromMetres(1)|};")]
	[InlineData("public Current this[int index] => {|#0:Length.FromMetres(1)|};")]
	[InlineData("public Consumer(Current current) { } public object Calculate(Length length) => new Consumer({|#0:length|});")]
	public async Task WrongDestinationExplainsExpectedAndActualDimensions(string member)
	{
		string source = $"public class Consumer {{ {member} }}";
		await new QuantityAnalyzerTest<DimensionMismatchAnalyzer>
		{
			TestCode = source,
			CompilerDiagnostics = CompilerDiagnostics.None,
			ExpectedDiagnostics = { new DiagnosticResult(DimensionMismatchAnalyzer.WrongDestination).WithLocation(0).WithArguments("Current", "Length") },
		}.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("public object Calculate(Length length, Length other) => length + other;")]
	[InlineData("public object Calculate(Length length, Current current) => length * current;")]
	[InlineData("public object Calculate(Length length, Current current) => length / current;")]
	[InlineData("public object Calculate(Energy energy, Torque torque) => energy + torque;")]
	[InlineData("public Current? Calculate() => null;")]
	[InlineData("public Current Calculate() => default;")]
	[InlineData("public object Calculate(Length length) => length;")]
	[InlineData("public object Calculate(Length length) => length + missing;")]
	[InlineData("public void Calculate(Length length) { Accept(length); } private void Accept(Length value) { } private void Accept(Current value) { }")]
	public async Task ValidOrUnrelatedCodeDoesNotProduceDiagnostics(string member)
	{
		string source = $"public class Consumer {{ {member} }}";
		await new QuantityAnalyzerTest<DimensionMismatchAnalyzer>
		{
			TestCode = source,
			CompilerDiagnostics = CompilerDiagnostics.None,
		}.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("+=")]
	[InlineData("-=")]
	public async Task CompoundAssignmentsExplainIncompatibleDimensions(string operation)
	{
		string source = $"public class Consumer {{ public void Calculate(Length length, Current current) {{ {{|#0:length {operation} current|}}; }} }}";
		await new QuantityAnalyzerTest<DimensionMismatchAnalyzer>
		{
			TestCode = source,
			CompilerDiagnostics = CompilerDiagnostics.None,
			ExpectedDiagnostics = { new DiagnosticResult(DimensionMismatchAnalyzer.IncompatibleOperands).WithLocation(0).WithArguments(operation, "Length", "Current") },
		}.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task NullableOperandsStillExplainDimensions()
	{
		const string source = "public class Consumer { public object Calculate(Length? length, Current? current) => {|#0:length + current|}; }";
		await new QuantityAnalyzerTest<DimensionMismatchAnalyzer>
		{
			TestCode = source,
			CompilerDiagnostics = CompilerDiagnostics.None,
			ExpectedDiagnostics = { new DiagnosticResult(DimensionMismatchAnalyzer.IncompatibleOperands).WithLocation(0).WithArguments("+", "Length", "Current") },
		}.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task ExistingUserOperatorIsRespected()
	{
		const string source = """
			public static class Custom
			{
				extension(Length)
				{
					public static double operator +(Length length, Current current) => 1;
				}
			}
			public class Consumer { public double Calculate(Length length, Current current) => length + current; }
			""";
		await new QuantityAnalyzerTest<DimensionMismatchAnalyzer>
		{
			TestCode = source,
			CompilerDiagnostics = CompilerDiagnostics.None,
		}.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task UnrelatedCompilationWithoutLibraryDoesNotCrash()
	{
		await new CSharpAnalyzerTest<DimensionMismatchAnalyzer, DefaultVerifier> { TestCode = "class Consumer { }" }
			.RunAsync(TestContext.Current.CancellationToken);
	}
}
