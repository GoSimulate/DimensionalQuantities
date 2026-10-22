namespace DimensionalQuantities.DimensionAnalysis;

internal readonly record struct UnitDefinition
{
	public UnitDefinition(string quantityName, UnitExpression expression, UnitExpression[]? additionalConversions = null)
	{
		QuantityName = quantityName;
		Expression = expression;
		AdditionalConversions = additionalConversions ?? [];
	}

	public UnitDefinition(string quantityName, UnitTerm[] terms) : this(quantityName, new UnitExpression(terms)) { }

	public string QuantityName { get; }
	public UnitExpression Expression { get; }
	public UnitExpression[] AdditionalConversions { get; }
}
