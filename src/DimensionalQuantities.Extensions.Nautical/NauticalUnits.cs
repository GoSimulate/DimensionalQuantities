using DimensionalQuantities.Units.Declarations;

namespace DimensionalQuantities.Extensions.Nautical;

[UnitProvider]
internal static class NauticalUnits
{
	[QuantityUnit(typeof(Length))]
	[UnitFactor(typeof(NauticalSymbols.NauticalMile))]
	private sealed class NauticalMileUnit { }

	[QuantityUnit(typeof(Length))]
	[UnitFactor(typeof(NauticalSymbols.Fathom))]
	private sealed class FathomUnit { }

	[QuantityUnit(typeof(Area))]
	[UnitFactor(typeof(NauticalSymbols.NauticalMile), Power = 2)]
	private sealed class SquareNauticalMileUnit { }

	[QuantityUnit(typeof(Speed))]
	[UnitFactor(typeof(NauticalSymbols.Knot))]
	private sealed class KnotUnit { }
}
