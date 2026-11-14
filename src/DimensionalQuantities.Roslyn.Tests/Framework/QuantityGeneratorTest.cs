using System.Collections.Immutable;
using DimensionalQuantities.Roslyn.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace DimensionalQuantities.Roslyn.Tests.Framework;

internal class QuantityGeneratorTest : CSharpSourceGeneratorTest<QuantityGenerator, DefaultVerifier>
{
	public QuantityGeneratorTest()
	{
		QuantityTestEnvironment.Configure(this);
	}

	public Compilation Compilation { get; private set; } = null!;

	protected override async Task<(Compilation compilation, ImmutableArray<Diagnostic> generatorDiagnostics)> GetProjectCompilationAsync(
		Project project, IVerifier verifier, CancellationToken cancellationToken)
	{
		(Compilation compilation, ImmutableArray<Diagnostic> generatorDiagnostics) result = await base.GetProjectCompilationAsync(project, verifier, cancellationToken);
		if (project.Name == TestState.Name)
		{
			Compilation = result.compilation;
		}

		return result;
	}
}
