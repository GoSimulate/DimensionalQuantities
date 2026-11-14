using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis.Testing;

namespace DimensionalQuantities.Roslyn.Tests.Generators;

[NotInParallel(nameof(CoreConversionBoundaryTests))]
public sealed class CoreConversionBoundaryTests
{
	[Test]
	[Arguments("Voltage.{|#0:FromVolts|}(12)", "CS0117")]
	[Arguments("{|#0:Voltage|}.FromPrefixedUnits(12, 0)", "CS1929")]
	[Arguments("Voltage.FromSiBaseUnit(12).{|#0:AsVolts|}()", "CS1061")]
	[Arguments("{|#0:Voltage.FromSiBaseUnit(12)|}.AsPrefixedUnits(0)", "CS1929")]
	public async Task CatalogueConversionsRequireAnExtensionPackage(string expression, string error)
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
				public class Consumer
				{
					public object Value() => {{expression}};
				}
				""",
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError(error).WithLocation(0));

		await test.RunAsync(TestContext.Current!.Execution.CancellationToken);
	}

	[Test]
	[Arguments("Length.FromMilliMetres(12).AsMetres()")]
	[Arguments("Mass.FromKiloGrams(12).AsGrams()")]
	[Arguments("Voltage.FromSiBaseUnit(12).AsSiBaseUnit()")]
	[Arguments("Voltage.FromArbitrarySiComponents(12).AsArbitrarySiComponents()")]
	[Arguments("(Length.FromMetres(2) * Current.FromAmps(3)).AsPrefixedUnits(3, 0)")]
	[Arguments("Voltage.FromSiBaseUnit(12).Squared().AsSiBaseUnit()")]
	public async Task BaseGenericAndIntermediateConversionsRemainAvailable(string expression)
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
				public class Consumer
				{
					public object Value() => {{expression}};
				}
				""",
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};

		await test.RunAsync(TestContext.Current!.Execution.CancellationToken);
	}
}
