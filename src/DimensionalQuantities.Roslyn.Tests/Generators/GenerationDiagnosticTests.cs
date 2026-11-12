using DimensionalQuantities.Roslyn.Tests.Testing;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generators;

public sealed class GenerationDiagnosticTests
{
	[Fact]
	public async Task GeneratedAliasWarnsWhenItShadowsAnImportedType()
	{
		const string source = """
			using Other;
			namespace Other { public class Current { } }
			public class Consumer { public Current Value { get; } }
			""";
		await new QuantityGeneratorTest
		{
			TestCode = source.Replace("public Current Value", "public {|#0:Current|} Value"),
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			ExpectedDiagnostics = { new DiagnosticResult("DQ0006", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("Current") },
		}.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task ImportedMetadataTypeShadowingIsDiagnosed()
	{
		QuantityGeneratorTest library = new()
		{
			TestCode = "namespace Other { public class Current { } }",
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
		};
		await library.RunAsync(TestContext.Current.CancellationToken);
		using MemoryStream bytes = new();
		EmitResult emitted = library.Compilation.WithAssemblyName("OtherLibrary").Emit(bytes, cancellationToken: TestContext.Current.CancellationToken);
		if (!emitted.Success)
		{
			throw new InvalidOperationException(string.Join(Environment.NewLine, emitted.Diagnostics));
		}
		await new QuantityGeneratorTest
		{
			TestCode = "using Other; public class Consumer { public {|#0:Current|} Value { get; } }",
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState = { AdditionalReferences = { MetadataReference.CreateFromImage(bytes.ToArray()) } },
			ExpectedDiagnostics = { new DiagnosticResult("DQ0006", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("Current") },
		}.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task CollisionWarningPreservesTheGeneratedAliasBinding()
	{
		const string source = "using Other; namespace Other { public class Current { } } public class Consumer { public Current Value { get; } }";
		QuantityGeneratorTest test = new()
		{
			TestCode = source.Replace("public Current Value", "public {|#0:Current|} Value"),
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			ExpectedDiagnostics = { new DiagnosticResult("DQ0006", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("Current") },
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		Compilation generated = test.Compilation;
		IPropertySymbol property = (IPropertySymbol)generated.GetTypeByMetadataName("Consumer")!.GetMembers("Value").Single();
		INamedTypeSymbol result = ((INamedTypeSymbol)property.Type).OriginalDefinition;
		INamedTypeSymbol expected = generated.GetTypeByMetadataName(typeof(Quantity<>).FullName!)!;

		Assert.Multiple(
			() => Assert.True(SymbolEqualityComparer.Default.Equals(expected, result)));
	}

	[Theory]
	[InlineData("public class Current { } public class Consumer { }")]
	[InlineData("public delegate void Current();")]
	[InlineData("using Current = Other.Current; namespace Other { public class Current { } } public class Consumer { public Current Value { get; } }")]
	public async Task ConflictingDeclarationsAndExplicitAliasesWarn(string source)
	{
		await new QuantityGeneratorTest
		{
			TestCode = source.Insert(source.IndexOf("Current", StringComparison.Ordinal) + "Current".Length, "|}").Insert(source.IndexOf("Current", StringComparison.Ordinal), "{|#0:"),
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			CompilerDiagnostics = CompilerDiagnostics.None,
			ExpectedDiagnostics = { new DiagnosticResult("DQ0006", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("Current") },
		}.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task StaticMemberUseOfImportedTypeWarnsWhenShadowed()
	{
		const string source = """
			using Other;
			namespace Other { public class Current { public static int Create() => 1; } }
			public class Consumer { public int Value => Current.Create(); }
			""";
		await new QuantityGeneratorTest
		{
			TestCode = source.Replace("=> Current.Create()", "=> {|#0:Current|}.Create()"),
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			CompilerDiagnostics = CompilerDiagnostics.None,
			ExpectedDiagnostics = { new DiagnosticResult("DQ0006", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("Current") },
		}.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task UnusedSameNamedTypeInAnotherNamespaceDoesNotWarn()
	{
		const string source = "namespace Other { public class Current { } } public class Consumer { public Current Value { get; } }";
		await new QuantityGeneratorTest
		{
			TestCode = source,
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
		}.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task QualifiedReferenceToSameNamedTypeDoesNotWarn()
	{
		const string source = "namespace Other { public class Current { } } public class Consumer { public Other.Current Value { get; } }";
		await new QuantityGeneratorTest
		{
			TestCode = source,
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
		}.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("NumberTwoOverZero")]
	[InlineData("NumberBanana")]
	public async Task InvalidEnglishExponentExplainsUnresolvedRequest(string name)
	{
		string source = $$"""
			using DimensionalQuantities.TypeSystem;
			public class Consumer { public object Calculate(Length length) => Power<{{name}}>.Of(length); }
			""";
		await new QuantityGeneratorTest
		{
			TestCode = source.Replace(name, "{|#0:" + name + "|}"),
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			CompilerDiagnostics = CompilerDiagnostics.None,
			ExpectedDiagnostics = { new DiagnosticResult("DQ0007", DiagnosticSeverity.Error).WithLocation(0).WithArguments(name) },
		}.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task FileLocalAliasElsewhereDoesNotHideInvalidEnglishExponent()
	{
		const string source = "using DimensionalQuantities.TypeSystem; public class Consumer { public object Calculate(Length length) => Power<NumberBanana>.Of(length); }";
		await new QuantityGeneratorTest
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			CompilerDiagnostics = CompilerDiagnostics.None,
			TestState =
			{
				Sources =
				{
					("Consumer.cs", source.Replace("NumberBanana", "{|#0:NumberBanana|}")),
					("OtherConsumer.cs", "using NumberBanana = System.Int32; public class OtherConsumer { }"),
				},
			},
			ExpectedDiagnostics = { new DiagnosticResult("DQ0007", DiagnosticSeverity.Error).WithLocation(0).WithArguments("NumberBanana") },
		}.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("using DimensionalQuantities.TypeSystem; public class Consumer { public object Calculate(Length length) => Power<NumberTwo>.Of(length); }")]
	[InlineData("using DimensionalQuantities.TypeSystem; public class Consumer { public object Calculate(Length length) => Power<Missing>.Of(length); }")]
	[InlineData("public class Power<T> { public static double Of(double value) => value; } public class Consumer { public object Calculate() => Power<NumberBanana>.Of(2); }")]
	[InlineData("using NumberTwo = DimensionalQuantities.TypeSystem.TerminalHex2; using DimensionalQuantities.TypeSystem; public class Consumer { public object Calculate(Length length) => Power<NumberTwo>.Of(length); }")]
	[InlineData("using DimensionalQuantities.TypeSystem; public class NumberBanana { } public class Consumer { public object Calculate(Length length) => Power<NumberBanana>.Of(length); }")]
	public async Task BoundNamesAndUnrelatedRequestsDoNotProduceGenerationDiagnostics(string source)
	{
		await new QuantityGeneratorTest
		{
			TestCode = source,
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			CompilerDiagnostics = CompilerDiagnostics.None,
		}.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task InvalidNumericEncodingDoesNotCrashGenerator()
	{
		const string source = """
			using DimensionalQuantities.TypeSystem;
			public class Consumer
			{
				public object Calculate(Length length) => Power<Fraction<TerminalHex1, TerminalHex0>>.Of(length);
			}
			""";
		await new QuantityGeneratorTest
		{
			TestCode = source,
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			CompilerDiagnostics = CompilerDiagnostics.None,
		}.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task InvalidEnglishExponentHasAResolutionDiagnostic()
	{
		const string source = "using DimensionalQuantities.TypeSystem; public class Consumer { public object Calculate(Length length) => Power<{|#0:NumberBanana|}>.Of(length); }";
		await new QuantityGeneratorTest
		{
			TestCode = source,
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			CompilerDiagnostics = CompilerDiagnostics.None,
			ExpectedDiagnostics = { new DiagnosticResult("DQ0007", DiagnosticSeverity.Error).WithLocation(0).WithArguments("NumberBanana") },
		}.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task ValidEnglishExponentHasNoResolutionDiagnostic()
	{
		const string source = "using DimensionalQuantities.TypeSystem; public class Consumer { public object Calculate(Length length) => Power<NumberTwo>.Of(length); }";
		await new QuantityGeneratorTest
		{
			TestCode = source,
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
		}.RunAsync(TestContext.Current.CancellationToken);
	}
}
