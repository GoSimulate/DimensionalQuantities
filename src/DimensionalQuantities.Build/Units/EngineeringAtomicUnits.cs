using DimensionalQuantities.Units;

namespace DimensionalQuantities.Build.Units;

internal static class EngineeringAtomicUnits
{
	public static readonly AtomicUnit Radian = new("Radian", "Radians", "rad");
	public static readonly AtomicUnit Hertz = new("Hertz", "Hertz", "Hz");
	public static readonly AtomicUnit Pascal = new("Pascal", "Pascals", "Pa");
	public static readonly AtomicUnit Joule = new("Joule", "Joules", "J");
	public static readonly AtomicUnit Newton = new("Newton", "Newtons", "N");
	public static readonly AtomicUnit Watt = new("Watt", "Watts", "W");
	public static readonly AtomicUnit Volt = new("Volt", "Volts", "V");
	public static readonly AtomicUnit Ohm = new("Ohm", "Ohms", "Ω");
	public static readonly AtomicUnit Henry = new("Henry", "Henries", "H");
	public static readonly AtomicUnit Weber = new("Weber", "Webers", "Wb");
	public static readonly AtomicUnit Tesla = new("Tesla", "Teslas", "T");
	public static readonly AtomicUnit Siemens = new("Siemens", "Siemens", "S");
}
