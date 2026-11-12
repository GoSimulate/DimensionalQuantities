using DimensionalQuantities.Roslyn.Tests.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generators;

public sealed class CoreConversionBoundaryTests
{
	[Theory]
	[InlineData("Voltage.FromVolts(12)")]
	[InlineData("Voltage.FromPrefixedUnits(12, 0)")]
	[InlineData("Voltage.FromSiBaseUnit(12).AsVolts()")]
	[InlineData("Voltage.FromSiBaseUnit(12).AsPrefixedUnits(0)")]
	public async Task CatalogueConversionsRequireAnExtensionPackage(string expression)
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $"public class Consumer {{ public object Value() => {expression}; }}",
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			CompilerDiagnostics = CompilerDiagnostics.None,
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		Diagnostic[] result = test.Compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

		Assert.Contains(result, diagnostic => diagnostic.Id is "CS0117" or "CS1061" or "CS1929");
	}

	[Theory]
	[InlineData("Length.FromMilliMetres(12).AsMetres()")]
	[InlineData("Mass.FromKiloGrams(12).AsGrams()")]
	[InlineData("Voltage.FromSiBaseUnit(12).AsSiBaseUnit()")]
	[InlineData("Voltage.FromArbitrarySiComponents(12).AsArbitrarySiComponents()")]
	[InlineData("(Length.FromMetres(2) * Current.FromAmps(3)).AsPrefixedUnits(3, 0)")]
	[InlineData("Voltage.FromSiBaseUnit(12).Squared().AsSiBaseUnit()")]
	public async Task BaseGenericAndIntermediateConversionsRemainAvailable(string expression)
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $"public class Consumer {{ public object Value() => {expression}; }}",
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		Diagnostic[] result = test.Compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

		Assert.Empty(result);
	}

}
