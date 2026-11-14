using DimensionalQuantities.Numerics;
using DimensionalQuantities.Roslyn.Generators;
using DimensionalQuantities.Units;

namespace DimensionalQuantities.Roslyn.Tests.Generators;

[NotInParallel(nameof(FractionalPrefixEmitterTests))]
public sealed class FractionalPrefixEmitterTests
{
	[Test]
	[Arguments("""
		public static Length FromPrefixedUnits(double value, int widgetPrefixExponent = 0) => Length.FromSiBaseUnit(value * 2d * global::System.Math.Pow(10, ((double)widgetPrefixExponent) * (1d / 2d)));
		""")]
	[Arguments("""
		public double AsPrefixedUnits(int widgetPrefixExponent = 0) => length.AsSiBaseUnit() / (2d * global::System.Math.Pow(10, ((double)widgetPrefixExponent) * (1d / 2d)));
		""")]
	[Arguments("""
		public double AsPrefixedUnits(int widgetPrefixExponent = 0) => delta.AsSiBaseUnit() / (2d * global::System.Math.Pow(10, ((double)widgetPrefixExponent) * (1d / 2d)));
		""")]
	public async Task FractionalPrefixConversionsUseFloatingPointDivision(string expectedDeclaration)
	{
		AtomicUnit widget = new("Widget", "Widgets", "w", Scale: 4);
		UnitConversionSpec[] units =
		[
			new("Length", "Length", [new UnitDefinition("Length", [new(widget, new Rational(1, 2))])],
				IncludeNamed: false, GenerateSiPrefixes: false, GeneratePrefixConversions: true)
		];

		string source = UnitConversionSourceEmitter.Emit(units, "Custom", "public");

		await Assert.That(source).Contains(expectedDeclaration);
	}
}
