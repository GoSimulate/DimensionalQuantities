using System.Collections.Immutable;
using DimensionalQuantities.Roslyn.Analyzers;
using Microsoft.CodeAnalysis;
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
				public object Calculate(Length length, Current current) => length {{operation}} current;
			}
			""";
		Diagnostic result = Assert.Single(await AnalyzerTestCompilation.Analyze(source, new DimensionMismatchAnalyzer()));
		Assert.Multiple(
			() => Assert.Equal("DQ0001", result.Id),
			() => Assert.Equal(DiagnosticSeverity.Error, result.Severity),
			() => Assert.Contains("Length", result.GetMessage()),
			() => Assert.Contains("Current", result.GetMessage()),
			() => Assert.Equal($"length {operation} current", source.Substring(result.Location.SourceSpan.Start, result.Location.SourceSpan.Length)));
	}

	[Theory]
	[InlineData("public void Calculate(Length length) { Current current = length; }")]
	[InlineData("public void Calculate(Length length, Current current) { current = length; }")]
	[InlineData("public Current Calculate(Length length) => length;")]
	[InlineData("public Current Calculate(Length length) { return length; }")]
	[InlineData("public void Calculate(Length length) { Accept(length); } private void Accept(Current current) { }")]
	[InlineData("public void Calculate(Length length) { Accept(current: length); } private void Accept(Current current) { }")]
	[InlineData("public object Calculate(Length length, Current current) => Current.Min(current, length);")]
	[InlineData("public object Calculate(Length length, Current current) => Current.Clamp(current, current, length);")]
	[InlineData("public Current Value { get; } = Length.FromMetres(1);")]
	[InlineData("public Current Value => Length.FromMetres(1);")]
	[InlineData("public Current this[int index] => Length.FromMetres(1);")]
	[InlineData("public Consumer(Current current) { } public object Calculate(Length length) => new Consumer(length);")]
	public async Task WrongDestinationExplainsExpectedAndActualDimensions(string member)
	{
		string source = $"public class Consumer {{ {member} }}";
		Diagnostic result = Assert.Single(await AnalyzerTestCompilation.Analyze(source, new DimensionMismatchAnalyzer()));
		Assert.Multiple(
			() => Assert.Equal("DQ0002", result.Id),
			() => Assert.Equal(DiagnosticSeverity.Error, result.Severity),
			() => Assert.Contains("Expected Current", result.GetMessage()),
			() => Assert.Contains("Length", result.GetMessage()),
			() => Assert.Equal("Consumer.cs", result.Location.SourceTree!.FilePath));
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
		ImmutableArray<Diagnostic> result = await AnalyzerTestCompilation.Analyze($"public class Consumer {{ {member} }}", new DimensionMismatchAnalyzer());
		Assert.Empty(result);
	}

	[Theory]
	[InlineData("+=")]
	[InlineData("-=")]
	public async Task CompoundAssignmentsExplainIncompatibleDimensions(string operation)
	{
		string source = $"public class Consumer {{ public void Calculate(Length length, Current current) {{ length {operation} current; }} }}";
		Diagnostic result = Assert.Single(await AnalyzerTestCompilation.Analyze(source, new DimensionMismatchAnalyzer()));
		Assert.Equal("DQ0001", result.Id);
	}

	[Fact]
	public async Task NullableOperandsStillExplainDimensions()
	{
		const string source = "public class Consumer { public object Calculate(Length? length, Current? current) => length + current; }";
		Diagnostic result = Assert.Single(await AnalyzerTestCompilation.Analyze(source, new DimensionMismatchAnalyzer()));
		Assert.Equal("DQ0001", result.Id);
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
		ImmutableArray<Diagnostic> result = await AnalyzerTestCompilation.Analyze(source, new DimensionMismatchAnalyzer());
		Assert.Empty(result);
	}

	[Fact]
	public async Task UnrelatedCompilationWithoutLibraryDoesNotCrash()
	{
		Microsoft.CodeAnalysis.CSharp.CSharpCompilation compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("Empty");
		ImmutableArray<Diagnostic> result = await Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzerExtensions.WithAnalyzers(compilation,
			[new DimensionMismatchAnalyzer()], options: null).GetAnalyzerDiagnosticsAsync(Xunit.TestContext.Current.CancellationToken);
		Assert.Empty(result);
	}
}
