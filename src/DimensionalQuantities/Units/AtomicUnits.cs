namespace DimensionalQuantities.Units;

internal static class AtomicUnits
{
	public static readonly AtomicUnit Metre = new(SiUnitMetadata.Metre.Singular, SiUnitMetadata.Metre.Plural,
		SiUnitMetadata.Metre.Symbol, SiBasePrefixExponents.Length);
	public static readonly AtomicUnit Gram = new(SiUnitMetadata.Gram.Singular, SiUnitMetadata.Gram.Plural,
		SiUnitMetadata.Gram.Symbol, SiBasePrefixExponents.Mass);
	public static readonly AtomicUnit Second = new(SiUnitMetadata.Second.Singular, SiUnitMetadata.Second.Plural,
		SiUnitMetadata.Second.Symbol, SiBasePrefixExponents.Time);
	public static readonly AtomicUnit Amp = new(SiUnitMetadata.Amp.Singular, SiUnitMetadata.Amp.Plural,
		SiUnitMetadata.Amp.Symbol, SiBasePrefixExponents.ElectricCurrent);
	public static readonly AtomicUnit Kelvin = new(SiUnitMetadata.Kelvin.Singular, SiUnitMetadata.Kelvin.Plural,
		SiUnitMetadata.Kelvin.Symbol, SiBasePrefixExponents.Temperature);
	public static readonly AtomicUnit Mole = new(SiUnitMetadata.Mole.Singular, SiUnitMetadata.Mole.Plural,
		SiUnitMetadata.Mole.Symbol, SiBasePrefixExponents.AmountOfSubstance);
	public static readonly AtomicUnit Candela = new(SiUnitMetadata.Candela.Singular, SiUnitMetadata.Candela.Plural,
		SiUnitMetadata.Candela.Symbol, SiBasePrefixExponents.LuminousIntensity);
}
