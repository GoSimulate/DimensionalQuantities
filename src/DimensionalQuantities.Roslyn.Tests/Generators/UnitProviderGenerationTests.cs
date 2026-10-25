using System.Collections.Immutable;
using System.Reflection;
using DimensionalQuantities.Roslyn.Generators;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
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
	public void IndependentProviderGeneratesScaledFactoriesAndAccessors()
	{
		const string consumer = """
			namespace Independent.Units;
			public static class Consumer
			{
				public static double[] Values() => [Length.FromFeet(10).AsMetres(), Length.FromMetres(0.6096).AsFeet()];
			}
			""";
		(Compilation compilation, ImmutableArray<Diagnostic> diagnostics) = Generate(FootProvider, consumer);
		Assert.Empty(diagnostics);
		double[] result = Evaluate(compilation, "Independent.Units.Consumer");
		Assert.Multiple(
			() => Assert.Equal(3.048, result[0], 12),
			() => Assert.Equal(2, result[1], 12));
	}

	[Fact]
	public void AffineConversionsApplyOffsetOnlyToAbsoluteQuantities()
	{
		const string source = """
			using DimensionalQuantities.Units.Declarations;
			[UnitProvider(GenerateSiPrefixes = true, GeneratePrefixConversions = true)]
			internal static class CustomUnits
			{
				[QuantityUnit(typeof(Length))]
				[UnitFactor(typeof(OffsetMetre))]
				private sealed class LengthUnit;
				[UnitSymbol("OffsetMetre", "OffsetMetres", "om", Scale = 2)]
				[AffineUnit(5)]
				private sealed class OffsetMetre;
			}
			public static class Consumer
			{
				public static double[] Values()
				{
					Length first = Length.FromOffsetMetres(3);
					Length second = Length.FromOffsetMetres(2);
					return [first.AsMetres(), first.AsOffsetMetres(), (first - second).AsDelta().AsOffsetMetres()];
				}
			}
			""";
		(Compilation compilation, ImmutableArray<Diagnostic> diagnostics) = Generate(source, "");
		Assert.Empty(diagnostics);
		Assert.Equal([16d, 3d, 1d], Evaluate(compilation, "Consumer"));
	}

	[Fact]
	public void ProviderConversionsArePublicAndNotRegeneratedByConsumers()
	{
		(Compilation provider, ImmutableArray<Diagnostic> providerDiagnostics) = Generate(FootProvider, "");
		Assert.Empty(providerDiagnostics);
		MetadataReference reference = MetadataReference.CreateFromImage(Emit(provider));
		const string consumer = """
			using Independent.Units;
			public static class Consumer
			{
				public static Area Value() => Length.FromFeet(1) * Length.FromFeet(2);
			}
			""";
		(Compilation compilation, ImmutableArray<Diagnostic> diagnostics) = Generate("", consumer, reference);
		INamespaceSymbol? localNamespace = compilation.Assembly.GlobalNamespace.GetNamespaceMembers()
			.SingleOrDefault(symbol => symbol.Name == "Independent");

		Assert.Multiple(
			() => Assert.Empty(diagnostics),
			() => Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)),
			() => Assert.Null(localNamespace));
	}

	[Fact]
	public void ProviderCanOptIntoNamedAndParameterisedSiPrefixes()
	{
		const string source = """
			using DimensionalQuantities.Units.Declarations;
			namespace Independent.Units;
			[UnitProvider(GenerateSiPrefixes = true, GeneratePrefixConversions = true)]
			internal static class CustomUnits
			{
				[QuantityUnit(typeof(Voltage))]
				[UnitFactor(typeof(Spark))]
				private sealed class VoltageUnit;
				[UnitSymbol("Spark", "Sparks", "sp")]
				private sealed class Spark;
			}
			public static class Consumer
			{
				public static double[] Values() => [Voltage.FromMilliSparks(500).AsSiBaseUnit(), Voltage.FromSiBaseUnit(2000).AsKiloSparks(),
					Voltage.FromPrefixedUnits(3, sparkPrefixExponent: 3).AsSiBaseUnit(), Voltage.FromSiBaseUnit(0.004).AsPrefixedUnits(-3)];
			}
			""";
		(Compilation compilation, ImmutableArray<Diagnostic> diagnostics) = Generate(source, "");
		Assert.Empty(diagnostics);
		double[] result = Evaluate(compilation, "Independent.Units.Consumer");
		Assert.Equal([0.5, 2, 3000, 4], result);
	}

	[Fact]
	public void CompoundFactorsPreserveOrderPowersAndAdditionalConversions()
	{
		const string source = """
			using DimensionalQuantities.Units.Declarations;
			namespace Independent.Units;
			[UnitProvider(GenerateSiPrefixes = true, GeneratePrefixConversions = true)]
			internal static class CustomUnits
			{
				[QuantityUnit(typeof(CurrentDensity))]
				[UnitFactor(typeof(Amp))]
				[UnitFactor(typeof(Metre), Power = -2)]
				[AdditionalUnit(typeof(PerSquareMillimetre))]
				private sealed class CurrentDensityUnit;
				[UnitFactor(typeof(Amp))]
				[UnitFactor(typeof(Metre), Power = -2, PrefixExponent = -3)]
				private sealed class PerSquareMillimetre;
				[UnitSymbol("Amp", "Amps", "A")]
				private sealed class Amp;
				[UnitSymbol("Metre", "Metres", "m")]
				private sealed class Metre;
			}
			public static class Consumer
			{
				public static double[] Values() => [CurrentDensity.FromAmpsPerSquareMilliMetre(1).AsSiBaseUnit(),
					CurrentDensity.FromPrefixedUnits(2, ampPrefixExponent: 0, metrePrefixExponent: -3).AsSiBaseUnit(),
					CurrentDensity.FromSiBaseUnit(1000000).AsAmpsPerSquareMilliMetre()];
			}
			""";
		(Compilation compilation, ImmutableArray<Diagnostic> diagnostics) = Generate(source, "");
		Assert.Empty(diagnostics);
		double[] result = Evaluate(compilation, "Independent.Units.Consumer");
		Assert.Equal([1000000d, 2000000d, 1d], result);
	}

	[Fact]
	public void ProviderUsesCoreSiSymbolsAcrossAssemblyBoundary()
	{
		const string source = """
			using DimensionalQuantities.Units.Declarations;
			using DimensionalQuantities.Units.Symbols;
			namespace Independent.Units;
			[UnitProvider(GenerateSiPrefixes = true, GeneratePrefixConversions = true)]
			internal static class CompoundUnits
			{
				[QuantityUnit(typeof(CurrentDensity))]
				[UnitFactor(typeof(Amp))]
				[UnitFactor(typeof(Metre), Power = -2)]
				private sealed class CurrentDensityUnit;
				[QuantityUnit(typeof(Density))]
				[UnitFactor(typeof(Gram), PrefixExponent = 3)]
				[UnitFactor(typeof(Metre), Power = -3)]
				private sealed class DensityUnit;
			}
			public static class Consumer
			{
				public static double[] Values() =>
					[CurrentDensity.FromAmpsPerSquareMetre(2).AsSiBaseUnit(),
					 CurrentDensity.FromPrefixedUnits(2, ampPrefixExponent: 0, metrePrefixExponent: -3).AsSiBaseUnit(),
					 Density.FromGramsPerCubicMetre(2).AsSiBaseUnit(),
					 Density.FromPrefixedUnits(2, gramPrefixExponent: 0, metrePrefixExponent: 0).AsSiBaseUnit()];
			}
			""";
		(Compilation compilation, ImmutableArray<Diagnostic> diagnostics) = Generate(source, "");
		Assert.Empty(diagnostics);
		Assert.Equal([2d, 2000000d, 0.002d, 0.002d], Evaluate(compilation, "Independent.Units.Consumer"));
	}

	[Fact]
	public void OneUnitDeclarationCanServeDistinctQuantityTypes()
	{
		const string source = """
			using DimensionalQuantities.Units.Declarations;
			namespace Independent.Units;
			[UnitProvider]
			internal static class CommonUnits
			{
				[QuantityUnit(typeof(Length))]
				[QuantityUnit(typeof(Time))]
				[UnitFactor(typeof(Token))]
				private sealed class TokenUnit;
				[UnitSymbol("Token", "Tokens", "token")]
				private sealed class Token;
			}
			public static class Consumer
			{
				public static double[] Values() =>
					[Length.FromTokens(2).AsSiBaseUnit(), Time.FromTokens(3).AsSiBaseUnit()];
			}
			""";
		(Compilation compilation, ImmutableArray<Diagnostic> diagnostics) = Generate(source, "");
		Assert.Empty(diagnostics);
		Assert.Equal([2d, 3d], Evaluate(compilation, "Independent.Units.Consumer"));
	}

	[Fact]
	public void ProviderFactoriesCanBeUsedInArithmeticInTheirOwnAssembly()
	{
		const string consumer = """
			namespace Independent.Units;
			public static class Consumer
			{
				public static double[] Values() => [(Length.FromFeet(2) * Length.FromFeet(3)).AsSiBaseUnit()];
			}
			""";
		(Compilation compilation, ImmutableArray<Diagnostic> diagnostics) = Generate(FootProvider, consumer);
		Assert.Empty(diagnostics);
		double[] result = Evaluate(compilation, "Independent.Units.Consumer");
		Assert.Equal(0.55741824, Assert.Single(result), 12);
	}

	[Fact]
	public void RecordClassProvidersGenerateConversions()
	{
		const string provider = """
			using DimensionalQuantities.Units.Declarations;
			namespace Independent.Units;
			[UnitProvider]
			internal record class CustomUnits
			{
				[QuantityUnit(typeof(Length))]
				[UnitFactor(typeof(Foot))]
				private sealed class LengthUnit;
				[UnitSymbol("Foot", "Feet", "ft", Scale = 0.3048)]
				private sealed class Foot;
			}
			""";
		const string consumer = """
			namespace Independent.Units;
			public static class Consumer
			{
				public static double[] Values() => [Length.FromFeet(1).AsMetres()];
			}
			""";
		(Compilation compilation, ImmutableArray<Diagnostic> diagnostics) = Generate(provider, consumer);
		Assert.Empty(diagnostics);
		double[] result = Evaluate(compilation, "Independent.Units.Consumer");
		Assert.Equal(0.3048, Assert.Single(result), 12);
	}

	[Theory]
	[InlineData("Bad name", "Feet")]
	[InlineData("Foot", "Bad names")]
	public void InvalidUnitNamesReportDeclarationDiagnostics(string singular, string plural)
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
		(Compilation compilation, ImmutableArray<Diagnostic> diagnostics) = Generate(source, "");
		Assert.Multiple(
			() => Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)),
			() => Assert.Equal("DQ0008", Assert.Single(diagnostics).Id));
	}

	[Fact]
	public void ProvidersInSameNamespaceCanConvertSameQuantity()
	{
		const string source = """
			using DimensionalQuantities.Units.Declarations;
			namespace Independent.Units;
			[UnitProvider]
			internal static class ImperialUnits
			{
				[QuantityUnit(typeof(Length))]
				[UnitFactor(typeof(Foot))]
				private sealed class LengthUnit;
				[UnitSymbol("Foot", "Feet", "ft", Scale = 0.3048)]
				private sealed class Foot;
			}
			[UnitProvider]
			internal static class SurveyUnits
			{
				[QuantityUnit(typeof(Length))]
				[UnitFactor(typeof(Yard))]
				private sealed class LengthUnit;
				[UnitSymbol("Yard", "Yards", "yd", Scale = 0.9144)]
				private sealed class Yard;
			}
			public static class Consumer
			{
				public static double[] Values() => [Length.FromFeet(3).AsYards(), Length.FromYards(2).AsFeet()];
			}
			""";
		(Compilation compilation, ImmutableArray<Diagnostic> diagnostics) = Generate(source, "");
		Assert.Empty(diagnostics);
		double[] result = Evaluate(compilation, "Independent.Units.Consumer");
		Assert.Multiple(
			() => Assert.Equal(1, result[0], 12),
			() => Assert.Equal(6, result[1], 12));
	}

	private static (Compilation Compilation, ImmutableArray<Diagnostic> Diagnostics) Generate(string provider, string consumer, params MetadataReference[] additionalReferences)
	{
		CSharpParseOptions options = new(LanguageVersion.Preview);
		string[] assemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<MetadataReference> references = assemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location)).Concat(additionalReferences);
		CSharpCompilation input = CSharpCompilation.Create("ProviderTest" + Guid.NewGuid().ToString("N"),
			[CSharpSyntaxTree.ParseText(provider, options), CSharpSyntaxTree.ParseText(consumer, options)], references, new(OutputKind.DynamicallyLinkedLibrary));
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: options);
		driver.RunGeneratorsAndUpdateCompilation(input, out Compilation output, out ImmutableArray<Diagnostic> diagnostics, TestContext.Current.CancellationToken);
		return (output, diagnostics);
	}

	private static double[] Evaluate(Compilation compilation, string consumerType)
	{
		Assembly assembly = Assembly.Load(Emit(compilation));
		return (double[])assembly.GetType(consumerType)!.GetMethod("Values")!.Invoke(null, null)!;
	}

	private static byte[] Emit(Compilation compilation)
	{
		using MemoryStream stream = new();
		EmitResult result = compilation.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);
		if (!result.Success)
		{
			throw new InvalidOperationException(string.Join(Environment.NewLine, result.Diagnostics));
		}
		return stream.ToArray();
	}
}
