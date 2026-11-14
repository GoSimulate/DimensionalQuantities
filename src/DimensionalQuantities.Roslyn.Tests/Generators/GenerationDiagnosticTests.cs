using System.Collections.Immutable;
using DimensionalQuantities.Roslyn.Generators;
using DimensionalQuantities.Roslyn.Tests.Framework;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generators;

public sealed class GenerationDiagnosticTests
{
	[Fact]
	public async Task GeneratedAliasWarnsWhenItShadowsAnImportedType()
	{
		QuantityGeneratorTest test = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};
		test.TestState.Sources.Add(("Consumer.cs", """
			using Other;
			namespace Other
			{
				public class Current { }
			}
			public class Consumer
			{
				public {|#0:Current|} Value { get; }
			}
			"""));
		test.ExpectedDiagnostics.Add(new DiagnosticResult("DQ0006", DiagnosticSeverity.Warning)
			.WithLocation(0).WithMessage("Generated alias 'Current' conflicts with a name in this source"));

		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task ImportedMetadataTypeShadowingIsDiagnosed()
	{
		QuantityGeneratorTest test = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};
		test.TestState.Sources.Add(("Consumer.cs", """
			using Other;
			public class Consumer
			{
				public {|#0:Current|} Value { get; }
			}
			"""));
		test.TestState.AdditionalReferences.Add(await CreateImportedLibraryReference());
		test.ExpectedDiagnostics.Add(new DiagnosticResult("DQ0006", DiagnosticSeverity.Warning)
			.WithLocation(0).WithMessage("Generated alias 'Current' conflicts with a name in this source"));

		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task CollisionWarningPreservesTheGeneratedAliasBinding()
	{
		QuantityGeneratorTest test = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};
		test.TestState.Sources.Add(("Consumer.cs", """
			using Other;
			namespace Other
			{
				public class Current { }
			}
			public class Consumer
			{
				public {|#0:Current|} Value { get; }
			}
			"""));
		test.ExpectedDiagnostics.Add(new DiagnosticResult("DQ0006", DiagnosticSeverity.Warning)
			.WithLocation(0).WithMessage("Generated alias 'Current' conflicts with a name in this source"));

		await test.RunAsync(TestContext.Current.CancellationToken);
		IPropertySymbol property = (IPropertySymbol)test.Compilation.GetTypeByMetadataName("Consumer")!.GetMembers("Value").Single();
		INamedTypeSymbol result = ((INamedTypeSymbol)property.Type).OriginalDefinition;
		INamedTypeSymbol expected = test.Compilation.GetTypeByMetadataName(typeof(Quantity<>).FullName!)!;

