using System.Collections.Immutable;
using DimensionalQuantities.Roslyn.Generators;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DimensionalQuantities.Roslyn.Tests.Analyzers;

internal static class AnalyzerTestCompilation
{
	public static CSharpCompilation Create(string source, bool generate = true)
	{
		CSharpParseOptions options = new(LanguageVersion.Preview);
		string[] assemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = assemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		CSharpCompilation compilation = CSharpCompilation.Create("AnalyzerConsumer",
			[CSharpSyntaxTree.ParseText(source, options, "Consumer.cs")], references, new(OutputKind.DynamicallyLinkedLibrary));
		if (!generate)
		{
			return compilation;
		}

		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: options);
		driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation generated, out _);
		return (CSharpCompilation)generated;
	}

	public static async Task<ImmutableArray<Diagnostic>> Analyze(string source, DiagnosticAnalyzer analyzer, bool generate = true)
	{
		CSharpCompilation compilation = Create(source, generate);
		return await compilation.WithAnalyzers([analyzer]).GetAnalyzerDiagnosticsAsync(Xunit.TestContext.Current.CancellationToken);
	}
}
