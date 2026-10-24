using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Naming;
using Xunit;

namespace DimensionalQuantities.Tests.Naming;

public sealed class DimensionNameGeneratorTests
{
	[Theory]
	[InlineData(1, 0, 0, 0, 0, 0, 0, "Length")]
	[InlineData(0, 1, 0, 0, 0, 0, 0, "Mass")]
	[InlineData(0, 0, 1, 0, 0, 0, 0, "Time")]
	[InlineData(0, 0, 0, 1, 0, 0, 0, "Current")]
	[InlineData(0, 0, 0, 0, 1, 0, 0, "Temperature")]
	[InlineData(0, 0, 0, 0, 0, 1, 0, "AmountOfSubstance")]
	[InlineData(0, 0, 0, 0, 0, 0, 1, "LuminousIntensity")]
	public void GetQuantityNameUsesBaseVocabulary(int length, int mass, int time, int electricCurrent, int temperature, int amountOfSubstance, int luminousIntensity, string expected)
	{
		Dimension original = new(length, mass, time, electricCurrent, temperature, amountOfSubstance, luminousIntensity);
		string result = DimensionNameGenerator.GetQuantityName(original);

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData(1, "Current")]
	[InlineData(2, "CurrentSquared")]
	[InlineData(3, "CurrentCubed")]
	[InlineData(4, "Current4")]
	[InlineData(12, "Current12")]
	[InlineData(-1, "InverseCurrent")]
	[InlineData(-2, "InverseCurrentSquared")]
	[InlineData(-3, "InverseCurrentCubed")]
	[InlineData(-4, "InverseCurrent4")]
	[InlineData(-12, "InverseCurrent12")]
	public void GetQuantityNameFormatsIntegerPowers(int exponent, string expected)
	{
		Dimension original = new(ElectricCurrent: exponent);
		string result = DimensionNameGenerator.GetQuantityName(original);

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData(1, 1, 1, 1, 1, 1, 1, "LengthMassTimeCurrentTemperatureAmountOfSubstanceLuminousIntensity")]
	[InlineData(3, 0, -12, 0, 0, 0, 0, "LengthCubedPerTime12")]
	[InlineData(1, 1, -2, 0, 0, 0, 0, "LengthMassPerTimeSquared")]
	[InlineData(2, 1, -3, 0, 0, 0, 0, "LengthSquaredMassPerTimeCubed")]
	[InlineData(0, 0, 0, 0, -1, 0, 1, "LuminousIntensityPerTemperature")]
	[InlineData(-2, -1, 3, -4, 1, -2, 1, "TimeCubedTemperatureLuminousIntensityPerLengthSquaredMassCurrent4AmountOfSubstanceSquared")]
	[InlineData(-1, -1, -1, -1, -1, -1, -1, "InverseLengthMassTimeCurrentTemperatureAmountOfSubstanceLuminousIntensity")]
	public void GetQuantityNameComposesFactorsInSiOrder(int length, int mass, int time, int electricCurrent, int temperature, int amountOfSubstance, int luminousIntensity, string expected)
	{
		Dimension original = new(length, mass, time, electricCurrent, temperature, amountOfSubstance, luminousIntensity);
		string result = DimensionNameGenerator.GetQuantityName(original);

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData(1, 2, "SquareRootCurrent")]
	[InlineData(1, 3, "CubeRootCurrent")]
	[InlineData(3, 2, "SquareRootCurrentCubed")]
	[InlineData(2, 3, "CubeRootCurrentSquared")]
	[InlineData(2, 4, "SquareRootCurrent")]
	[InlineData(4, 2, "CurrentSquared")]
	[InlineData(-3, 2, "InverseSquareRootCurrentCubed")]
	[InlineData(1, -2, "InverseSquareRootCurrent")]
	[InlineData(1, 12, "Root12Current")]
	[InlineData(1, 4, "Root4Current")]
	[InlineData(1, 5, "Root5Current")]
	[InlineData(4, 5, "Root5Current4")]
	[InlineData(1, 45, "Root45Current")]
	public void GetQuantityNameFormatsReducedRationalPowers(int numerator, int denominator, string expected)
	{
		Dimension original = new(ElectricCurrent: new(numerator, denominator));
		string result = DimensionNameGenerator.GetQuantityName(original);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void GetQuantityNameKeepsFractionalPowersAttachedToTheirFactors()
	{
		Dimension original = new(Length: new(1, 2), Time: new(-3, 2), ElectricCurrent: 1);
		string result = DimensionNameGenerator.GetQuantityName(original);
		string expected = "SquareRootLengthCurrentPerSquareRootTimeCubed";

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData(1, 0, 0, 0, 0, 0, 0, "m")]
	[InlineData(0, 1, 0, 0, 0, 0, 0, "kg")]
	[InlineData(0, 0, 1, 0, 0, 0, 0, "s")]
	[InlineData(0, 0, 0, 1, 0, 0, 0, "A")]
	[InlineData(0, 0, 0, 0, 1, 0, 0, "K")]
	[InlineData(0, 0, 0, 0, 0, 1, 0, "mol")]
	[InlineData(0, 0, 0, 0, 0, 0, 1, "cd")]
	[InlineData(1, 1, 1, 1, 1, 1, 1, "mkgsAKmolcd")]
	[InlineData(3, 0, -12, 0, 0, 0, 0, "m<sup>3</sup>s<sup>-12</sup>")]
	[InlineData(0, 0, -1, 0, 0, 0, 0, "s<sup>-1</sup>")]
	[InlineData(-2, 1, 3, 0, 0, 0, 0, "m<sup>-2</sup>kgs<sup>3</sup>")]
	public void GetUnitExpressionUsesAdjacentSiSymbolsInAxisOrder(int length, int mass, int time, int electricCurrent, int temperature, int amountOfSubstance, int luminousIntensity, string expected)
	{
		Dimension original = new(length, mass, time, electricCurrent, temperature, amountOfSubstance, luminousIntensity);
		string result = DimensionNameGenerator.GetUnitExpression(original);

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData(1, 2, "A<sup>1/2</sup>")]
	[InlineData(3, 2, "A<sup>3/2</sup>")]
	[InlineData(-3, 2, "A<sup>-3/2</sup>")]
	[InlineData(1, -2, "A<sup>-1/2</sup>")]
	[InlineData(2, 4, "A<sup>1/2</sup>")]
	[InlineData(4, 2, "A<sup>2</sup>")]
	[InlineData(2, 2, "A")]
	[InlineData(4, 5, "A<sup>4/5</sup>")]
	public void GetUnitExpressionFormatsReducedRationalPowers(int numerator, int denominator, string expected)
	{
		Dimension original = new(ElectricCurrent: new(numerator, denominator));
		string result = DimensionNameGenerator.GetUnitExpression(original);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void GetUnitExpressionKeepsFractionalPowersAttachedToTheirSymbols()
	{
		Dimension original = new(Length: new(1, 2), Time: new(-3, 2), ElectricCurrent: 1);
		string result = DimensionNameGenerator.GetUnitExpression(original);
		string expected = "m<sup>1/2</sup>s<sup>-3/2</sup>A";

		Assert.Equal(expected, result);
	}

	[Fact]
	public void NamingMethodsRejectDimensionlessQuantities()
	{
		Dimension original = default;

		Assert.Multiple(
			() => Assert.Throws<NotSupportedException>(() => DimensionNameGenerator.GetQuantityName(original)),
			() => Assert.Throws<NotSupportedException>(() => DimensionNameGenerator.GetUnitExpression(original)));
	}
}
