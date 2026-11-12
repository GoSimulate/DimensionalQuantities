using DimensionalQuantities.Roslyn.Generators;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace DimensionalQuantities.Roslyn.Tests.Testing;

internal sealed class QuantityAnalyzerTest<TAnalyzer> : CSharpAnalyzerTest<TAnalyzer, DefaultVerifier> where TAnalyzer : DiagnosticAnalyzer, new()
{
	public QuantityAnalyzerTest()
	{
		QuantityTestConfiguration.Apply(this);
		TestBehaviors |= TestBehaviors.SkipGeneratedSourcesCheck;
	}

	public bool Generate { get; init; } = true;

	protected override IEnumerable<Type> GetSourceGenerators() => Generate ? [typeof(QuantityGenerator)] : [];
}
