using System.Numerics;
using DimensionalQuantities.Numerics;
using DimensionalQuantities.TypeSystem;
using Xunit;

namespace DimensionalQuantities.Tests.TypeSystem;

public sealed class TypeSystemNumberCodecTests
{
	[Theory]
	[InlineData(typeof(TerminalHex0), 0)]
	[InlineData(typeof(TerminalHex1), 1)]
	[InlineData(typeof(TerminalHex2), 2)]
	[InlineData(typeof(TerminalHex3), 3)]
	[InlineData(typeof(TerminalHex4), 4)]
	[InlineData(typeof(TerminalHex5), 5)]
	[InlineData(typeof(TerminalHex6), 6)]
	[InlineData(typeof(TerminalHex7), 7)]
	[InlineData(typeof(TerminalHex8), 8)]
	[InlineData(typeof(TerminalHex9), 9)]
	[InlineData(typeof(TerminalHexA), 10)]
	[InlineData(typeof(TerminalHexB), 11)]
	[InlineData(typeof(TerminalHexC), 12)]
	[InlineData(typeof(TerminalHexD), 13)]
	[InlineData(typeof(TerminalHexE), 14)]
	[InlineData(typeof(TerminalHexF), 15)]
	public void ParseDecodesEachTerminalDigit(Type type, int expected)
	{
		Rational expectedValue = new(expected);
		Rational result = TypeSystemNumberCodec.Parse(type);

		Assert.Equal(expectedValue, result);
	}

	public static TheoryData<ITypeSystemNumber, int, int> NumberCases => new()
	{
		{ new Hex<TerminalHex3, Hex<TerminalHex2, TerminalHex1>>(), 291, 1 },
		{ new Negative<Hex<TerminalHexB, TerminalHex2>>(), -43, 1 },
		{ new Fraction<TerminalHex2, TerminalHex4>(), 1, 2 },
		{ new Fraction<Negative<TerminalHex3>, TerminalHex2>(), -3, 2 },
		{ new Fraction<TerminalHex0, TerminalHex2>(), 0, 1 }
	};

	[Theory]
	[MemberData(nameof(NumberCases))]
	public void ParseGenericDecodesNumber<T>(T _, int numerator, int denominator) where T : ITypeSystemNumber
	{
		Rational result = TypeSystemNumberCodec.Parse<T>();
		Rational expected = new(numerator, denominator);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void ParseTypeDecodesIntegerBeyondLongRange()
	{
		Type encoding = typeof(TerminalHex1);
		for (int index = 0; index < 16; index++)
		{
			encoding = typeof(Hex<,>).MakeGenericType(typeof(TerminalHex0), encoding);
		}

		Rational expected = new(BigInteger.One << 64);
		Rational result = TypeSystemNumberCodec.Parse(encoding);

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData(0, 1, typeof(TerminalHex0))]
	[InlineData(1, 1, typeof(TerminalHex1))]
	[InlineData(15, 1, typeof(TerminalHexF))]
	[InlineData(16, 1, typeof(Hex<TerminalHex0, TerminalHex1>))]
	[InlineData(291, 1, typeof(Hex<TerminalHex3, Hex<TerminalHex2, TerminalHex1>>))]
	[InlineData(-43, 1, typeof(Negative<Hex<TerminalHexB, TerminalHex2>>))]
	[InlineData(-3, 2, typeof(Fraction<Negative<TerminalHex3>, TerminalHex2>))]
	[InlineData(2, 4, typeof(Fraction<TerminalHex1, TerminalHex2>))]
	[InlineData(0, 2, typeof(TerminalHex0))]
	public void CreateTypeUsesCanonicalEncoding(int numerator, int denominator, Type expected)
	{
		Rational value = new(numerator, denominator);
		Type result = TypeSystemNumberCodec.CreateType(value);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void CreateTypePreservesLargeExactFraction()
	{
		BigInteger numerator = (BigInteger.One << 80) + 3;
		BigInteger denominator = (BigInteger.One << 65) + 2;
		Rational expected = new(numerator, denominator);

		Type encoding = TypeSystemNumberCodec.CreateType(expected);
		Rational result = TypeSystemNumberCodec.Parse(encoding);

		Assert.Multiple(
			() => Assert.Equal(typeof(Fraction<,>), encoding.GetGenericTypeDefinition()),
			() => Assert.Equal(expected, result));
	}

	[Theory]
	[InlineData(typeof(int))]
	[InlineData(typeof(CustomNumber))]
	[InlineData(typeof(Hex<CustomDigit, TerminalHex1>))]
	[InlineData(typeof(Hex<,>))]
	public void ParseTypeRejectsUnsupportedTypesAndMarkerImplementations(Type type)
	{
		Assert.Throws<ArgumentException>(() => TypeSystemNumberCodec.Parse(type));
	}

	[Fact]
	public void ParseGenericRejectsUnsupportedMarkerImplementation()
	{
		Assert.Throws<ArgumentException>(() => TypeSystemNumberCodec.Parse<CustomNumber>());
	}

	private readonly struct CustomNumber : ITypeSystemNumber;

	private readonly struct CustomDigit : IHexDigit;
}
