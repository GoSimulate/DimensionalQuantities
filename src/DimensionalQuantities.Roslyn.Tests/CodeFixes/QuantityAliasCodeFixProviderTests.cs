using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.CodeFixes;

public sealed class QuantityAliasCodeFixProviderTests
{
	private const string LengthType = "Quantity<global::DimensionalQuantities.TypeSystem.Dimension<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex0, "
		+ "global::DimensionalQuantities.TypeSystem.TerminalHex0, global::DimensionalQuantities.TypeSystem.TerminalHex0, global::DimensionalQuantities.TypeSystem.TerminalHex0, "
		+ "global::DimensionalQuantities.TypeSystem.TerminalHex0, global::DimensionalQuantities.TypeSystem.TerminalHex0>>";

	private const string LengthCurrentType = "Quantity<global::DimensionalQuantities.TypeSystem.Dimension<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex0, "
		+ "global::DimensionalQuantities.TypeSystem.TerminalHex0, global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex0, "
		+ "global::DimensionalQuantities.TypeSystem.TerminalHex0, global::DimensionalQuantities.TypeSystem.TerminalHex0>>";

	[Theory]
	[InlineData("""
		public class Consumer
		{
			public TYPE Value { get; }
		}
		""", """
		public class Consumer
		{
			public Length Value { get; }
		}
		""")]
	[InlineData("""
		public class Consumer
		{
			public TYPE? Value { get; }
		}
		""", """
		public class Consumer
		{
			public Length? Value { get; }
		}
		""")]
	[InlineData("""
		public class Consumer
		{
			public /* measurement */ TYPE Value { get; }
		}
		""", """
		public class Consumer
		{
			public /* measurement */ Length Value { get; }
		}
		""")]
	[InlineData("""
		public class Consumer
		{
			public TYPE Value { get; }
		}
		""", """
		public class Consumer
		{
			public LengthCurrent Value { get; }
		}
		""", LengthCurrentType)]
	public async Task FixUsesAliasAndPreservesSurroundingSource(string template, string expected, string? rawType = null)
	{
		string type = rawType ?? LengthType;
		QuantityCodeFixTest test = new()
		{
			TestCode = template.Replace("TYPE", $$"""
				global::DimensionalQuantities.TypeSystem.{|#0:{{type}}|}
				"""),
			FixedCode = expected,
			CodeActionEquivalenceKey = rawType is null ? "UseAlias:Length" : "UseAlias:LengthCurrent"
		};
		test.ExpectedDiagnostics.Add(Suggestion(0, rawType is null ? "Length" : "LengthCurrent"));
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task NameOfDoesNotOfferFixThatChangesTheReturnedName()
	{
		QuantityCodeFixTest test = new()
		{
			TestCode = $$"""
				public class Consumer
				{
					public string Value() => nameof(global::DimensionalQuantities.TypeSystem.{|#0:{{LengthType}}|});
				}
				""",
			NumberOfIncrementalIterations = 0,
			NumberOfFixAllIterations = 0
		};
		test.ExpectedDiagnostics.Add(Suggestion(0, "Length"));
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task ShadowedAliasDoesNotOfferUnsafeFix()
	{
		QuantityCodeFixTest test = new()
		{
			TestCode = $$"""
				namespace ConsumerNamespace
				{
					public class Length
					{
					}
					public class Consumer
					{
						public global::DimensionalQuantities.TypeSystem.{|#0:{{LengthType}}|} Value { get; }
					}
				}
				""",
			NumberOfIncrementalIterations = 0,
			NumberOfFixAllIterations = 0
		};
		test.ExpectedDiagnostics.Add(Suggestion(0, "Length"));
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task FixAllReplacesMultipleRawQuantities()
	{
		QuantityCodeFixTest test = new()
		{
			TestCode = $$"""
				public class Consumer
				{
					public global::DimensionalQuantities.TypeSystem.{|#0:{{LengthType}}|} First { get; }
					public global::DimensionalQuantities.TypeSystem.{|#1:{{LengthType}}|} Second { get; }
				}
				""",
			FixedCode = """
				public class Consumer
				{
					public Length First { get; }
					public Length Second { get; }
				}
				""",
			CodeActionEquivalenceKey = "UseAlias:Length",
			NumberOfIncrementalIterations = 2,
			NumberOfFixAllIterations = 1,
			NumberOfFixAllInDocumentIterations = 1
		};
		test.ExpectedDiagnostics.AddRange([Suggestion(0, "Length"), Suggestion(1, "Length")]);
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	private static DiagnosticResult Suggestion(int location, string alias) => new DiagnosticResult("DQ0003", DiagnosticSeverity.Info).WithLocation(location).WithArguments(alias);
}
