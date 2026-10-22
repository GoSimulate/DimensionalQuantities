using DimensionalQuantities.Numerics;

namespace DimensionalQuantities.Units;

internal readonly record struct UnitTerm
{
	public UnitTerm(AtomicUnit unit, Rational? power = null, int prefixExponent = 0)
	{
		Unit = unit;
		Power = power ?? Rational.One;
		PrefixExponent = prefixExponent;
	}

	public AtomicUnit Unit { get; init; }
	public Rational Power { get; init; }
	public int PrefixExponent { get; init; }
}
