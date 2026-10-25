using DimensionalQuantities.Units.Declarations;

namespace DimensionalQuantities.Extensions.Imperial;

internal static class ImperialSymbols
{
	[UnitSymbol("Inch", "Inches", "in", Scale = 0.0254)]
	internal sealed class Inch { }
	[UnitSymbol("Foot", "Feet", "ft", Scale = 0.3048)]
	internal sealed class Foot { }
	[UnitSymbol("Yard", "Yards", "yd", Scale = 0.9144)]
	internal sealed class Yard { }
	[UnitSymbol("Mile", "Miles", "mi", Scale = 1609.344)]
	internal sealed class Mile { }
	[UnitSymbol("Chain", "Chains", "ch", Scale = 20.1168)]
	internal sealed class Chain { }
	[UnitSymbol("Furlong", "Furlongs", "fur", Scale = 201.168)]
	internal sealed class Furlong { }
	[UnitSymbol("Acre", "Acres", "ac", Scale = 4046.8564224)]
	internal sealed class Acre { }
	[UnitSymbol("USGallon", "USGallons", "gal (US)", Scale = 0.003785411784)]
	internal sealed class USGallon { }
	[UnitSymbol("USQuart", "USQuarts", "qt (US)", Scale = 0.000946352946)]
	internal sealed class USQuart { }
	[UnitSymbol("USPint", "USPints", "pt (US)", Scale = 0.000473176473)]
	internal sealed class USPint { }
	[UnitSymbol("USFluidOunce", "USFluidOunces", "fl oz (US)", Scale = 0.0000295735295625)]
	internal sealed class USFluidOunce { }
	[UnitSymbol("ImperialGallon", "ImperialGallons", "gal (imp)", Scale = 0.00454609)]
	internal sealed class ImperialGallon { }
	[UnitSymbol("ImperialQuart", "ImperialQuarts", "qt (imp)", Scale = 0.0011365225)]
	internal sealed class ImperialQuart { }
	[UnitSymbol("ImperialPint", "ImperialPints", "pt (imp)", Scale = 0.00056826125)]
	internal sealed class ImperialPint { }
	[UnitSymbol("ImperialFluidOunce", "ImperialFluidOunces", "fl oz (imp)", Scale = 0.0000284130625)]
	internal sealed class ImperialFluidOunce { }
	[UnitSymbol("Ounce", "Ounces", "oz", Scale = 0.028349523125)]
	internal sealed class Ounce { }
	[UnitSymbol("Pound", "Pounds", "lb", Scale = 0.45359237)]
	internal sealed class Pound { }
	[UnitSymbol("Stone", "Stones", "st", Scale = 6.35029318)]
	internal sealed class Stone { }
	[UnitSymbol("USShortTon", "USShortTons", "ton (US)", Scale = 907.18474)]
	internal sealed class USShortTon { }
	[UnitSymbol("ImperialLongTon", "ImperialLongTons", "ton (imp)", Scale = 1016.0469088)]
	internal sealed class ImperialLongTon { }
	[UnitSymbol("Hour", "Hours", "h", Scale = 3600)]
	internal sealed class Hour { }
	[UnitSymbol("PoundForce", "PoundForces", "lbf", Scale = 4.4482216152605)]
	internal sealed class PoundForce { }
	[UnitSymbol("Psi", "Psi", "psi", Scale = 4.4482216152605 / (0.0254 * 0.0254))]
	internal sealed class Psi { }
	[UnitSymbol("BritishThermalUnitIT", "BritishThermalUnitsIT", "Btu(IT)", Scale = 1055.05585262)]
	internal sealed class BritishThermalUnitIT { }
	[UnitSymbol("MechanicalHorsepower", "MechanicalHorsepower", "hp", Scale = 745.69987158227022)]
	internal sealed class MechanicalHorsepower { }
	[UnitSymbol("Rankine", "Rankine", "°R", Scale = 5d / 9d)]
	internal sealed class Rankine { }
}
