using DimensionalQuantities.TypeSystem;
using AllAxes = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<
	DimensionalQuantities.TypeSystem.TerminalHex1,
	DimensionalQuantities.TypeSystem.TerminalHex2,
	DimensionalQuantities.TypeSystem.Negative<DimensionalQuantities.TypeSystem.TerminalHex1>,
	DimensionalQuantities.TypeSystem.TerminalHex3,
	DimensionalQuantities.TypeSystem.Negative<DimensionalQuantities.TypeSystem.TerminalHex2>,
	DimensionalQuantities.TypeSystem.TerminalHex1,
	DimensionalQuantities.TypeSystem.TerminalHex4>>;
using Current = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<
	DimensionalQuantities.TypeSystem.TerminalHex0,
	DimensionalQuantities.TypeSystem.TerminalHex0,
	DimensionalQuantities.TypeSystem.TerminalHex0,
	DimensionalQuantities.TypeSystem.TerminalHex1,
	DimensionalQuantities.TypeSystem.TerminalHex0,
	DimensionalQuantities.TypeSystem.TerminalHex0,
	DimensionalQuantities.TypeSystem.TerminalHex0>>;
using Length = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<
	DimensionalQuantities.TypeSystem.TerminalHex1,
	DimensionalQuantities.TypeSystem.TerminalHex0,
	DimensionalQuantities.TypeSystem.TerminalHex0,
	DimensionalQuantities.TypeSystem.TerminalHex0,
	DimensionalQuantities.TypeSystem.TerminalHex0,
	DimensionalQuantities.TypeSystem.TerminalHex0,
	DimensionalQuantities.TypeSystem.TerminalHex0>>;
using Mass = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<
	DimensionalQuantities.TypeSystem.TerminalHex0,
	DimensionalQuantities.TypeSystem.TerminalHex1,
	DimensionalQuantities.TypeSystem.TerminalHex0,
	DimensionalQuantities.TypeSystem.TerminalHex0,
	DimensionalQuantities.TypeSystem.TerminalHex0,
	DimensionalQuantities.TypeSystem.TerminalHex0,
	DimensionalQuantities.TypeSystem.TerminalHex0>>;
using SquareRootLength = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<
	DimensionalQuantities.TypeSystem.Fraction<DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.TerminalHex2>,
	DimensionalQuantities.TypeSystem.TerminalHex0,
	DimensionalQuantities.TypeSystem.TerminalHex0,
	DimensionalQuantities.TypeSystem.TerminalHex0,
	DimensionalQuantities.TypeSystem.TerminalHex0,
	DimensionalQuantities.TypeSystem.TerminalHex0,
	DimensionalQuantities.TypeSystem.TerminalHex0>>;

namespace DimensionalQuantities.Tests.TypeSystem;

public sealed class QuantityTests
{
	[Test]
	[Arguments(2, 3, true, false, true, false)]
	[Arguments(3, 2, false, true, false, true)]
	[Arguments(-2, -2, false, false, true, true)]
	[Arguments(double.PositiveInfinity, 3, false, true, false, true)]
	public async Task RelationalOperatorsCompareMatchingDimensions(double leftValue, double rightValue, bool less, bool greater, bool lessOrEqual, bool greaterOrEqual)
	{
		Current left = Current.FromSiBaseUnit(leftValue);
		Current right = Current.FromSiBaseUnit(rightValue);

		using (Assert.Multiple())
		{
			await Assert.That(left < right).IsEqualTo(less);
			await Assert.That(left > right).IsEqualTo(greater);
			await Assert.That(left <= right).IsEqualTo(lessOrEqual);
			await Assert.That(left >= right).IsEqualTo(greaterOrEqual);
		}
	}

