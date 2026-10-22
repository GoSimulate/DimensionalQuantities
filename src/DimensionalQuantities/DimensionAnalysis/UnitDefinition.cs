namespace DimensionalQuantities.DimensionAnalysis;

internal readonly record struct UnitDefinition
{
	public UnitDefinition(
		string quantityName,
		string siUnitNameSingular,
		string siUnitNamePlural,
		string siUnitSymbol,
		string? prefixableUnitNameSingular = null,
		string? prefixableUnitNamePlural = null,
		string? prefixableUnitSymbol = null,
		int siBasePrefixExponent = 0)
	{
		QuantityName = quantityName;
		SiUnitNameSingular = siUnitNameSingular;
		SiUnitNamePlural = siUnitNamePlural;
		SiUnitSymbol = siUnitSymbol;
		PrefixableUnitNameSingular = prefixableUnitNameSingular ?? siUnitNameSingular;
		PrefixableUnitNamePlural = prefixableUnitNamePlural ?? siUnitNamePlural;
		PrefixableUnitSymbol = prefixableUnitSymbol ?? siUnitSymbol;
		SiBasePrefixExponent = siBasePrefixExponent;
	}

	public string QuantityName { get; }
	public string SiUnitNameSingular { get; }
	public string SiUnitNamePlural { get; }
	public string SiUnitSymbol { get; }
	public string PrefixableUnitNameSingular { get; }
	public string PrefixableUnitNamePlural { get; }
	public string PrefixableUnitSymbol { get; }
	public int SiBasePrefixExponent { get; }
}
