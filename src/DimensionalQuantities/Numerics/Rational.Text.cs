using System.Globalization;
using System.Numerics;
using System.Text;

namespace DimensionalQuantities.Numerics;

internal readonly partial record struct Rational
{
	// Formatting applies the requested integer format to each component.
	// Parsing accepts either one integer or two integers separated by '/'.
	public override string ToString() => ToString(null, CultureInfo.CurrentCulture);

	public string ToString(string? format, IFormatProvider? formatProvider)
	{
		string numerator = Numerator.ToString(format, formatProvider);
		return Denominator.IsOne ? numerator : numerator + "/" + Denominator.ToString(format, formatProvider);
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
	{
		string text = ToString(format.IsEmpty ? null : new(format), provider);
		if (!text.AsSpan().TryCopyTo(destination))
		{
			charsWritten = 0;
			return false;
		}

		charsWritten = text.Length;
		return true;
	}

	public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
	{
		string text = ToString(format.IsEmpty ? null : new(format), provider);
		if (Encoding.UTF8.GetByteCount(text) > utf8Destination.Length)
		{
			bytesWritten = 0;
			return false;
		}

		bytesWritten = Encoding.UTF8.GetBytes(text, utf8Destination);
		return true;
	}

	public static Rational Parse(string s, IFormatProvider? provider) => Parse(s, NumberStyles.Integer, provider);
	public static Rational Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Integer, provider);
	public static Rational Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) => Parse(Encoding.UTF8.GetString(utf8Text), provider);

	public static Rational Parse(string s, NumberStyles style, IFormatProvider? provider)
	{
		ArgumentNullException.ThrowIfNull(s);
		return Parse(s.AsSpan(), style, provider);
	}

	public static Rational Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider)
	{
		int separator = s.IndexOf('/');
		return separator < 0 ? new(BigInteger.Parse(s, style, provider)) : new(BigInteger.Parse(s[..separator], style, provider), BigInteger.Parse(s[(separator + 1)..], style, provider));
	}

	public static bool TryParse(string? s, IFormatProvider? provider, out Rational result) => TryParse(s.AsSpan(), NumberStyles.Integer, provider, out result);

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Rational result) => TryParse(s, NumberStyles.Integer, provider, out result);

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out Rational result) => TryParse(Encoding.UTF8.GetString(utf8Text), provider, out result);

	public static bool TryParse(string? s, NumberStyles style, IFormatProvider? provider, out Rational result) => TryParse(s.AsSpan(), style, provider, out result);

	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out Rational result)
	{
		result = Zero;
		int separator = s.IndexOf('/');
		ReadOnlySpan<char> numeratorText = separator < 0 ? s : s[..separator];
		if (!BigInteger.TryParse(numeratorText, style, provider, out BigInteger numerator))
		{
			return false;
		}

		BigInteger denominator = BigInteger.One;
		if (separator >= 0 && (!BigInteger.TryParse(s[(separator + 1)..], style, provider, out denominator) || denominator.IsZero))
		{
			return false;
		}

		result = new(numerator, denominator);
		return true;
	}
}