	[Test]
	public async Task RelationalOperatorsReturnFalseForNanOnEitherSide()
	{
		Current nan = Current.FromSiBaseUnit(double.NaN);
		Current finite = Current.FromSiBaseUnit(1);

		using (Assert.Multiple())
		{
			await Assert.That(nan < finite).IsFalse();
			await Assert.That(nan > finite).IsFalse();
			await Assert.That(nan <= finite).IsFalse();
			await Assert.That(nan >= finite).IsFalse();
			await Assert.That(finite < nan).IsFalse();
			await Assert.That(finite > nan).IsFalse();
			await Assert.That(finite <= nan).IsFalse();
			await Assert.That(finite >= nan).IsFalse();
		}
	}

	[Test]
	[Arguments(-3, 3)]
	[Arguments(3, 3)]
	[Arguments(double.NegativeInfinity, double.PositiveInfinity)]
	public async Task AbsReturnsQuantityWithAbsoluteSiMagnitude(double value, double expectedValue)
	{
		Current original = Current.FromSiBaseUnit(value);
		Current result = Current.Abs(original);
		Current expected = Current.FromSiBaseUnit(expectedValue);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	[Arguments(2, 3, 2, 3)]
	[Arguments(3, 2, 2, 3)]
	[Arguments(-2, -2, -2, -2)]
	public async Task MinAndMaxReturnMatchingDimension(double leftValue, double rightValue, double expectedMinValue, double expectedMaxValue)
	{
		Current left = Current.FromSiBaseUnit(leftValue);
		Current right = Current.FromSiBaseUnit(rightValue);
		Current minResult = Current.Min(left, right);
		Current maxResult = Current.Max(left, right);
		Current expectedMin = Current.FromSiBaseUnit(expectedMinValue);
		Current expectedMax = Current.FromSiBaseUnit(expectedMaxValue);

		using (Assert.Multiple())
		{
			await Assert.That(minResult).IsEqualTo(expectedMin);
			await Assert.That(maxResult).IsEqualTo(expectedMax);
		}
	}

	[Test]
	[Arguments(-2, 0, 10, 0)]
	[Arguments(5, 0, 10, 5)]
	[Arguments(12, 0, 10, 10)]
	public async Task ClampReturnsQuantityWithinBounds(double value, double minValue, double maxValue, double expectedValue)
	{
		Current original = Current.FromSiBaseUnit(value);
		Current min = Current.FromSiBaseUnit(minValue);
		Current max = Current.FromSiBaseUnit(maxValue);
		Current result = Current.Clamp(original, min, max);
		Current expected = Current.FromSiBaseUnit(expectedValue);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task ClampRejectsReversedBounds()
	{
		Current original = Current.FromSiBaseUnit(5);
		Current min = Current.FromSiBaseUnit(10);
		Current max = Current.FromSiBaseUnit(0);

		await Assert.That(() => Current.Clamp(original, min, max)).ThrowsExactly<ArgumentException>();
	}

	[Test]
	public async Task HelpersFollowDoubleNanAndSignedZeroBehavior()
	{
		Current nan = Current.FromSiBaseUnit(double.NaN);
		Current finite = Current.FromSiBaseUnit(1);
		Current negativeZero = Current.FromSiBaseUnit(-0.0);
		Current positiveZero = Current.FromSiBaseUnit(0.0);
		Current absResult = Current.Abs(negativeZero);
		Current clampResult = Current.Clamp(negativeZero, negativeZero, positiveZero);
		Current nanMinimumResult = Current.Clamp(Current.FromSiBaseUnit(2), nan, finite);
		Current nanMaximumResult = Current.Clamp(Current.FromSiBaseUnit(-1), positiveZero, nan);

		using (Assert.Multiple())
		{
			await Assert.That(double.IsNaN(Current.Abs(nan).AsSiBaseUnit())).IsTrue();
			await Assert.That(double.IsNaN(Current.Min(nan, finite).AsSiBaseUnit())).IsTrue();
			await Assert.That(double.IsNaN(Current.Max(finite, nan).AsSiBaseUnit())).IsTrue();
			await Assert.That(double.IsNaN(Current.Clamp(nan, negativeZero, finite).AsSiBaseUnit())).IsTrue();
			await Assert.That(nanMinimumResult).IsEqualTo(finite);
			await Assert.That(nanMaximumResult).IsEqualTo(positiveZero);
			await Assert.That(BitConverter.DoubleToInt64Bits(absResult.AsSiBaseUnit())).IsEqualTo(0L);
			await Assert.That(BitConverter.DoubleToInt64Bits(clampResult.AsSiBaseUnit())).IsEqualTo(long.MinValue);
		}
	}

	[Test]
	[Arguments(6, 2, 3)]
	[Arguments(-6, 2, -3)]
	[Arguments(0, 2, 0)]
	public async Task DividingMatchingDimensionsReturnsDouble(double numerator, double denominator, double expected)
	{
		SquareRootLength original = SquareRootLength.FromSiBaseUnit(numerator);
		SquareRootLength divisor = SquareRootLength.FromSiBaseUnit(denominator);
		double result = original / divisor;

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	[Arguments(3, 2, 5)]
	[Arguments(-3, 2, -1)]
	[Arguments(3, -3, 0)]
	public async Task AdditionPreservesDimensionAndAddsSiMagnitudes(double first, double second, double expectedValue)
	{
		Current original = Current.FromSiBaseUnit(first);
		Current other = Current.FromSiBaseUnit(second);
		Current result = original + other;
		Current expected = Current.FromSiBaseUnit(expectedValue);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	[Arguments(3, 2, 1)]
	[Arguments(2, 3, -1)]
	[Arguments(-3, -3, 0)]
	public async Task SubtractionPreservesDimensionAndSubtractsSiMagnitudes(double first, double second, double expectedValue)
	{
		Current original = Current.FromSiBaseUnit(first);
		Current other = Current.FromSiBaseUnit(second);
		Current result = original - other;
		Current expected = Current.FromSiBaseUnit(expectedValue);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	[Arguments(3, -3)]
	[Arguments(-3, 3)]
	[Arguments(0, 0)]
	public async Task NegationPreservesDimensionAndReversesSign(double value, double expectedValue)
	{
		SquareRootLength original = SquareRootLength.FromSiBaseUnit(value);
		SquareRootLength result = -original;
		SquareRootLength expected = SquareRootLength.FromSiBaseUnit(expectedValue);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task MultiplyingAndDividingByScalarsPreservesQuantityTypeAndSiMagnitude()
	{
		Current original = Current.FromSiBaseUnit(6);
		Current rightProduct = original * 2;
		Current leftProduct = 2f * original;
		Current quotient = original / 2;
		Current expectedProduct = Current.FromSiBaseUnit(12);
		Current expectedQuotient = Current.FromSiBaseUnit(3);

		using (Assert.Multiple())
		{
			await Assert.That(rightProduct).IsEqualTo(expectedProduct);
			await Assert.That(leftProduct).IsEqualTo(expectedProduct);
			await Assert.That(quotient).IsEqualTo(expectedQuotient);
		}
	}

	[Test]
	[Arguments(0)]
	[Arguments(-0.125)]
	[Arguments(1234.5)]
	public async Task ConstructionPreservesSiMagnitude(double expected)
	{
		Current original = Current.FromSiBaseUnit(expected);
		double result = original.AsSiBaseUnit();

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	[Arguments(0)]
	[Arguments(-4.5)]
	[Arguments(12.25)]
	public async Task AsDeltaPreservesSiMagnitudeForAnyQuantity(double expected)
	{
		Current original = Current.FromSiBaseUnit(expected);
		Delta<Current> result = original.AsDelta();

		await Assert.That(result.AsSiBaseUnit()).IsEqualTo(expected);
	}

	[Test]
	[Arguments(20, 10, 10)]
	[Arguments(10, 10, 0)]
	[Arguments(10, 20, -10)]
	public async Task AsDeltaUsesTheMagnitudeAfterSubtraction(double firstValue, double secondValue, double expected)
	{
		Length first = Length.FromSiBaseUnit(firstValue);
		Length second = Length.FromSiBaseUnit(secondValue);
		Delta<Length> result = (first - second).AsDelta();

		await Assert.That(result.AsSiBaseUnit()).IsEqualTo(expected);
	}

	[Test]
	public async Task ArbitraryComponentsUseKilogramsByDefaultAndAllowGrams()
	{
		Mass original = Mass.FromArbitrarySiComponents(2, massPrefixExponent: 0);
		double siResult = original.AsSiBaseUnit();
		double gramResult = original.AsArbitrarySiComponents(massPrefixExponent: 0);

		using (Assert.Multiple())
		{
			await Assert.That(siResult).IsEqualTo(0.002);
			await Assert.That(gramResult).IsEqualTo(2);
			await Assert.That(Mass.FromArbitrarySiComponents(2).AsSiBaseUnit()).IsEqualTo(2);
		}
	}

	[Test]
	public async Task ArbitraryComponentsApplyAllSevenAxisPrefixesAndOverallScale()
	{
		AllAxes original = AllAxes.FromArbitrarySiComponents(2, 1, 0, 2, 3, 4, 5, 6, 2);
		double result = original.AsSiBaseUnit();
		double expected = 2e25;

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task ArbitraryComponentsApplyFractionalDimensionPower()
	{
		SquareRootLength original = SquareRootLength.FromArbitrarySiComponents(4, lengthPrefixExponent: -3);
		double result = original.AsSiBaseUnit();
		double expected = 4 * Math.Sqrt(0.001);

		await Assert.That(Math.Round(result, 12)).IsEqualTo(Math.Round(expected, 12));
	}

	[Test]
	public async Task ArbitraryComponentsUseEachClosedQuantityDimensionAcrossInterleavedConversions()
	{
		Length firstLength = Length.FromArbitrarySiComponents(2, lengthPrefixExponent: -2);
		SquareRootLength squareRootLength = SquareRootLength.FromArbitrarySiComponents(2, lengthPrefixExponent: -2);
		Length secondLength = Length.FromArbitrarySiComponents(3, lengthPrefixExponent: -2);
		double firstLengthResult = firstLength.AsSiBaseUnit();
		double squareRootLengthResult = squareRootLength.AsSiBaseUnit();
		double secondLengthResult = secondLength.AsSiBaseUnit();
		double squareRootLengthComponentsResult = squareRootLength.AsArbitrarySiComponents(lengthPrefixExponent: -2);

		using (Assert.Multiple())
		{
			await Assert.That(Math.Round(firstLengthResult, 12)).IsEqualTo(Math.Round(0.02, 12));
			await Assert.That(Math.Round(squareRootLengthResult, 12)).IsEqualTo(Math.Round(0.2, 12));
			await Assert.That(Math.Round(secondLengthResult, 12)).IsEqualTo(Math.Round(0.03, 12));
			await Assert.That(Math.Round(squareRootLengthComponentsResult, 12)).IsEqualTo(Math.Round(2.0, 12));
		}
	}

	[Test]
	public async Task ArbitraryComponentsAccessorAppliesPrefixAndOverallScale()
	{
		Current original = Current.FromSiBaseUnit(0.3);
		double result = original.AsArbitrarySiComponents(electricCurrentPrefixExponent: -3, overallScaleExponent: 2);
		double expected = 3;

		await Assert.That(Math.Round(result, 12)).IsEqualTo(Math.Round(expected, 12));
	}

	[Test]
	public async Task RawSiMagnitudeIsNotPartOfPublicApi()
	{
		Type quantityType = typeof(Current);

		using (Assert.Multiple())
		{
			await Assert.That(quantityType.GetProperties()).IsEmpty();
			await Assert.That(quantityType.GetFields()).IsEmpty();
			await Assert.That(quantityType.GetConstructor([typeof(double)])).IsNull();
		}
	}
}
