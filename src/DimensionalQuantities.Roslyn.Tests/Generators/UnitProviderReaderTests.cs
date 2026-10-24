using DimensionalQuantities.Roslyn.Generators;
using DimensionalQuantities.TypeSystem;
using DimensionalQuantities.Numerics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generators;

public sealed class UnitProviderReaderTests
{
	[Fact]
	public void InvalidPowerDenominatorReportsDiagnostic()
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
		Assert.Multiple(
			() => Assert.Empty(units),
			() => Assert.Equal("DQ0008", Assert.Single(diagnostics).Id));
	}

	[Fact]
	public void RationalFactorPowerAndScaleAreReadFromDeclarations()
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
		UnitConversionSpec result = Assert.Single(units);
		Assert.Multiple(
			() => Assert.Empty(diagnostics),
			() => Assert.Equal(new Rational(1, 2), result.Units[0].Expression.Terms[0].Power),
			() => Assert.Equal(4, result.Units[0].Expression.Terms[0].Unit.Scale));
	}

	[Fact]
	public void MultipleAliasesForSameQuantityShareOneUnitDefinition()
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
		UnitConversionSpec result = Assert.Single(units);
		Assert.Multiple(
			() => Assert.Empty(diagnostics),
			() => Assert.Single(result.Units),
			() => Assert.Equal("Pascal", result.ExtensionName));
	}

	[Fact]
	public void OneDeclarationCanAssociateDistinctQuantityTypes()
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
		Assert.Multiple(
			() => Assert.Empty(diagnostics),
			() => Assert.All(units, unit => Assert.Equal("Token", unit.ExtensionName)),
			() => Assert.Equal(expected.Order(StringComparer.Ordinal), units.Select(unit => unit.ReceiverType).Order(StringComparer.Ordinal)));
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
		source = $"global using Length = {lengthType};\nglobal using Time = {timeType};\nglobal using Pressure = {pressureType};\nglobal using Stress = {pressureType};\n" + source;
		CSharpCompilation input = CSharpCompilation.Create("UnitProviderReaderTest" + Guid.NewGuid().ToString("N"),
			[CSharpSyntaxTree.ParseText(source, options)], references, new(OutputKind.DynamicallyLinkedLibrary));
		UnitProviderReader reader = new(input);
		List<Diagnostic> diagnostics = [];
		IReadOnlyList<UnitConversionSpec> units = reader.Read(input.GetTypeByMetadataName("Units")!, diagnostics.Add);
		return (units, diagnostics);
	}
}
