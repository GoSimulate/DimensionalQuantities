using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Naming;
using DimensionalQuantities.Numerics;

namespace DimensionalQuantities.Tests.Naming;

public sealed class DimensionNameParserTests
{
	[Test]
	[Arguments("LengthCurrent", 1, 0, 0, 1, 0, 0, 0)]
	[Arguments("LengthSquaredTimeCubed", 2, 0, 3, 0, 0, 0, 0)]
	[Arguments("Mass4", 0, 4, 0, 0, 0, 0, 0)]
	[Arguments("Time1048576", 0, 0, 1048576, 0, 0, 0, 0)]
	[Arguments("InverseLengthCurrent", -1, 0, 0, -1, 0, 0, 0)]
	[Arguments("LengthPerTimeCurrentSquared", 1, 0, -1, -2, 0, 0, 0)]
	[Arguments("LengthMassTimeCurrentTemperatureAmountOfSubstanceLuminousIntensity", 1, 1, 1, 1, 1, 1, 1)]
	public async Task ParsesCanonicalDimensionNames(string name, int length, int mass, int time, int current, int temperature, int amount, int intensity)
	{
		bool success = DimensionNameParser.TryParse(name, out Dimension result);
		Dimension expected = new(length, mass, time, current, temperature, amount, intensity);

		using (Assert.Multiple())
		{
			await Assert.That(success).IsTrue();
			await Assert.That(result).IsEqualTo(expected);
			await Assert.That(DimensionNameGenerator.GetQuantityName(result)).IsEqualTo(name);
		}
	}

	[Test]
	[Arguments("SquareRootLengthCurrent", 1, 2, 1, 1)]
	[Arguments("CubeRootLengthSquared", 2, 3, 0, 1)]
	[Arguments("Root5LengthCubedPerRoot4Current", 3, 5, -1, 4)]
	public async Task ParsesRationalPowers(string name, int lengthNumerator, int lengthDenominator, int currentNumerator, int currentDenominator)
	{
		bool success = DimensionNameParser.TryParse(name, out Dimension result);
		Dimension expected = new(Length: new Rational(lengthNumerator, lengthDenominator), ElectricCurrent: new Rational(currentNumerator, currentDenominator));

		using (Assert.Multiple())
		{
			await Assert.That(success).IsTrue();
			await Assert.That(result).IsEqualTo(expected);
		}
	}

	[Test]
	[Arguments(null)]
	[Arguments("")]
	[Arguments("Missing")]
	[Arguments("length")]
	[Arguments("Length0")]
	[Arguments("Length1")]
	[Arguments("Length2")]
	[Arguments("Length04")]
	[Arguments("CurrentLength")]
	[Arguments("LengthLength")]
	[Arguments("LengthPerLength")]
	[Arguments("LengthPerTimePerCurrent")]
	[Arguments("PerLength")]
	[Arguments("Inverse")]
	[Arguments("Root0Length")]
	[Arguments("Root1Length")]
	[Arguments("Root2Length")]
	[Arguments("SquareRootLengthSquared")]
	[Arguments("LengthPow4")]
	[Arguments("Length Current")]
	public async Task RejectsNoncanonicalOrInvalidNames(string? name)
	{
		bool success = DimensionNameParser.TryParse(name, out Dimension result);

		using (Assert.Multiple())
		{
			await Assert.That(success).IsFalse();
			await Assert.That(result).IsEqualTo(default);
		}
	}
}
