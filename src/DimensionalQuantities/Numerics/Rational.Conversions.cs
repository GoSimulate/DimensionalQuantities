using System.Numerics;

namespace DimensionalQuantities.Numerics;

internal readonly partial record struct Rational
{
	public static Rational CreateChecked<TOther>(TOther value) where TOther : INumberBase<TOther> =>
		TryConvertFrom(value, out Rational result) || TOther.TryConvertToChecked(value, out result) ? result : throw new NotSupportedException($"Cannot convert {typeof(TOther)} to Rational.");

	public static Rational CreateSaturating<TOther>(TOther value) where TOther : INumberBase<TOther> =>
		TryConvertFrom(value, out Rational result) || TOther.TryConvertToSaturating(value, out result) ? result : throw new NotSupportedException($"Cannot convert {typeof(TOther)} to Rational.");

	public static Rational CreateTruncating<TOther>(TOther value) where TOther : INumberBase<TOther> =>
		TryConvertFrom(value, out Rational result) || TOther.TryConvertToTruncating(value, out result) ? result : throw new NotSupportedException($"Cannot convert {typeof(TOther)} to Rational.");

	// All finite built-in inputs are represented exactly, so incoming conversion
	// modes coincide. Infinity and NaN have no rational representation.
	static bool INumberBase<Rational>.TryConvertFromChecked<TOther>(TOther value, out Rational result) => TryConvertFrom(value, out result);
	static bool INumberBase<Rational>.TryConvertFromSaturating<TOther>(TOther value, out Rational result) => TryConvertFrom(value, out result);
	static bool INumberBase<Rational>.TryConvertFromTruncating<TOther>(TOther value, out Rational result) => TryConvertFrom(value, out result);

	private static bool TryConvertFrom<TOther>(TOther value, out Rational result) where TOther : INumberBase<TOther>
	{
		switch (value)
		{
			case Rational rational:
				result = rational;
				return true;
			case Complex number:
				if (number.Imaginary != 0)
				{
					throw new RationalNumberCantHaveComplexComponentsException();
				}

				result = FromDouble(number.Real);
				return true;
			case decimal number:
				result = FromDecimal(number);
				return true;
			case double number:
				result = FromDouble(number);
				return true;
			case float number:
				result = FromDouble(number);
				return true;
			case Half number:
				result = FromDouble((double)number);
				return true;
		}

		if (IsBuiltInInteger<TOther>())
		{
			result = new(BigInteger.CreateChecked(value));
			return true;
		}

		result = Zero;
		return false;
	}

	private static bool IsBuiltInInteger<T>() =>
		typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte) ||
		typeof(T) == typeof(short) || typeof(T) == typeof(ushort) ||
		typeof(T) == typeof(int) || typeof(T) == typeof(uint) ||
		typeof(T) == typeof(long) || typeof(T) == typeof(ulong) ||
		typeof(T) == typeof(nint) || typeof(T) == typeof(nuint) ||
		typeof(T) == typeof(Int128) || typeof(T) == typeof(UInt128) ||
		typeof(T) == typeof(char) || typeof(T) == typeof(BigInteger);

	private static Rational FromDecimal(decimal value)
	{
		int[] bits = decimal.GetBits(value);
		BigInteger numerator = (uint)bits[0] | ((BigInteger)(uint)bits[1] << 32) | ((BigInteger)(uint)bits[2] << 64);
		if (bits[3] < 0)
		{
			numerator = -numerator;
		}

		int scale = (bits[3] >> 16) & 0xff;
		return new(numerator, BigInteger.Pow(10, scale));
	}

	private static Rational FromDouble(double value)
	{
		if (!double.IsFinite(value))
		{
			throw new OverflowException("NaN and infinity cannot be represented as a Rational.");
		}

		ulong bits = BitConverter.DoubleToUInt64Bits(value);
		int exponentBits = (int)((bits >> 52) & 0x7ff);
		BigInteger significand = bits & 0x000f_ffff_ffff_ffff;
		int exponent = -1074;
		if (exponentBits != 0)
		{
			significand += BigInteger.One << 52;
			exponent = exponentBits - 1023 - 52;
		}
		if ((bits >> 63) != 0)
		{
			significand = -significand;
		}

		return exponent >= 0 ? new(significand << exponent) : new(significand, BigInteger.One << -exponent);
	}

	static bool INumberBase<Rational>.TryConvertToChecked<TOther>(Rational value, out TOther result) => TryConvertTo(value, ConversionMode.Checked, out result);
	static bool INumberBase<Rational>.TryConvertToSaturating<TOther>(Rational value, out TOther result) => TryConvertTo(value, ConversionMode.Saturating, out result);
	static bool INumberBase<Rational>.TryConvertToTruncating<TOther>(Rational value, out TOther result) => TryConvertTo(value, ConversionMode.Truncating, out result);

	private enum ConversionMode { Checked, Saturating, Truncating }

	private static bool TryConvertTo<TOther>(Rational value, ConversionMode mode, out TOther result) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(Rational))
		{
			result = (TOther)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(Complex))
		{
			result = (TOther)(object)new Complex(value.ToBinaryFloat(53, -1022, 1023), 0);
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			result = (TOther)(object)value.ToBinaryFloat(53, -1022, 1023);
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			result = (TOther)(object)(float)value.ToBinaryFloat(24, -126, 127);
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			result = (TOther)(object)(Half)value.ToBinaryFloat(11, -14, 15);
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			result = (TOther)(object)value.ToDecimal(mode);
			return true;
		}

		if (!IsBuiltInInteger<TOther>())
		{
			result = TOther.Zero;
			return false;
		}

		// Integral targets discard the fractional part towards zero; the target
		// type owns overflow, saturation and truncation of the resulting integer.
		BigInteger whole = value.Numerator / value.Denominator;
		result = mode switch
		{
			ConversionMode.Checked => TOther.CreateChecked(whole),
			ConversionMode.Saturating => TOther.CreateSaturating(whole),
			_ => TOther.CreateTruncating(whole)
		};
		return true;
	}

	// Round the fraction directly to the target precision. Converting numerator
	// and denominator separately would lose accuracy or produce infinity/infinity.
	private double ToBinaryFloat(int precision, int minimumExponent, int maximumExponent)
	{
		if (IsZero(this))
		{
			return 0;
		}

		BigInteger numerator = BigInteger.Abs(Numerator);
		BigInteger denominator = Denominator;
		long exponent = numerator.GetBitLength() - denominator.GetBitLength();
		if (exponent > maximumExponent + 1L)
		{
			return Numerator.Sign * double.PositiveInfinity;
		}

		if (exponent < minimumExponent - precision)
		{
			return Numerator.Sign < 0 ? -0.0 : 0.0;
		}

		bool belowPowerOfTwo = exponent >= 0 ? numerator < (denominator << (int)exponent) : (numerator << (int)-exponent) < denominator;
		if (belowPowerOfTwo)
		{
			exponent--;
		}

		int shift = (int)Math.Max(exponent - precision + 1, minimumExponent - precision + 1);
		BigInteger rounded = shift >= 0 ? RoundQuotient(numerator, denominator << shift) : RoundQuotient(numerator << -shift, denominator);

		if (rounded.GetBitLength() + shift - 1 > maximumExponent)
		{
			return Numerator.Sign * double.PositiveInfinity;
		}

		return Numerator.Sign * Math.ScaleB((double)rounded, shift);
	}

	private decimal ToDecimal(ConversionMode mode)
	{
		BigInteger numerator = BigInteger.Abs(Numerator);
		BigInteger maximum = new(decimal.MaxValue);
		if (numerator > maximum * Denominator)
		{
			if (mode == ConversionMode.Checked)
			{
				throw new OverflowException("Rational exceeds the decimal range.");
			}

			return Numerator.Sign < 0 ? decimal.MinValue : decimal.MaxValue;
		}

		for (int scale = 28; scale >= 0; scale--)
		{
			BigInteger rounded = RoundQuotient(numerator * BigInteger.Pow(10, scale), Denominator);
			if (rounded > maximum)
			{
				continue;
			}

			uint low = (uint)(rounded & uint.MaxValue);
			uint middle = (uint)((rounded >> 32) & uint.MaxValue);
			uint high = (uint)(rounded >> 64);
			return new(unchecked((int)low), unchecked((int)middle), unchecked((int)high), Numerator.Sign < 0, (byte)scale);
		}

		throw new OverflowException("Rational exceeds the decimal range.");
	}

	private static BigInteger RoundQuotient(BigInteger numerator, BigInteger denominator)
	{
		BigInteger quotient = BigInteger.DivRem(numerator, denominator, out BigInteger remainder);
		int comparison = (remainder * 2).CompareTo(denominator);
		return comparison > 0 || (comparison == 0 && !quotient.IsEven) ? quotient + 1 : quotient;
	}
}
