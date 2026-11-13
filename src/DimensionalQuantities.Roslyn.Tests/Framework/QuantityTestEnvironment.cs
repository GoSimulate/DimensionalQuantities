using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;

namespace DimensionalQuantities.Roslyn.Tests.Framework;

internal static class QuantityTestEnvironment
{
	public static void Configure<TVerifier>(AnalyzerTest<TVerifier> test) where TVerifier : IVerifier, new()
	{
		test.ReferenceAssemblies = ReferenceAssemblies.Net.Net100;
		test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		test.SolutionTransforms.Add((solution, project) => solution.WithProjectParseOptions(project, new CSharpParseOptions(LanguageVersion.Preview)));
	}
}
