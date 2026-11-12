using System.Collections.Immutable;
using DimensionalQuantities.Roslyn.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace DimensionalQuantities.Roslyn.Tests.Testing;

internal sealed class QuantityGeneratorTest : CSharpSourceGeneratorTest<QuantityGenerator, DefaultVerifier>
{
	public QuantityGeneratorTest()
	{
		QuantityTestConfiguration.Apply(this);
	}

	public Compilation Compilation { get; private set; } = null!;

	public Type? AdditionalGenerator { get; init; }

	protected override IEnumerable<Type> GetSourceGenerators() => AdditionalGenerator is null
		? [typeof(QuantityGenerator)] : [typeof(QuantityGenerator), AdditionalGenerator];

	protected override async Task<(Compilation compilation, ImmutableArray<Diagnostic> generatorDiagnostics)> GetProjectCompilationAsync(
		Project project, IVerifier verifier, CancellationToken cancellationToken)
	{
		(Compilation compilation, ImmutableArray<Diagnostic> generatorDiagnostics) result = await base.GetProjectCompilationAsync(project, verifier, cancellationToken);
		Compilation = result.compilation;
		return result;
	}
}
