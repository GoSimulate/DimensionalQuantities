using DimensionalQuantities.Units.Declarations;

namespace DimensionalQuantities.Extensions.CgsEmag;

internal static class CgsEmagSymbols
{
	[UnitSymbol("Dyne", "Dynes", "dyn", Scale = 1e-5)]
	internal sealed class Dyne { }
	[UnitSymbol("Erg", "Ergs", "erg", Scale = 1e-7)]
	internal sealed class Erg { }
	[UnitSymbol("Barye", "Baryes", "Ba", Scale = 0.1)]
	internal sealed class Barye { }
	[UnitSymbol("Gal", "Gals", "Gal", Scale = 0.01)]
	internal sealed class Gal { }
	[UnitSymbol("Abampere", "Abamperes", "abA", Scale = 10)]
	internal sealed class Abampere { }
	[UnitSymbol("Biot", "Biots", "Bi", Scale = 10)]
	internal sealed class Biot { }
	[UnitSymbol("Abcoulomb", "Abcoulombs", "abC", Scale = 10)]
	internal sealed class Abcoulomb { }
	[UnitSymbol("Abvolt", "Abvolts", "abV", Scale = 1e-8)]
	internal sealed class Abvolt { }
	[UnitSymbol("Abohm", "Abohms", "abΩ", Scale = 1e-9)]
	internal sealed class Abohm { }
	[UnitSymbol("Abhenry", "Abhenries", "abH", Scale = 1e-9)]
	internal sealed class Abhenry { }
	[UnitSymbol("Abfarad", "Abfarads", "abF", Scale = 1e9)]
	internal sealed class Abfarad { }
	[UnitSymbol("Maxwell", "Maxwells", "Mx", Scale = 1e-8)]
	internal sealed class Maxwell { }
	[UnitSymbol("Gauss", "Gauss", "G", Scale = 1e-4)]
	internal sealed class Gauss { }
	[UnitSymbol("Oersted", "Oersteds", "Oe", Scale = 79.57747154594767)]
	internal sealed class Oersted { }
}
