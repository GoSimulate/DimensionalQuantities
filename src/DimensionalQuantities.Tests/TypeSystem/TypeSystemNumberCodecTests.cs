using System.Numerics;
using DimensionalQuantities.Numerics;
using DimensionalQuantities.TypeSystem;

namespace DimensionalQuantities.Tests.TypeSystem;

public sealed class TypeSystemNumberCodecTests
{
	[Test]
	[Arguments(typeof(TerminalHex0), 0)]
	[Arguments(typeof(TerminalHex1), 1)]
	[Arguments(typeof(TerminalHex2), 2)]
	[Arguments(typeof(TerminalHex3), 3)]
	[Arguments(typeof(TerminalHex4), 4)]
	[Arguments(typeof(TerminalHex5), 5)]
	[Arguments(typeof(TerminalHex6), 6)]
	[Arguments(typeof(TerminalHex7), 7)]
	[Arguments(typeof(TerminalHex8), 8)]
	[Arguments(typeof(TerminalHex9), 9)]
	[Arguments(typeof(TerminalHexA), 10)]
	[Arguments(typeof(TerminalHexB), 11)]
	[Arguments(typeof(TerminalHexC), 12)]
	[Arguments(typeof(TerminalHexD), 13)]
	[Arguments(typeof(TerminalHexE), 14)]
	[Arguments(typeof(TerminalHexF), 15)]
	public async Task ParseDecodesEachTerminalDigit(Type type, int expected)
	{
		Rational expectedValue = new(expected);
		Rational result = TypeSystemNumberCodec.Parse(type);

		await Assert.That(result).IsEqualTo(expectedValue);
	}

	public static IEnumerable<(Func<object>, int, int)> NumberCases() =>
	[
		(() => TypeSystemNumberCodec.Parse<Hex<TerminalHex3, Hex<TerminalHex2, TerminalHex1>>>(), 291, 1),
		(() => TypeSystemNumberCodec.Parse<Negative<Hex<TerminalHexB, TerminalHex2>>>(), -43, 1),
		(() => TypeSystemNumberCodec.Parse<Fraction<TerminalHex2, TerminalHex4>>(), 1, 2),
		(() => TypeSystemNumberCodec.Parse<Fraction<Negative<TerminalHex3>, TerminalHex2>>(), -3, 2),
		(() => TypeSystemNumberCodec.Parse<Fraction<TerminalHex0, TerminalHex2>>(), 0, 1)
	];

	[Test]
	[MethodDataSource(nameof(NumberCases))]
	public async Task ParseGenericDecodesNumber(Func<object> parse, int numerator, int denominator)
	{
		Rational result = (Rational)parse();
		Rational expected = new(numerator, denominator);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task ParseTypeDecodesIntegerBeyondLongRange()
	{
		Type encoding = typeof(TerminalHex1);
		for (int index = 0; index < 16; index++)
		{
			encoding = typeof(Hex<,>).MakeGenericType(typeof(TerminalHex0), encoding);
		}

		Rational expected = new(BigInteger.One << 64);
		Rational result = TypeSystemNumberCodec.Parse(encoding);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	[Arguments(0, 1, typeof(TerminalHex0))]
	[Arguments(1, 1, typeof(TerminalHex1))]
	[Arguments(15, 1, typeof(TerminalHexF))]
	[Arguments(16, 1, typeof(Hex<TerminalHex0, TerminalHex1>))]
	[Arguments(291, 1, typeof(Hex<TerminalHex3, Hex<TerminalHex2, TerminalHex1>>))]
	[Arguments(-43, 1, typeof(Negative<Hex<TerminalHexB, TerminalHex2>>))]
	[Arguments(-3, 2, typeof(Fraction<Negative<TerminalHex3>, TerminalHex2>))]
	[Arguments(2, 4, typeof(Fraction<TerminalHex1, TerminalHex2>))]
	[Arguments(0, 2, typeof(TerminalHex0))]
	public async Task CreateTypeUsesCanonicalEncoding(int numerator, int denominator, Type expected)
	{
		Rational value = new(numerator, denominator);
		Type result = TypeSystemNumberCodec.CreateType(value);

		await Assert.That(result).IsEqualTo(expected);
	}

	[Test]
	public async Task CreateTypePreservesLargeExactFraction()
	{
		BigInteger numerator = (BigInteger.One << 80) + 3;
		BigInteger denominator = (BigInteger.One << 65) + 2;
		Rational expected = new(numerator, denominator);

		Type encoding = TypeSystemNumberCodec.CreateType(expected);
		Rational result = TypeSystemNumberCodec.Parse(encoding);

		using (Assert.Multiple())
		{
			await Assert.That(encoding.GetGenericTypeDefinition()).IsEqualTo(typeof(Fraction<,>));
			await Assert.That(result).IsEqualTo(expected);
		}
	}

	[Test]
	[Arguments(typeof(int))]
	[Arguments(typeof(CustomNumber))]
	[Arguments(typeof(Hex<CustomDigit, TerminalHex1>))]
	[Arguments(typeof(Hex<,>))]
	public async Task ParseTypeRejectsUnsupportedTypesAndMarkerImplementations(Type type)
	{
		await Assert.That(() => TypeSystemNumberCodec.Parse(type)).ThrowsExactly<ArgumentException>();
	}

	[Test]
	public async Task ParseGenericRejectsUnsupportedMarkerImplementation()
	{
		await Assert.That(() => TypeSystemNumberCodec.Parse<CustomNumber>()).ThrowsExactly<ArgumentException>();
	}

	private readonly struct CustomNumber : ITypeSystemNumber;

	private readonly struct CustomDigit : IHexDigit;
}
