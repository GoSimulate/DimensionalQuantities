namespace DimensionalQuantities.Units;

internal static class AtomicUnits
{
	public static readonly AtomicUnit Metre = new("Metre", "Metres", "m", SiBasePrefixExponents.Length);
	public static readonly AtomicUnit Gram = new("Gram", "Grams", "g", SiBasePrefixExponents.Mass);
	public static readonly AtomicUnit Second = new("Second", "Seconds", "s", SiBasePrefixExponents.Time);
	public static readonly AtomicUnit Amp = new("Amp", "Amps", "A", SiBasePrefixExponents.ElectricCurrent);
	public static readonly AtomicUnit Kelvin = new("Kelvin", "Kelvin", "K", SiBasePrefixExponents.Temperature);
	public static readonly AtomicUnit Mole = new("Mole", "Moles", "mol", SiBasePrefixExponents.AmountOfSubstance);
	public static readonly AtomicUnit Candela = new("Candela", "Candelas", "cd", SiBasePrefixExponents.LuminousIntensity);
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
