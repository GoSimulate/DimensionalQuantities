namespace DimensionalQuantities.Units;

internal readonly record struct UnitDefinition
{
	public UnitDefinition(string name, UnitExpression expression, UnitExpression[]? additionalConversions = null, double offset = 0)
	{
		Name = name;
		Expression = expression;
		AdditionalConversions = additionalConversions ?? [];
		Offset = offset;
	}

	public UnitDefinition(string name, UnitTerm[] terms) : this(name, new UnitExpression(terms)) { }

	public string Name { get; }
	public UnitExpression Expression { get; }
	public UnitExpression[] AdditionalConversions { get; }
	public double Offset { get; }
}
