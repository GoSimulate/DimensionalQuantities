using DimensionalQuantities.Units;
using DimensionalQuantities.Numerics;
using Xunit;

namespace DimensionalQuantities.Tests.Units;

public sealed class UnitExpressionTests
{
	private static readonly AtomicUnit _newton = new("Newton", "Newtons", "N");
	private static readonly AtomicUnit _volt = new("Volt", "Volts", "V");
	private static readonly AtomicUnit _watt = new("Watt", "Watts", "W");
	[Theory]
	[InlineData(1, "Metres")]
	[InlineData(2, "SquareMetres")]
	[InlineData(3, "CubicMetres")]
	[InlineData(5, "Power5Metres")]
	public void GetNameFormatsAtomicPowers(int power, string expected)
	{
		UnitExpression original = new(new UnitTerm(AtomicUnits.Metre, power));
		string result = original.GetName();

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData(1, 2, "SquareRootMetres")]
	[InlineData(3, 2, "SquareRootCubicMetres")]
	[InlineData(2, 3, "CubeRootSquareMetres")]
	[InlineData(1, 4, "Root4Metres")]
	[InlineData(-1, 2, "InverseSquareRootMetre")]
	public void GetNameFormatsFractionalPowers(int numerator, int denominator, string expected)
	{
		UnitExpression original = new(new UnitTerm(AtomicUnits.Metre, new Rational(numerator, denominator)));
		string result = original.GetName();

		Assert.Equal(expected, result);
	}

	[Fact]
	public void GetNameFormatsCompoundNumeratorAndDenominator()
	{
		UnitExpression original = new(new UnitTerm(AtomicUnits.Amp), new UnitTerm(AtomicUnits.Second, -1));
		string result = original.GetName();
		string expected = "AmpsPerSecond";

		Assert.Equal(expected, result);
	}

	[Fact]
	public void GetNamePluralisesOnlyTheLastNumeratorFactor()
	{
		UnitExpression original = new(new UnitTerm(_newton), new UnitTerm(AtomicUnits.Metre));
		string result = original.GetName();
		string expected = "NewtonMetres";

		Assert.Equal(expected, result);
	}

	[Fact]
	public void GetNameFormatsInverseWithSingularUnitName()
	{
		UnitExpression original = new(new UnitTerm(AtomicUnits.Second, -1));
		string result = original.GetName();
		string expected = "InverseSecond";

		Assert.Equal(expected, result);
	}

	[Fact]
	public void GetNamePreservesKelvinSpelling()
	{
		UnitExpression original = new(new UnitTerm(AtomicUnits.Kelvin));
		string result = original.GetName();
		string expected = "Kelvin";

		Assert.Equal(expected, result);
	}

	[Fact]
	public void GetNameFormatsKilogramsWithFixedPrefixCasing()
	{
		UnitExpression original = new(new UnitTerm(AtomicUnits.Gram, prefixExponent: 3));
		string result = original.GetName();
		string expected = "Kilograms";

		Assert.Equal(expected, result);
	}

	[Fact]
	public void GetNameCapitalisesThePrefixOfAVariableFactor()
	{
		UnitExpression original = new(new UnitTerm(AtomicUnits.Gram, prefixExponent: 3));
		string result = original.GetName(variableFactorIndex: 0);
		string expected = "KiloGrams";

		Assert.Equal(expected, result);
	}

	[Fact]
	public void GetNameFormatsReferenceAwareCurrentDensityDenominator()
	{
		UnitExpression currentDensityReference = new(new UnitTerm(AtomicUnits.Amp), new UnitTerm(AtomicUnits.Metre, -2));
		UnitExpression currentDensity = currentDensityReference.WithPrefix(1, -3);
		string currentDensityResult = currentDensity.GetName(currentDensityReference);
		string expected = "AmpsPerSquareMilliMetre";

		Assert.Equal(expected, currentDensityResult);
	}

	[Fact]
	public void GetNameFormatsReferenceAwareMassDensityDenominator()
	{
		UnitExpression reference = new(new UnitTerm(AtomicUnits.Gram, prefixExponent: 3), new UnitTerm(AtomicUnits.Metre, -3));
		UnitExpression original = new(new UnitTerm(AtomicUnits.Gram), new UnitTerm(AtomicUnits.Metre, -3, -2));
		string result = original.GetName(reference);
		string expected = "GramsPerCubicCentiMetre";

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData(1, 0, "m")]
	[InlineData(2, 0, "m²")]
	[InlineData(3, 0, "m³")]
	[InlineData(1, -3, "mm")]
	public void GetSymbolFormatsUnitPowerAndPrefix(int power, int prefixExponent, string expected)
	{
		UnitExpression original = new(new UnitTerm(AtomicUnits.Metre, power, prefixExponent));
		string result = original.GetSymbol();

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData(1, 2, "m¹⁄²")]
	[InlineData(3, 2, "m³⁄²")]
	[InlineData(-1, 2, "m⁻¹⁄²")]
	public void GetSymbolFormatsFractionalPowers(int numerator, int denominator, string expected)
	{
		UnitExpression original = new(new UnitTerm(AtomicUnits.Metre, new Rational(numerator, denominator)));
		string result = original.GetSymbol();

		Assert.Equal(expected, result);
	}

