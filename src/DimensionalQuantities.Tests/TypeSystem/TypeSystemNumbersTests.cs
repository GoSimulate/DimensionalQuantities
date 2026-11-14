using DimensionalQuantities.TypeSystem;

namespace DimensionalQuantities.Tests.TypeSystem;

public sealed class TypeSystemNumbersTests
{
	[Test]
	[Arguments(typeof(TerminalHex0), typeof(TerminalHex1))]
	[Arguments(typeof(TerminalHexF), typeof(Hex<TerminalHex0, TerminalHex1>))]
	public async Task HexAcceptsDigitsAndPositiveRemainders(Type digit, Type remainder)
	{
		Type result = typeof(Hex<,>).MakeGenericType(digit, remainder);

		await Assert.That(typeof(IPositiveTypeSystemInteger).IsAssignableFrom(result)).IsTrue();
	}

	[Test]
	[Arguments(typeof(int), typeof(TerminalHex1))]
	[Arguments(typeof(Hex<TerminalHex0, TerminalHex1>), typeof(TerminalHex1))]
	[Arguments(typeof(Fraction<TerminalHex1, TerminalHex2>), typeof(TerminalHex1))]
	[Arguments(typeof(TerminalHexC), typeof(TerminalHex0))]
	[Arguments(typeof(TerminalHexC), typeof(Negative<TerminalHex1>))]
	public async Task HexRejectsNonDigitsAndNonPositiveRemainders(Type digit, Type remainder)
	{
		await Assert.That(() => typeof(Hex<,>).MakeGenericType(digit, remainder)).ThrowsExactly<ArgumentException>();
	}

	[Test]
	[Arguments(typeof(TerminalHexC))]
	[Arguments(typeof(Hex<TerminalHex0, TerminalHex1>))]
	public async Task NegativeAcceptsPositiveIntegerMagnitudes(Type magnitude)
	{
		Type result = typeof(Negative<>).MakeGenericType(magnitude);

		await Assert.That(typeof(ITypeSystemInteger).IsAssignableFrom(result)).IsTrue();
	}

	[Test]
	[Arguments(typeof(TerminalHex0))]
	[Arguments(typeof(Negative<TerminalHex1>))]
	[Arguments(typeof(Fraction<TerminalHex1, TerminalHex2>))]
	public async Task NegativeRejectsZeroNegativeAndFractionalMagnitudes(Type magnitude)
	{
		await Assert.That(() => typeof(Negative<>).MakeGenericType(magnitude)).ThrowsExactly<ArgumentException>();
	}

	[Test]
	[Arguments(typeof(TerminalHex1), typeof(TerminalHex2))]
	[Arguments(typeof(Negative<TerminalHex1>), typeof(Hex<TerminalHex0, TerminalHex1>))]
	public async Task FractionAcceptsSignedIntegerNumeratorsAndPositiveDenominators(Type numerator, Type denominator)
	{
		Type result = typeof(Fraction<,>).MakeGenericType(numerator, denominator);

		await Assert.That(typeof(ITypeSystemNumber).IsAssignableFrom(result)).IsTrue();
	}

	[Test]
	[Arguments(typeof(Fraction<TerminalHex1, TerminalHex2>), typeof(TerminalHex2))]
	[Arguments(typeof(TerminalHex1), typeof(TerminalHex0))]
	[Arguments(typeof(TerminalHex1), typeof(Negative<TerminalHex2>))]
	[Arguments(typeof(TerminalHex1), typeof(Fraction<TerminalHex1, TerminalHex2>))]
	public async Task FractionRejectsFractionalComponentsAndNonPositiveDenominators(Type numerator, Type denominator)
	{
		await Assert.That(() => typeof(Fraction<,>).MakeGenericType(numerator, denominator)).ThrowsExactly<ArgumentException>();
	}
}
