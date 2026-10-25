using System.Collections.Immutable;
using System.Text;
using DimensionalQuantities.Roslyn.Discovery;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DimensionalQuantities.Roslyn.Generators;

[Generator]
public sealed class QuantityGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		IncrementalValuesProvider<INamedTypeSymbol> providers = UnitProviderGenerationCoordinator.FindProviders(context);
		QuantityGenerationPlan defaultPlan = DefaultQuantityDiscovery.Discover();
		context.RegisterPostInitializationOutput(output =>
		{
			string source = QuantitySourceEmitter.Emit([.. defaultPlan.KnownQuantities, .. defaultPlan.AdditionalQuantities]);
			output.AddSource("Quantities.g.cs", SourceText.From(source, Encoding.UTF8));
			string powerSource = PowerSourceEmitter.Emit(defaultPlan, "DefaultPowerExtensions");
			output.AddSource("DefaultPowers.g.cs", SourceText.From(powerSource, Encoding.UTF8));
			string methodSource = PowerMethodSourceEmitter.Emit(defaultPlan, "DefaultPowerMethodExtensions");
			output.AddSource("DefaultPowerMethods.g.cs", SourceText.From(methodSource, Encoding.UTF8));
		});

		IncrementalValuesProvider<SyntaxTree> inputTrees = context.SyntaxProvider.CreateSyntaxProvider(
			static (node, _) => node is CompilationUnitSyntax unit && IsDiscoveryInputTree(unit.SyntaxTree),
			static (syntaxContext, _) => syntaxContext.Node.SyntaxTree);
		IncrementalValueProvider<((Compilation Left, ImmutableArray<SyntaxTree> Right) Left, ImmutableArray<INamedTypeSymbol> Right)> inputs =
			context.CompilationProvider.Combine(inputTrees.Collect()).Combine(providers.Collect());
		context.RegisterSourceOutput(inputs, (output, input) =>
		{
			CSharpCompilation original = (CSharpCompilation)input.Left.Left;
			ImmutableArray<SyntaxTree> sourceTrees = input.Left.Right;
			ImmutableArray<GeneratedSource> providerSources = UnitProviderGenerationCoordinator.Generate(original, input.Right, output.ReportDiagnostic);
			foreach (GeneratedSource source in providerSources)
			{
				output.AddSource(source.HintName, SourceText.From(source.Text, Encoding.UTF8));
			}

			CSharpCompilation withProviders = original.AddGeneratedSources(providerSources);
			foreach (Diagnostic diagnostic in GenerationDiagnostics.FindInvalidEnglishExponents(original, sourceTrees, output.CancellationToken))
			{
				output.ReportDiagnostic(diagnostic);
			}

			QuantityDiscoveryResult result = QuantityDiscoveryCoordinator.Discover(withProviders,
				sourceTrees, defaultPlan, output.CancellationToken);
			foreach (Diagnostic diagnostic in AliasCollisionDiagnostics.Find(original, result, sourceTrees, output.CancellationToken))
			{
				output.ReportDiagnostic(diagnostic);
			}

			foreach (GeneratedSource source in result.Sources)
			{
				output.AddSource(source.HintName, SourceText.From(source.Text, Encoding.UTF8));
			}
		});
	}

	private static bool IsDiscoveryInputTree(SyntaxTree tree)
	{
		string path = tree.FilePath.Replace('\\', '/');
		const string generatedPrefix = "/DimensionalQuantities.Roslyn.Generators.QuantityGenerator/";
		return !path.EndsWith(generatedPrefix + "Quantities.g.cs", StringComparison.Ordinal)
			&& !path.EndsWith(generatedPrefix + "DefaultPowers.g.cs", StringComparison.Ordinal)
			&& !path.EndsWith(generatedPrefix + "DefaultPowerMethods.g.cs", StringComparison.Ordinal);
	}
}
