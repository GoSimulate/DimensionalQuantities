using System.Numerics;
using DimensionalQuantities.Numerics;
using Xunit;

namespace DimensionalQuantities.Tests.Numerics;

public sealed class EnglishRationalParserTests
{
	[Theory]
	[InlineData("Zero", 0)]
	[InlineData("One", 1)]
	[InlineData("Nine", 9)]
	[InlineData("Ten", 10)]
	[InlineData("Eleven", 11)]
	[InlineData("Twelve", 12)]
	[InlineData("Thirteen", 13)]
	[InlineData("Nineteen", 19)]
	[InlineData("Twenty", 20)]
	[InlineData("TwentyOne", 21)]
	[InlineData("FortyTwo", 42)]
	[InlineData("NinetyNine", 99)]
	[InlineData("OneHundred", 100)]
	[InlineData("OneHundredAndOne", 101)]
	[InlineData("OneHundredAndTwentyThree", 123)]
	[InlineData("OneHundredTwentyThree", 123)]
	[InlineData("NineHundredAndNinetyNine", 999)]
	[InlineData("OneThousand", 1000)]
	[InlineData("OneThousandAndOne", 1001)]
	[InlineData("OneThousandOneHundredAndOne", 1101)]
	[InlineData("TwelveThousandThreeHundredAndFortyFive", 12345)]
	[InlineData("TwoHundredAndThirtyFourThousandFiveHundredAndSix", 234506)]
	[InlineData("TwoHundredThousandAndSix", 200006)]
	[InlineData("OneMillionTwoHundredAndThirtyFourThousandFiveHundredAndSixtySeven", 1234567)]
	[InlineData("OneMillionAndOne", 1000001)]
	[InlineData("OneBillion", 1000000000)]
	[InlineData("MinusThree", -3)]
	[InlineData("MinusOneHundredAndTwentyThree", -123)]
	[InlineData("MinusZero", 0)]
	public void ParsesIntegerNames(string text, int expectedValue)
	{
		bool success = EnglishRationalParser.TryParse(text, out Rational result);
		Rational expected = new(expectedValue);

		Assert.Multiple(
			() => Assert.True(success),
			() => Assert.Equal(expected, result));
	}

	[Theory]
	[InlineData("Half", 1, 2)]
	[InlineData("OneHalf", 1, 2)]
	[InlineData("ThreeHalves", 3, 2)]
	[InlineData("Quarter", 1, 4)]
	[InlineData("OneQuarter", 1, 4)]
	[InlineData("ThreeQuarters", 3, 4)]
	[InlineData("Third", 1, 3)]
	[InlineData("OneThird", 1, 3)]
	[InlineData("TwoThirds", 2, 3)]
	[InlineData("Fourth", 1, 4)]
	[InlineData("ThreeFourths", 3, 4)]
	[InlineData("TwoFifths", 2, 5)]
	[InlineData("FiveSixths", 5, 6)]
	[InlineData("SixSevenths", 6, 7)]
	[InlineData("SevenEighths", 7, 8)]
	[InlineData("EightNinths", 8, 9)]
	[InlineData("NineTenths", 9, 10)]
	[InlineData("TwoElevenths", 2, 11)]
	[InlineData("FiveTwelfths", 5, 12)]
	[InlineData("OneTwentieth", 1, 20)]
	[InlineData("ThreeHundredths", 3, 100)]
	[InlineData("Thousandth", 1, 1000)]
	[InlineData("OneThousandth", 1, 1000)]
	[InlineData("ThreeThousandths", 3, 1000)]
	[InlineData("Millionth", 1, 1000000)]
	[InlineData("OneMillionth", 1, 1000000)]
	[InlineData("ThreeMillionths", 3, 1000000)]
	[InlineData("MinusThreeMillionths", -3, 1000000)]
	[InlineData("TwoQuarters", 1, 2)]
	[InlineData("ZeroThirds", 0, 1)]
	[InlineData("MinusHalf", -1, 2)]
	[InlineData("MinusThreeQuarters", -3, 4)]
	public void ParsesNaturalFractions(string text, int numerator, int denominator)
	{
		bool success = EnglishRationalParser.TryParse(text, out Rational result);
		Rational expected = new(numerator, denominator);

		Assert.Multiple(
			() => Assert.True(success),
			() => Assert.Equal(expected, result));
	}

