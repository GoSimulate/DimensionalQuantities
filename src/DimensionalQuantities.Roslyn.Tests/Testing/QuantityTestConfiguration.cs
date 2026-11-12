using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;

namespace DimensionalQuantities.Roslyn.Tests.Testing;

internal static class QuantityTestConfiguration
{
	public static void Apply<TVerifier>(AnalyzerTest<TVerifier> test) where TVerifier : IVerifier, new()
	{
		test.ReferenceAssemblies = new ReferenceAssemblies("net11.0");
		test.TestState.AdditionalReferences.AddRange(((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
			.Split(Path.PathSeparator).Select(path => MetadataReference.CreateFromFile(path)));
		test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		test.SolutionTransforms.Add((solution, projectId) => solution.WithProjectParseOptions(projectId, new CSharpParseOptions(LanguageVersion.Preview)));
	}
}
