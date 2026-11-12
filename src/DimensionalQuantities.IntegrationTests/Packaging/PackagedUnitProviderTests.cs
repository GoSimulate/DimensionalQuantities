using Microsoft.Build.Utilities.ProjectCreation;
using Xunit;

namespace DimensionalQuantities.IntegrationTests.Packaging;

public sealed partial class PackagedGeneratorTests
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
		double[] result = EvaluateProvider(FootProvider, consumer, "Independent.Units.Consumer");
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
		Assert.Equal([16d, 3d, 1d], EvaluateProvider(source, "", "Consumer"));
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
		double[] result = EvaluateProvider(source, "", "Independent.Units.Consumer");
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
		double[] result = EvaluateProvider(source, "", "Independent.Units.Consumer");
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
		Assert.Equal([2d, 2000000d, 0.002d, 0.002d], EvaluateProvider(source, "", "Independent.Units.Consumer"));
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
		Assert.Equal([2d, 3d], EvaluateProvider(source, "", "Independent.Units.Consumer"));
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
		double[] result = EvaluateProvider(FootProvider, consumer, "Independent.Units.Consumer");
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
		double[] result = EvaluateProvider(provider, consumer, "Independent.Units.Consumer");
		Assert.Equal(0.3048, Assert.Single(result), 12);
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
		double[] result = EvaluateProvider(source, "", "Independent.Units.Consumer");
		Assert.Multiple(
			() => Assert.Equal(1, result[0], 12),
			() => Assert.Equal(6, result[1], 12));
	}

	private double[] EvaluateProvider(string provider, string consumer, string consumerType)
	{
		using PackageRepository repository = PackageRepository.Create(_scratch, new Uri("https://api.nuget.org/v3/index.json"));
		Package core = PackCore(repository);
		ProjectCreator project = CreateProject("ProviderConsumer", consumer, core);
		File.WriteAllText(Path.Combine(_scratch, "ProviderConsumer", "Provider.cs"), provider);
		Build(project);
		string output = OutputDirectory("ProviderConsumer");
		using PackageAssemblyContext context = new(output);
		Type type = context.LoadFromAssemblyPath(Path.Combine(output, "ProviderConsumer.dll")).GetType(consumerType, throwOnError: true)!;
		return Assert.IsType<double[]>(type.GetMethod("Values")!.Invoke(null, null));
	}
}
