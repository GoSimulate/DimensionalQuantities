using System.Collections.Immutable;
using DimensionalQuantities.Roslyn.Generators;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generators;

public sealed class MetricConversionGenerationTests
{
	[Fact]
	public void EnabledMetricCompilationEmitsPublicConversions()
	{
		(Compilation compilation, GeneratorDriverRunResult result, ImmutableArray<Diagnostic> diagnostics) = Generate("true");
		INamedTypeSymbol? extensions = compilation.GetTypeByMetadataName("DimensionalQuantities.Extensions.Metric.VoltageExtensions");

		Assert.Multiple(
			() => Assert.Empty(diagnostics),
			() => Assert.Contains(Assert.Single(result.Results).GeneratedSources,
				generated => generated.HintName == "MetricConversions.g.cs"),
			() => Assert.Equal(Accessibility.Public, extensions?.DeclaredAccessibility),
			() => Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken)
				.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("false")]
	public void ConsumerCompilationDoesNotEmitMetricConversions(string? propertyValue)
	{
		(Compilation compilation, GeneratorDriverRunResult result, ImmutableArray<Diagnostic> diagnostics) = Generate(propertyValue);
		Assert.Multiple(
			() => Assert.Empty(diagnostics),
			() => Assert.DoesNotContain(Assert.Single(result.Results).GeneratedSources,
				generated => generated.HintName == "MetricConversions.g.cs"),
			() => Assert.Null(compilation.GetTypeByMetadataName("DimensionalQuantities.Extensions.Metric.VoltageExtensions")));
	}

	private static (Compilation Compilation, GeneratorDriverRunResult Result, ImmutableArray<Diagnostic> Diagnostics) Generate(string? propertyValue)
	{
		CSharpParseOptions options = new(LanguageVersion.Preview);
		string[] platformAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = platformAssemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		CSharpCompilation input = CSharpCompilation.Create("MetricGenerationTest",
			[CSharpSyntaxTree.ParseText("internal static class Input { }", options)], references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()],
			parseOptions: options, optionsProvider: new MetricOptionsProvider(propertyValue));
		driver = driver.RunGeneratorsAndUpdateCompilation(input, out Compilation output, out ImmutableArray<Diagnostic> diagnostics,
			TestContext.Current.CancellationToken);
		return (output, driver.GetRunResult(), diagnostics);
	}

	private sealed class MetricOptionsProvider : AnalyzerConfigOptionsProvider
	{
		private readonly AnalyzerConfigOptions _options;

		public MetricOptionsProvider(string? propertyValue) => _options = new MetricOptions(propertyValue);

		public override AnalyzerConfigOptions GlobalOptions => _options;
		public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _options;
		public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _options;
	}

	private sealed class MetricOptions : AnalyzerConfigOptions
	{
		private readonly string? _propertyValue;

		public MetricOptions(string? propertyValue) => _propertyValue = propertyValue;

		public override bool TryGetValue(string key, out string value)
		{
			if (key == "build_property.GenerateMetricConversions" && _propertyValue is not null)
			{
				value = _propertyValue;
				return true;
			}

			value = string.Empty;
			return false;
		}
	}
}
