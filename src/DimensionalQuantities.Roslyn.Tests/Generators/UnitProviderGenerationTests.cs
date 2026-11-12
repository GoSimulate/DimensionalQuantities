using DimensionalQuantities.Roslyn.Tests.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generators;

public sealed class UnitProviderGenerationTests
{

	private const string FootProvider = """
		using DimensionalQuantities.Units.Declarations;
		namespace Independent.Units;

		[UnitProvider]
		internal static class CustomUnits
		{
			[QuantityUnit(typeof(Length))]
			[UnitFactor(typeof(Foot))]
			private sealed class LengthUnit;

			[UnitSymbol("Foot", "Feet", "ft", Scale = 0.3048)]
			private sealed class Foot;
		}
		""";

	[Fact]
	public async Task ProviderConversionsArePublicAndNotRegeneratedByConsumers()
	{
		QuantityGeneratorTest providerTest = new()
		{
			TestCode = FootProvider,
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
		};
		await providerTest.RunAsync(TestContext.Current.CancellationToken);
		using MemoryStream bytes = new();
		EmitResult emitted = providerTest.Compilation.Emit(bytes, cancellationToken: TestContext.Current.CancellationToken);
		if (!emitted.Success)
		{
			throw new InvalidOperationException(string.Join(Environment.NewLine, emitted.Diagnostics));
		}
		const string consumer = """
			using Independent.Units;
			public static class Consumer
			{
				public static Area Value() => Length.FromFeet(1) * Length.FromFeet(2);
			}
			""";
		QuantityGeneratorTest consumerTest = new()
		{
			TestCode = consumer,
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState = { AdditionalReferences = { MetadataReference.CreateFromImage(bytes.ToArray()) } },
		};
		await consumerTest.RunAsync(TestContext.Current.CancellationToken);
		Assert.DoesNotContain(consumerTest.Compilation.Assembly.GlobalNamespace.GetNamespaceMembers(), symbol => symbol.Name == "Independent");
	}

	[Theory]
	[InlineData("Bad name", "Feet")]
	[InlineData("Foot", "Bad names")]
	public async Task InvalidUnitNamesReportDeclarationDiagnostics(string singular, string plural)
	{
		string source = $$"""
			using DimensionalQuantities.Units.Declarations;
			[UnitProvider(GeneratePrefixConversions = true)]
			internal static class Units
			{
				[QuantityUnit(typeof(Length))]
				[UnitFactor(typeof(Foot))]
				private sealed class LengthUnit;
				[UnitSymbol("{{singular}}", "{{plural}}", "ft")]
				private sealed class Foot;
			}
			""";
		await new QuantityGeneratorTest
		{
			TestCode = source,
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			ExpectedDiagnostics = { new DiagnosticResult("DQ0008", DiagnosticSeverity.Error).WithSpan(3, 23, 3, 28).WithMessage("Unit provider for Length produces an invalid C# identifier.") },
		}.RunAsync(TestContext.Current.CancellationToken);
	}
}
