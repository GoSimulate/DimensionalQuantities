using Xunit;
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
	[Theory]
	[InlineData(2, 3, true, false, true, false)]
	[InlineData(3, 2, false, true, false, true)]
	[InlineData(-2, -2, false, false, true, true)]
	[InlineData(double.PositiveInfinity, 3, false, true, false, true)]
	public void RelationalOperatorsCompareMatchingDimensions(double leftValue, double rightValue, bool less, bool greater, bool lessOrEqual, bool greaterOrEqual)
	{
		Current left = Current.FromSiBaseUnit(leftValue);
		Current right = Current.FromSiBaseUnit(rightValue);

		Assert.Multiple(
			() => Assert.Equal(less, left < right),
			() => Assert.Equal(greater, left > right),
			() => Assert.Equal(lessOrEqual, left <= right),
			() => Assert.Equal(greaterOrEqual, left >= right));
	}

	[Fact]
	public void RelationalOperatorsReturnFalseForNanOnEitherSide()
	{
		Current nan = Current.FromSiBaseUnit(double.NaN);
		Current finite = Current.FromSiBaseUnit(1);

		Assert.Multiple(
			() => Assert.False(nan < finite),
			() => Assert.False(nan > finite),
			() => Assert.False(nan <= finite),
			() => Assert.False(nan >= finite),
			() => Assert.False(finite < nan),
			() => Assert.False(finite > nan),
			() => Assert.False(finite <= nan),
			() => Assert.False(finite >= nan));
	}

	[Theory]
	[InlineData(-3, 3)]
	[InlineData(3, 3)]
	[InlineData(double.NegativeInfinity, double.PositiveInfinity)]
	public void AbsReturnsQuantityWithAbsoluteSiMagnitude(double value, double expectedValue)
	{
		Current original = Current.FromSiBaseUnit(value);
		Current result = Current.Abs(original);
		Current expected = Current.FromSiBaseUnit(expectedValue);

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData(2, 3, 2, 3)]
	[InlineData(3, 2, 2, 3)]
	[InlineData(-2, -2, -2, -2)]
	public void MinAndMaxReturnMatchingDimension(double leftValue, double rightValue, double expectedMinValue, double expectedMaxValue)
	{
		Current left = Current.FromSiBaseUnit(leftValue);
		Current right = Current.FromSiBaseUnit(rightValue);
		Current minResult = Current.Min(left, right);
		Current maxResult = Current.Max(left, right);
		Current expectedMin = Current.FromSiBaseUnit(expectedMinValue);
		Current expectedMax = Current.FromSiBaseUnit(expectedMaxValue);

		Assert.Multiple(
			() => Assert.Equal(expectedMin, minResult),
			() => Assert.Equal(expectedMax, maxResult));
	}

	[Theory]
	[InlineData(-2, 0, 10, 0)]
	[InlineData(5, 0, 10, 5)]
	[InlineData(12, 0, 10, 10)]
	public void ClampReturnsQuantityWithinBounds(double value, double minValue, double maxValue, double expectedValue)
	{
		Current original = Current.FromSiBaseUnit(value);
		Current min = Current.FromSiBaseUnit(minValue);
		Current max = Current.FromSiBaseUnit(maxValue);
		Current result = Current.Clamp(original, min, max);
		Current expected = Current.FromSiBaseUnit(expectedValue);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void ClampRejectsReversedBounds()
	{
		Current original = Current.FromSiBaseUnit(5);
		Current min = Current.FromSiBaseUnit(10);
		Current max = Current.FromSiBaseUnit(0);

		Assert.Throws<ArgumentException>(() => Current.Clamp(original, min, max));
	}

	[Fact]
	public void HelpersFollowDoubleNanAndSignedZeroBehavior()
	{
		Current nan = Current.FromSiBaseUnit(double.NaN);
		Current finite = Current.FromSiBaseUnit(1);
		Current negativeZero = Current.FromSiBaseUnit(-0.0);
		Current positiveZero = Current.FromSiBaseUnit(0.0);
		Current absResult = Current.Abs(negativeZero);
		Current clampResult = Current.Clamp(negativeZero, negativeZero, positiveZero);
		Current nanMinimumResult = Current.Clamp(Current.FromSiBaseUnit(2), nan, finite);
		Current nanMaximumResult = Current.Clamp(Current.FromSiBaseUnit(-1), positiveZero, nan);

		Assert.Multiple(
			() => Assert.True(double.IsNaN(Current.Abs(nan).AsSiBaseUnit())),
			() => Assert.True(double.IsNaN(Current.Min(nan, finite).AsSiBaseUnit())),
			() => Assert.True(double.IsNaN(Current.Max(finite, nan).AsSiBaseUnit())),
			() => Assert.True(double.IsNaN(Current.Clamp(nan, negativeZero, finite).AsSiBaseUnit())),
			() => Assert.Equal(finite, nanMinimumResult),
			() => Assert.Equal(positiveZero, nanMaximumResult),
			() => Assert.Equal(0L, BitConverter.DoubleToInt64Bits(absResult.AsSiBaseUnit())),
			() => Assert.Equal(long.MinValue, BitConverter.DoubleToInt64Bits(clampResult.AsSiBaseUnit())));
	}

	[Theory]
	[InlineData(6, 2, 3)]
	[InlineData(-6, 2, -3)]
	[InlineData(0, 2, 0)]
	public void DividingMatchingDimensionsReturnsDouble(double numerator, double denominator, double expected)
	{
		SquareRootLength original = SquareRootLength.FromSiBaseUnit(numerator);
		SquareRootLength divisor = SquareRootLength.FromSiBaseUnit(denominator);
		double result = original / divisor;

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData(3, 2, 5)]
	[InlineData(-3, 2, -1)]
	[InlineData(3, -3, 0)]
	public void AdditionPreservesDimensionAndAddsSiMagnitudes(double first, double second, double expectedValue)
	{
		Current original = Current.FromSiBaseUnit(first);
		Current other = Current.FromSiBaseUnit(second);
		Current result = original + other;
		Current expected = Current.FromSiBaseUnit(expectedValue);

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData(3, 2, 1)]
	[InlineData(2, 3, -1)]
	[InlineData(-3, -3, 0)]
	public void SubtractionPreservesDimensionAndSubtractsSiMagnitudes(double first, double second, double expectedValue)
	{
		Current original = Current.FromSiBaseUnit(first);
		Current other = Current.FromSiBaseUnit(second);
		Current result = original - other;
		Current expected = Current.FromSiBaseUnit(expectedValue);

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData(3, -3)]
	[InlineData(-3, 3)]
	[InlineData(0, 0)]
	public void NegationPreservesDimensionAndReversesSign(double value, double expectedValue)
	{
		SquareRootLength original = SquareRootLength.FromSiBaseUnit(value);
		SquareRootLength result = -original;
		SquareRootLength expected = SquareRootLength.FromSiBaseUnit(expectedValue);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void MultiplyingAndDividingByScalarsPreservesQuantityTypeAndSiMagnitude()
	{
		Current original = Current.FromSiBaseUnit(6);
		Current rightProduct = original * 2;
		Current leftProduct = 2f * original;
		Current quotient = original / 2;
		Current expectedProduct = Current.FromSiBaseUnit(12);
		Current expectedQuotient = Current.FromSiBaseUnit(3);

		Assert.Multiple(
			() => Assert.Equal(expectedProduct, rightProduct),
			() => Assert.Equal(expectedProduct, leftProduct),
			() => Assert.Equal(expectedQuotient, quotient));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-0.125)]
	[InlineData(1234.5)]
	public void ConstructionPreservesSiMagnitude(double expected)
	{
		Current original = Current.FromSiBaseUnit(expected);
		double result = original.AsSiBaseUnit();

		Assert.Equal(expected, result);
	}

	[Fact]
	public void ArbitraryComponentsUseKilogramsByDefaultAndAllowGrams()
	{
		Mass original = Mass.FromArbitrarySiComponents(2, massPrefixExponent: 0);
		double siResult = original.AsSiBaseUnit();
		double gramResult = original.AsArbitrarySiComponents(massPrefixExponent: 0);

		Assert.Multiple(
			() => Assert.Equal(0.002, siResult),
			() => Assert.Equal(2, gramResult),
			() => Assert.Equal(2, Mass.FromArbitrarySiComponents(2).AsSiBaseUnit()));
	}

	[Fact]
	public void ArbitraryComponentsApplyAllSevenAxisPrefixesAndOverallScale()
	{
		AllAxes original = AllAxes.FromArbitrarySiComponents(2, 1, 0, 2, 3, 4, 5, 6, 2);
		double result = original.AsSiBaseUnit();
		double expected = 2e25;

		Assert.Equal(expected, result);
	}

	[Fact]
	public void ArbitraryComponentsApplyFractionalDimensionPower()
	{
		SquareRootLength original = SquareRootLength.FromArbitrarySiComponents(4, lengthPrefixExponent: -3);
		double result = original.AsSiBaseUnit();
		double expected = 4 * Math.Sqrt(0.001);

		Assert.Equal(expected, result, 12);
	}

	[Fact]
	public void ArbitraryComponentsUseEachClosedQuantityDimensionAcrossInterleavedConversions()
	{
		Length firstLength = Length.FromArbitrarySiComponents(2, lengthPrefixExponent: -2);
		SquareRootLength squareRootLength = SquareRootLength.FromArbitrarySiComponents(2, lengthPrefixExponent: -2);
		Length secondLength = Length.FromArbitrarySiComponents(3, lengthPrefixExponent: -2);
		double firstLengthResult = firstLength.AsSiBaseUnit();
		double squareRootLengthResult = squareRootLength.AsSiBaseUnit();
		double secondLengthResult = secondLength.AsSiBaseUnit();
		double squareRootLengthComponentsResult = squareRootLength.AsArbitrarySiComponents(lengthPrefixExponent: -2);

		Assert.Multiple(
			() => Assert.Equal(0.02, firstLengthResult, 12),
			() => Assert.Equal(0.2, squareRootLengthResult, 12),
			() => Assert.Equal(0.03, secondLengthResult, 12),
			() => Assert.Equal(2, squareRootLengthComponentsResult, 12));
	}

	[Fact]
	public void ArbitraryComponentsAccessorAppliesPrefixAndOverallScale()
	{
		Current original = Current.FromSiBaseUnit(0.3);
		double result = original.AsArbitrarySiComponents(electricCurrentPrefixExponent: -3, overallScaleExponent: 2);
		double expected = 3;

		Assert.Equal(expected, result, 12);
	}

	[Fact]
	public void RawSiMagnitudeIsNotPartOfPublicApi()
	{
		Type quantityType = typeof(Current);

		Assert.Multiple(
			() => Assert.Empty(quantityType.GetProperties()),
			() => Assert.Empty(quantityType.GetFields()),
			() => Assert.Null(quantityType.GetConstructor([typeof(double)])));
	}
}
