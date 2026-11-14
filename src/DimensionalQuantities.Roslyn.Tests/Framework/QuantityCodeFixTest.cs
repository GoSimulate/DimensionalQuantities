using System.Collections.Immutable;
using DimensionalQuantities.Roslyn.Analyzers;
using DimensionalQuantities.Roslyn.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Framework;

internal sealed class QuantityCodeFixTest : CSharpCodeFixTest<QuantityEncodingAnalyzer, QuantityAliasCodeFixProvider, DefaultVerifier>
{
	public QuantityCodeFixTest()
	{
		QuantityTestEnvironment.Configure(this);
		// Fixes resolve aliases through their Document.Project, so the workspace must run the generator.
		SolutionTransforms.Add((solution, project) => solution.AddAnalyzerReference(project, new QuantitySourceReference()));
	}

	protected override ImmutableArray<CodeAction> FilterCodeActions(ImmutableArray<CodeAction> actions)
	{
		// Framework selection accepts the first matching action, so reject additional alternatives.
		Assert.InRange(actions.Length, 0, 1);
		return base.FilterCodeActions(actions);
	}
}
