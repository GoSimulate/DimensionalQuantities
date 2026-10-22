using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using DimensionalQuantities.Roslyn.Generators;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generators;

public sealed partial class QuantityGeneratorTests
{
	private static readonly CSharpParseOptions _parseOptions = new(LanguageVersion.CSharp14);

	[Fact]
	public void BaseAliasesHaveUnprefixedFactoriesAndAccessors()
	{
		const string source = """
			public static class Consumer
			{
				public static double UseBaseQuantities()
				{
					Length length = Length.FromMetres(1);
					Mass mass = Mass.FromGrams(1000);
					Time time = Time.FromSeconds(1);
					Current current = Current.FromAmps(1);
					Temperature temperature = Temperature.FromKelvin(1);
					AmountOfSubstance amount = AmountOfSubstance.FromMoles(1);
					LuminousIntensity intensity = LuminousIntensity.FromCandelas(1);
					return length.AsMetres() + mass.AsGrams() + time.AsSeconds() + current.AsAmps()
						+ temperature.AsKelvin() + amount.AsMoles() + intensity.AsCandelas();
				}
			}
			""";

		Compile(source);
	}

	[Fact]
	public void SemanticPipelineResolvesPostInitialisationQuantityAliasesAndFactories()
	{
		const string source = """
			public static class Consumer
			{
				public static Length Use(Length input) => Length.FromMetres(2);
			}
			""";
		CSharpCompilation result = Compile(source, new SemanticProbeGenerator());
		INamedTypeSymbol probe = result.GetTypeByMetadataName("SemanticProbe")!;
		IFieldSymbol aliasResolved = Assert.IsAssignableFrom<IFieldSymbol>(Assert.Single(probe.GetMembers("AliasResolved")));
		IFieldSymbol factoryResolved = Assert.IsAssignableFrom<IFieldSymbol>(Assert.Single(probe.GetMembers("FactoryResolved")));

		Assert.Multiple(
			() => Assert.Equal(true, aliasResolved.ConstantValue),
			() => Assert.Equal(true, factoryResolved.ConstantValue));
	}

	[Fact]
	public void CatalogueAliasesAreAvailableToConsumers()
	{
		const string source = """
			public static class Consumer
			{
				public static global::System.Type[] Types() =>
				[
					typeof(Area), typeof(Volume), typeof(Frequency), typeof(AngularVelocity), typeof(Density),
					typeof(Pressure), typeof(Stress), typeof(YoungsModulus), typeof(Energy), typeof(Torque),
					typeof(Power), typeof(SpecificPower), typeof(SpecificPowerLoss), typeof(TemperatureCoefficient),
					typeof(PowerDensity), typeof(VolumetricPowerLoss), typeof(Voltage), typeof(Resistance),
					typeof(Inductance), typeof(MagneticFlux), typeof(FluxLinkage), typeof(MagneticFluxDensity),
					typeof(MagneticPolarisation), typeof(RemanentFluxDensity), typeof(MagneticFieldStrength),
					typeof(CoerciveFieldStrength), typeof(CurrentDensity), typeof(Permeability), typeof(Resistivity),
					typeof(Conductivity), typeof(MagneticReluctivity), typeof(MagneticVectorPotential)
				];
			}
			""";

		Compile(source);
	}

	private static T Evaluate<T>(string expression)
	{
		string source = $$"""
			public static class Consumer
			{
				public static global::{{typeof(T).FullName}} Value() => {{expression}};
			}
			""";
		CSharpCompilation compilation = Compile(source);
		using MemoryStream assemblyBytes = new();
		EmitResult emitResult = compilation.Emit(assemblyBytes, cancellationToken: TestContext.Current.CancellationToken);
		Assert.True(emitResult.Success, string.Join(Environment.NewLine, emitResult.Diagnostics));
		Assembly assembly = Assembly.Load(assemblyBytes.ToArray());
		MethodInfo method = assembly.GetType("Consumer")!.GetMethod("Value")!;
		return Assert.IsType<T>(method.Invoke(null, null));
	}

	private static CSharpCompilation Compile(string source, IIncrementalGenerator? additionalGenerator = null)
	{
		CSharpCompilation input = CreateInputCompilation(source);
		ISourceGenerator[] generators = additionalGenerator is null
			? [new QuantityGenerator().AsSourceGenerator()]
			: [new QuantityGenerator().AsSourceGenerator(), additionalGenerator.AsSourceGenerator()];
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generators, parseOptions: _parseOptions);
		driver.RunGeneratorsAndUpdateCompilation(input, out Compilation output, out ImmutableArray<Diagnostic> generatorDiagnostics);
		Diagnostic[] compilationErrors = output.GetDiagnostics(TestContext.Current.CancellationToken)
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
		Assert.Multiple(
			() => Assert.True(generatorDiagnostics.IsEmpty, string.Join(Environment.NewLine, generatorDiagnostics)),
			() => Assert.True(compilationErrors.Length == 0, string.Join(Environment.NewLine, compilationErrors)));
		return (CSharpCompilation)output;
	}

	private static CSharpCompilation CreateInputCompilation(string source)
	{
		string[] platformAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = platformAssemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		return CSharpCompilation.Create(
			"GeneratedQuantityConsumer",
			[CSharpSyntaxTree.ParseText(source, _parseOptions, path: "Consumer.cs")],
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
	}

	private sealed class SemanticProbeGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			IncrementalValuesProvider<(bool IsInvocation, bool Resolved)> observed = context.SyntaxProvider.CreateSyntaxProvider(
				static (node, _) => node.SyntaxTree.FilePath == "Consumer.cs" && node is (ParameterSyntax or InvocationExpressionSyntax),
				static (syntaxContext, _) =>
				{
					bool isInvocation = syntaxContext.Node is InvocationExpressionSyntax;
					ITypeSymbol? type = syntaxContext.Node switch
					{
						ParameterSyntax parameter => (syntaxContext.SemanticModel.GetDeclaredSymbol(parameter) as IParameterSymbol)?.Type,
						InvocationExpressionSyntax invocation => (syntaxContext.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol)?.ReturnType,
						_ => null
					};
					INamedTypeSymbol? quantity = syntaxContext.SemanticModel.Compilation.GetTypeByMetadataName(typeof(Quantity<>).FullName!);
					bool resolved = type is INamedTypeSymbol named && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, quantity);
					return (isInvocation, resolved);
				});
			context.RegisterSourceOutput(observed.Collect(), static (output, symbols) =>
			{
				bool aliasResolved = symbols.Any(symbol => !symbol.IsInvocation && symbol.Resolved);
				bool factoryResolved = symbols.Any(symbol => symbol.IsInvocation && symbol.Resolved);
				string source = $"internal static class SemanticProbe {{ public const bool AliasResolved = {aliasResolved.ToString().ToLowerInvariant()}; public const bool FactoryResolved = {factoryResolved.ToString().ToLowerInvariant()}; }}";
				output.AddSource("SemanticProbe.g.cs", SourceText.From(source, Encoding.UTF8));
			});
		}
	}
}
