using System.Collections.Immutable;
using DimensionalQuantities.Roslyn.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace DimensionalQuantities.Roslyn.Tests.Framework;

internal sealed class QuantityGeneratorTest : CSharpSourceGeneratorTest<QuantityGenerator, DefaultVerifier>
{
	public QuantityGeneratorTest()
	{
		QuantityTestEnvironment.Configure(this);
		// Cases assert the relevant declarations/expressions, rather than snapshotting the entire default catalogue.
		TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck;
	}

	public Compilation Compilation { get; private set; } = null!;
	public ImmutableArray<SyntaxTree> GeneratedSources { get; private set; } = [];

	protected override async Task<(Compilation compilation, ImmutableArray<Diagnostic> generatorDiagnostics)> GetProjectCompilationAsync(
		Project project, IVerifier verifier, CancellationToken cancellationToken)
	{
		(Compilation compilation, ImmutableArray<Diagnostic> generatorDiagnostics) result = await base.GetProjectCompilationAsync(project, verifier, cancellationToken);
		if (project.Name == TestState.Name)
		{
			Compilation = result.compilation;
			HashSet<string?> inputPaths = project.Documents.Select(document => document.FilePath).ToHashSet(StringComparer.Ordinal);
			GeneratedSources = [.. result.compilation.SyntaxTrees.Where(tree => !inputPaths.Contains(tree.FilePath))];
		}
		return result;
	}
}
