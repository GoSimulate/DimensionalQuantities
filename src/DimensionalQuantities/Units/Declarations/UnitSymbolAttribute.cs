namespace DimensionalQuantities.Units.Declarations;

/// <summary>Defines the spelling and SI scale of an atomic unit.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class UnitSymbolAttribute : Attribute
{
	public UnitSymbolAttribute(string singular, string plural, string symbol)
	{
		Singular = singular;
		Plural = plural;
		Symbol = symbol;
	}

	public string Singular { get; }
	public string Plural { get; }
	public string Symbol { get; }
	public int SiBasePrefixExponent { get; set; }
	public double Scale { get; set; } = 1;
}
