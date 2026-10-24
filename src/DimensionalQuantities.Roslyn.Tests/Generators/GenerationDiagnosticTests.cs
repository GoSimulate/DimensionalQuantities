using System.Collections.Immutable;
using DimensionalQuantities.Roslyn.Generators;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generators;

public sealed class GenerationDiagnosticTests
{
	[Fact]
	public void GeneratedAliasWarnsWhenItShadowsAnImportedType()
	{
		const string source = """
			using Other;
			namespace Other { public class Current { } }
			public class Consumer { public Current Value { get; } }
			""";
		Diagnostic result = Assert.Single(Generate(source));
		Assert.Multiple(
			() => Assert.Equal("DQ0006", result.Id),
			() => Assert.Equal(DiagnosticSeverity.Warning, result.Severity),
			() => Assert.Contains("Current", result.GetMessage()),
			() => Assert.Equal("Consumer.cs", result.Location.SourceTree!.FilePath));
	}

	[Fact]
	public void ImportedMetadataTypeShadowingIsDiagnosed()
	{
		CSharpCompilation library = CreateCompilation("namespace Other { public class Current { } }").WithAssemblyName("OtherLibrary");
		using MemoryStream bytes = new();
		Microsoft.CodeAnalysis.Emit.EmitResult emitted = library.Emit(bytes, cancellationToken: TestContext.Current.CancellationToken);
		if (!emitted.Success)
		{
			throw new InvalidOperationException(string.Join(Environment.NewLine, emitted.Diagnostics));
		}
		CSharpCompilation consumer = CreateCompilation("using Other; public class Consumer { public Current Value { get; } }")
			.AddReferences(MetadataReference.CreateFromImage(bytes.ToArray()));
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: new CSharpParseOptions(LanguageVersion.Preview));
		driver = driver.RunGenerators(consumer, TestContext.Current.CancellationToken);
		Diagnostic result = Assert.Single(driver.GetRunResult().Diagnostics);

