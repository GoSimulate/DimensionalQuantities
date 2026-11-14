using DimensionalQuantities.Numerics;
using DimensionalQuantities.Roslyn.Generators;
using DimensionalQuantities.Units;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generators;

public sealed class FractionalPrefixEmitterTests
{
	[Theory]
	[InlineData("""
		public static Length FromPrefixedUnits(double value, int widgetPrefixExponent = 0) => Length.FromSiBaseUnit(value * 2d * global::System.Math.Pow(10, ((double)widgetPrefixExponent) * (1d / 2d)));
		""")]
	[InlineData("""
		public double AsPrefixedUnits(int widgetPrefixExponent = 0) => length.AsSiBaseUnit() / (2d * global::System.Math.Pow(10, ((double)widgetPrefixExponent) * (1d / 2d)));
		""")]
	[InlineData("""
		public double AsPrefixedUnits(int widgetPrefixExponent = 0) => delta.AsSiBaseUnit() / (2d * global::System.Math.Pow(10, ((double)widgetPrefixExponent) * (1d / 2d)));
		""")]
	public void FractionalPrefixConversionsUseFloatingPointDivision(string expectedDeclaration)
	{
		AtomicUnit widget = new("Widget", "Widgets", "w", Scale: 4);
		UnitConversionSpec[] units =
		[
			new("Length", "Length", [new UnitDefinition("Length", [new(widget, new Rational(1, 2))])],
				IncludeNamed: false, GenerateSiPrefixes: false, GeneratePrefixConversions: true)
		];

		string source = UnitConversionSourceEmitter.Emit(units, "Custom", "public");

		Assert.Contains(expectedDeclaration, source);
	}
}
