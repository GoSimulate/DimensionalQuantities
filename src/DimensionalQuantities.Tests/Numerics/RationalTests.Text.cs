using System.Globalization;
using System.Text;
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
	[InlineData(null)]
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
	public void CharacterSpanFormattingAndParsingRoundTrip()
	{
		Rational original = new(-3, 4);
		Span<char> buffer = stackalloc char[20];
		bool formatted = original.TryFormat(buffer, out int written, default, CultureInfo.InvariantCulture);
		bool parsed = Rational.TryParse(buffer[..written], CultureInfo.InvariantCulture, out Rational result);
		string formattedText = new(buffer[..written]);
		Rational expected = new(-3, 4);

		Assert.Multiple(
			() => Assert.True(formatted),
			() => Assert.True(parsed),
			() => Assert.Equal("-3/4", formattedText),
			() => Assert.Equal(expected, result));
	}

	[Fact]
	public void Utf8FormattingAndParsingHonourCulture()
	{
		Rational original = new(-3, 4);
		NumberFormatInfo provider = new() { NegativeSign = "−" };
		Span<byte> buffer = stackalloc byte[20];
		bool formatted = original.TryFormat(buffer, out int written, default, provider);
		bool parsed = Rational.TryParse(buffer[..written], provider, out Rational result);
		string formattedText = Encoding.UTF8.GetString(buffer[..written]);
		Rational expected = new(-3, 4);

		Assert.Multiple(
			() => Assert.True(formatted),
			() => Assert.True(parsed),
			() => Assert.Equal("−3/4", formattedText),
			() => Assert.Equal(expected, result));
	}

	[Fact]
	public void FormattingReportsInsufficientDestination()
	{
		Rational original = new(3, 4);
		Span<char> characters = stackalloc char[2];
		Span<byte> bytes = stackalloc byte[2];

		bool charactersFormatted = original.TryFormat(characters, out int charsWritten, default, CultureInfo.InvariantCulture);
		bool bytesFormatted = original.TryFormat(bytes, out int bytesWritten, default, CultureInfo.InvariantCulture);

		Assert.Multiple(
			() => Assert.False(charactersFormatted),
			() => Assert.False(bytesFormatted),
			() => Assert.Equal(0, charsWritten),
			() => Assert.Equal(0, bytesWritten));
	}
}
