using System.Numerics;
using DimensionalQuantities.Numerics;

namespace DimensionalQuantities.Tests.Numerics;

public sealed partial class RationalTests
{
	[Test]
	public async Task ConstructionReducesFractionToLowestTerms()
	{
		Rational result = new(6, 8);

		using (Assert.Multiple())
		{
			await Assert.That(result.Numerator).IsEqualTo(3);
			await Assert.That(result.Denominator).IsEqualTo(4);
		}
	}

	[Test]
	public async Task NegativeDenominatorMovesSignToNumerator()
	{
		Rational result = new(2, -4);
		Rational expected = new(-1, 2);

		using (Assert.Multiple())
		{
			await Assert.That(result.Numerator).IsEqualTo(-1);
			await Assert.That(result.Denominator).IsEqualTo(2);
			await Assert.That(result).IsEqualTo(expected);
		}
	}

	[Test]
	public async Task TwoNegativeSignsCancel()
	{
		Rational result = new(-2, -4);
		Rational expected = new(1, 2);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task ZeroHasCanonicalDenominator()
	{
		Rational result = new(0, 7);
		Rational expected = default;

		using (Assert.Multiple())
		{
			await Assert.That(result.Numerator).IsEqualTo(0);
			await Assert.That(result.Denominator).IsEqualTo(1);
			await Assert.That(result).IsEqualTo(expected);
		}
	}

	[Test]
	public async Task DefaultRepresentsZeroOverOne()
	{
		Rational result = default;

		using (Assert.Multiple())
		{
			await Assert.That(result.Numerator).IsEqualTo(0);
			await Assert.That(result.Denominator).IsEqualTo(1);
		}
	}

	[Test]
	public async Task EquivalentFractionsHaveEqualValuesAndHashes()
	{
		Rational result = new(2, 4);
		Rational expected = new(1, 2);

		using (Assert.Multiple())
		{
			await Assert.That(result).IsEqualTo(expected);
			await Assert.That(result.GetHashCode()).IsEqualTo(expected.GetHashCode());
		}
	}

	[Test]
	public async Task IntegerConversionProducesWholeFraction()
	{
		Rational result = 3;
		Rational expected = new(3, 1);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task AdditionCombinesDifferentDenominators()
	{
		Rational original = new(1, 2);
		Rational operand = new(1, 3);
		Rational result = original + operand;
		Rational expected = new(5, 6);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task SubtractionCombinesDifferentDenominators()
	{
		Rational original = new(1, 3);
		Rational operand = new(1, 2);
		Rational result = original - operand;
		Rational expected = new(-1, 6);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task MultiplicationReducesResult()
	{
		Rational original = new(2, 3);
		Rational operand = new(3, 4);
		Rational result = original * operand;
		Rational expected = new(1, 2);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task DivisionUsesReciprocal()
	{
		Rational original = new(2, 3);
		Rational operand = new(4, 5);
		Rational result = original / operand;
		Rational expected = new(5, 6);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task DefaultZeroIsAdditiveIdentity()
	{
		Rational original = new(2, 3);
		Rational zero = default;
		Rational result = original + zero;
		Rational expected = new(2, 3);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task ZeroDenominatorIsRejected()
	{
		await Assert.That(() => new Rational(1, 0)).ThrowsExactly<DivideByZeroException>();
	}

	[Test]
	public async Task DivisionByZeroIsRejected()
	{
		Rational original = new(1, 2);
		Rational zero = default;

		await Assert.That(() => original / zero).ThrowsExactly<DivideByZeroException>();
	}

	[Test]
	public async Task ComparisonUsesFractionValues()
	{
		Rational original = new(-1, 2);
		Rational other = new(-1, 3);

		using (Assert.Multiple())
		{
			await Assert.That(original < other).IsTrue();
			await Assert.That(original <= other).IsTrue();
			await Assert.That(other > original).IsTrue();
			await Assert.That(other >= original).IsTrue();
			await Assert.That(original.CompareTo(other)).IsEqualTo(-1);
			await Assert.That(original.CompareTo((object)other)).IsEqualTo(-1);
		}
	}

	[Test]
	public async Task RemainderUsesQuotientTruncatedTowardsZero()
	{
		Rational original = new(-7, 3);
		Rational divisor = new(1, 2);
		Rational result = original % divisor;
		Rational expected = new(-1, 3);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task UnaryPlusPreservesValue()
	{
		Rational original = new(1, 3);
		Rational result = +original;
		Rational expected = original;

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task UnaryMinusNegatesValue()
	{
		Rational original = new(1, 3);
		Rational result = -original;
		Rational expected = new(-1, 3);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task IncrementAddsOneAndPreservesOriginalValue()
	{
		Rational original = new(1, 3);
		Rational result = original;
		result++;
		Rational expected = new(4, 3);

		using (Assert.Multiple())
		{
			await Assert.That(result).IsEqualTo(expected);
			await Assert.That(original).IsEqualTo(new Rational(1, 3));
		}
	}

	[Test]
	public async Task DecrementSubtractsOneAndPreservesOriginalValue()
	{
		Rational original = new(4, 3);
		Rational result = original;
		result--;
		Rational expected = new(1, 3);

		using (Assert.Multiple())
		{
			await Assert.That(result).IsEqualTo(expected);
			await Assert.That(original).IsEqualTo(new Rational(4, 3));
		}
	}

	[Test]
	public async Task ClassificationDistinguishesFractionsAndIntegers()
	{
		Rational fraction = new(-3, 2);
		Rational even = new(8, 2);
		Rational odd = new(-6, 2);

		using (Assert.Multiple())
		{
			await Assert.That(Rational.IsFinite(fraction)).IsTrue();
			await Assert.That(Rational.IsRealNumber(fraction)).IsTrue();
			await Assert.That(Rational.IsNormal(fraction)).IsTrue();
			await Assert.That(Rational.IsNegative(fraction)).IsTrue();
			await Assert.That(Rational.IsInteger(fraction)).IsFalse();
			await Assert.That(Rational.IsEvenInteger(fraction)).IsFalse();
			await Assert.That(Rational.IsOddInteger(fraction)).IsFalse();
			await Assert.That(Rational.IsEvenInteger(even)).IsTrue();
			await Assert.That(Rational.IsOddInteger(odd)).IsTrue();
			await Assert.That(Rational.IsZero(default)).IsTrue();
			await Assert.That(Rational.IsNormal(default)).IsFalse();
		}
	}

	[Test]
	public async Task EqualMagnitudesPreferPositiveForMaximum()
	{
		Rational original = new(-2, 3);
		Rational positive = new(2, 3);
		Rational result = Rational.MaxMagnitude(original, positive);
		Rational expected = positive;

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task EqualMagnitudesPreferNegativeForMinimum()
	{
		Rational original = new(-2, 3);
		Rational positive = new(2, 3);
		Rational result = Rational.MinMagnitude(positive, original);
		Rational expected = original;

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task AbsoluteValueMakesNegativeFractionPositive()
	{
		Rational original = new(-2, 3);
		Rational result = Rational.Abs(original);
		Rational expected = new(2, 3);

		await Assert.That(result).IsEqualTo(expected);
	}

}