		Assert.Equal("DQ0006", result.Id);
	}

	[Fact]
	public void CollisionWarningPreservesTheGeneratedAliasBinding()
	{
		const string source = "using Other; namespace Other { public class Current { } } public class Consumer { public Current Value { get; } }";
		CSharpCompilation original = CreateCompilation(source);
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: new CSharpParseOptions(LanguageVersion.Preview));
		driver.RunGeneratorsAndUpdateCompilation(original, out Compilation generated, out ImmutableArray<Diagnostic> diagnostics, TestContext.Current.CancellationToken);
		IPropertySymbol property = (IPropertySymbol)generated.GetTypeByMetadataName("Consumer")!.GetMembers("Value").Single();
		INamedTypeSymbol result = ((INamedTypeSymbol)property.Type).OriginalDefinition;
		INamedTypeSymbol expected = generated.GetTypeByMetadataName(typeof(Quantity<>).FullName!)!;

		Assert.Multiple(
			() => Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "DQ0006"),
			() => Assert.True(SymbolEqualityComparer.Default.Equals(expected, result)));
	}

	[Theory]
	[InlineData("public class Current { } public class Consumer { }")]
	[InlineData("public delegate void Current();")]
	[InlineData("using Current = Other.Current; namespace Other { public class Current { } } public class Consumer { public Current Value { get; } }")]
	public void ConflictingDeclarationsAndExplicitAliasesWarn(string source)
	{
		ImmutableArray<Diagnostic> results = Generate(source);
		Assert.Contains(results, diagnostic => diagnostic.Id == "DQ0006" && diagnostic.Severity == DiagnosticSeverity.Warning);
	}

	[Fact]
	public void StaticMemberUseOfImportedTypeWarnsWhenShadowed()
	{
		const string source = """
			using Other;
			namespace Other { public class Current { public static int Create() => 1; } }
			public class Consumer { public int Value => Current.Create(); }
			""";
		Diagnostic result = Assert.Single(Generate(source));
		Assert.Equal("DQ0006", result.Id);
	}

	[Fact]
	public void UnusedSameNamedTypeInAnotherNamespaceDoesNotWarn()
	{
		const string source = "namespace Other { public class Current { } } public class Consumer { public Current Value { get; } }";
		Assert.Empty(Generate(source));
	}

	[Fact]
	public void QualifiedReferenceToSameNamedTypeDoesNotWarn()
	{
		const string source = "namespace Other { public class Current { } } public class Consumer { public Other.Current Value { get; } }";
		Assert.Empty(Generate(source));
	}

	[Theory]
	[InlineData("NumberTwoOverZero")]
	[InlineData("NumberBanana")]
	public void InvalidEnglishExponentExplainsUnresolvedRequest(string name)
	{
		string source = $$"""
			using DimensionalQuantities.TypeSystem;
			public class Consumer { public object Calculate(Length length) => Power<{{name}}>.Of(length); }
			""";
		Diagnostic result = Assert.Single(Generate(source));
		Assert.Multiple(
			() => Assert.Equal("DQ0007", result.Id),
			() => Assert.Equal(DiagnosticSeverity.Error, result.Severity),
			() => Assert.Contains(name, result.GetMessage()),
			() => Assert.Equal(name, source.Substring(result.Location.SourceSpan.Start, result.Location.SourceSpan.Length)));
	}

	[Fact]
	public void FileLocalAliasElsewhereDoesNotHideInvalidEnglishExponent()
	{
		const string source = "using DimensionalQuantities.TypeSystem; public class Consumer { public object Calculate(Length length) => Power<NumberBanana>.Of(length); }";
		CSharpCompilation compilation = CreateCompilation(source).AddSyntaxTrees(CSharpSyntaxTree.ParseText(
			"using NumberBanana = System.Int32; public class OtherConsumer { }",
			new CSharpParseOptions(LanguageVersion.Preview), "OtherConsumer.cs", cancellationToken: TestContext.Current.CancellationToken));
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: new CSharpParseOptions(LanguageVersion.Preview));
		driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
		Diagnostic diagnostic = Assert.Single(driver.GetRunResult().Diagnostics);

		Assert.Multiple(
			() => Assert.Equal("DQ0007", diagnostic.Id),
			() => Assert.Equal("Consumer.cs", diagnostic.Location.SourceTree!.FilePath));
	}

	[Theory]
	[InlineData("using DimensionalQuantities.TypeSystem; public class Consumer { public object Calculate(Length length) => Power<NumberTwo>.Of(length); }")]
	[InlineData("using DimensionalQuantities.TypeSystem; public class Consumer { public object Calculate(Length length) => Power<Missing>.Of(length); }")]
	[InlineData("public class Power<T> { public static double Of(double value) => value; } public class Consumer { public object Calculate() => Power<NumberBanana>.Of(2); }")]
	[InlineData("using NumberTwo = DimensionalQuantities.TypeSystem.TerminalHex2; using DimensionalQuantities.TypeSystem; public class Consumer { public object Calculate(Length length) => Power<NumberTwo>.Of(length); }")]
	[InlineData("using DimensionalQuantities.TypeSystem; public class NumberBanana { } public class Consumer { public object Calculate(Length length) => Power<NumberBanana>.Of(length); }")]
	public void BoundNamesAndUnrelatedRequestsDoNotProduceGenerationDiagnostics(string source)
	{
		Assert.Empty(Generate(source));
	}

	[Fact]
	public void InvalidNumericEncodingDoesNotCrashGenerator()
	{
		const string source = """
			using DimensionalQuantities.TypeSystem;
			public class Consumer
			{
				public object Calculate(Length length) => Power<Fraction<TerminalHex1, TerminalHex0>>.Of(length);
			}
			""";
		ImmutableArray<Diagnostic> results = Generate(source);
		Assert.DoesNotContain(results, diagnostic => diagnostic.Id == "CS8785");
	}

	[Fact]
	public void CorrectingAnEnglishExponentRemovesItsDiagnostic()
	{
		const string source = "using DimensionalQuantities.TypeSystem; public class Consumer { public object Calculate(Length length) => Power<NumberBanana>.Of(length); }";
		CSharpCompilation original = CreateCompilation(source);
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: new CSharpParseOptions(LanguageVersion.Preview));
		driver = driver.RunGenerators(original, TestContext.Current.CancellationToken);
		ImmutableArray<Diagnostic> initial = driver.GetRunResult().Diagnostics;
		CSharpCompilation edited = CreateCompilation(source.Replace("NumberBanana", "NumberTwo"));
		driver = driver.RunGenerators(edited, TestContext.Current.CancellationToken);
		ImmutableArray<Diagnostic> result = driver.GetRunResult().Diagnostics;

		Assert.Multiple(
			() => Assert.Contains(initial, diagnostic => diagnostic.Id == "DQ0007"),
			() => Assert.Empty(result));
	}

	private static ImmutableArray<Diagnostic> Generate(string source)
	{
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: new CSharpParseOptions(LanguageVersion.Preview));
		driver = driver.RunGenerators(CreateCompilation(source), TestContext.Current.CancellationToken);
		return driver.GetRunResult().Diagnostics;
	}

	private static CSharpCompilation CreateCompilation(string source)
	{
		string[] assemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = assemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		return CSharpCompilation.Create("Consumer", [CSharpSyntaxTree.ParseText(source,
			new CSharpParseOptions(LanguageVersion.Preview), "Consumer.cs", cancellationToken: TestContext.Current.CancellationToken)],
			references, new(OutputKind.DynamicallyLinkedLibrary));
	}
}
