using DimensionalQuantities.Roslyn.CodeFixes;
using DimensionalQuantities.Roslyn.Analyzers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.CodeFixes;

public sealed class QuantityAliasFixTests
{
	private const string RawLength = "Quantity<Dimension<TerminalHex1, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0>>";
	private const string RawTime = "Quantity<Dimension<TerminalHex0, TerminalHex0, TerminalHex1, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0>>";
	private const string RawLengthCurrent = "Quantity<Dimension<TerminalHex1, TerminalHex0, TerminalHex0, TerminalHex1, TerminalHex0, TerminalHex0, TerminalHex0>>";

	[Theory]
	[InlineData(RawTime, "Time")]
	[InlineData(RawLengthCurrent, "LengthCurrent")]
	public async Task FixUsesTheSuggestedNonLengthAlias(string rawType, string alias)
	{
		QuantityCodeFixTest test = new()
		{
			TestCode = $$"""
				using DimensionalQuantities.TypeSystem;

				class Measurement
				{
					{|#0:{{rawType}}|} field;
				}
				""",
			FixedCode = $$"""
				using DimensionalQuantities.TypeSystem;

				class Measurement
				{
					{{alias}} field;
				}
				""",
			CodeActionEquivalenceKey = "UseAlias:" + alias,
			CodeActionVerifier = (action, verifier) => Assert.Equal("Use " + alias, action.Title)
		};
		test.ExpectedDiagnostics.Add(Suggestion(0, alias));
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public void ProviderAdvertisesOnlyAliasSuggestionAndActualBatchFixer()
	{
		QuantityAliasCodeFixProvider provider = new();
		Assert.Multiple(
			() => Assert.Equal(new[] { "DQ0003" }, provider.FixableDiagnosticIds),
			() => Assert.Same(Microsoft.CodeAnalysis.CodeFixes.WellKnownFixAllProviders.BatchFixer, provider.GetFixAllProvider()));
	}

	[Theory]
	[InlineData(
		"""
			{0} field;
			""",
		"""
			Length field;
			""")]
	[InlineData(
		"""
			{0}?[] field;
			""",
		"""
			Length?[] field;
			""")]
	[InlineData(
		"""
			System.Collections.Generic.List<{0}> field;
			""",
		"""
			System.Collections.Generic.List<Length> field;
			""")]
	[InlineData(
		"""
			global::DimensionalQuantities.TypeSystem.{0} field;
			""",
		"""
			Length field;
			""")]
	[InlineData(
		"""
			/* before */ {0} /* after */ field;
			""",
		"""
			/* before */ Length /* after */ field;
			""")]
	public async Task TypeReplacementPreservesWrappersAndExteriorSyntax(string declaration, string fixedDeclaration)
	{
		QuantityCodeFixTest test = new()
		{
			TestCode = $$"""
				using DimensionalQuantities.TypeSystem;

				class Measurement
				{
					{{string.Format(declaration, "{|#0:" + RawLength + "|}")}}
				}
				""",
			FixedCode = $$"""
				using DimensionalQuantities.TypeSystem;

				class Measurement
				{
					{{fixedDeclaration}}
				}
				""",
			CodeActionEquivalenceKey = "UseAlias:Length",
			CodeActionVerifier = (action, verifier) => Assert.Equal("Use Length", action.Title)
		};
		test.ExpectedDiagnostics.Add(Suggestion(0, "Length"));
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData(
		"""
			class Length
			{
			}
			""")]
	[InlineData(
		"""
			using Length = System.String;
			""")]
	public async Task DifferentlyBoundAliasWithholdsFix(string shadow)
	{
		QuantityCodeFixTest test = new()
		{
			TestCode = $$"""
				using DimensionalQuantities.TypeSystem;

				namespace Consumer
				{
					{{shadow}}

					class Measurement
					{
						{|#0:{{RawLength}}|} field;
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
	public async Task SameTypeVisibleAliasIsEligible()
	{
		QuantityCodeFixTest test = new()
		{
			TestCode = $$"""
				using DimensionalQuantities.TypeSystem;

				namespace Consumer
				{
					using Length = {{RawLength}};

					class Measurement
					{
						{|#0:{{RawLength}}|} field;
					}
				}
				""",
			FixedCode = $$"""
				using DimensionalQuantities.TypeSystem;

				namespace Consumer
				{
					using Length = {{RawLength}};

					class Measurement
					{
						Length field;
					}
				}
				""",
			CodeActionEquivalenceKey = "UseAlias:Length",
			CodeActionVerifier = (action, verifier) => Assert.Equal("Use Length", action.Title)
		};
		test.ExpectedDiagnostics.Add(Suggestion(0, "Length"));
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData(
		"""
			nameof({0})
			""")]
	[InlineData(
		"""
			nameof(System.Collections.Generic.List<{0}>)
			""")]
	public async Task RealNameofOperationWithholdsFix(string expression)
	{
		QuantityCodeFixTest test = new()
		{
			TestCode = $$"""
				using DimensionalQuantities.TypeSystem;

				class Measurement
				{
					string name = {{string.Format(expression, "{|#0:" + RawLength + "|}")}};
				}
				""",
			NumberOfIncrementalIterations = 0,
			NumberOfFixAllIterations = 0
		};
		test.ExpectedDiagnostics.Add(Suggestion(0, "Length"));
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task OrdinaryInvocationNamedNameofIsEligible()
	{
		QuantityCodeFixTest test = new()
		{
			TestCode = $$"""
				using DimensionalQuantities.TypeSystem;

				class Measurement
				{
					static string @nameof(System.Type type) => type.Name;
					string name = @nameof(typeof({|#0:{{RawLength}}|}));
				}
				""",
			FixedCode = """
				using DimensionalQuantities.TypeSystem;

				class Measurement
				{
					static string @nameof(System.Type type) => type.Name;
					string name = @nameof(typeof(Length));
				}
				""",
			CodeActionEquivalenceKey = "UseAlias:Length",
			CodeActionVerifier = (action, verifier) => Assert.Equal("Use Length", action.Title)
		};
		test.ExpectedDiagnostics.Add(Suggestion(0, "Length"));
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task LengthFixAllChangesEligibleSitesAcrossDocumentsAndKeepsOtherAliases()
	{
		QuantityCodeFixTest test = new()
		{
			CodeActionEquivalenceKey = "UseAlias:Length",
			NumberOfIncrementalIterations = 3,
			NumberOfFixAllIterations = 1,
			NumberOfFixAllInDocumentIterations = 2
		};
		test.TestState.Sources.Add(("Measurements.cs", $$"""
			using DimensionalQuantities.TypeSystem;

			class Measurement
			{
				{|#0:{{RawLength}}|} first;
				{|#1:{{RawLength}}|} second;
				{|#2:{{RawTime}}|} time;
				string name = nameof({|#3:{{RawLength}}|});
			}
			"""));
		test.TestState.Sources.Add(("Other.cs", $$"""
			using DimensionalQuantities.TypeSystem;

			class Other
			{
				{|#4:{{RawLength}}|} value;
			}

			namespace Hidden
			{
				class Length
				{
				}

				class Shadowed
				{
					{|#5:{{RawLength}}|} value;
				}
			}
			"""));
		test.FixedState.Sources.Add(("Measurements.cs", $$"""
			using DimensionalQuantities.TypeSystem;

			class Measurement
			{
				Length first;
				Length second;
				{|#2:{{RawTime}}|} time;
				string name = nameof({|#3:{{RawLength}}|});
			}
			"""));
		test.FixedState.Sources.Add(("Other.cs", $$"""
			using DimensionalQuantities.TypeSystem;

			class Other
			{
				Length value;
			}

			namespace Hidden
			{
				class Length
				{
				}

				class Shadowed
				{
					{|#5:{{RawLength}}|} value;
				}
			}
			"""));
		foreach (int location in new[] { 0, 1, 3, 4, 5 })
		{
			test.ExpectedDiagnostics.Add(Suggestion(location, "Length"));
		}

		test.ExpectedDiagnostics.Add(Suggestion(2, "Time"));
		test.FixedState.ExpectedDiagnostics.AddRange(new[] { Suggestion(2, "Time"), Suggestion(3, "Length"), Suggestion(5, "Length") });
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("", "")]
	[InlineData(
		"""
			using First;
			using Second;
			""",
		"""
			namespace First
			{
				public class Length
				{
				}
			}

			namespace Second
			{
				public class Length
				{
				}
			}
			""")]
	[InlineData(
		"""
			using Length = Hidden.Container.Length;
			""",
		"""
			namespace Hidden
			{
				public class Container
				{
					private class Length
					{
					}
				}
			}
			""")]
	public async Task MissingAmbiguousOrInaccessibleReplacementWithholdsFix(string directives, string declarations)
	{
		CSharpCodeFixTest<QuantityEncodingAnalyzer, QuantityAliasCodeFixProvider, DefaultVerifier> test = new()
		{
			TestCode = $$"""
				using DimensionalQuantities.TypeSystem;
				{{directives}}

				class Measurement
				{
					{|#0:{{RawLength}}|} field;
				}

				{{declarations}}
				""",
			CompilerDiagnostics = CompilerDiagnostics.None,
			NumberOfIncrementalIterations = 0,
			NumberOfFixAllIterations = 0
		};
		QuantityTestEnvironment.Configure(test);
		test.ExpectedDiagnostics.Add(Suggestion(0, "Length"));
		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	private static DiagnosticResult Suggestion(int location, string alias) => new DiagnosticResult("DQ0003", DiagnosticSeverity.Info).WithLocation(location).WithArguments(alias);
}
