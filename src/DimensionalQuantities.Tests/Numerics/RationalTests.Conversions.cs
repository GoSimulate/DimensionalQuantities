using System.Numerics;
using DimensionalQuantities.Numerics;

namespace DimensionalQuantities.Tests.Numerics;

public sealed partial class RationalTests
{
	[Test]
	public async Task DecimalCastPreservesExactBaseTenValue()
	{
		decimal original = 0.1m;
		Rational result = (Rational)original;

		using (Assert.Multiple())
		{
			await Assert.That(result).IsEqualTo(new Rational(1, 10));
			await Assert.That((decimal)result).IsEqualTo(original);
		}
	}

	[Test]
	public async Task DoubleCastPreservesExactBinaryValue()
	{
		double original = 0.1d;
		Rational result = (Rational)original;

		using (Assert.Multiple())
		{
			await Assert.That(result).IsEqualTo(new Rational(3602879701896397, 36028797018963968));
			await Assert.That((double)result).IsEqualTo(original);
		}
	}

	[Test]
	[Arguments(double.NaN)]
	[Arguments(double.PositiveInfinity)]
	[Arguments(double.NegativeInfinity)]
	public async Task NonFiniteNumbersHaveNoRationalRepresentation(double value) =>
		await Assert.That(() => (Rational)value).ThrowsExactly<OverflowException>();

	[Test]
	public async Task IntegerCastsTruncateTowardsZeroAndCheckRange()
	{
		Rational fraction = new(-7, 3);
		Rational tooLarge = new(601, 2);

		using (Assert.Multiple())
		{
			await Assert.That((int)fraction).IsEqualTo(-2);
			await Assert.That((byte)new Rational(85, 2)).IsEqualTo((byte)42);
			await Assert.That(() => (byte)tooLarge).ThrowsExactly<OverflowException>();
		}
	}

	[Test]
	public async Task IntegerCastsCoverSignedAndUnsignedRanges()
	{
		Rational value = new(42);
		Rational negative = new(-1);

		using (Assert.Multiple())
		{
			await Assert.That((sbyte)value).IsEqualTo((sbyte)42);
			await Assert.That((short)value).IsEqualTo((short)42);
			await Assert.That((ushort)value).IsEqualTo((ushort)42);
			await Assert.That((uint)value).IsEqualTo(42u);
			await Assert.That((long)value).IsEqualTo(42L);
			await Assert.That((ulong)value).IsEqualTo(42UL);
			await Assert.That(() => (uint)negative).ThrowsExactly<OverflowException>();
			await Assert.That(() => (sbyte)new Rational(128)).ThrowsExactly<OverflowException>();
		}
	}

	[Test]
	public async Task BigIntegerRoundTripPreservesWholeValue()
	{
		BigInteger original = BigInteger.One << 128;
		Rational converted = original;
		BigInteger result = (BigInteger)converted;

		await Assert.That(result).IsEqualTo(original);
	}

	[Test]
	public async Task PrimitiveIntegerInputsPreserveValues()
	{
		Rational expected = new(42);

		using (Assert.Multiple())
		{
			await Assert.That((Rational)(byte)42).IsEqualTo(expected);
			await Assert.That((Rational)(sbyte)42).IsEqualTo(expected);
			await Assert.That((Rational)(short)42).IsEqualTo(expected);
			await Assert.That((Rational)(ushort)42).IsEqualTo(expected);
			await Assert.That((Rational)42).IsEqualTo(expected);
			await Assert.That((Rational)42u).IsEqualTo(expected);
			await Assert.That((Rational)42L).IsEqualTo(expected);
			await Assert.That((Rational)42UL).IsEqualTo(expected);
		}
	}

	[Test]
	[Arguments(0.0)]
	[Arguments(-0.125)]
	[Arguments(double.Epsilon)]
	[Arguments(double.MaxValue)]
	[Arguments(double.MinValue)]
	public async Task DoubleRoundTripPreservesRepresentableValues(double original)
	{
		Rational converted = (Rational)original;
		await Assert.That((double)converted).IsEqualTo(original);
	}

	[Test]
	[Arguments(float.Epsilon)]
	[Arguments(float.MaxValue)]
	[Arguments(-0.1f)]
	public async Task SingleRoundTripPreservesRepresentableValues(float original)
	{
		Rational converted = (Rational)original;
		await Assert.That((float)converted).IsEqualTo(original);
	}

	[Test]
	public async Task BinaryConversionRoundsMidpointToEven()
	{
		Rational original = new((BigInteger.One << 53) + 1, BigInteger.One << 53);
		await Assert.That((double)original).IsEqualTo(1.0);
	}

	[Test]
	public async Task BinaryConversionDividesBeforeLosingLargeComponents()
	{
		BigInteger scale = BigInteger.One << 2000;
		Rational original = new(scale + 1, 2 * scale + 1);
		await Assert.That((double)original).IsEqualTo(0.5);
	}

	[Test]
	public async Task BinaryConversionRoundsAtSubnormalBoundary()
	{
		Rational halfwayToMinimum = new(1, BigInteger.One << 1075);
		Rational halfwayToSecond = new(3, BigInteger.One << 1075);

		using (Assert.Multiple())
		{
			await Assert.That((double)halfwayToMinimum).IsEqualTo(0.0);
			await Assert.That((double)halfwayToSecond).IsEqualTo(2 * double.Epsilon);
		}
	}

	[Test]
	public async Task DecimalConversionRoundsRepeatingFractions()
	{
		Rational original = new(1, 3);
		await Assert.That((decimal)original).IsEqualTo(1m / 3m);
	}

	[Test]
	public async Task DecimalCastRejectsOutOfRangeValue()
	{
		Rational original = new(new BigInteger(decimal.MaxValue) + 1);
		await Assert.That(() => (decimal)original).ThrowsExactly<OverflowException>();
	}

	[Test]
	[Arguments(1.5)]
	[Arguments(-0.125)]
	[Arguments(0.0)]
	public async Task RealComplexInputsPreserveFractions(double real)
	{
		Complex original = new(real, 0);
		Rational converted = (Rational)original;
		using (Assert.Multiple())
		{
			await Assert.That(converted).IsEqualTo((Rational)real);
			await Assert.That((Complex)converted).IsEqualTo(original);
		}
	}

	[Test]
	[Arguments(1.0)]
	[Arguments(-1.0)]
	public async Task NonRealComplexInputsAreRejected(double imaginary)
	{
		Complex original = new(1.5, imaginary);
		await Assert.That(() => (Rational)original).ThrowsExactly<RationalNumberCantHaveComplexComponentsException>();
	}
}
