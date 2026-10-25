namespace DimensionalQuantities.Units.Declarations;

/// <summary>One ordered factor of a declared unit expression.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class UnitFactorAttribute : Attribute
{
	public UnitFactorAttribute(Type symbolType) => SymbolType = symbolType;

	public Type SymbolType { get; }
	public int Power { get; set; } = 1;
	public int PowerDenominator { get; set; } = 1;
	public int PrefixExponent { get; set; }
}
