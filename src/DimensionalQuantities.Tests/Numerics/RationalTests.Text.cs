using System.Globalization;
using DimensionalQuantities.Numerics;

namespace DimensionalQuantities.Tests.Numerics;

public sealed partial class RationalTests
{
	[Test]
	[Arguments("6/8", 3, 4)]
	[Arguments(" 6 / -8 ", -3, 4)]
	[Arguments("7", 7, 1)]
	[Arguments("0/15", 0, 1)]
	public async Task ParsingNormalisesFractions(string text, int numerator, int denominator)
	{
		Rational result = Rational.Parse(text, CultureInfo.InvariantCulture);
		Rational expected = new(numerator, denominator);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	[Arguments((string?)null)]
	[Arguments("")]
	[Arguments("1/0")]
	[Arguments("1/2/3")]
	[Arguments("x/2")]
	[Arguments("1/")]
	public async Task TryParseRejectsInvalidFractions(string? text)
	{
		bool success = Rational.TryParse(text, CultureInfo.InvariantCulture, out Rational result);
		Rational expected = default;

		using (Assert.Multiple())
		{
			await Assert.That(success).IsFalse();
			await Assert.That(result).IsEqualTo(expected);
		}
	}

	[Test]
	[Arguments("Seventeen", 17, 1)]
	[Arguments("TwoThirds", 2, 3)]
	[Arguments("MinusThreeOverTwo", -3, 2)]
	[Arguments("TwoOverFour", 1, 2)]
	public async Task TryParseAcceptsEnglishNames(string text, int numerator, int denominator)
	{
		bool success = Rational.TryParse(text, CultureInfo.InvariantCulture, out Rational result);
		Rational expected = new(numerator, denominator);

		using (Assert.Multiple())
		{
			await Assert.That(success).IsTrue();
			await Assert.That(result).IsEqualTo(expected);
		}
	}

	[Test]
	public async Task TryParseWithNumberStylesAcceptsEnglishNames()
	{
		bool success = Rational.TryParse("OneHundredAndTwentyThreeOverFortyFive", NumberStyles.Integer, CultureInfo.InvariantCulture, out Rational result);
		Rational expected = new(41, 15);

		using (Assert.Multiple())
		{
			await Assert.That(success).IsTrue();
			await Assert.That(result).IsEqualTo(expected);
		}
	}

	[Test]
	[Arguments("OneOverZero")]
	[Arguments("OneTwo")]
	[Arguments("One/Two")]
	public async Task TryParseRejectsInvalidEnglishNames(string text)
	{
		bool success = Rational.TryParse(text, CultureInfo.InvariantCulture, out Rational result);
		Rational expected = default;

		using (Assert.Multiple())
		{
			await Assert.That(success).IsFalse();
			await Assert.That(result).IsEqualTo(expected);
		}
	}

	[Test]
	public async Task TryParseHonoursComponentNumberStyles()
	{
		bool success = Rational.TryParse("1,000/2,000", NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out Rational result);
		Rational expected = new(1, 2);

		using (Assert.Multiple())
		{
			await Assert.That(success).IsTrue();
			await Assert.That(result).IsEqualTo(expected);
		}
	}

	[Test]
	public async Task ParseRejectsZeroDenominator()
	{
		await Assert.That(() => Rational.Parse("1/0", CultureInfo.InvariantCulture)).ThrowsExactly<DivideByZeroException>();
	}

	[Test]
	public async Task ParseHonoursComponentNumberStyles()
	{
		Rational result = Rational.Parse("1,000/2,000", NumberStyles.AllowThousands, CultureInfo.InvariantCulture);
		Rational expected = new(1, 2);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task FormattingProducesReducedFraction()
	{
		Rational original = new(-6, 8);
		string result = original.ToString(null, CultureInfo.InvariantCulture);
		string expected = "-3/4";

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task FormattingWholeNumbersOmitsDenominator()
	{
		Rational original = new(6, 2);
		string result = original.ToString(null, CultureInfo.InvariantCulture);
		string expected = "3";

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task StringFormattingAndParsingRoundTrip()
	{
		Rational original = new(-3, 4);
		string formattedText = original.ToString(null, CultureInfo.InvariantCulture);
		bool parsed = Rational.TryParse(formattedText, CultureInfo.InvariantCulture, out Rational result);
		Rational expected = new(-3, 4);

		using (Assert.Multiple())
		{
			await Assert.That(parsed).IsTrue();
			await Assert.That(formattedText).IsEqualTo("-3/4");
			await Assert.That(result).IsEqualTo(expected);
		}
	}

	[Test]
	public async Task FormattingAndParsingHonourCulture()
	{
		Rational original = new(-3, 4);
		NumberFormatInfo provider = new() { NegativeSign = "−" };
		string formattedText = original.ToString(null, provider);
		bool parsed = Rational.TryParse(formattedText, provider, out Rational result);
		Rational expected = new(-3, 4);

		using (Assert.Multiple())
		{
			await Assert.That(parsed).IsTrue();
			await Assert.That(formattedText).IsEqualTo("−3/4");
			await Assert.That(result).IsEqualTo(expected);
		}
	}
}
