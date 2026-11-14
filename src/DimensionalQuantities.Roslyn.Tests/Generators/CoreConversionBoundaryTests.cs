using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generators;

public sealed class CoreConversionBoundaryTests
{
	[Theory]
	[InlineData("Voltage.{|#0:FromVolts|}(12)", "CS0117")]
	[InlineData("{|#0:Voltage|}.FromPrefixedUnits(12, 0)", "CS1929")]
	[InlineData("Voltage.FromSiBaseUnit(12).{|#0:AsVolts|}()", "CS1061")]
	[InlineData("{|#0:Voltage.FromSiBaseUnit(12)|}.AsPrefixedUnits(0)", "CS1929")]
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

		await test.RunAsync(TestContext.Current.CancellationToken);
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
			TestCode = $$"""
				public class Consumer
				{
					public object Value() => {{expression}};
				}
				""",
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};

		await test.RunAsync(TestContext.Current.CancellationToken);
	}
}