		Assert.True(SymbolEqualityComparer.Default.Equals(expected, result));
	}

	[Fact]
	public async Task ConflictingGlobalTypeDeclarationWarns()
	{
		QuantityGeneratorTest test = new()
		{
			// The conflicting global name causes cascading errors throughout the generated catalogue.
			// This case verifies the collision warning; successful compilations are checked separately.
			CompilerDiagnostics = CompilerDiagnostics.None,
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};
		test.TestState.Sources.Add(("Consumer.cs", """
			public class {|#0:Current|} { }
			public class Consumer { }
			"""));
		test.ExpectedDiagnostics.Add(new DiagnosticResult("DQ0006", DiagnosticSeverity.Warning)
			.WithLocation(0).WithMessage("Generated alias 'Current' conflicts with a name in this source"));

		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task ConflictingDelegateDeclarationWarns()
	{
		QuantityGeneratorTest test = new()
		{
			// The conflicting global name causes cascading errors throughout the generated catalogue.
			// This case verifies the collision warning; successful compilations are checked separately.
			CompilerDiagnostics = CompilerDiagnostics.None,
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};
		test.TestState.Sources.Add(("Consumer.cs", """
			public delegate void {|#0:Current|}();
			"""));
		test.ExpectedDiagnostics.Add(new DiagnosticResult("DQ0006", DiagnosticSeverity.Warning)
			.WithLocation(0).WithMessage("Generated alias 'Current' conflicts with a name in this source"));

		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task ConflictingExplicitAliasWarns()
	{
		QuantityGeneratorTest test = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};
		test.TestState.Sources.Add(("Consumer.cs", """
			using {|#0:Current|} = Other.Current;
			namespace Other
			{
				public class Current { }
			}
			public class Consumer
			{
				public Current Value { get; }
			}
			"""));
		test.ExpectedDiagnostics.Add(new DiagnosticResult("DQ0006", DiagnosticSeverity.Warning)
			.WithLocation(0).WithMessage("Generated alias 'Current' conflicts with a name in this source"));
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS1537").WithLocation(0));

		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task StaticMemberUseOfImportedTypeWarnsWhenShadowed()
	{
		QuantityGeneratorTest test = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};
		test.TestState.Sources.Add(("Consumer.cs", """
			using Other;
			namespace Other
			{
				public class Current
				{
					public static int Create() => 1;
				}
			}
			public class Consumer
			{
				public int Value => {|#0:Current|}.{|#1:Create|}();
			}
			"""));
		test.ExpectedDiagnostics.Add(new DiagnosticResult("DQ0006", DiagnosticSeverity.Warning)
			.WithLocation(0).WithMessage("Generated alias 'Current' conflicts with a name in this source"));
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS0117").WithLocation(1));

		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task UnusedSameNamedTypeInAnotherNamespaceDoesNotWarn()
	{
		QuantityGeneratorTest test = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};
		test.TestState.Sources.Add(("Consumer.cs", """
			namespace Other
			{
				public class Current { }
			}
			public class Consumer
			{
				public Current Value { get; }
			}
			"""));

		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task QualifiedReferenceToSameNamedTypeDoesNotWarn()
	{
		QuantityGeneratorTest test = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};
		test.TestState.Sources.Add(("Consumer.cs", """
			namespace Other
			{
				public class Current { }
			}
			public class Consumer
			{
				public Other.Current Value { get; }
			}
			"""));

		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Theory]
	[InlineData("NumberTwoOverZero")]
	[InlineData("NumberBanana")]
	public async Task InvalidEnglishExponentExplainsUnresolvedRequest(string name)
	{
		QuantityGeneratorTest test = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};
		test.TestState.Sources.Add(("Consumer.cs", $$"""
			using DimensionalQuantities.TypeSystem;
			public class Consumer
			{
				public object Calculate(Length length) => {|#1:Power<{|#0:{{name}}|}>|}.Of(length);
			}
			"""));
		test.ExpectedDiagnostics.Add(new DiagnosticResult("DQ0007", DiagnosticSeverity.Error)
			.WithLocation(0).WithMessage($"English exponent '{name}' cannot be resolved"));
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS0246").WithLocation(0));
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS1929").WithLocation(1));

		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task FileLocalAliasElsewhereDoesNotHideInvalidEnglishExponent()
	{
		QuantityGeneratorTest test = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};
		test.TestState.Sources.Add(("Consumer.cs", """
			using DimensionalQuantities.TypeSystem;
			public class Consumer
			{
				public object Calculate(Length length) => {|#1:Power<{|#0:NumberBanana|}>|}.Of(length);
			}
			"""));
		test.TestState.Sources.Add(("OtherConsumer.cs", """
			using NumberBanana = System.Int32;
			public class OtherConsumer { }
			"""));
		test.ExpectedDiagnostics.Add(new DiagnosticResult("DQ0007", DiagnosticSeverity.Error)
			.WithLocation(0).WithMessage("English exponent 'NumberBanana' cannot be resolved"));
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS0246").WithLocation(0));
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS1929").WithLocation(1));

		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task ValidEnglishExponentDoesNotProduceGenerationDiagnostics()
	{
		QuantityGeneratorTest test = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};
		test.TestState.Sources.Add(("Consumer.cs", """
			using DimensionalQuantities.TypeSystem;
			public class Consumer
			{
				public object Calculate(Length length) => Power<NumberTwo>.Of(length);
			}
			"""));

		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task UnrelatedMissingNameDoesNotProduceGenerationDiagnostics()
	{
		QuantityGeneratorTest test = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};
		test.TestState.Sources.Add(("Consumer.cs", """
			using DimensionalQuantities.TypeSystem;
			public class Consumer
			{
				public object Calculate(Length length) => {|#1:Power<{|#0:Missing|}>|}.Of(length);
			}
			"""));
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS0246").WithLocation(0));
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS1929").WithLocation(1));

		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task UnrelatedPowerTypeDoesNotProduceGenerationDiagnostics()
	{
		QuantityGeneratorTest test = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};
		test.TestState.Sources.Add(("Consumer.cs", """
			public class Power<T>
			{
				public static double Of(double value) => value;
			}
			public class Consumer
			{
				public object Calculate() => Power<{|#0:NumberBanana|}>.Of(2);
			}
			"""));
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS0246").WithLocation(0));

		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task ExplicitlyBoundExponentDoesNotProduceGenerationDiagnostics()
	{
		QuantityGeneratorTest test = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};
		test.TestState.Sources.Add(("Consumer.cs", """
			using NumberTwo = DimensionalQuantities.TypeSystem.TerminalHex2;
			using DimensionalQuantities.TypeSystem;
			public class Consumer
			{
				public object Calculate(Length length) => Power<NumberTwo>.Of(length);
			}
			"""));

		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task BoundInvalidEnglishNameDoesNotProduceGenerationDiagnostics()
	{
		QuantityGeneratorTest test = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};
		test.TestState.Sources.Add(("Consumer.cs", """
			using DimensionalQuantities.TypeSystem;
			public class NumberBanana { }
			public class Consumer
			{
				public object Calculate(Length length) => {|#1:Power<{|#0:NumberBanana|}>|}.Of(length);
			}
			"""));
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS0453").WithLocation(0));
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS1929").WithLocation(1));

		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task InvalidNumericEncodingDoesNotCrashGenerator()
	{
		QuantityGeneratorTest test = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};
		test.TestState.Sources.Add(("Consumer.cs", """
			using DimensionalQuantities.TypeSystem;
			public class Consumer
			{
				public object Calculate(Length length) => {|#0:Power<Fraction<TerminalHex1, {|#1:TerminalHex0|}>>|}.Of(length);
			}
			"""));

		test.ExpectedDiagnostics.AddRange([
			DiagnosticResult.CompilerError("CS1929").WithLocation(0),
			DiagnosticResult.CompilerError("CS0315").WithLocation(1)
		]);

		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public void CorrectingAnEnglishExponentRemovesItsDiagnostic()
	{
		const string source = """
			using DimensionalQuantities.TypeSystem;
			public class Consumer
			{
				public object Calculate(Length length) => Power<NumberBanana>.Of(length);
			}
			""";
		CSharpCompilation original = CreateLifecycleCompilation(source);
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: new CSharpParseOptions(LanguageVersion.Preview));
		driver = driver.RunGenerators(original, TestContext.Current.CancellationToken);
		ImmutableArray<Diagnostic> initial = driver.GetRunResult().Diagnostics;
		CSharpCompilation edited = CreateLifecycleCompilation(source.Replace("NumberBanana", "NumberTwo"));
		driver = driver.RunGenerators(edited, TestContext.Current.CancellationToken);
		ImmutableArray<Diagnostic> result = driver.GetRunResult().Diagnostics;

		Assert.Multiple(
			() => Assert.Contains(initial, diagnostic => diagnostic.Id == "DQ0007"),
			() => Assert.Empty(result));
	}

	private static async Task<PortableExecutableReference> CreateImportedLibraryReference()
	{
		const string source = """
			namespace Other
			{
				public class Current { }
			}
			""";
		ImmutableArray<MetadataReference> references = await ReferenceAssemblies.Net.Net100.ResolveAsync(
			LanguageNames.CSharp, TestContext.Current.CancellationToken);
		CSharpCompilation library = CSharpCompilation.Create("OtherLibrary",
			[CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))],
			references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		using MemoryStream bytes = new();
		Microsoft.CodeAnalysis.Emit.EmitResult result = library.Emit(bytes, cancellationToken: TestContext.Current.CancellationToken);
		if (!result.Success)
		{
			throw new InvalidOperationException(string.Join(Environment.NewLine, result.Diagnostics));
		}

		return MetadataReference.CreateFromImage(bytes.ToArray());
	}

	private static CSharpCompilation CreateLifecycleCompilation(string source)
	{
		string[] assemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = assemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		return CSharpCompilation.Create("Consumer", [CSharpSyntaxTree.ParseText(source,
			new CSharpParseOptions(LanguageVersion.Preview), "Consumer.cs", cancellationToken: TestContext.Current.CancellationToken)],
			references, new(OutputKind.DynamicallyLinkedLibrary));
	}
}
