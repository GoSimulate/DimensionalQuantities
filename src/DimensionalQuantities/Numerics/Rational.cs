using System.Numerics;

namespace DimensionalQuantities.Numerics;

/// <summary>An exact, reduced fraction with a positive denominator. Default is zero.</summary>
internal readonly partial record struct Rational
{
	// Offset storage makes the zero-initialised struct represent 0/1.
	private readonly BigInteger _denominatorMinusOne;

	public Rational(BigInteger numerator) : this(numerator, BigInteger.One)
	{
	}

	public Rational(BigInteger numerator, BigInteger denominator)
	{
		if (denominator.IsZero)
		{
			throw new DivideByZeroException();
		}

		if (denominator.Sign < 0)
		{
			numerator = -numerator;
			denominator = -denominator;
		}

		BigInteger divisor = BigInteger.GreatestCommonDivisor(numerator, denominator);
		Numerator = numerator / divisor;
		_denominatorMinusOne = denominator / divisor - BigInteger.One;
	}

	public BigInteger Numerator { get; }
	public BigInteger Denominator => _denominatorMinusOne + BigInteger.One;

	public static Rational Zero => default;
	public static Rational One => new(1);
	public static Rational AdditiveIdentity => Zero;
	public static Rational MultiplicativeIdentity => One;
	public static int Radix => 2;

	public static implicit operator Rational(int value) => new(value);

	public static Rational operator +(Rational left, Rational right) => new(left.Numerator * right.Denominator + right.Numerator * left.Denominator, left.Denominator * right.Denominator);

	public static Rational operator -(Rational left, Rational right) => new(left.Numerator * right.Denominator - right.Numerator * left.Denominator, left.Denominator * right.Denominator);

	public static Rational operator *(Rational left, Rational right) => new(left.Numerator * right.Numerator, left.Denominator * right.Denominator);

	public static Rational operator /(Rational left, Rational right) => new(left.Numerator * right.Denominator, left.Denominator * right.Numerator);

	public static Rational operator %(Rational left, Rational right)
	{
		Rational quotient = left / right;
		BigInteger whole = quotient.Numerator / quotient.Denominator;
		return left - new Rational(whole) * right;
	}

	public static Rational operator +(Rational value) => value;
	public static Rational operator -(Rational value) => new(-value.Numerator, value.Denominator);
	public static Rational operator ++(Rational value) => value + One;
	public static Rational operator --(Rational value) => value - One;
	public static bool operator <(Rational left, Rational right) => left.CompareTo(right) < 0;
	public static bool operator >(Rational left, Rational right) => left.CompareTo(right) > 0;
	public static bool operator <=(Rational left, Rational right) => left.CompareTo(right) <= 0;
	public static bool operator >=(Rational left, Rational right) => left.CompareTo(right) >= 0;

	public int CompareTo(Rational other) => (Numerator * other.Denominator).CompareTo(other.Numerator * Denominator);

	public int CompareTo(object? obj) => obj switch
	{
		null => 1,
		Rational other => CompareTo(other),
		_ => throw new ArgumentException("Value must be a Rational.", nameof(obj))
	};

	public static Rational Abs(Rational value) => new(BigInteger.Abs(value.Numerator), value.Denominator);
	public static bool IsCanonical(Rational value) => true;
	public static bool IsComplexNumber(Rational value) => false;
	public static bool IsEvenInteger(Rational value) => IsInteger(value) && value.Numerator.IsEven;
	public static bool IsFinite(Rational value) => true;
	public static bool IsImaginaryNumber(Rational value) => false;
	public static bool IsInfinity(Rational value) => false;
	public static bool IsInteger(Rational value) => value.Denominator.IsOne;
	public static bool IsNaN(Rational value) => false;
	public static bool IsNegative(Rational value) => value.Numerator.Sign < 0;
	public static bool IsNegativeInfinity(Rational value) => false;
	public static bool IsNormal(Rational value) => !IsZero(value);
	public static bool IsOddInteger(Rational value) => IsInteger(value) && !value.Numerator.IsEven;
	public static bool IsPositive(Rational value) => value.Numerator.Sign >= 0;
	public static bool IsPositiveInfinity(Rational value) => false;
	public static bool IsRealNumber(Rational value) => true;
	public static bool IsSubnormal(Rational value) => false;
	public static bool IsZero(Rational value) => value.Numerator.IsZero;

	public static Rational MaxMagnitude(Rational x, Rational y)
	{
		int comparison = Abs(x).CompareTo(Abs(y));
		return comparison > 0 || (comparison == 0 && x >= y) ? x : y;
	}

	public static Rational MinMagnitude(Rational x, Rational y)
	{
		int comparison = Abs(x).CompareTo(Abs(y));
		return comparison < 0 || (comparison == 0 && x <= y) ? x : y;
	}

	public static Rational MaxMagnitudeNumber(Rational x, Rational y) => MaxMagnitude(x, y);
	public static Rational MinMagnitudeNumber(Rational x, Rational y) => MinMagnitude(x, y);
}
