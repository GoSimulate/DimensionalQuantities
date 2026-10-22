using System.Numerics;

namespace DimensionalQuantities.Numerics;

internal readonly partial record struct Rational
{
	public static implicit operator Rational(byte value) => new(value);
	public static implicit operator Rational(sbyte value) => new(value);
	public static implicit operator Rational(short value) => new(value);
	public static implicit operator Rational(ushort value) => new(value);
	public static implicit operator Rational(uint value) => new(value);
	public static implicit operator Rational(long value) => new(value);
	public static implicit operator Rational(ulong value) => new(value);
	public static implicit operator Rational(BigInteger value) => new(value);

	public static explicit operator Rational(float value) => FromDouble(value);
	public static explicit operator Rational(double value) => FromDouble(value);
	public static explicit operator Rational(decimal value) => FromDecimal(value);
	public static explicit operator Rational(Complex value)
	{
		if (value.Imaginary != 0)
		{
			throw new RationalNumberCantHaveComplexComponentsException();
		}

		return FromDouble(value.Real);
	}

	public static explicit operator byte(Rational value) => checked((byte)value.WholePart);
	public static explicit operator sbyte(Rational value) => checked((sbyte)value.WholePart);
	public static explicit operator short(Rational value) => checked((short)value.WholePart);
	public static explicit operator ushort(Rational value) => checked((ushort)value.WholePart);
	public static explicit operator int(Rational value) => checked((int)value.WholePart);
	public static explicit operator uint(Rational value) => checked((uint)value.WholePart);
	public static explicit operator long(Rational value) => checked((long)value.WholePart);
	public static explicit operator ulong(Rational value) => checked((ulong)value.WholePart);
	public static explicit operator BigInteger(Rational value) => value.WholePart;
	public static explicit operator double(Rational value) => value.ToBinaryFloat(53, -1022, 1023);
	public static explicit operator float(Rational value) => (float)value.ToBinaryFloat(24, -126, 127);
	public static explicit operator decimal(Rational value) => value.ToDecimal();
	public static explicit operator Complex(Rational value) => new((double)value, 0);

	private BigInteger WholePart => Numerator / Denominator;

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
		if (double.IsNaN(value) || double.IsInfinity(value))
		{
			throw new OverflowException("NaN and infinity cannot be represented as a Rational.");
		}

		ulong bits = unchecked((ulong)BitConverter.DoubleToInt64Bits(value));
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
		long exponent = GetBitLength(numerator) - GetBitLength(denominator);
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

		if (GetBitLength(rounded) + shift - 1 > maximumExponent)
		{
			return Numerator.Sign * double.PositiveInfinity;
		}

		return Numerator.Sign * ScaleRoundedBinary(rounded, shift);
	}

	private static long GetBitLength(BigInteger value)
	{
		byte[] bytes = value.ToByteArray();
		int top = bytes.Length - 1;
		while (top > 0 && bytes[top] == 0)
		{
			top--;
		}

		byte mostSignificant = bytes[top];
		int bits = 0;
		while (mostSignificant != 0)
		{
			bits++;
			mostSignificant >>= 1;
		}

		return 8L * top + bits;
	}

	private static double ScaleRoundedBinary(BigInteger rounded, int shift)
	{
		long bitLength = GetBitLength(rounded);
		long exponent = bitLength - 1 + shift;
		ulong bits;
		if (exponent >= -1022)
		{
			BigInteger significand = bitLength <= 53 ? rounded << (int)(53 - bitLength) : rounded >> (int)(bitLength - 53);
			bits = ((ulong)(exponent + 1023) << 52) | ((ulong)significand & 0x000f_ffff_ffff_ffff);
		}
		else
		{
			bits = (ulong)(rounded << (shift + 1074));
		}

		return BitConverter.Int64BitsToDouble(unchecked((long)bits));
	}

	private decimal ToDecimal()
	{
		BigInteger numerator = BigInteger.Abs(Numerator);
		BigInteger maximum = new(decimal.MaxValue);
		if (numerator > maximum * Denominator)
		{
			throw new OverflowException("Rational exceeds the decimal range.");
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
