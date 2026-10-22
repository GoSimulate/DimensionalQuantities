using System.Numerics;
using DimensionalQuantities.Numerics;
using Xunit;

namespace DimensionalQuantities.Tests.Numerics;

public sealed partial class RationalTests
{
	[Fact]
	public void DecimalCastPreservesExactBaseTenValue()
	{
		decimal original = 0.1m;
		Rational result = (Rational)original;

		Assert.Multiple(
			() => Assert.Equal(new Rational(1, 10), result),
			() => Assert.Equal(original, (decimal)result));
	}

	[Fact]
	public void DoubleCastPreservesExactBinaryValue()
	{
		double original = 0.1d;
		Rational result = (Rational)original;

		Assert.Multiple(
			() => Assert.Equal(new Rational(3602879701896397, 36028797018963968), result),
			() => Assert.Equal(original, (double)result));
	}

	[Theory]
	[InlineData(double.NaN)]
	[InlineData(double.PositiveInfinity)]
	[InlineData(double.NegativeInfinity)]
	public void NonFiniteNumbersHaveNoRationalRepresentation(double value) =>
		Assert.Throws<OverflowException>(() => (Rational)value);

	[Fact]
	public void IntegerCastsTruncateTowardsZeroAndCheckRange()
	{
		Rational fraction = new(-7, 3);
		Rational tooLarge = new(601, 2);

		Assert.Multiple(
			() => Assert.Equal(-2, (int)fraction),
			() => Assert.Equal((byte)42, (byte)new Rational(85, 2)),
			() => Assert.Throws<OverflowException>(() => (byte)tooLarge));
	}

	[Fact]
	public void IntegerCastsCoverSignedAndUnsignedRanges()
	{
		Rational value = new(42);
		Rational negative = new(-1);

		Assert.Multiple(
			() => Assert.Equal((sbyte)42, (sbyte)value),
			() => Assert.Equal((short)42, (short)value),
			() => Assert.Equal((ushort)42, (ushort)value),
			() => Assert.Equal(42u, (uint)value),
			() => Assert.Equal(42L, (long)value),
			() => Assert.Equal(42UL, (ulong)value),
			() => Assert.Throws<OverflowException>(() => (uint)negative),
			() => Assert.Throws<OverflowException>(() => (sbyte)new Rational(128)));
	}

	[Fact]
	public void BigIntegerRoundTripPreservesWholeValue()
	{
		BigInteger original = BigInteger.One << 128;
		Rational converted = original;
		BigInteger result = (BigInteger)converted;

		Assert.Equal(original, result);
	}

	[Fact]
	public void PrimitiveIntegerInputsPreserveValues()
	{
		Rational expected = new(42);

		Assert.Multiple(
			() => Assert.Equal(expected, (Rational)(byte)42),
			() => Assert.Equal(expected, (Rational)(sbyte)42),
			() => Assert.Equal(expected, (Rational)(short)42),
			() => Assert.Equal(expected, (Rational)(ushort)42),
			() => Assert.Equal(expected, (Rational)42),
			() => Assert.Equal(expected, (Rational)42u),
			() => Assert.Equal(expected, (Rational)42L),
			() => Assert.Equal(expected, (Rational)42UL));
	}

	[Theory]
	[InlineData(0.0)]
	[InlineData(-0.125)]
	[InlineData(double.Epsilon)]
	[InlineData(double.MaxValue)]
	[InlineData(double.MinValue)]
	public void DoubleRoundTripPreservesRepresentableValues(double original)
	{
		Rational converted = (Rational)original;
		Assert.Equal(original, (double)converted);
	}

	[Theory]
	[InlineData(float.Epsilon)]
	[InlineData(float.MaxValue)]
	[InlineData(-0.1f)]
	public void SingleRoundTripPreservesRepresentableValues(float original)
	{
		Rational converted = (Rational)original;
		Assert.Equal(original, (float)converted);
	}

	[Fact]
	public void BinaryConversionRoundsMidpointToEven()
	{
		Rational original = new((BigInteger.One << 53) + 1, BigInteger.One << 53);
		Assert.Equal(1.0, (double)original);
	}

	[Fact]
	public void BinaryConversionDividesBeforeLosingLargeComponents()
	{
		BigInteger scale = BigInteger.One << 2000;
		Rational original = new(scale + 1, 2 * scale + 1);
		Assert.Equal(0.5, (double)original);
	}

	[Fact]
	public void BinaryConversionRoundsAtSubnormalBoundary()
	{
		Rational halfwayToMinimum = new(1, BigInteger.One << 1075);
		Rational halfwayToSecond = new(3, BigInteger.One << 1075);

		Assert.Multiple(
			() => Assert.Equal(0.0, (double)halfwayToMinimum),
			() => Assert.Equal(2 * double.Epsilon, (double)halfwayToSecond));
	}

	[Fact]
	public void DecimalConversionRoundsRepeatingFractions()
	{
		Rational original = new(1, 3);
		Assert.Equal(1m / 3m, (decimal)original);
	}

	[Fact]
	public void DecimalCastRejectsOutOfRangeValue()
	{
		Rational original = new(new BigInteger(decimal.MaxValue) + 1);
		Assert.Throws<OverflowException>(() => (decimal)original);
	}

	[Theory]
	[InlineData(1.5)]
	[InlineData(-0.125)]
	[InlineData(0.0)]
	public void RealComplexInputsPreserveFractions(double real)
	{
		Complex original = new(real, 0);
		Rational converted = (Rational)original;
		Assert.Multiple(
			() => Assert.Equal((Rational)real, converted),
			() => Assert.Equal(original, (Complex)converted));
	}

	[Theory]
	[InlineData(1.0)]
	[InlineData(-1.0)]
	public void NonRealComplexInputsAreRejected(double imaginary)
	{
		Complex original = new(1.5, imaginary);
		Assert.Throws<RationalNumberCantHaveComplexComponentsException>(() => (Rational)original);
	}
}
