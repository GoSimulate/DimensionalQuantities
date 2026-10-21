using System.Numerics;
using DimensionalQuantities.Numerics;
using Xunit;

namespace DimensionalQuantities.Tests.Numerics;

public sealed partial class RationalTests
{
	[Fact]
	public void DecimalConversionPreservesExactBaseTenValue()
	{
		decimal original = 0.1m;
		Rational result = Rational.CreateChecked(original);
		Rational expected = new(1, 10);

		Assert.Multiple(
			() => Assert.Equal(expected, result),
			() => Assert.Equal(original, decimal.CreateChecked(result)));
	}

	[Fact]
	public void DoubleConversionPreservesExactBinaryValue()
	{
		double original = 0.1d;
		Rational result = Rational.CreateChecked(original);
		Rational expected = new(3602879701896397, 36028797018963968);

		Assert.Multiple(
			() => Assert.Equal(expected, result),
			() => Assert.Equal(original, double.CreateChecked(result)));
	}

	[Fact]
	public void GenericInputModesPreserveFiniteFractions()
	{
		Rational checkedResult = Rational.CreateChecked(0.125m);
		Rational saturatingResult = Rational.CreateSaturating(0.125m);
		Rational truncatingResult = Rational.CreateTruncating(0.125m);
		Rational expected = new(1, 8);

		Assert.Multiple(
			() => Assert.Equal(expected, checkedResult),
			() => Assert.Equal(expected, saturatingResult),
			() => Assert.Equal(expected, truncatingResult));
	}

	[Theory]
	[InlineData(double.NaN)]
	[InlineData(double.PositiveInfinity)]
	[InlineData(double.NegativeInfinity)]
	public void NonFiniteNumbersHaveNoRationalRepresentation(double value)
	{
		Assert.Multiple(
			() => Assert.Throws<OverflowException>(() => Rational.CreateChecked(value)),
			() => Assert.Throws<OverflowException>(() => Rational.CreateSaturating(value)),
			() => Assert.Throws<OverflowException>(() => Rational.CreateTruncating(value)));
	}

	[Fact]
	public void IntegralConversionDiscardsFractionTowardsZero()
	{
		Rational original = new(-7, 3);
		int result = int.CreateChecked(original);
		int expected = -2;

		Assert.Equal(expected, result);
	}

	[Fact]
	public void IntegralOutputModesUseTargetConversionRules()
	{
		Rational original = new(601, 2);
		byte saturatingResult = byte.CreateSaturating(original);
		byte truncatingResult = byte.CreateTruncating(original);

		Assert.Multiple(
			() => Assert.Equal(byte.MaxValue, saturatingResult),
			() => Assert.Equal((byte)44, truncatingResult),
			() => Assert.Throws<OverflowException>(() => byte.CreateChecked(original)));
	}

	[Fact]
	public void BigIntegerRoundTripPreservesWholeValue()
	{
		BigInteger original = BigInteger.One << 128;
		Rational converted = Rational.CreateChecked(original);
		BigInteger result = BigInteger.CreateChecked(converted);
		BigInteger expected = original;

		Assert.Equal(expected, result);
	}

	[Fact]
	public void PrimitiveIntegerInputsPreserveValues()
	{
		Rational expected = new(42);

		Assert.Multiple(
			() => Assert.Equal(expected, Rational.CreateChecked((byte)42)),
			() => Assert.Equal(expected, Rational.CreateChecked((sbyte)42)),
			() => Assert.Equal(expected, Rational.CreateChecked((short)42)),
			() => Assert.Equal(expected, Rational.CreateChecked((ushort)42)),
			() => Assert.Equal(expected, Rational.CreateChecked(42)),
			() => Assert.Equal(expected, Rational.CreateChecked(42u)),
			() => Assert.Equal(expected, Rational.CreateChecked(42L)),
			() => Assert.Equal(expected, Rational.CreateChecked(42UL)),
			() => Assert.Equal(expected, Rational.CreateChecked((nint)42)),
			() => Assert.Equal(expected, Rational.CreateChecked((nuint)42)),
			() => Assert.Equal(expected, Rational.CreateChecked((Int128)42)),
			() => Assert.Equal(expected, Rational.CreateChecked((UInt128)42)));
	}

