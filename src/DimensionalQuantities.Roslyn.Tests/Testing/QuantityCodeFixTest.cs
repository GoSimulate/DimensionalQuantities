using DimensionalQuantities.Roslyn.Analyzers;
using DimensionalQuantities.Roslyn.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace DimensionalQuantities.Roslyn.Tests.Testing;

internal sealed class QuantityCodeFixTest : CSharpCodeFixTest<QuantityEncodingAnalyzer, QuantityAliasCodeFixProvider, DefaultVerifier>
{
	public QuantityCodeFixTest()
	{
		QuantityTestConfiguration.Apply(this);
		// The fix resolves aliases through its Document, so generation must run in the workspace.
		SolutionTransforms.Add((solution, projectId) => solution.AddAnalyzerReference(projectId, new QuantityGeneratorReference()));
	}

}
