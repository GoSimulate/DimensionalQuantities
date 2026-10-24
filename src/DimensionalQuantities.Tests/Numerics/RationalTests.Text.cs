using System.Globalization;
using DimensionalQuantities.Numerics;
using Xunit;

namespace DimensionalQuantities.Tests.Numerics;

public sealed partial class RationalTests
{
	[Theory]
	[InlineData("6/8", 3, 4)]
	[InlineData(" 6 / -8 ", -3, 4)]
	[InlineData("7", 7, 1)]
	[InlineData("0/15", 0, 1)]
	public void ParsingNormalisesFractions(string text, int numerator, int denominator)
	{
		Rational result = Rational.Parse(text, CultureInfo.InvariantCulture);
		Rational expected = new(numerator, denominator);

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData((string?)null)]
	[InlineData("")]
	[InlineData("1/0")]
	[InlineData("1/2/3")]
	[InlineData("x/2")]
	[InlineData("1/")]
	public void TryParseRejectsInvalidFractions(string? text)
	{
		bool success = Rational.TryParse(text, CultureInfo.InvariantCulture, out Rational result);
		Rational expected = default;

		Assert.Multiple(
			() => Assert.False(success),
			() => Assert.Equal(expected, result));
	}

	[Theory]
	[InlineData("Seventeen", 17, 1)]
	[InlineData("TwoThirds", 2, 3)]
	[InlineData("MinusThreeOverTwo", -3, 2)]
	[InlineData("TwoOverFour", 1, 2)]
	public void TryParseAcceptsEnglishNames(string text, int numerator, int denominator)
	{
		bool success = Rational.TryParse(text, CultureInfo.InvariantCulture, out Rational result);
		Rational expected = new(numerator, denominator);

		Assert.Multiple(
			() => Assert.True(success),
			() => Assert.Equal(expected, result));
	}

	[Fact]
	public void TryParseWithNumberStylesAcceptsEnglishNames()
	{
		bool success = Rational.TryParse("OneHundredAndTwentyThreeOverFortyFive", NumberStyles.Integer, CultureInfo.InvariantCulture, out Rational result);
		Rational expected = new(41, 15);

		Assert.Multiple(
			() => Assert.True(success),
			() => Assert.Equal(expected, result));
	}

	[Theory]
	[InlineData("OneOverZero")]
	[InlineData("OneTwo")]
	[InlineData("One/Two")]
	public void TryParseRejectsInvalidEnglishNames(string text)
	{
		bool success = Rational.TryParse(text, CultureInfo.InvariantCulture, out Rational result);
		Rational expected = default;

		Assert.Multiple(
			() => Assert.False(success),
			() => Assert.Equal(expected, result));
	}

	[Fact]
	public void TryParseHonoursComponentNumberStyles()
	{
		bool success = Rational.TryParse("1,000/2,000", NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out Rational result);
		Rational expected = new(1, 2);

		Assert.Multiple(
			() => Assert.True(success),
			() => Assert.Equal(expected, result));
	}

	[Fact]
	public void ParseRejectsZeroDenominator()
	{
		Assert.Throws<DivideByZeroException>(() => Rational.Parse("1/0", CultureInfo.InvariantCulture));
	}

	[Fact]
	public void ParseHonoursComponentNumberStyles()
	{
		Rational result = Rational.Parse("1,000/2,000", NumberStyles.AllowThousands, CultureInfo.InvariantCulture);
		Rational expected = new(1, 2);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void FormattingProducesReducedFraction()
	{
		Rational original = new(-6, 8);
		string result = original.ToString(null, CultureInfo.InvariantCulture);
		string expected = "-3/4";

		Assert.Equal(expected, result);
	}

	[Fact]
	public void FormattingWholeNumbersOmitsDenominator()
	{
		Rational original = new(6, 2);
		string result = original.ToString(null, CultureInfo.InvariantCulture);
		string expected = "3";

		Assert.Equal(expected, result);
	}

	[Fact]
	public void StringFormattingAndParsingRoundTrip()
	{
		Rational original = new(-3, 4);
		string formattedText = original.ToString(null, CultureInfo.InvariantCulture);
		bool parsed = Rational.TryParse(formattedText, CultureInfo.InvariantCulture, out Rational result);
		Rational expected = new(-3, 4);

		Assert.Multiple(
			() => Assert.True(parsed),
			() => Assert.Equal("-3/4", formattedText),
			() => Assert.Equal(expected, result));
	}

	[Fact]
	public void FormattingAndParsingHonourCulture()
	{
		Rational original = new(-3, 4);
		NumberFormatInfo provider = new() { NegativeSign = "−" };
		string formattedText = original.ToString(null, provider);
		bool parsed = Rational.TryParse(formattedText, provider, out Rational result);
		Rational expected = new(-3, 4);

		Assert.Multiple(
			() => Assert.True(parsed),
			() => Assert.Equal("−3/4", formattedText),
			() => Assert.Equal(expected, result));
	}
}
