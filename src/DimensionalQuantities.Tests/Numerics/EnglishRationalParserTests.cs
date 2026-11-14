using System.Numerics;
using DimensionalQuantities.Numerics;

namespace DimensionalQuantities.Tests.Numerics;

public sealed class EnglishRationalParserTests
{
	[Test]
	[Arguments("Zero", 0)]
	[Arguments("One", 1)]
	[Arguments("Nine", 9)]
	[Arguments("Ten", 10)]
	[Arguments("Eleven", 11)]
	[Arguments("Twelve", 12)]
	[Arguments("Thirteen", 13)]
	[Arguments("Nineteen", 19)]
	[Arguments("Twenty", 20)]
	[Arguments("TwentyOne", 21)]
	[Arguments("FortyTwo", 42)]
	[Arguments("NinetyNine", 99)]
	[Arguments("OneHundred", 100)]
	[Arguments("OneHundredAndOne", 101)]
	[Arguments("OneHundredAndTwentyThree", 123)]
	[Arguments("OneHundredTwentyThree", 123)]
	[Arguments("NineHundredAndNinetyNine", 999)]
	[Arguments("OneThousand", 1000)]
	[Arguments("OneThousandAndOne", 1001)]
	[Arguments("OneThousandOneHundredAndOne", 1101)]
	[Arguments("TwelveThousandThreeHundredAndFortyFive", 12345)]
	[Arguments("TwoHundredAndThirtyFourThousandFiveHundredAndSix", 234506)]
	[Arguments("TwoHundredThousandAndSix", 200006)]
	[Arguments("OneMillionTwoHundredAndThirtyFourThousandFiveHundredAndSixtySeven", 1234567)]
	[Arguments("OneMillionAndOne", 1000001)]
	[Arguments("OneBillion", 1000000000)]
	[Arguments("MinusThree", -3)]
	[Arguments("MinusOneHundredAndTwentyThree", -123)]
	[Arguments("MinusZero", 0)]
	public async Task ParsesIntegerNames(string text, int expectedValue)
	{
		bool success = EnglishRationalParser.TryParse(text, out Rational result);
		Rational expected = new(expectedValue);

		using (Assert.Multiple())
		{
			await Assert.That(success).IsTrue();
			await Assert.That(result).IsEqualTo(expected);
		}
	}

	[Test]
	[Arguments("Half", 1, 2)]
	[Arguments("OneHalf", 1, 2)]
	[Arguments("ThreeHalves", 3, 2)]
	[Arguments("Quarter", 1, 4)]
	[Arguments("OneQuarter", 1, 4)]
	[Arguments("ThreeQuarters", 3, 4)]
	[Arguments("Third", 1, 3)]
	[Arguments("OneThird", 1, 3)]
	[Arguments("TwoThirds", 2, 3)]
	[Arguments("Fourth", 1, 4)]
	[Arguments("ThreeFourths", 3, 4)]
	[Arguments("TwoFifths", 2, 5)]
	[Arguments("FiveSixths", 5, 6)]
	[Arguments("SixSevenths", 6, 7)]
	[Arguments("SevenEighths", 7, 8)]
	[Arguments("EightNinths", 8, 9)]
	[Arguments("NineTenths", 9, 10)]
	[Arguments("TwoElevenths", 2, 11)]
	[Arguments("FiveTwelfths", 5, 12)]
	[Arguments("OneTwentieth", 1, 20)]
	[Arguments("ThreeHundredths", 3, 100)]
	[Arguments("Thousandth", 1, 1000)]
	[Arguments("OneThousandth", 1, 1000)]
	[Arguments("ThreeThousandths", 3, 1000)]
	[Arguments("Millionth", 1, 1000000)]
	[Arguments("OneMillionth", 1, 1000000)]
	[Arguments("ThreeMillionths", 3, 1000000)]
	[Arguments("MinusThreeMillionths", -3, 1000000)]
	[Arguments("TwoQuarters", 1, 2)]
	[Arguments("ZeroThirds", 0, 1)]
	[Arguments("MinusHalf", -1, 2)]
	[Arguments("MinusThreeQuarters", -3, 4)]
	public async Task ParsesNaturalFractions(string text, int numerator, int denominator)
	{
		bool success = EnglishRationalParser.TryParse(text, out Rational result);
		Rational expected = new(numerator, denominator);

		using (Assert.Multiple())
		{
			await Assert.That(success).IsTrue();
			await Assert.That(result).IsEqualTo(expected);
		}
	}

