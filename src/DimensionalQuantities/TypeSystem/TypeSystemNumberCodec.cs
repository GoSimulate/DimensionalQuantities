using System.Numerics;
using DimensionalQuantities.Numerics;

namespace DimensionalQuantities.TypeSystem;

/// <summary>Converts between exact rational values and type-encoded numbers for diagnostics and tests.</summary>
internal static class TypeSystemNumberCodec
{
	private static readonly Type[] _terminalDigits =
	[
		typeof(TerminalHex0), typeof(TerminalHex1), typeof(TerminalHex2), typeof(TerminalHex3),
		typeof(TerminalHex4), typeof(TerminalHex5), typeof(TerminalHex6), typeof(TerminalHex7),
		typeof(TerminalHex8), typeof(TerminalHex9), typeof(TerminalHexA), typeof(TerminalHexB),
		typeof(TerminalHexC), typeof(TerminalHexD), typeof(TerminalHexE), typeof(TerminalHexF)
	];

	public static Rational Parse<T>() where T : ITypeSystemNumber => Parse(typeof(T));

	public static Type CreateType(Rational value)
	{
		if (value.Denominator.IsOne)
		{
			return CreateIntegerType(value.Numerator);
		}

		return typeof(Fraction<,>).MakeGenericType(CreateIntegerType(value.Numerator), CreateMagnitudeType(value.Denominator));
	}

	public static Rational Parse(Type type)
	{
		ArgumentNullException.ThrowIfNull(type);

		int digit = Array.IndexOf(_terminalDigits, type);
		if (digit >= 0)
		{
			return new(digit);
		}

		if (type.IsConstructedGenericType)
		{
			Type definition = type.GetGenericTypeDefinition();
			Type[] arguments = type.GetGenericArguments();

			if (definition == typeof(Hex<,>))
			{
				return 16 * Parse(arguments[1]) + Parse(arguments[0]);
			}

			if (definition == typeof(Negative<>))
			{
				return -Parse(arguments[0]);
			}

			if (definition == typeof(Fraction<,>))
			{
				return Parse(arguments[0]) / Parse(arguments[1]);
			}
		}

		throw new ArgumentException($"Type '{type}' is not a supported type-system number encoding.", nameof(type));
	}

	private static Type CreateIntegerType(BigInteger value)
	{
		if (value.Sign < 0)
		{
			return typeof(Negative<>).MakeGenericType(CreateMagnitudeType(-value));
		}

		return CreateMagnitudeType(value);
	}

	private static Type CreateMagnitudeType(BigInteger value)
	{
		if (value < _terminalDigits.Length)
		{
			return _terminalDigits[(int)value];
		}

		BigInteger rest = BigInteger.DivRem(value, 16, out BigInteger digit);
		return typeof(Hex<,>).MakeGenericType(_terminalDigits[(int)digit], CreateMagnitudeType(rest));
	}
}
