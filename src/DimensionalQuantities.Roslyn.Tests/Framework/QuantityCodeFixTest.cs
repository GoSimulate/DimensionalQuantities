using DimensionalQuantities.Roslyn.Analyzers;
using DimensionalQuantities.Roslyn.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace DimensionalQuantities.Roslyn.Tests.Framework;

internal sealed class QuantityCodeFixTest : CSharpCodeFixTest<QuantityEncodingAnalyzer, QuantityAliasCodeFixProvider, DefaultVerifier>
{
	public QuantityCodeFixTest()
	{
		QuantityTestEnvironment.Configure(this);
		// Fixes resolve aliases through their Document.Project, so the workspace must run the generator.
		SolutionTransforms.Add((solution, project) => solution.AddAnalyzerReference(project, new QuantitySourceReference()));
	}
}
