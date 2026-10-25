using System.Collections.Immutable;
using DimensionalQuantities.Roslyn.Generators;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generators;

public sealed class CoreConversionBoundaryTests
{
	[Theory]
	[InlineData("Voltage.FromVolts(12)")]
	[InlineData("Voltage.FromPrefixedUnits(12, 0)")]
	[InlineData("Voltage.FromSiBaseUnit(12).AsVolts()")]
	[InlineData("Voltage.FromSiBaseUnit(12).AsPrefixedUnits(0)")]
	public void CatalogueConversionsRequireAnExtensionPackage(string expression)
	{
		Diagnostic[] result = Compile($"public class Consumer {{ public object Value() => {expression}; }}");

		Assert.Contains(result, diagnostic => diagnostic.Id is "CS0117" or "CS1061" or "CS1929");
	}

	[Theory]
	[InlineData("Length.FromMilliMetres(12).AsMetres()")]
	[InlineData("Mass.FromKiloGrams(12).AsGrams()")]
	[InlineData("Voltage.FromSiBaseUnit(12).AsSiBaseUnit()")]
	[InlineData("Voltage.FromArbitrarySiComponents(12).AsArbitrarySiComponents()")]
	[InlineData("(Length.FromMetres(2) * Current.FromAmps(3)).AsPrefixedUnits(3, 0)")]
	[InlineData("Voltage.FromSiBaseUnit(12).Squared().AsSiBaseUnit()")]
	public void BaseGenericAndIntermediateConversionsRemainAvailable(string expression)
	{
		Diagnostic[] result = Compile($"public class Consumer {{ public object Value() => {expression}; }}");

		Assert.Empty(result);
	}

	private static Diagnostic[] Compile(string source)
	{
		CSharpParseOptions options = new(LanguageVersion.Preview);
		string[] assemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = assemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		CSharpCompilation compilation = CSharpCompilation.Create("Consumer", [CSharpSyntaxTree.ParseText(source, options)],
			references, new(OutputKind.DynamicallyLinkedLibrary));
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: options);
		driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation result, out ImmutableArray<Diagnostic> diagnostics,
			TestContext.Current.CancellationToken);
		return diagnostics.Concat(result.GetDiagnostics(TestContext.Current.CancellationToken))
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
	}
}
