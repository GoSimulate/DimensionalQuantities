using System.Globalization;
using System.Numerics;

namespace DimensionalQuantities.Numerics;

internal readonly partial record struct Rational
{
	// Formatting applies the requested integer format to each component.
	// Parsing accepts numeric integers and fractions; TryParse also accepts English names.
	public override string ToString() => ToString(null, CultureInfo.CurrentCulture);

	public string ToString(string? format, IFormatProvider? formatProvider)
	{
		string numerator = Numerator.ToString(format, formatProvider);
		return Denominator.IsOne ? numerator : numerator + "/" + Denominator.ToString(format, formatProvider);
	}

	public static Rational Parse(string s, IFormatProvider? provider) => Parse(s, NumberStyles.Integer, provider);

	public static Rational Parse(string s, NumberStyles style, IFormatProvider? provider)
	{
		if (s is null)
		{
			throw new ArgumentNullException(nameof(s));
		}

		int separator = s.IndexOf('/');
		return separator < 0 ? new(BigInteger.Parse(s, style, provider)) : new(BigInteger.Parse(s.Substring(0, separator), style, provider), BigInteger.Parse(s.Substring(separator + 1), style, provider));
	}

	public static bool TryParse(string? s, IFormatProvider? provider, out Rational result) => TryParse(s, NumberStyles.Integer, provider, out result);

	public static bool TryParse(string? s, NumberStyles style, IFormatProvider? provider, out Rational result)
	{
		result = Zero;
		if (s is null)
		{
			return false;
		}

		int separator = s.IndexOf('/');
		string numeratorText = separator < 0 ? s : s.Substring(0, separator);
		if (!BigInteger.TryParse(numeratorText, style, provider, out BigInteger numerator))
		{
			return EnglishRationalParser.TryParse(s, out result);
		}

		BigInteger denominator = BigInteger.One;
		if (separator >= 0 && (!BigInteger.TryParse(s.Substring(separator + 1), style, provider, out denominator) || denominator.IsZero))
		{
			return false;
		}

		result = new(numerator, denominator);
		return true;
	}
}
