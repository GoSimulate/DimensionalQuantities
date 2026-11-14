using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Naming;

namespace DimensionalQuantities.Tests.Naming;

public sealed class DimensionNameGeneratorTests
{
	[Test]
	[Arguments(1, 0, 0, 0, 0, 0, 0, "Length")]
	[Arguments(0, 1, 0, 0, 0, 0, 0, "Mass")]
	[Arguments(0, 0, 1, 0, 0, 0, 0, "Time")]
	[Arguments(0, 0, 0, 1, 0, 0, 0, "Current")]
	[Arguments(0, 0, 0, 0, 1, 0, 0, "Temperature")]
	[Arguments(0, 0, 0, 0, 0, 1, 0, "AmountOfSubstance")]
	[Arguments(0, 0, 0, 0, 0, 0, 1, "LuminousIntensity")]
	public async Task GetQuantityNameUsesBaseVocabulary(int length, int mass, int time, int electricCurrent, int temperature, int amountOfSubstance, int luminousIntensity, string expected)
	{
		Dimension original = new(length, mass, time, electricCurrent, temperature, amountOfSubstance, luminousIntensity);
		string result = DimensionNameGenerator.GetQuantityName(original);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	[Arguments(1, "Current")]
	[Arguments(2, "CurrentSquared")]
	[Arguments(3, "CurrentCubed")]
	[Arguments(4, "Current4")]
	[Arguments(12, "Current12")]
	[Arguments(-1, "InverseCurrent")]
	[Arguments(-2, "InverseCurrentSquared")]
	[Arguments(-3, "InverseCurrentCubed")]
	[Arguments(-4, "InverseCurrent4")]
	[Arguments(-12, "InverseCurrent12")]
	public async Task GetQuantityNameFormatsIntegerPowers(int exponent, string expected)
	{
		Dimension original = new(ElectricCurrent: exponent);
		string result = DimensionNameGenerator.GetQuantityName(original);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	[Arguments(1, 1, 1, 1, 1, 1, 1, "LengthMassTimeCurrentTemperatureAmountOfSubstanceLuminousIntensity")]
	[Arguments(3, 0, -12, 0, 0, 0, 0, "LengthCubedPerTime12")]
	[Arguments(1, 1, -2, 0, 0, 0, 0, "LengthMassPerTimeSquared")]
	[Arguments(2, 1, -3, 0, 0, 0, 0, "LengthSquaredMassPerTimeCubed")]
	[Arguments(0, 0, 0, 0, -1, 0, 1, "LuminousIntensityPerTemperature")]
	[Arguments(-2, -1, 3, -4, 1, -2, 1, "TimeCubedTemperatureLuminousIntensityPerLengthSquaredMassCurrent4AmountOfSubstanceSquared")]
	[Arguments(-1, -1, -1, -1, -1, -1, -1, "InverseLengthMassTimeCurrentTemperatureAmountOfSubstanceLuminousIntensity")]
	public async Task GetQuantityNameComposesFactorsInSiOrder(int length, int mass, int time, int electricCurrent, int temperature, int amountOfSubstance, int luminousIntensity, string expected)
	{
		Dimension original = new(length, mass, time, electricCurrent, temperature, amountOfSubstance, luminousIntensity);
		string result = DimensionNameGenerator.GetQuantityName(original);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	[Arguments(1, 2, "SquareRootCurrent")]
	[Arguments(1, 3, "CubeRootCurrent")]
	[Arguments(3, 2, "SquareRootCurrentCubed")]
	[Arguments(2, 3, "CubeRootCurrentSquared")]
	[Arguments(2, 4, "SquareRootCurrent")]
	[Arguments(4, 2, "CurrentSquared")]
	[Arguments(-3, 2, "InverseSquareRootCurrentCubed")]
	[Arguments(1, -2, "InverseSquareRootCurrent")]
	[Arguments(1, 12, "Root12Current")]
	[Arguments(1, 4, "Root4Current")]
	[Arguments(1, 5, "Root5Current")]
	[Arguments(4, 5, "Root5Current4")]
	[Arguments(1, 45, "Root45Current")]
	public async Task GetQuantityNameFormatsReducedRationalPowers(int numerator, int denominator, string expected)
	{
		Dimension original = new(ElectricCurrent: new(numerator, denominator));
		string result = DimensionNameGenerator.GetQuantityName(original);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task GetQuantityNameKeepsFractionalPowersAttachedToTheirFactors()
	{
		Dimension original = new(Length: new(1, 2), Time: new(-3, 2), ElectricCurrent: 1);
		string result = DimensionNameGenerator.GetQuantityName(original);
		string expected = "SquareRootLengthCurrentPerSquareRootTimeCubed";

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	[Arguments(1, 0, 0, 0, 0, 0, 0, "m")]
	[Arguments(0, 1, 0, 0, 0, 0, 0, "kg")]
	[Arguments(0, 0, 1, 0, 0, 0, 0, "s")]
	[Arguments(0, 0, 0, 1, 0, 0, 0, "A")]
	[Arguments(0, 0, 0, 0, 1, 0, 0, "K")]
	[Arguments(0, 0, 0, 0, 0, 1, 0, "mol")]
	[Arguments(0, 0, 0, 0, 0, 0, 1, "cd")]
	[Arguments(1, 1, 1, 1, 1, 1, 1, "mkgsAKmolcd")]
	[Arguments(3, 0, -12, 0, 0, 0, 0, "m<sup>3</sup>s<sup>-12</sup>")]
	[Arguments(0, 0, -1, 0, 0, 0, 0, "s<sup>-1</sup>")]
	[Arguments(-2, 1, 3, 0, 0, 0, 0, "m<sup>-2</sup>kgs<sup>3</sup>")]
	public async Task GetUnitExpressionUsesAdjacentSiSymbolsInAxisOrder(int length, int mass, int time, int electricCurrent, int temperature, int amountOfSubstance, int luminousIntensity, string expected)
	{
		Dimension original = new(length, mass, time, electricCurrent, temperature, amountOfSubstance, luminousIntensity);
		string result = DimensionNameGenerator.GetUnitExpression(original);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	[Arguments(1, 2, "A<sup>1/2</sup>")]
	[Arguments(3, 2, "A<sup>3/2</sup>")]
	[Arguments(-3, 2, "A<sup>-3/2</sup>")]
	[Arguments(1, -2, "A<sup>-1/2</sup>")]
	[Arguments(2, 4, "A<sup>1/2</sup>")]
	[Arguments(4, 2, "A<sup>2</sup>")]
	[Arguments(2, 2, "A")]
	[Arguments(4, 5, "A<sup>4/5</sup>")]
	public async Task GetUnitExpressionFormatsReducedRationalPowers(int numerator, int denominator, string expected)
	{
		Dimension original = new(ElectricCurrent: new(numerator, denominator));
		string result = DimensionNameGenerator.GetUnitExpression(original);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task GetUnitExpressionKeepsFractionalPowersAttachedToTheirSymbols()
	{
		Dimension original = new(Length: new(1, 2), Time: new(-3, 2), ElectricCurrent: 1);
		string result = DimensionNameGenerator.GetUnitExpression(original);
		string expected = "m<sup>1/2</sup>s<sup>-3/2</sup>A";

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task NamingMethodsRejectDimensionlessQuantities()
	{
		Dimension original = default;

		using (Assert.Multiple())
		{
			await Assert.That(() => DimensionNameGenerator.GetQuantityName(original)).ThrowsExactly<NotSupportedException>();
			await Assert.That(() => DimensionNameGenerator.GetUnitExpression(original)).ThrowsExactly<NotSupportedException>();
		}
	}
}
