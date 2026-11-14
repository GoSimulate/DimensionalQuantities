using DimensionalQuantities.Roslyn.Generators;
using DimensionalQuantities.TypeSystem;
using DimensionalQuantities.Numerics;
using DimensionalQuantities.Units;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DimensionalQuantities.Roslyn.Tests.Generators;

[NotInParallel(nameof(UnitProviderReaderTests))]
public sealed class UnitProviderReaderTests
{
	[Test]
	public async Task InvalidPowerDenominatorReportsDiagnostic()
	{
		const string source = """
			using DimensionalQuantities.Units.Declarations;
			[UnitProvider]
			internal static class Units
			{
				[QuantityUnit(typeof(Length))]
				[UnitFactor(typeof(Foot), PowerDenominator = 0)]
				private sealed class LengthUnit;
				[UnitSymbol("Foot", "Feet", "ft")]
				private sealed class Foot;
			}
			""";
		(IReadOnlyList<UnitConversionSpec> units, List<Diagnostic> diagnostics) = Read(source);
		using (Assert.Multiple())
		{
			await Assert.That(units).IsEmpty();
			await Assert.That((await Assert.That(diagnostics).HasSingleItem()).Id).IsEqualTo("DQ0008");
		}
	}

	[Test]
	public async Task RationalFactorPowerAndScaleAreReadFromDeclarations()
	{
		const string source = """
			using DimensionalQuantities.Units.Declarations;
			[UnitProvider]
			internal static class Units
			{
				[QuantityUnit(typeof(Length))]
				[UnitFactor(typeof(SquareMetre), PowerDenominator = 2)]
				private sealed class LengthUnit;
				[UnitSymbol("SquareMetre", "SquareMetres", "m²", Scale = 4)]
				private sealed class SquareMetre;
			}
			""";
		(IReadOnlyList<UnitConversionSpec> units, List<Diagnostic> diagnostics) = Read(source);
		UnitConversionSpec result = (await Assert.That(units).HasSingleItem());
		using (Assert.Multiple())
		{
			await Assert.That(diagnostics).IsEmpty();
			await Assert.That(result.Units[0].Expression.Terms[0].Power).IsEqualTo(new Rational(1, 2));
			await Assert.That(result.Units[0].Expression.Terms[0].Unit.Scale).IsEqualTo(4);
		}
	}

	[Test]
	public async Task MultipleAliasesForSameQuantityShareOneUnitDefinition()
	{
		const string source = """
			using DimensionalQuantities.Units.Declarations;
			[UnitProvider]
			internal static class Units
			{
				[QuantityUnit(typeof(Pressure))]
				[QuantityUnit(typeof(Stress))]
				[UnitFactor(typeof(Pascal))]
				private sealed class PascalUnit;
				[UnitSymbol("Pascal", "Pascals", "Pa")]
				private sealed class Pascal;
			}
			""";
		(IReadOnlyList<UnitConversionSpec> units, List<Diagnostic> diagnostics) = Read(source);
		UnitConversionSpec result = (await Assert.That(units).HasSingleItem());
		using (Assert.Multiple())
		{
			await Assert.That(diagnostics).IsEmpty();
			await Assert.That(result.Units).HasSingleItem();
			await Assert.That(result.ExtensionName).IsEqualTo("Pascal");
		}
	}

	[Test]
	public async Task AffineOffsetIsReadForAnAdditionalUnit()
	{
		const string source = """
			using DimensionalQuantities.Units.Declarations;
			[UnitProvider]
			internal static class Units
			{
				[QuantityUnit(typeof(Temperature))]
				[UnitFactor(typeof(Rankine))]
				[AdditionalUnit(typeof(FahrenheitUnit))]
				private sealed class TemperatureUnit;
				[UnitFactor(typeof(Fahrenheit))]
				private sealed class FahrenheitUnit;
				[UnitSymbol("Rankine", "Rankine", "°R", Scale = 5d / 9d)]
				private sealed class Rankine;
				[UnitSymbol("Fahrenheit", "Fahrenheit", "°F", Scale = 5d / 9d)]
				[AffineUnit(459.67)]
				private sealed class Fahrenheit;
			}
			""";
		(IReadOnlyList<UnitConversionSpec> units, List<Diagnostic> diagnostics) = Read(source);
		UnitDefinition unit = (await Assert.That((await Assert.That(units).HasSingleItem()).Units).HasSingleItem());
		using (Assert.Multiple())
		{
			await Assert.That(diagnostics).IsEmpty();
			await Assert.That(unit.OffsetBeforeScale).IsNull();
			await Assert.That((await Assert.That(unit.AdditionalConversions).HasSingleItem()).OffsetBeforeScale).IsEqualTo(459.67);
		}
	}

