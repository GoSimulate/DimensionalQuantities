using DimensionalQuantities.Roslyn.Analyzers;
using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
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
		QuantityAnalyzerTest<DimensionMismatchAnalyzer> test = new()
		{
			TestCode = $$"""
				public class Consumer
				{
					public object Calculate(Length length, Current current) => {|#0:length {{operation}} current|};
				}
				"""
		};
		test.ExpectedDiagnostics.AddRange([
			DiagnosticResult.CompilerError("CS0019").WithLocation(0),
			new DiagnosticResult("DQ0001", DiagnosticSeverity.Error).WithLocation(0).WithArguments(operation, "Length", "Current")
		]);
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("""
		public void Calculate(Length length)
		{
			Current current = {|#0:length|};
		}
		""", "CS0029")]
	[InlineData("""
		public void Calculate(Length length, Current current)
		{
			current = {|#0:length|};
		}
		""", "CS0029")]
	[InlineData("""
		public Current Calculate(Length length) => {|#0:length|};
		""", "CS0029")]
	[InlineData("""
		public Current Calculate(Length length)
		{
			return {|#0:length|};
		}
		""", "CS0029")]
	[InlineData("""
		public void Calculate(Length length)
		{
			Accept({|#0:length|});
		}
		private void Accept(Current current)
		{
		}
		""", "CS1503")]
	[InlineData("""
		public void Calculate(Length length)
		{
			Accept(current: {|#0:length|});
		}
		private void Accept(Current current)
		{
		}
		""", "CS1503")]
	[InlineData("""
		public object Calculate(Length length, Current current) => Current.Min(current, {|#0:length|});
		""", "CS1503")]
	[InlineData("""
		public object Calculate(Length length, Current current) => Current.Clamp(current, current, {|#0:length|});
		""", "CS1503")]
	[InlineData("""
		public Current Value { get; } = {|#0:Length.FromMetres(1)|};
		""", "CS0029")]
	[InlineData("""
		public Current Value => {|#0:Length.FromMetres(1)|};
		""", "CS0029")]
	[InlineData("""
		public Current this[int index] => {|#0:Length.FromMetres(1)|};
		""", "CS0029")]
	[InlineData("""
		public Consumer(Current current)
		{
		}
		public object Calculate(Length length) => new Consumer({|#0:length|});
		""", "CS1503")]
	public async Task WrongDestinationExplainsExpectedAndActualDimensions(string member, string compilerError)
	{
		QuantityAnalyzerTest<DimensionMismatchAnalyzer> test = new();
		test.TestState.Sources.Add(("Consumer.cs", $$"""
			public class Consumer
			{
				{{member}}
			}
			"""));
		test.ExpectedDiagnostics.AddRange([
			DiagnosticResult.CompilerError(compilerError).WithLocation(0),
			new DiagnosticResult("DQ0002", DiagnosticSeverity.Error).WithLocation(0).WithArguments("Current", "Length")
		]);
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("""
		public object Calculate(Length length, Length other) => length + other;
		""")]
	[InlineData("""
		public object Calculate(Length length, Current current) => length * current;
		""")]
	[InlineData("""
		public object Calculate(Length length, Current current) => length / current;
		""")]
	[InlineData("""
		public object Calculate(Energy energy, Torque torque) => energy + torque;
		""")]
	[InlineData("""
		public Current? Calculate() => null;
		""")]
	[InlineData("""
		public Current Calculate() => default;
		""")]
	[InlineData("""
		public object Calculate(Length length) => length;
		""")]
	[InlineData("""
		public object Calculate(Length length) => length + {|CS0103:missing|};
		""")]
	[InlineData("""
		public void Calculate(Length length)
		{
			Accept(length);
		}
		private void Accept(Length value)
		{
		}
		private void Accept(Current value)
		{
		}
		""")]
	public async Task ValidOrUnrelatedCodeDoesNotProduceDiagnostics(string member)
	{
		QuantityAnalyzerTest<DimensionMismatchAnalyzer> test = new()
		{
			TestCode = $$"""
				public class Consumer
				{
					{{member}}
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("+=")]
	[InlineData("-=")]
	public async Task CompoundAssignmentsExplainIncompatibleDimensions(string operation)
	{
		QuantityAnalyzerTest<DimensionMismatchAnalyzer> test = new()
		{
			TestCode = $$"""
				public class Consumer
				{
					public void Calculate(Length length, Current current)
					{
						{|#0:length {{operation}} current|};
					}
				}
				"""
		};
		test.ExpectedDiagnostics.AddRange([
			DiagnosticResult.CompilerError("CS0019").WithLocation(0),
			new DiagnosticResult("DQ0001", DiagnosticSeverity.Error).WithLocation(0).WithArguments(operation, "Length", "Current")
		]);
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task NullableOperandsStillExplainDimensions()
	{
		QuantityAnalyzerTest<DimensionMismatchAnalyzer> test = new()
		{
			TestCode = """
				public class Consumer
				{
					public object Calculate(Length? length, Current? current) => {|#0:length + current|};
				}
				"""
		};
		test.ExpectedDiagnostics.AddRange([
			DiagnosticResult.CompilerError("CS0019").WithLocation(0),
			new DiagnosticResult("DQ0001", DiagnosticSeverity.Error).WithLocation(0).WithArguments("+", "Length", "Current")
		]);
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task ExistingUserOperatorIsRespected()
	{
		QuantityAnalyzerTest<DimensionMismatchAnalyzer> test = new()
		{
			TestCode = """
				public static class Custom
				{
					extension(Length)
					{
						public static double operator +(Length length, Current current) => 1;
					}
				}
				public class Consumer
				{
					public double Calculate(Length length, Current current) => length + current;
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task UnrelatedCompilationWithoutLibraryDoesNotCrash()
	{
		CSharpAnalyzerTest<DimensionMismatchAnalyzer, DefaultVerifier> test = new()
		{
			ReferenceAssemblies = ReferenceAssemblies.Net.Net100
		};
		test.TestState.Sources.Add(("Consumer.cs", ""));
		await test.RunAsync(TestContext.Current.CancellationToken);
	}
}
