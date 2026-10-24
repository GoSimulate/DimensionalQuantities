namespace DimensionalQuantities.Units;

internal readonly record struct UnitDefinition
{
	public UnitDefinition(string quantityName, UnitExpression expression, UnitExpression[]? additionalConversions = null, double offset = 0)
	{
		QuantityName = quantityName;
		Expression = expression;
		AdditionalConversions = additionalConversions ?? [];
		Offset = offset;
	}

	public UnitDefinition(string quantityName, UnitTerm[] terms) : this(quantityName, new UnitExpression(terms)) { }

	public string QuantityName { get; }
	public UnitExpression Expression { get; }
	public UnitExpression[] AdditionalConversions { get; }
	public double Offset { get; }
}