	[Test]
	[Arguments("TwoOverThree", 2, 3)]
	[Arguments("SeventeenOverTwentyThree", 17, 23)]
	[Arguments("OneHundredAndTwentyThreeOverFortyFive", 41, 15)]
	[Arguments("TwoOverFour", 1, 2)]
	[Arguments("SixOverThree", 2, 1)]
	[Arguments("ZeroOverSeven", 0, 1)]
	[Arguments("MinusThreeOverTwo", -3, 2)]
	[Arguments("ThreeOverMinusTwo", -3, 2)]
	[Arguments("MinusThreeOverMinusTwo", 3, 2)]
	public async Task ParsesFractionsSeparatedByOver(string text, int numerator, int denominator)
	{
		bool success = EnglishRationalParser.TryParse(text, out Rational result);
		Rational expected = new(numerator, denominator);

		using (Assert.Multiple())
		{
			await Assert.That(success).IsTrue();
			await Assert.That(result).IsEqualTo(expected);
		}
	}

	[Test]
	public async Task ParsesNumbersLargerThanLong()
	{
		bool success = EnglishRationalParser.TryParse("TwentyQuintillionAndOneOverThree", out Rational result);
		Rational expected = new(20 * BigInteger.Pow(10, 18) + 1, 3);

		using (Assert.Multiple())
		{
			await Assert.That(success).IsTrue();
			await Assert.That(result).IsEqualTo(expected);
		}
	}

	[Test]
	[Arguments("OneTrillion", 12)]
	[Arguments("OneQuadrillion", 15)]
	[Arguments("OneQuintillion", 18)]
	[Arguments("OneSextillion", 21)]
	[Arguments("OneSeptillion", 24)]
	[Arguments("OneOctillion", 27)]
	[Arguments("OneNonillion", 30)]
	[Arguments("OneDecillion", 33)]
	public async Task ParsesLargeScaleNames(string text, int exponent)
	{
		bool success = EnglishRationalParser.TryParse(text, out Rational result);
		Rational expected = new(BigInteger.Pow(10, exponent));

		using (Assert.Multiple())
		{
			await Assert.That(success).IsTrue();
			await Assert.That(result).IsEqualTo(expected);
		}
	}

	[Test]
	[Arguments((string?)null)]
	[Arguments("")]
	[Arguments(" ")]
	[Arguments("one")]
	[Arguments("ONE")]
	[Arguments("One Two")]
	[Arguments("Twenty-One")]
	[Arguments("Twenty1")]
	[Arguments("ThreePointFive")]
	[Arguments("OneTwo")]
	[Arguments("TwentyTen")]
	[Arguments("ZeroOne")]
	[Arguments("ZeroHundred")]
	[Arguments("Hundred")]
	[Arguments("OneHundredHundred")]
	[Arguments("TenHundred")]
	[Arguments("OneThousandMillion")]
	[Arguments("OneThousandAndTwoHundred")]
	[Arguments("OneMillionTwoMillion")]
	[Arguments("OneAndTwo")]
	[Arguments("AndOne")]
	[Arguments("OneHundredAnd")]
	[Arguments("OneHundredAndAndOne")]
	[Arguments("Minus")]
	[Arguments("MinusMinusOne")]
	[Arguments("OneMinusTwo")]
	[Arguments("OneOverZero")]
	[Arguments("ZeroOverZero")]
	[Arguments("OneOverMinusZero")]
	[Arguments("OverTwo")]
	[Arguments("OneOver")]
	[Arguments("OneOverTwoOverThree")]
	[Arguments("HalfOverTwo")]
	[Arguments("OneHalfExtra")]
	[Arguments("TwoHalf")]
	[Arguments("OneThirds")]
	[Arguments("Halves")]
	[Arguments("OneAndAHalf")]
	[Arguments("OneUndecillion")]
	public async Task RejectsInvalidNames(string? text)
	{
		bool success = EnglishRationalParser.TryParse(text, out Rational result);
		Rational expected = default;

		using (Assert.Multiple())
		{
			await Assert.That(success).IsFalse();
			await Assert.That(result).IsEqualTo(expected);
		}
	}
}
