using DimensionalQuantities.Numerics;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Units;

namespace DimensionalQuantities.Tests.Dimensions;

public sealed class DimensionTests
{
	[Test]
	public async Task ComponentsPairNonzeroExponentsWithTheirNamesInSiOrder()
	{
		Dimension original = new(1, -2, new(3, 2), 4, 0, -6, 7);
		IEnumerable<DimensionComponent> result = original.GetComponents();
		DimensionComponent[] expected =
		[
			new(1, new("Length", [new(AtomicUnits.Metre)])),
			new(-2, new("Mass", [new(AtomicUnits.Gram, prefixExponent: 3)])),
			new(new(3, 2), new("Time", [new(AtomicUnits.Second)])),
			new(4, new("Current", [new(AtomicUnits.Amp)])),
			new(-6, new("AmountOfSubstance", [new(AtomicUnits.Mole)])),
			new(7, new("LuminousIntensity", [new(AtomicUnits.Candela)]))
		];

		await Assert.That(result).IsEquivalentTo(expected, TUnit.Assertions.Enums.CollectionOrdering.Matching);
	}

	[Test]
	public async Task DimensionlessHasNoComponents()
	{
		Dimension original = default;
		IEnumerable<DimensionComponent> result = original.GetComponents();

		await Assert.That(result).IsEmpty();
	}

	[Test]
	public async Task MultiplicationOperatorCombinesDimensions()
	{
		Dimension original = new(ElectricCurrent: new(1, 2));
		Dimension operand = new(ElectricCurrent: new(3, 2), Time: -1);
		Dimension result = original * operand;
		Dimension expected = new(ElectricCurrent: 2, Time: -1);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task DivisionOperatorSubtractsDivisorDimensions()
	{
		Dimension original = new(Length: 1, Time: new(1, 2));
		Dimension operand = new(Time: new(3, 2));
		Dimension result = original / operand;
		Dimension expected = new(Length: 1, Time: -1);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task MultiplicationAddsEveryExponent()
	{
		Dimension original = new(1, -2, 3, -4, 5, -6, 7);
		Dimension operand = new(-7, 6, -5, 4, -3, 2, -1);
		Dimension result = original.Multiply(operand);
		Dimension expected = new(-6, 4, -2, 0, 2, -4, 6);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task DivisionSubtractsEveryExponent()
	{
		Dimension original = new(1, -2, 3, -4, 5, -6, 7);
		Dimension operand = new(-7, 6, -5, 4, -3, 2, -1);
		Dimension result = original.Divide(operand);
		Dimension expected = new(8, -8, 8, -8, 8, -8, 8);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task DimensionlessIsMultiplicativeIdentity()
	{
		Dimension original = new(Length: 2, Time: -3);
		Dimension dimensionless = default;
		Dimension multipliedResult = original.Multiply(dimensionless);
		Dimension reverseMultipliedResult = dimensionless.Multiply(original);
		Dimension expected = original;

		using (Assert.Multiple())
		{
			await Assert.That(multipliedResult).IsEqualTo(expected);
			await Assert.That(reverseMultipliedResult).IsEqualTo(expected);
		}
	}

	[Test]
	public async Task DivisionByDimensionlessPreservesDimension()
	{
		Dimension original = new(Length: 2, Time: -3);
		Dimension dimensionless = default;
		Dimension result = original.Divide(dimensionless);
		Dimension expected = original;

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task SelfDivisionCancelsEveryExponent()
	{
		Dimension original = new(Length: 2, Time: -3);
		Dimension result = original.Divide(original);
		Dimension expected = default;

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task GroupingDoesNotChangeElectricalLossDimension()
	{
		Dimension current = new(ElectricCurrent: 1);
		Dimension resistance = new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -2);
		Dimension result = current.Multiply(current).Multiply(resistance);
		Dimension regroupedResult = current.Multiply(current.Multiply(resistance));
		Dimension expected = new(Length: 2, Mass: 1, Time: -3);

		using (Assert.Multiple())
		{
			await Assert.That(result).IsEqualTo(expected);
			await Assert.That(regroupedResult).IsEqualTo(expected);
		}
	}

	[Test]
	[Arguments(-2)]
	[Arguments(-1)]
	[Arguments(0)]
	[Arguments(1)]
	[Arguments(3)]
	public async Task IntegerPowersScaleAllExponents(int power)
	{
		Dimension original = new(1, -2, 3, -4, 5, -6, 7);
		Dimension result = original.Pow(power);
		Dimension expected = new(power, -2 * power, 3 * power, -4 * power, 5 * power, -6 * power, 7 * power);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	[Arguments(1)]
	[Arguments(2)]
	[Arguments(3)]
	public async Task ExactRootsDivideAllExponents(int degree)
	{
		Dimension original = new(6, -12, 18, -24, 30, -36, 42);
		Dimension result = original.Root(degree);
		Dimension expected = new(6 / degree, -12 / degree, 18 / degree, -24 / degree, 30 / degree, -36 / degree, 42 / degree);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task RootsProduceFractionalExponents()
	{
		Dimension original = new(1, -2, 3, -4, 5, -6, 7);
		Dimension result = original.Root(2);
		Dimension expected = new(new(1, 2), -1, new(3, 2), -2, new(5, 2), -3, new(7, 2));

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task FractionalPowerScalesDimension()
	{
		Dimension original = new(Length: 3);
		Dimension result = original.Pow(new(2, 3));
		Dimension expected = new(Length: 2);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task MultiplicationCombinesFractionalExponentsExactly()
	{
		Dimension original = new(ElectricCurrent: new(1, 3));
		Dimension operand = new(ElectricCurrent: new(2, 3));
		Dimension result = original.Multiply(operand);
		Dimension expected = new(ElectricCurrent: 1);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task DivisionSubtractsFractionalExponentsExactly()
	{
		Dimension original = new(ElectricCurrent: 2);
		Dimension operand = new(ElectricCurrent: new(3, 2));
		Dimension result = original.Divide(operand);
		Dimension expected = new(ElectricCurrent: new(1, 2));

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task NoiseDensityTimesRootBandwidthProducesCurrent()
	{
		Dimension original = new(ElectricCurrent: 1, Time: new(1, 2));
		Dimension bandwidth = new(Time: -1);
		Dimension result = original.Multiply(bandwidth.Root(2));
		Dimension expected = new(ElectricCurrent: 1);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task DimensionlessHasAnExactRoot()
	{
		Dimension original = default;
		Dimension result = original.Root(2);
		Dimension expected = default;

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	[Arguments(0)]
	[Arguments(-1)]
	public async Task RootRequiresPositiveDegree(int degree)
	{
		Dimension original = default;

		await Assert.That(() => original.Root(degree)).ThrowsExactly<ArgumentOutOfRangeException>();
	}
}
