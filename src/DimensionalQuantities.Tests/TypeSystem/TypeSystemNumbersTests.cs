using DimensionalQuantities.TypeSystem;
using Xunit;

namespace DimensionalQuantities.Tests.TypeSystem;

public sealed class TypeSystemNumbersTests
{
	[Theory]
	[InlineData(typeof(TerminalHex0), typeof(TerminalHex1))]
	[InlineData(typeof(TerminalHexF), typeof(Hex<TerminalHex0, TerminalHex1>))]
	public void HexAcceptsDigitsAndPositiveRemainders(Type digit, Type remainder)
	{
		Type result = typeof(Hex<,>).MakeGenericType(digit, remainder);

		Assert.True(typeof(IPositiveTypeSystemInteger).IsAssignableFrom(result));
	}

	[Theory]
	[InlineData(typeof(int), typeof(TerminalHex1))]
	[InlineData(typeof(Hex<TerminalHex0, TerminalHex1>), typeof(TerminalHex1))]
	[InlineData(typeof(Fraction<TerminalHex1, TerminalHex2>), typeof(TerminalHex1))]
	[InlineData(typeof(TerminalHexC), typeof(TerminalHex0))]
	[InlineData(typeof(TerminalHexC), typeof(Negative<TerminalHex1>))]
	public void HexRejectsNonDigitsAndNonPositiveRemainders(Type digit, Type remainder)
	{
		Assert.Throws<ArgumentException>(() => typeof(Hex<,>).MakeGenericType(digit, remainder));
	}

	[Theory]
	[InlineData(typeof(TerminalHexC))]
	[InlineData(typeof(Hex<TerminalHex0, TerminalHex1>))]
	public void NegativeAcceptsPositiveIntegerMagnitudes(Type magnitude)
	{
		Type result = typeof(Negative<>).MakeGenericType(magnitude);

		Assert.True(typeof(ITypeSystemInteger).IsAssignableFrom(result));
	}

	[Theory]
	[InlineData(typeof(TerminalHex0))]
	[InlineData(typeof(Negative<TerminalHex1>))]
	[InlineData(typeof(Fraction<TerminalHex1, TerminalHex2>))]
	public void NegativeRejectsZeroNegativeAndFractionalMagnitudes(Type magnitude)
	{
		Assert.Throws<ArgumentException>(() => typeof(Negative<>).MakeGenericType(magnitude));
	}

	[Theory]
	[InlineData(typeof(TerminalHex1), typeof(TerminalHex2))]
	[InlineData(typeof(Negative<TerminalHex1>), typeof(Hex<TerminalHex0, TerminalHex1>))]
	public void FractionAcceptsSignedIntegerNumeratorsAndPositiveDenominators(Type numerator, Type denominator)
	{
		Type result = typeof(Fraction<,>).MakeGenericType(numerator, denominator);

		Assert.True(typeof(ITypeSystemNumber).IsAssignableFrom(result));
	}

	[Theory]
	[InlineData(typeof(Fraction<TerminalHex1, TerminalHex2>), typeof(TerminalHex2))]
	[InlineData(typeof(TerminalHex1), typeof(TerminalHex0))]
	[InlineData(typeof(TerminalHex1), typeof(Negative<TerminalHex2>))]
	[InlineData(typeof(TerminalHex1), typeof(Fraction<TerminalHex1, TerminalHex2>))]
	public void FractionRejectsFractionalComponentsAndNonPositiveDenominators(Type numerator, Type denominator)
	{
		Assert.Throws<ArgumentException>(() => typeof(Fraction<,>).MakeGenericType(numerator, denominator));
	}
}