	[Theory]
	[InlineData(0.0)]
	[InlineData(-0.125)]
	[InlineData(double.Epsilon)]
	[InlineData(double.MaxValue)]
	[InlineData(double.MinValue)]
	public void DoubleRoundTripPreservesRepresentableValues(double original)
	{
		Rational converted = Rational.CreateChecked(original);
		double result = double.CreateChecked(converted);
		double expected = original;

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData(float.Epsilon)]
	[InlineData(float.MaxValue)]
	[InlineData(-0.1f)]
	public void SingleRoundTripPreservesRepresentableValues(float original)
	{
		Rational converted = Rational.CreateChecked(original);
		float result = float.CreateChecked(converted);
		float expected = original;

		Assert.Equal(expected, result);
	}

	[Fact]
	public void HalfRoundTripPreservesRepresentableValues()
	{
		Half original = Half.Epsilon;
		Rational converted = Rational.CreateChecked(original);
		Half result = Half.CreateChecked(converted);
		Half expected = original;

		Assert.Equal(expected, result);
	}

	[Fact]
	public void BinaryConversionRoundsMidpointToEven()
	{
		Rational original = new((BigInteger.One << 53) + 1, BigInteger.One << 53);
		double result = double.CreateChecked(original);
		double expected = 1.0;

		Assert.Equal(expected, result);
	}

	[Fact]
	public void BinaryConversionDividesBeforeLosingLargeComponents()
	{
		BigInteger scale = BigInteger.One << 2000;
		Rational original = new(scale + 1, 2 * scale + 1);
		double result = double.CreateChecked(original);
		double expected = 0.5;

		Assert.Equal(expected, result);
	}

	[Fact]
	public void DecimalConversionRoundsRepeatingFractions()
	{
		Rational original = new(1, 3);
		decimal result = decimal.CreateChecked(original);
		decimal expected = 1m / 3m;

		Assert.Equal(expected, result);
	}

	[Fact]
	public void DecimalOutputModesRespectTargetRange()
	{
		Rational original = new(new BigInteger(decimal.MaxValue) + 1);
		decimal saturatingResult = decimal.CreateSaturating(original);
		decimal truncatingResult = decimal.CreateTruncating(original);
		decimal expected = decimal.MaxValue;

		Assert.Multiple(
			() => Assert.Equal(expected, saturatingResult),
			() => Assert.Equal(expected, truncatingResult),
			() => Assert.Throws<OverflowException>(() => decimal.CreateChecked(original)));
	}
	[Theory]
	[InlineData(1.5)]
	[InlineData(-0.125)]
	[InlineData(0.0)]
	public void RealComplexInputsPreserveFractions(double real)
	{
		Complex original = new(real, 0);
		Rational checkedResult = Rational.CreateChecked(original);
		Rational saturatingResult = Rational.CreateSaturating(original);
		Rational truncatingResult = Rational.CreateTruncating(original);
		Rational expected = Rational.CreateChecked(real);

		Assert.Multiple(
			() => Assert.Equal(expected, checkedResult),
			() => Assert.Equal(expected, saturatingResult),
			() => Assert.Equal(expected, truncatingResult));
	}

	[Fact]
	public void ComplexOutputsPreserveFractionalRealPart()
	{
		Rational original = new(3, 2);
		Complex checkedResult = Complex.CreateChecked(original);
		Complex saturatingResult = Complex.CreateSaturating(original);
		Complex truncatingResult = Complex.CreateTruncating(original);
		Complex expected = new(1.5, 0);

		Assert.Multiple(
			() => Assert.Equal(expected, checkedResult),
			() => Assert.Equal(expected, saturatingResult),
			() => Assert.Equal(expected, truncatingResult));
	}

	[Theory]
	[InlineData(1.0)]
	[InlineData(-1.0)]
	public void NonRealComplexInputsAreRejected(double imaginary)
	{
		Complex original = new(1.5, imaginary);

		Assert.Multiple(
			() => Assert.Throws<RationalNumberCantHaveComplexComponentsException>(() => Rational.CreateChecked(original)),
			() => Assert.Throws<RationalNumberCantHaveComplexComponentsException>(() => Rational.CreateSaturating(original)),
			() => Assert.Throws<RationalNumberCantHaveComplexComponentsException>(() => Rational.CreateTruncating(original)));
	}
}
