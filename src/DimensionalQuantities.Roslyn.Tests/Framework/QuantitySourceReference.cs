using System.Collections.Immutable;
using DimensionalQuantities.Roslyn.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DimensionalQuantities.Roslyn.Tests.Framework;

internal sealed class QuantitySourceReference : AnalyzerReference
{
	private readonly ImmutableArray<ISourceGenerator> _generators = [new QuantityGenerator().AsSourceGenerator()];

	public override string? FullPath => null;
	public override object Id => typeof(QuantityGenerator);
	public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language) => [];
	public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages() => [];
	public override ImmutableArray<ISourceGenerator> GetGenerators(string language) => language == LanguageNames.CSharp ? _generators : [];
}
