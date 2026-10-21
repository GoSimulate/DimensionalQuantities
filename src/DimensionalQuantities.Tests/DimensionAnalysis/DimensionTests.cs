using DimensionalQuantities.Numerics;
using DimensionalQuantities.DimensionAnalysis;
using Xunit;

namespace DimensionalQuantities.Tests.DimensionAnalysis;

public sealed class DimensionTests
{
	[Fact]
	public void MultiplicationOperatorCombinesDimensions()
	{
		Dimension original = new(ElectricCurrent: new(1, 2));
		Dimension operand = new(ElectricCurrent: new(3, 2), Time: -1);
		Dimension result = original * operand;
		Dimension expected = new(ElectricCurrent: 2, Time: -1);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void DivisionOperatorSubtractsDivisorDimensions()
	{
		Dimension original = new(Length: 1, Time: new(1, 2));
		Dimension operand = new(Time: new(3, 2));
		Dimension result = original / operand;
		Dimension expected = new(Length: 1, Time: -1);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void MultiplicationAddsEveryExponent()
	{
		Dimension original = new(1, -2, 3, -4, 5, -6, 7);
		Dimension operand = new(-7, 6, -5, 4, -3, 2, -1);
		Dimension result = original.Multiply(operand);
		Dimension expected = new(-6, 4, -2, 0, 2, -4, 6);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void DivisionSubtractsEveryExponent()
	{
		Dimension original = new(1, -2, 3, -4, 5, -6, 7);
		Dimension operand = new(-7, 6, -5, 4, -3, 2, -1);
		Dimension result = original.Divide(operand);
		Dimension expected = new(8, -8, 8, -8, 8, -8, 8);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void DimensionlessIsMultiplicativeIdentity()
	{
		Dimension original = new(Length: 2, Time: -3);
		Dimension dimensionless = default;
		Dimension multipliedResult = original.Multiply(dimensionless);
		Dimension reverseMultipliedResult = dimensionless.Multiply(original);
		Dimension expected = original;

		Assert.Multiple(
			() => Assert.Equal(expected, multipliedResult),
			() => Assert.Equal(expected, reverseMultipliedResult));
	}

	[Fact]
	public void DivisionByDimensionlessPreservesDimension()
	{
		Dimension original = new(Length: 2, Time: -3);
		Dimension dimensionless = default;
		Dimension result = original.Divide(dimensionless);
		Dimension expected = original;

		Assert.Equal(expected, result);
	}

	[Fact]
	public void SelfDivisionCancelsEveryExponent()
	{
		Dimension original = new(Length: 2, Time: -3);
		Dimension result = original.Divide(original);
		Dimension expected = default;

		Assert.Equal(expected, result);
	}

	[Fact]
	public void GroupingDoesNotChangeElectricalLossDimension()
	{
		Dimension current = new(ElectricCurrent: 1);
		Dimension resistance = new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -2);
		Dimension result = current.Multiply(current).Multiply(resistance);
		Dimension regroupedResult = current.Multiply(current.Multiply(resistance));
		Dimension expected = new(Length: 2, Mass: 1, Time: -3);

		Assert.Multiple(
			() => Assert.Equal(expected, result),
			() => Assert.Equal(expected, regroupedResult));
	}

	[Theory]
	[InlineData(-2)]
	[InlineData(-1)]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(3)]
	public void IntegerPowersScaleAllExponents(int power)
	{
		Dimension original = new(1, -2, 3, -4, 5, -6, 7);
		Dimension result = original.Pow(power);
		Dimension expected = new(power, -2 * power, 3 * power, -4 * power, 5 * power, -6 * power, 7 * power);

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	public void ExactRootsDivideAllExponents(int degree)
	{
		Dimension original = new(6, -12, 18, -24, 30, -36, 42);
		Dimension result = original.Root(degree);
		Dimension expected = new(6 / degree, -12 / degree, 18 / degree, -24 / degree, 30 / degree, -36 / degree, 42 / degree);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void RootsProduceFractionalExponents()
	{
		Dimension original = new(1, -2, 3, -4, 5, -6, 7);
		Dimension result = original.Root(2);
		Dimension expected = new(new(1, 2), -1, new(3, 2), -2, new(5, 2), -3, new(7, 2));

		Assert.Equal(expected, result);
	}

	[Fact]
	public void FractionalPowerScalesDimension()
	{
		Dimension original = new(Length: 3);
		Dimension result = original.Pow(new(2, 3));
		Dimension expected = new(Length: 2);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void MultiplicationCombinesFractionalExponentsExactly()
	{
		Dimension original = new(ElectricCurrent: new(1, 3));
		Dimension operand = new(ElectricCurrent: new(2, 3));
		Dimension result = original.Multiply(operand);
		Dimension expected = new(ElectricCurrent: 1);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void DivisionSubtractsFractionalExponentsExactly()
	{
		Dimension original = new(ElectricCurrent: 2);
		Dimension operand = new(ElectricCurrent: new(3, 2));
		Dimension result = original.Divide(operand);
		Dimension expected = new(ElectricCurrent: new(1, 2));

		Assert.Equal(expected, result);
	}

	[Fact]
	public void NoiseDensityTimesRootBandwidthProducesCurrent()
	{
		Dimension original = new(ElectricCurrent: 1, Time: new(1, 2));
		Dimension bandwidth = new(Time: -1);
		Dimension result = original.Multiply(bandwidth.Root(2));
		Dimension expected = new(ElectricCurrent: 1);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void DimensionlessHasAnExactRoot()
	{
		Dimension original = default;
		Dimension result = original.Root(2);
		Dimension expected = default;

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void RootRequiresPositiveDegree(int degree)
	{
		Dimension original = default;

		Assert.Throws<ArgumentOutOfRangeException>(() => original.Root(degree));
	}
}
