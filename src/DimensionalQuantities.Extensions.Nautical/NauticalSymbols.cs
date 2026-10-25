using DimensionalQuantities.Units.Declarations;

namespace DimensionalQuantities.Extensions.Nautical;

internal static class NauticalSymbols
{
	[UnitSymbol("NauticalMile", "NauticalMiles", "nmi", Scale = 1852)]
	internal sealed class NauticalMile { }

	[UnitSymbol("Fathom", "Fathoms", "ftm", Scale = 1.8288)]
	internal sealed class Fathom { }

	[UnitSymbol("Knot", "Knots", "kn", Scale = 1852d / 3600d)]
	internal sealed class Knot { }
}
