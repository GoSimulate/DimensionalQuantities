namespace DimensionalQuantities.Units;

internal readonly record struct UnitDefinition
{
	public UnitDefinition(string name, UnitExpression expression, UnitExpression[]? additionalConversions = null)
	{
		Name = name;
		Expression = expression;
		AdditionalConversions = additionalConversions ?? [];
	}

	public UnitDefinition(string name, UnitTerm[] terms) : this(name, new UnitExpression(terms)) { }

	public string Name { get; }
	public UnitExpression Expression { get; }
	public UnitExpression[] AdditionalConversions { get; }
}
