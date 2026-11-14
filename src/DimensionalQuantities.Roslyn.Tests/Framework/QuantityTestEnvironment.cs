using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;

namespace DimensionalQuantities.Roslyn.Tests.Framework;

internal static class QuantityTestEnvironment
{
	public static void UseCurrentRuntimeReferences<TVerifier>(AnalyzerTest<TVerifier> test) where TVerifier : IVerifier, new()
	{
		// Union syntax requires the .NET 11 runtime contracts absent from the .NET 10 reference pack.
		test.ReferenceAssemblies = new ReferenceAssemblies("net11.0");
		string[] paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		test.TestState.AdditionalReferences.AddRange(paths.Select(path => MetadataReference.CreateFromFile(path)));
	}

	public static void Configure<TVerifier>(AnalyzerTest<TVerifier> test) where TVerifier : IVerifier, new()
	{
		test.ReferenceAssemblies = ReferenceAssemblies.Net.Net100;
		test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		test.SolutionTransforms.Add((solution, project) => solution.WithProjectParseOptions(project, new CSharpParseOptions(LanguageVersion.Preview)));
	}
}
