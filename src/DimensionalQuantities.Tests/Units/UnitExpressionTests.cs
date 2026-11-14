using DimensionalQuantities.Units;
using DimensionalQuantities.Numerics;

namespace DimensionalQuantities.Tests.Units;

public sealed class UnitExpressionTests
{
	private static readonly AtomicUnit _newton = new("Newton", "Newtons", "N");
	private static readonly AtomicUnit _volt = new("Volt", "Volts", "V");
	private static readonly AtomicUnit _watt = new("Watt", "Watts", "W");
	[Test]
	[Arguments(1, "Metres")]
	[Arguments(2, "SquareMetres")]
	[Arguments(3, "CubicMetres")]
	[Arguments(5, "Power5Metres")]
	public async Task GetNameFormatsAtomicPowers(int power, string expected)
	{
		UnitExpression original = new(new UnitTerm(AtomicUnits.Metre, power));
		string result = original.GetName();

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	[Arguments(1, 2, "SquareRootMetres")]
	[Arguments(3, 2, "SquareRootCubicMetres")]
	[Arguments(2, 3, "CubeRootSquareMetres")]
	[Arguments(1, 4, "Root4Metres")]
	[Arguments(-1, 2, "InverseSquareRootMetre")]
	public async Task GetNameFormatsFractionalPowers(int numerator, int denominator, string expected)
	{
		UnitExpression original = new(new UnitTerm(AtomicUnits.Metre, new Rational(numerator, denominator)));
		string result = original.GetName();

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	[Arguments(1, 12, "Root12Amps")]
	[Arguments(4, 5, "Root5Power4Amps")]
	[Arguments(-3, 2, "InverseSquareRootCubicAmp")]
	public async Task GetNameFormatsHigherAndInverseRationalPowers(int numerator, int denominator, string expected)
	{
		UnitExpression original = new(new UnitTerm(AtomicUnits.Amp, new Rational(numerator, denominator)));
		string result = original.GetName();

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task GetNameKeepsFractionalPowersWithTheirFactorsInOrderedCompound()
	{
		UnitExpression original = new(
			new UnitTerm(AtomicUnits.Metre, new Rational(1, 2)),
			new UnitTerm(AtomicUnits.Amp),
			new UnitTerm(AtomicUnits.Second, new Rational(-3, 2)));
		string result = original.GetName();
		string expected = "SquareRootMetreAmpsPerSquareRootCubicSecond";

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task GetNameFormatsCompoundNumeratorAndDenominator()
	{
		UnitExpression original = new(new UnitTerm(AtomicUnits.Amp), new UnitTerm(AtomicUnits.Second, -1));
		string result = original.GetName();
		string expected = "AmpsPerSecond";

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task GetNamePluralisesOnlyTheLastNumeratorFactor()
	{
		UnitExpression original = new(new UnitTerm(_newton), new UnitTerm(AtomicUnits.Metre));
		string result = original.GetName();
		string expected = "NewtonMetres";

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task GetNameFormatsInverseWithSingularUnitName()
	{
		UnitExpression original = new(new UnitTerm(AtomicUnits.Second, -1));
		string result = original.GetName();
		string expected = "InverseSecond";

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task GetNamePreservesKelvinSpelling()
	{
		UnitExpression original = new(new UnitTerm(AtomicUnits.Kelvin));
		string result = original.GetName();
		string expected = "Kelvin";

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task GetNameFormatsKilogramsWithFixedPrefixCasing()
	{
		UnitExpression original = new(new UnitTerm(AtomicUnits.Gram, prefixExponent: 3));
		string result = original.GetName();
		string expected = "Kilograms";

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task GetNameCapitalisesThePrefixOfAVariableFactor()
	{
		UnitExpression original = new(new UnitTerm(AtomicUnits.Gram, prefixExponent: 3));
		string result = original.GetName(variableFactorIndex: 0);
		string expected = "KiloGrams";

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task GetNameFormatsReferenceAwareCurrentDensityDenominator()
	{
		UnitExpression currentDensityReference = new(new UnitTerm(AtomicUnits.Amp), new UnitTerm(AtomicUnits.Metre, -2));
		UnitExpression currentDensity = currentDensityReference.WithPrefix(1, -3);
		string currentDensityResult = currentDensity.GetName(currentDensityReference);
		string expected = "AmpsPerSquareMilliMetre";

		await Assert.That(currentDensityResult).IsEqualTo(expected);
	}

	[Test]
	public async Task GetNameFormatsReferenceAwareMassDensityDenominator()
	{
		UnitExpression reference = new(new UnitTerm(AtomicUnits.Gram, prefixExponent: 3), new UnitTerm(AtomicUnits.Metre, -3));
		UnitExpression original = new(new UnitTerm(AtomicUnits.Gram), new UnitTerm(AtomicUnits.Metre, -3, -2));
		string result = original.GetName(reference);
		string expected = "GramsPerCubicCentiMetre";

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	[Arguments(1, 0, "m")]
	[Arguments(2, 0, "m²")]
	[Arguments(3, 0, "m³")]
	[Arguments(1, -3, "mm")]
	public async Task GetSymbolFormatsUnitPowerAndPrefix(int power, int prefixExponent, string expected)
	{
		UnitExpression original = new(new UnitTerm(AtomicUnits.Metre, power, prefixExponent));
		string result = original.GetSymbol();

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	[Arguments(1, 2, "m¹⁄²")]
	[Arguments(3, 2, "m³⁄²")]
	[Arguments(-1, 2, "m⁻¹⁄²")]
	public async Task GetSymbolFormatsFractionalPowers(int numerator, int denominator, string expected)
	{
		UnitExpression original = new(new UnitTerm(AtomicUnits.Metre, new Rational(numerator, denominator)));
		string result = original.GetSymbol();

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task GetSymbolFormatsInverseAndCompoundUnits()
	{
		UnitExpression inverse = new(new UnitTerm(AtomicUnits.Second, -1));
		UnitExpression compound = new(new UnitTerm(AtomicUnits.Amp), new UnitTerm(AtomicUnits.Metre, -2, -3));
		string inverseResult = inverse.GetSymbol();
		string compoundResult = compound.GetSymbol();

		using (Assert.Multiple())
		{
			await Assert.That(inverseResult).IsEqualTo("s⁻¹");
			await Assert.That(compoundResult).IsEqualTo("A/mm²");
		}
	}

	[Test]
	[Arguments(1, -3, -3)]
	[Arguments(1, 3, 3)]
	[Arguments(2, -3, -6)]
	[Arguments(-2, -3, 6)]
	[Arguments(3, -3, -9)]
	[Arguments(-3, -3, 9)]
	public async Task GetSiScaleExponentAppliesMetrePrefixPowerAndSign(int power, int prefixExponent, int expected)
	{
		UnitExpression original = new(new UnitTerm(AtomicUnits.Metre, power, prefixExponent));
		Rational result = original.GetSiScaleExponent();

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	[Arguments(1, 2, -3, -3, 2)]
	[Arguments(-1, 2, -3, 3, 2)]
	[Arguments(3, 2, -3, -9, 2)]
	public async Task GetSiScaleExponentAppliesFractionalPower(int numerator, int denominator, int prefixExponent, int expectedNumerator, int expectedDenominator)
	{
		UnitExpression original = new(new UnitTerm(AtomicUnits.Metre, new Rational(numerator, denominator), prefixExponent));
		Rational result = original.GetSiScaleExponent();
		Rational expected = new(expectedNumerator, expectedDenominator);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task GetSiScaleExponentCombinesFractionalGramOffsetAndPrefixedDenominator()
	{
		UnitExpression original = new(new UnitTerm(AtomicUnits.Gram, new Rational(1, 2)), new UnitTerm(AtomicUnits.Second, -1, -3));
		Rational result = original.GetSiScaleExponent();
		Rational expected = new(3, 2);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	[Arguments(3, 3)]
	[Arguments(-3, -3)]
	[Arguments(-27, -27)]
	[Arguments(30, 30)]
	[Arguments(-30, -30)]
	public async Task GetSiScaleExponentAppliesVoltagePrefix(int prefixExponent, int expected)
	{
		UnitExpression original = new(new UnitTerm(_volt, prefixExponent: prefixExponent));
		Rational result = original.GetSiScaleExponent();

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	[Arguments(0, -3)]
	[Arguments(3, 0)]
	[Arguments(-3, -6)]
	public async Task GetSiScaleExponentAccountsForGramBasePrefixOffset(int prefixExponent, int expected)
	{
		UnitExpression original = new(new UnitTerm(AtomicUnits.Gram, prefixExponent: prefixExponent));
		Rational result = original.GetSiScaleExponent();

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task GetSiScaleExponentCombinesReciprocalFactors()
	{
		UnitExpression ampPerSquareMillimetre = new(new UnitTerm(AtomicUnits.Amp), new UnitTerm(AtomicUnits.Metre, -2, -3));
		UnitExpression gramPerCubicCentimetre = new(new UnitTerm(AtomicUnits.Gram), new UnitTerm(AtomicUnits.Metre, -3, -2));
		Rational currentDensityResult = ampPerSquareMillimetre.GetSiScaleExponent();
		Rational massDensityResult = gramPerCubicCentimetre.GetSiScaleExponent();

		using (Assert.Multiple())
		{
			await Assert.That(currentDensityResult).IsEqualTo(6);
			await Assert.That(massDensityResult).IsEqualTo(3);
		}
	}

	[Test]
	public async Task GetSiScaleExponentCancelsKilogramInWattsPerKilogram()
	{
		UnitExpression original = new(new UnitTerm(_watt), new UnitTerm(AtomicUnits.Gram, -1, 3));
		Rational result = original.GetSiScaleExponent();
		Rational expected = Rational.Zero;

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task GetSiScaleExponentIncludesKilowattPrefix()
	{
		UnitExpression original = new(new UnitTerm(_watt, prefixExponent: 3), new UnitTerm(AtomicUnits.Gram, -1, 3));
		Rational result = original.GetSiScaleExponent();
		Rational expected = 3;

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task WithPrefixChangesOnlyTheSelectedTermAndPreservesOriginal()
	{
		UnitExpression original = new(new UnitTerm(AtomicUnits.Amp), new UnitTerm(AtomicUnits.Metre, -2));
		UnitExpression result = original.WithPrefix(1, -3);
		UnitExpression expected = new(new UnitTerm(AtomicUnits.Amp), new UnitTerm(AtomicUnits.Metre, -2, -3));
		UnitExpression expectedOriginal = new(new UnitTerm(AtomicUnits.Amp), new UnitTerm(AtomicUnits.Metre, -2));

		using (Assert.Multiple())
		{
			await Assert.That(result).IsEqualTo(expected);
			await Assert.That(original).IsEqualTo(expectedOriginal);
		}
	}

	[Test]
	public async Task GetScaleCombinesNonDecimalFactorsAndNegativePowers()
	{
		AtomicUnit foot = new("Foot", "Feet", "ft", Scale: 0.3048);
		AtomicUnit minute = new("Minute", "Minutes", "min", Scale: 60);
		UnitExpression original = new(new UnitTerm(foot, 2), new UnitTerm(minute, -1));
		double result = original.GetScale();
		double expected = 0.001548384;

		await Assert.That(Math.Round(result, 12)).IsEqualTo(Math.Round(expected, 12));
	}

	[Test]
	public async Task GetScaleAppliesFractionalPowers()
	{
		AtomicUnit unit = new("Sample", "Samples", "x", Scale: 16);
		UnitExpression original = new(new UnitTerm(unit, new Rational(3, 2)));
		double result = original.GetScale();
		double expected = 64;

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task GetNameAcceptsAReferenceWithDifferentFactors()
	{
		UnitExpression reference = new(new UnitTerm(AtomicUnits.Amp));
		UnitExpression original = new(new UnitTerm(AtomicUnits.Amp), new UnitTerm(AtomicUnits.Metre, -2));
		string result = original.GetName(reference);
		string expected = "AmpsPerSquareMetre";

		await Assert.That(result).IsEqualTo(expected);
	}
}
