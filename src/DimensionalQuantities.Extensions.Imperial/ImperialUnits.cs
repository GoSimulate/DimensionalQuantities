using DimensionalQuantities.Units.Declarations;
using DimensionalQuantities.Units.Symbols;

namespace DimensionalQuantities.Extensions.Imperial;

[UnitProvider]
internal static class ImperialUnits
{
	[QuantityUnit(typeof(Length))]
	[UnitFactor(typeof(ImperialSymbols.Foot))]
	[AdditionalUnit(typeof(InchUnit))]
	[AdditionalUnit(typeof(YardUnit))]
	[AdditionalUnit(typeof(MileUnit))]
	[AdditionalUnit(typeof(ChainUnit))]
	[AdditionalUnit(typeof(FurlongUnit))]
	private sealed class LengthUnit { }
	[UnitFactor(typeof(ImperialSymbols.Inch))]
	private sealed class InchUnit { }
	[UnitFactor(typeof(ImperialSymbols.Yard))]
	private sealed class YardUnit { }
	[UnitFactor(typeof(ImperialSymbols.Mile))]
	private sealed class MileUnit { }
	[UnitFactor(typeof(ImperialSymbols.Chain))]
	private sealed class ChainUnit { }
	[UnitFactor(typeof(ImperialSymbols.Furlong))]
	private sealed class FurlongUnit { }

	[QuantityUnit(typeof(Area))]
	[UnitFactor(typeof(ImperialSymbols.Foot), Power = 2)]
	[AdditionalUnit(typeof(AcreUnit))]
	private sealed class AreaUnit { }
	[UnitFactor(typeof(ImperialSymbols.Acre))]
	private sealed class AcreUnit { }

	[QuantityUnit(typeof(Volume))]
	[UnitFactor(typeof(ImperialSymbols.Foot), Power = 3)]
	[AdditionalUnit(typeof(CubicInchUnit))]
	[AdditionalUnit(typeof(CubicYardUnit))]
	[AdditionalUnit(typeof(USGallonUnit))]
	[AdditionalUnit(typeof(USQuartUnit))]
	[AdditionalUnit(typeof(USPintUnit))]
	[AdditionalUnit(typeof(USFluidOunceUnit))]
	[AdditionalUnit(typeof(ImperialGallonUnit))]
	[AdditionalUnit(typeof(ImperialQuartUnit))]
	[AdditionalUnit(typeof(ImperialPintUnit))]
	[AdditionalUnit(typeof(ImperialFluidOunceUnit))]
	private sealed class VolumeUnit { }
	[UnitFactor(typeof(ImperialSymbols.Inch), Power = 3)]
	private sealed class CubicInchUnit { }
	[UnitFactor(typeof(ImperialSymbols.Yard), Power = 3)]
	private sealed class CubicYardUnit { }
	[UnitFactor(typeof(ImperialSymbols.USGallon))]
	private sealed class USGallonUnit { }
	[UnitFactor(typeof(ImperialSymbols.USQuart))]
	private sealed class USQuartUnit { }
	[UnitFactor(typeof(ImperialSymbols.USPint))]
	private sealed class USPintUnit { }
	[UnitFactor(typeof(ImperialSymbols.USFluidOunce))]
	private sealed class USFluidOunceUnit { }
	[UnitFactor(typeof(ImperialSymbols.ImperialGallon))]
	private sealed class ImperialGallonUnit { }
	[UnitFactor(typeof(ImperialSymbols.ImperialQuart))]
	private sealed class ImperialQuartUnit { }
	[UnitFactor(typeof(ImperialSymbols.ImperialPint))]
	private sealed class ImperialPintUnit { }
	[UnitFactor(typeof(ImperialSymbols.ImperialFluidOunce))]
	private sealed class ImperialFluidOunceUnit { }

	[QuantityUnit(typeof(Mass))]
	[UnitFactor(typeof(ImperialSymbols.Pound))]
	[AdditionalUnit(typeof(OunceUnit))]
	[AdditionalUnit(typeof(StoneUnit))]
	[AdditionalUnit(typeof(USShortTonUnit))]
	[AdditionalUnit(typeof(ImperialLongTonUnit))]
	private sealed class MassUnit { }
	[UnitFactor(typeof(ImperialSymbols.Ounce))]
	private sealed class OunceUnit { }
	[UnitFactor(typeof(ImperialSymbols.Stone))]
	private sealed class StoneUnit { }
	[UnitFactor(typeof(ImperialSymbols.USShortTon))]
	private sealed class USShortTonUnit { }
	[UnitFactor(typeof(ImperialSymbols.ImperialLongTon))]
	private sealed class ImperialLongTonUnit { }

	[QuantityUnit(typeof(Speed))]
	[UnitFactor(typeof(ImperialSymbols.Mile))]
	[UnitFactor(typeof(ImperialSymbols.Hour), Power = -1)]
	[AdditionalUnit(typeof(FeetPerSecondUnit))]
	private sealed class SpeedUnit { }
	[UnitFactor(typeof(ImperialSymbols.Foot))]
	[UnitFactor(typeof(Second), Power = -1)]
	private sealed class FeetPerSecondUnit { }

	[QuantityUnit(typeof(Force))]
	[UnitFactor(typeof(ImperialSymbols.PoundForce))]
	private sealed class ForceUnit { }

	[QuantityUnit(typeof(Pressure))]
	[QuantityUnit(typeof(Stress))]
	[QuantityUnit(typeof(YoungsModulus))]
	[UnitFactor(typeof(ImperialSymbols.Psi))]
	private sealed class PressureUnit { }

	[QuantityUnit(typeof(Energy))]
	[UnitFactor(typeof(ImperialSymbols.Foot))]
	[UnitFactor(typeof(ImperialSymbols.PoundForce))]
	[AdditionalUnit(typeof(BritishThermalUnitITUnit))]
	private sealed class EnergyUnit { }
	[UnitFactor(typeof(ImperialSymbols.BritishThermalUnitIT))]
	private sealed class BritishThermalUnitITUnit { }

	[QuantityUnit(typeof(Power))]
	[UnitFactor(typeof(ImperialSymbols.MechanicalHorsepower))]
	[AdditionalUnit(typeof(BritishThermalUnitsITPerHourUnit))]
	private sealed class PowerUnit { }
	[UnitFactor(typeof(ImperialSymbols.BritishThermalUnitIT))]
	[UnitFactor(typeof(ImperialSymbols.Hour), Power = -1)]
	private sealed class BritishThermalUnitsITPerHourUnit { }

	[QuantityUnit(typeof(Temperature))]
	[UnitFactor(typeof(ImperialSymbols.Rankine))]
	[AdditionalUnit(typeof(FahrenheitUnit))]
	private sealed class TemperatureUnit { }
	[UnitFactor(typeof(ImperialSymbols.Fahrenheit))]
	private sealed class FahrenheitUnit { }
}
