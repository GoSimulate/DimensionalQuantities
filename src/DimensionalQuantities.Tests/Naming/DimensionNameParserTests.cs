using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Naming;
using DimensionalQuantities.Numerics;
using Xunit;

namespace DimensionalQuantities.Tests.Naming;

public sealed class DimensionNameParserTests
{
	[Theory]
	[InlineData("LengthCurrent", 1, 0, 0, 1, 0, 0, 0)]
	[InlineData("LengthSquaredTimeCubed", 2, 0, 3, 0, 0, 0, 0)]
	[InlineData("Mass4", 0, 4, 0, 0, 0, 0, 0)]
	[InlineData("Time1048576", 0, 0, 1048576, 0, 0, 0, 0)]
	[InlineData("InverseLengthCurrent", -1, 0, 0, -1, 0, 0, 0)]
	[InlineData("LengthPerTimeCurrentSquared", 1, 0, -1, -2, 0, 0, 0)]
	[InlineData("LengthMassTimeCurrentTemperatureAmountOfSubstanceLuminousIntensity", 1, 1, 1, 1, 1, 1, 1)]
	public void ParsesCanonicalDimensionNames(string name, int length, int mass, int time, int current, int temperature, int amount, int intensity)
	{
		bool success = DimensionNameParser.TryParse(name, out Dimension result);
		Dimension expected = new(length, mass, time, current, temperature, amount, intensity);

		Assert.Multiple(
			() => Assert.True(success),
			() => Assert.Equal(expected, result),
			() => Assert.Equal(name, DimensionNameGenerator.GetQuantityName(result)));
	}

	[Theory]
	[InlineData("SquareRootLengthCurrent", 1, 2, 1, 1)]
	[InlineData("CubeRootLengthSquared", 2, 3, 0, 1)]
	[InlineData("Root5LengthCubedPerRoot4Current", 3, 5, -1, 4)]
	public void ParsesRationalPowers(string name, int lengthNumerator, int lengthDenominator, int currentNumerator, int currentDenominator)
	{
		bool success = DimensionNameParser.TryParse(name, out Dimension result);
		Dimension expected = new(Length: new Rational(lengthNumerator, lengthDenominator), ElectricCurrent: new Rational(currentNumerator, currentDenominator));

		Assert.Multiple(
			() => Assert.True(success),
			() => Assert.Equal(expected, result));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("Missing")]
	[InlineData("length")]
	[InlineData("Length0")]
	[InlineData("Length1")]
	[InlineData("Length2")]
	[InlineData("Length04")]
	[InlineData("CurrentLength")]
	[InlineData("LengthLength")]
	[InlineData("LengthPerLength")]
	[InlineData("LengthPerTimePerCurrent")]
	[InlineData("PerLength")]
	[InlineData("Inverse")]
	[InlineData("Root0Length")]
	[InlineData("Root1Length")]
	[InlineData("Root2Length")]
	[InlineData("SquareRootLengthSquared")]
	[InlineData("LengthPow4")]
	[InlineData("Length Current")]
	public void RejectsNoncanonicalOrInvalidNames(string? name)
	{
		bool success = DimensionNameParser.TryParse(name, out Dimension result);

		Assert.Multiple(
			() => Assert.False(success),
			() => Assert.Equal(default, result));
	}
}
