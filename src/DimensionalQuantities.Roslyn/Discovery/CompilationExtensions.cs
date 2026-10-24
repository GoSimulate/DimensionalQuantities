using System.Collections.Immutable;
using DimensionalQuantities.Roslyn.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DimensionalQuantities.Roslyn.Discovery;

internal static class CompilationExtensions
{
	public static INamedTypeSymbol GetRequiredType(this Compilation compilation, Type type) =>
		compilation.GetTypeByMetadataName(type.FullName!)
		?? throw new InvalidOperationException($"Required type '{type.FullName}' is missing or ambiguous in the consuming project. Check its DimensionalQuantities reference.");

	public static CSharpCompilation AddGeneratedSources(this CSharpCompilation compilation, ImmutableArray<GeneratedSource> sources)
	{
		if (sources.IsEmpty)
		{
			return compilation;
		}

		CSharpParseOptions options = compilation.SyntaxTrees.OfType<CSharpSyntaxTree>()
			.Select(tree => (CSharpParseOptions)tree.Options).FirstOrDefault() ?? CSharpParseOptions.Default;
		SyntaxTree[] trees = sources.Select(source => CSharpSyntaxTree.ParseText(source.Text, options, path: source.HintName)).ToArray();
		return compilation.AddSyntaxTrees(trees);
	}
}