	[Theory]
	[InlineData("TwoOverThree", 2, 3)]
	[InlineData("SeventeenOverTwentyThree", 17, 23)]
	[InlineData("OneHundredAndTwentyThreeOverFortyFive", 41, 15)]
	[InlineData("TwoOverFour", 1, 2)]
	[InlineData("SixOverThree", 2, 1)]
	[InlineData("ZeroOverSeven", 0, 1)]
	[InlineData("MinusThreeOverTwo", -3, 2)]
	[InlineData("ThreeOverMinusTwo", -3, 2)]
	[InlineData("MinusThreeOverMinusTwo", 3, 2)]
	public void ParsesFractionsSeparatedByOver(string text, int numerator, int denominator)
	{
		bool success = EnglishRationalParser.TryParse(text, out Rational result);
		Rational expected = new(numerator, denominator);

		Assert.Multiple(
			() => Assert.True(success),
			() => Assert.Equal(expected, result));
	}

	[Fact]
	public void ParsesNumbersLargerThanLong()
	{
		bool success = EnglishRationalParser.TryParse("TwentyQuintillionAndOneOverThree", out Rational result);
		Rational expected = new(20 * BigInteger.Pow(10, 18) + 1, 3);

		Assert.Multiple(
			() => Assert.True(success),
			() => Assert.Equal(expected, result));
	}

	[Theory]
	[InlineData("OneTrillion", 12)]
	[InlineData("OneQuadrillion", 15)]
	[InlineData("OneQuintillion", 18)]
	[InlineData("OneSextillion", 21)]
	[InlineData("OneSeptillion", 24)]
	[InlineData("OneOctillion", 27)]
	[InlineData("OneNonillion", 30)]
	[InlineData("OneDecillion", 33)]
	public void ParsesLargeScaleNames(string text, int exponent)
	{
		bool success = EnglishRationalParser.TryParse(text, out Rational result);
		Rational expected = new(BigInteger.Pow(10, exponent));

		Assert.Multiple(
			() => Assert.True(success),
			() => Assert.Equal(expected, result));
	}

	[Theory]
	[InlineData((string?)null)]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("one")]
	[InlineData("ONE")]
	[InlineData("One Two")]
	[InlineData("Twenty-One")]
	[InlineData("Twenty1")]
	[InlineData("ThreePointFive")]
	[InlineData("OneTwo")]
	[InlineData("TwentyTen")]
	[InlineData("ZeroOne")]
	[InlineData("ZeroHundred")]
	[InlineData("Hundred")]
	[InlineData("OneHundredHundred")]
	[InlineData("TenHundred")]
	[InlineData("OneThousandMillion")]
	[InlineData("OneThousandAndTwoHundred")]
	[InlineData("OneMillionTwoMillion")]
	[InlineData("OneAndTwo")]
	[InlineData("AndOne")]
	[InlineData("OneHundredAnd")]
	[InlineData("OneHundredAndAndOne")]
	[InlineData("Minus")]
	[InlineData("MinusMinusOne")]
	[InlineData("OneMinusTwo")]
	[InlineData("OneOverZero")]
	[InlineData("ZeroOverZero")]
	[InlineData("OneOverMinusZero")]
	[InlineData("OverTwo")]
	[InlineData("OneOver")]
	[InlineData("OneOverTwoOverThree")]
	[InlineData("HalfOverTwo")]
	[InlineData("OneHalfExtra")]
	[InlineData("TwoHalf")]
	[InlineData("OneThirds")]
	[InlineData("Halves")]
	[InlineData("OneAndAHalf")]
	[InlineData("OneUndecillion")]
	public void RejectsInvalidNames(string? text)
	{
		bool success = EnglishRationalParser.TryParse(text, out Rational result);
		Rational expected = default;

		Assert.Multiple(
			() => Assert.False(success),
			() => Assert.Equal(expected, result));
	}
}
