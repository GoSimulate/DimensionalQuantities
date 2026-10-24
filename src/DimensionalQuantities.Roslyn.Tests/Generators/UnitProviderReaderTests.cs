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

	private static (IReadOnlyList<UnitConversionSpec> Units, List<Diagnostic> Diagnostics) Read(string source)
	{
		CSharpParseOptions options = new(LanguageVersion.Preview);
		string[] assemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<MetadataReference> references = assemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		string lengthType = QuantityTypeNameFormatter.Format(new DimensionalQuantities.Dimensions.Dimension(Length: 1));
		source = $"global using Length = {lengthType};\n" + source;
		CSharpCompilation input = CSharpCompilation.Create("UnitProviderReaderTest" + Guid.NewGuid().ToString("N"),
			[CSharpSyntaxTree.ParseText(source, options)], references, new(OutputKind.DynamicallyLinkedLibrary));
		UnitProviderReader reader = new(input);
		List<Diagnostic> diagnostics = [];
		IReadOnlyList<UnitConversionSpec> units = reader.Read(input.GetTypeByMetadataName("Units")!, diagnostics.Add);
		return (units, diagnostics);
	}
}
