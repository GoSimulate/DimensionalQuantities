using System.Numerics;
using DimensionalQuantities.Numerics;
using Xunit;

namespace DimensionalQuantities.Tests.Numerics;

public sealed partial class RationalTests
{
	[Fact]
	public void ConstructionReducesFractionToLowestTerms()
	{
		Rational result = new(6, 8);

		Assert.Multiple(
			() => Assert.Equal(3, result.Numerator),
			() => Assert.Equal(4, result.Denominator));
	}

	[Fact]
	public void NegativeDenominatorMovesSignToNumerator()
	{
		Rational result = new(2, -4);
		Rational expected = new(-1, 2);

		Assert.Multiple(
			() => Assert.Equal(-1, result.Numerator),
			() => Assert.Equal(2, result.Denominator),
			() => Assert.Equal(expected, result));
	}

	[Fact]
	public void TwoNegativeSignsCancel()
	{
		Rational result = new(-2, -4);
		Rational expected = new(1, 2);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void ZeroHasCanonicalDenominator()
	{
		Rational result = new(0, 7);
		Rational expected = default;

		Assert.Multiple(
			() => Assert.Equal(0, result.Numerator),
			() => Assert.Equal(1, result.Denominator),
			() => Assert.Equal(expected, result));
	}

	[Fact]
	public void DefaultRepresentsZeroOverOne()
	{
		Rational result = default;

		Assert.Multiple(
			() => Assert.Equal(0, result.Numerator),
			() => Assert.Equal(1, result.Denominator));
	}

	[Fact]
	public void EquivalentFractionsHaveEqualValuesAndHashes()
	{
		Rational result = new(2, 4);
		Rational expected = new(1, 2);

		Assert.Multiple(
			() => Assert.Equal(expected, result),
			() => Assert.Equal(expected.GetHashCode(), result.GetHashCode()));
	}

	[Fact]
	public void IntegerConversionProducesWholeFraction()
	{
		Rational result = 3;
		Rational expected = new(3, 1);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void AdditionCombinesDifferentDenominators()
	{
		Rational original = new(1, 2);
		Rational operand = new(1, 3);
		Rational result = original + operand;
		Rational expected = new(5, 6);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void SubtractionCombinesDifferentDenominators()
	{
		Rational original = new(1, 3);
		Rational operand = new(1, 2);
		Rational result = original - operand;
		Rational expected = new(-1, 6);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void MultiplicationReducesResult()
	{
		Rational original = new(2, 3);
		Rational operand = new(3, 4);
		Rational result = original * operand;
		Rational expected = new(1, 2);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void DivisionUsesReciprocal()
	{
		Rational original = new(2, 3);
		Rational operand = new(4, 5);
		Rational result = original / operand;
		Rational expected = new(5, 6);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void DefaultZeroIsAdditiveIdentity()
	{
		Rational original = new(2, 3);
		Rational zero = default;
		Rational result = original + zero;
		Rational expected = new(2, 3);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void ZeroDenominatorIsRejected()
	{
		Assert.Throws<DivideByZeroException>(() => new Rational(1, 0));
	}

	[Fact]
	public void DivisionByZeroIsRejected()
	{
		Rational original = new(1, 2);
		Rational zero = default;

		Assert.Throws<DivideByZeroException>(() => original / zero);
	}

	[Fact]
	public void ComparisonUsesFractionValues()
	{
		Rational original = new(-1, 2);
		Rational other = new(-1, 3);

		Assert.Multiple(
			() => Assert.True(original < other),
			() => Assert.True(original <= other),
			() => Assert.True(other > original),
			() => Assert.True(other >= original),
			() => Assert.Equal(-1, original.CompareTo(other)),
			() => Assert.Equal(-1, original.CompareTo((object)other)));
	}

	[Fact]
	public void RemainderUsesQuotientTruncatedTowardsZero()
	{
		Rational original = new(-7, 3);
		Rational divisor = new(1, 2);
		Rational result = original % divisor;
		Rational expected = new(-1, 3);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void UnaryPlusPreservesValue()
	{
		Rational original = new(1, 3);
		Rational result = +original;
		Rational expected = original;

		Assert.Equal(expected, result);
	}

	[Fact]
	public void UnaryMinusNegatesValue()
	{
		Rational original = new(1, 3);
		Rational result = -original;
		Rational expected = new(-1, 3);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void IncrementAddsOneAndPreservesOriginalValue()
	{
		Rational original = new(1, 3);
		Rational result = original;
		result++;
		Rational expected = new(4, 3);

		Assert.Multiple(
			() => Assert.Equal(expected, result),
			() => Assert.Equal(new Rational(1, 3), original));
	}

	[Fact]
	public void DecrementSubtractsOneAndPreservesOriginalValue()
	{
		Rational original = new(4, 3);
		Rational result = original;
		result--;
		Rational expected = new(1, 3);

		Assert.Multiple(
			() => Assert.Equal(expected, result),
			() => Assert.Equal(new Rational(4, 3), original));
	}

	[Fact]
	public void ClassificationDistinguishesFractionsAndIntegers()
	{
		Rational fraction = new(-3, 2);
		Rational even = new(8, 2);
		Rational odd = new(-6, 2);

		Assert.Multiple(
			() => Assert.True(Rational.IsFinite(fraction)),
			() => Assert.True(Rational.IsRealNumber(fraction)),
			() => Assert.True(Rational.IsNormal(fraction)),
			() => Assert.True(Rational.IsNegative(fraction)),
			() => Assert.False(Rational.IsInteger(fraction)),
			() => Assert.False(Rational.IsEvenInteger(fraction)),
			() => Assert.False(Rational.IsOddInteger(fraction)),
			() => Assert.True(Rational.IsEvenInteger(even)),
			() => Assert.True(Rational.IsOddInteger(odd)),
			() => Assert.True(Rational.IsZero(default)),
			() => Assert.False(Rational.IsNormal(default)));
	}

	[Fact]
	public void EqualMagnitudesPreferPositiveForMaximum()
	{
		Rational original = new(-2, 3);
		Rational positive = new(2, 3);
		Rational result = Rational.MaxMagnitude(original, positive);
		Rational expected = positive;

		Assert.Equal(expected, result);
	}

	[Fact]
	public void EqualMagnitudesPreferNegativeForMinimum()
	{
		Rational original = new(-2, 3);
		Rational positive = new(2, 3);
		Rational result = Rational.MinMagnitude(positive, original);
		Rational expected = original;

		Assert.Equal(expected, result);
	}

	[Fact]
	public void AbsoluteValueMakesNegativeFractionPositive()
	{
		Rational original = new(-2, 3);
		Rational result = Rational.Abs(original);
		Rational expected = new(2, 3);

		Assert.Equal(expected, result);
	}

}
