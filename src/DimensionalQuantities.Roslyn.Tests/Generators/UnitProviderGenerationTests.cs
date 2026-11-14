using System.Reflection;
using DimensionalQuantities.Roslyn.Tests.Framework;
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
	public async Task IndependentProviderGeneratesScaledFactoriesAndAccessors()
	{
		const string consumer = """
			namespace Independent.Units;
			public static class Consumer
			{
				public static double[] Values() => [Length.FromFeet(10).AsMetres(), Length.FromMetres(0.6096).AsFeet()];
			}
			""";
		QuantityGeneratorTest test = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};
		test.TestState.Sources.Add(FootProvider);
		test.TestState.Sources.Add(consumer);

		await test.RunAsync(TestContext.Current.CancellationToken);
		double[] result = Evaluate(test.Compilation, "Independent.Units.Consumer");
		Assert.Multiple(
			() => Assert.Equal(3.048, result[0], 12),
			() => Assert.Equal(2, result[1], 12));
	}

	[Fact]
	public async Task AffineConversionsApplyOffsetOnlyToAbsoluteQuantities()
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
		QuantityGeneratorTest test = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};
		test.TestState.Sources.Add(source);

		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.Equal([16d, 3d, 1d], Evaluate(test.Compilation, "Consumer"));
	}

	[Fact]
	public async Task ProviderConversionsArePublicAndNotRegeneratedByConsumers()
	{
		QuantityGeneratorTest provider = new()
		{
			TestCode = FootProvider,
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};
		provider.SolutionTransforms.Add((solution, project) => solution.WithProjectAssemblyName(project, "Independent.Provider"));
		await provider.RunAsync(TestContext.Current.CancellationToken);
		MetadataReference reference = MetadataReference.CreateFromImage(Emit(provider.Compilation));
		const string consumer = """
			using Independent.Units;
			public static class Consumer
			{
				public static Area Value() => Length.FromFeet(1) * Length.FromFeet(2);
			}
			""";
		QuantityGeneratorTest test = new()
		{
			TestCode = consumer,
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};
		test.TestState.AdditionalReferences.Add(reference);

		await test.RunAsync(TestContext.Current.CancellationToken);
		INamespaceSymbol? localNamespace = test.Compilation.Assembly.GlobalNamespace.GetNamespaceMembers()
			.SingleOrDefault(symbol => symbol.Name == "Independent");

		Assert.Null(localNamespace);
	}

	[Fact]
	public async Task ProviderCanOptIntoNamedAndParameterisedSiPrefixes()
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
		QuantityGeneratorTest test = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};
		test.TestState.Sources.Add(source);

		await test.RunAsync(TestContext.Current.CancellationToken);
		double[] result = Evaluate(test.Compilation, "Independent.Units.Consumer");
		Assert.Equal([0.5, 2, 3000, 4], result);
	}

	[Fact]
	public async Task CompoundFactorsPreserveOrderPowersAndAdditionalConversions()
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
		QuantityGeneratorTest test = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};
		test.TestState.Sources.Add(source);

		await test.RunAsync(TestContext.Current.CancellationToken);
		double[] result = Evaluate(test.Compilation, "Independent.Units.Consumer");
		Assert.Equal([1000000d, 2000000d, 1d], result);
	}

	[Fact]
	public async Task ProviderUsesCoreSiSymbolsAcrossAssemblyBoundary()
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
		QuantityGeneratorTest test = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};
		test.TestState.Sources.Add(source);

		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.Equal([2d, 2000000d, 0.002d, 0.002d], Evaluate(test.Compilation, "Independent.Units.Consumer"));
	}

	[Fact]
	public async Task OneUnitDeclarationCanServeDistinctQuantityTypes()
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
		QuantityGeneratorTest test = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};
		test.TestState.Sources.Add(source);

		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.Equal([2d, 3d], Evaluate(test.Compilation, "Independent.Units.Consumer"));
	}

	[Fact]
	public async Task ProviderFactoriesCanBeUsedInArithmeticInTheirOwnAssembly()
	{
		const string consumer = """
			namespace Independent.Units;
			public static class Consumer
			{
				public static double[] Values() => [(Length.FromFeet(2) * Length.FromFeet(3)).AsSiBaseUnit()];
			}
			""";
		QuantityGeneratorTest test = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};
		test.TestState.Sources.Add(FootProvider);
		test.TestState.Sources.Add(consumer);

		await test.RunAsync(TestContext.Current.CancellationToken);
		double[] result = Evaluate(test.Compilation, "Independent.Units.Consumer");
		Assert.Equal(0.55741824, Assert.Single(result), 12);
	}

	[Fact]
	public async Task RecordClassProvidersGenerateConversions()
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
		QuantityGeneratorTest test = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};
		test.TestState.Sources.Add(provider);
		test.TestState.Sources.Add(consumer);

		await test.RunAsync(TestContext.Current.CancellationToken);
		double[] result = Evaluate(test.Compilation, "Independent.Units.Consumer");
		Assert.Equal(0.3048, Assert.Single(result), 12);
	}

	[Theory]
	[InlineData("Bad name", "Feet")]
	[InlineData("Foot", "Bad names")]
	public async Task InvalidUnitNamesReportDeclarationDiagnostics(string singular, string plural)
	{
		string source = $$"""
			using DimensionalQuantities.Units.Declarations;
			[UnitProvider(GeneratePrefixConversions = true)]
			internal static class {|#0:Units|}
			{
				[QuantityUnit(typeof(Length))]
				[UnitFactor(typeof(Foot))]
				private sealed class LengthUnit;
				[UnitSymbol("{{singular}}", "{{plural}}", "ft")]
				private sealed class Foot;
			}
			""";
		QuantityGeneratorTest test = new()
		{
			TestCode = source,
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};
		test.ExpectedDiagnostics.Add(new DiagnosticResult("DQ0008", DiagnosticSeverity.Error)
			.WithLocation(0).WithMessage("Unit provider for Length produces an invalid C# identifier."));

		await test.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task ProvidersInSameNamespaceCanConvertSameQuantity()
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
		QuantityGeneratorTest test = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
		};
		test.TestState.Sources.Add(source);

		await test.RunAsync(TestContext.Current.CancellationToken);
		double[] result = Evaluate(test.Compilation, "Independent.Units.Consumer");
		Assert.Multiple(
			() => Assert.Equal(1, result[0], 12),
			() => Assert.Equal(6, result[1], 12));
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
