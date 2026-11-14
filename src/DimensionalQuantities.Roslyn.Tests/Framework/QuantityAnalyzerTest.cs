using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace DimensionalQuantities.Roslyn.Tests.Framework;

internal sealed class QuantityAnalyzerTest<TAnalyzer> : CSharpAnalyzerTest<TAnalyzer, DefaultVerifier> where TAnalyzer : DiagnosticAnalyzer, new()
{
	public QuantityAnalyzerTest(bool generate = true)
	{
		QuantityTestEnvironment.Configure(this);
		if (generate)
		{
			SolutionTransforms.Add((solution, project) => solution.AddAnalyzerReference(project, new QuantitySourceReference()));
		}
	}
}
