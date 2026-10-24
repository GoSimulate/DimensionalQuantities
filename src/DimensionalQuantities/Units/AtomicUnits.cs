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
}