	[Test]
	[Arguments("[UnitFactor(typeof(Celsius), Power = 2)]", "")]
	[Arguments("[UnitFactor(typeof(Celsius), PrefixExponent = 3)]", "")]
	[Arguments("[UnitFactor(typeof(Celsius))]", "[UnitFactor(typeof(Metre))]")]
	public async Task AffineUnitsRejectPoweredPrefixedAndCompoundDeclarations(string factor, string additionalFactor)
	{
		string source = $$"""
			using DimensionalQuantities.Units.Declarations;
			[UnitProvider]
			internal static class Units
			{
				[QuantityUnit(typeof(Temperature))]
				{{factor}}
				{{additionalFactor}}
				private sealed class TemperatureUnit;
				[UnitSymbol("Celsius", "Celsius", "°C")]
				[AffineUnit(273.15)]
				private sealed class Celsius;
				[UnitSymbol("Metre", "Metres", "m")]
				private sealed class Metre;
			}
			""";
		(IReadOnlyList<UnitConversionSpec> units, List<Diagnostic> diagnostics) = Read(source);
		using (Assert.Multiple())
		{
			await Assert.That(units).IsEmpty();
			await Assert.That((await Assert.That(diagnostics).HasSingleItem()).Id).IsEqualTo("DQ0008");
		}
	}

	[Test]
	public async Task OneDeclarationCanAssociateDistinctQuantityTypes()
	{
		const string source = """
			using DimensionalQuantities.Units.Declarations;
			[UnitProvider]
			internal static class Units
			{
				[QuantityUnit(typeof(Length))]
				[QuantityUnit(typeof(Time))]
				[UnitFactor(typeof(Token))]
				private sealed class TokenUnit;
				[UnitSymbol("Token", "Tokens", "token")]
				private sealed class Token;
			}
			""";
		(IReadOnlyList<UnitConversionSpec> units, List<Diagnostic> diagnostics) = Read(source);
		string[] expected =
		[
			QuantityTypeNameFormatter.Format(new DimensionalQuantities.Dimensions.Dimension(Length: 1)),
			QuantityTypeNameFormatter.Format(new DimensionalQuantities.Dimensions.Dimension(Time: 1))
		];
		using (Assert.Multiple())
		{
			await Assert.That(diagnostics).IsEmpty();
			foreach (UnitConversionSpec unit in units)
			{
				await Assert.That(unit.ExtensionName).IsEqualTo("Token");
			}
			await Assert.That(units.Select(unit => unit.ReceiverType).Order(StringComparer.Ordinal)).IsEquivalentTo(expected.Order(StringComparer.Ordinal), TUnit.Assertions.Enums.CollectionOrdering.Matching);
		}
	}

	private static (IReadOnlyList<UnitConversionSpec> Units, List<Diagnostic> Diagnostics) Read(string source)
	{
		CSharpParseOptions options = new(LanguageVersion.Preview);
		string[] assemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<MetadataReference> references = assemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		string lengthType = QuantityTypeNameFormatter.Format(new DimensionalQuantities.Dimensions.Dimension(Length: 1));
		string timeType = QuantityTypeNameFormatter.Format(new DimensionalQuantities.Dimensions.Dimension(Time: 1));
		string pressureType = QuantityTypeNameFormatter.Format(new DimensionalQuantities.Dimensions.Dimension(Length: -1, Mass: 1, Time: -2));
		string temperatureType = QuantityTypeNameFormatter.Format(new DimensionalQuantities.Dimensions.Dimension(Temperature: 1));
		source = $"global using Length = {lengthType};\nglobal using Time = {timeType};\nglobal using Pressure = {pressureType};\nglobal using Stress = {pressureType};\nglobal using Temperature = {temperatureType};\n" + source;
		CSharpCompilation input = CSharpCompilation.Create("UnitProviderReaderTest" + Guid.NewGuid().ToString("N"),
			[CSharpSyntaxTree.ParseText(source, options)], references, new(OutputKind.DynamicallyLinkedLibrary));
		UnitProviderReader reader = new(input);
		List<Diagnostic> diagnostics = [];
		IReadOnlyList<UnitConversionSpec> units = reader.Read(input.GetTypeByMetadataName("Units")!, diagnostics.Add);
		return (units, diagnostics);
	}
}