	[Fact]
	public void GetSymbolFormatsInverseAndCompoundUnits()
	{
		UnitExpression inverse = new(new UnitTerm(AtomicUnits.Second, -1));
		UnitExpression compound = new(new UnitTerm(AtomicUnits.Amp), new UnitTerm(AtomicUnits.Metre, -2, -3));
		string inverseResult = inverse.GetSymbol();
		string compoundResult = compound.GetSymbol();

		Assert.Multiple(
			() => Assert.Equal("s⁻¹", inverseResult),
			() => Assert.Equal("A/mm²", compoundResult));
	}

	[Theory]
	[InlineData(1, -3, -3)]
	[InlineData(1, 3, 3)]
	[InlineData(2, -3, -6)]
	[InlineData(-2, -3, 6)]
	[InlineData(3, -3, -9)]
	[InlineData(-3, -3, 9)]
	public void GetSiScaleExponentAppliesMetrePrefixPowerAndSign(int power, int prefixExponent, int expected)
	{
		UnitExpression original = new(new UnitTerm(AtomicUnits.Metre, power, prefixExponent));
		Rational result = original.GetSiScaleExponent();

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData(1, 2, -3, -3, 2)]
	[InlineData(-1, 2, -3, 3, 2)]
	[InlineData(3, 2, -3, -9, 2)]
	public void GetSiScaleExponentAppliesFractionalPower(int numerator, int denominator, int prefixExponent, int expectedNumerator, int expectedDenominator)
	{
		UnitExpression original = new(new UnitTerm(AtomicUnits.Metre, new Rational(numerator, denominator), prefixExponent));
		Rational result = original.GetSiScaleExponent();
		Rational expected = new(expectedNumerator, expectedDenominator);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void GetSiScaleExponentCombinesFractionalGramOffsetAndPrefixedDenominator()
	{
		UnitExpression original = new(new UnitTerm(AtomicUnits.Gram, new Rational(1, 2)), new UnitTerm(AtomicUnits.Second, -1, -3));
		Rational result = original.GetSiScaleExponent();
		Rational expected = new(3, 2);

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData(3, 3)]
	[InlineData(-3, -3)]
	[InlineData(-27, -27)]
	[InlineData(30, 30)]
	[InlineData(-30, -30)]
	public void GetSiScaleExponentAppliesVoltagePrefix(int prefixExponent, int expected)
	{
		UnitExpression original = new(new UnitTerm(_volt, prefixExponent: prefixExponent));
		Rational result = original.GetSiScaleExponent();

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData(0, -3)]
	[InlineData(3, 0)]
	[InlineData(-3, -6)]
	public void GetSiScaleExponentAccountsForGramBasePrefixOffset(int prefixExponent, int expected)
	{
		UnitExpression original = new(new UnitTerm(AtomicUnits.Gram, prefixExponent: prefixExponent));
		Rational result = original.GetSiScaleExponent();

		Assert.Equal(expected, result);
	}

	[Fact]
	public void GetSiScaleExponentCombinesReciprocalFactors()
	{
		UnitExpression ampPerSquareMillimetre = new(new UnitTerm(AtomicUnits.Amp), new UnitTerm(AtomicUnits.Metre, -2, -3));
		UnitExpression gramPerCubicCentimetre = new(new UnitTerm(AtomicUnits.Gram), new UnitTerm(AtomicUnits.Metre, -3, -2));
		Rational currentDensityResult = ampPerSquareMillimetre.GetSiScaleExponent();
		Rational massDensityResult = gramPerCubicCentimetre.GetSiScaleExponent();

		Assert.Multiple(
			() => Assert.Equal(6, currentDensityResult),
			() => Assert.Equal(3, massDensityResult));
	}

	[Fact]
	public void GetSiScaleExponentCancelsKilogramInWattsPerKilogram()
	{
		UnitExpression original = new(new UnitTerm(_watt), new UnitTerm(AtomicUnits.Gram, -1, 3));
		Rational result = original.GetSiScaleExponent();
		Rational expected = Rational.Zero;

		Assert.Equal(expected, result);
	}

	[Fact]
	public void GetSiScaleExponentIncludesKilowattPrefix()
	{
		UnitExpression original = new(new UnitTerm(_watt, prefixExponent: 3), new UnitTerm(AtomicUnits.Gram, -1, 3));
		Rational result = original.GetSiScaleExponent();
		Rational expected = 3;

		Assert.Equal(expected, result);
	}

	[Fact]
	public void WithPrefixChangesOnlyTheSelectedTermAndPreservesOriginal()
	{
		UnitExpression original = new(new UnitTerm(AtomicUnits.Amp), new UnitTerm(AtomicUnits.Metre, -2));
		UnitExpression result = original.WithPrefix(1, -3);
		UnitExpression expected = new(new UnitTerm(AtomicUnits.Amp), new UnitTerm(AtomicUnits.Metre, -2, -3));
		UnitExpression expectedOriginal = new(new UnitTerm(AtomicUnits.Amp), new UnitTerm(AtomicUnits.Metre, -2));

		Assert.Multiple(
			() => Assert.Equal(expected, result),
			() => Assert.Equal(expectedOriginal, original));
	}
}
