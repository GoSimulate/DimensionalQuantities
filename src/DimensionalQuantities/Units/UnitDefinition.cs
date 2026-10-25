namespace DimensionalQuantities.Units;

internal readonly record struct UnitDefinition
{
	public UnitDefinition(string name, UnitExpression expression, UnitConversion[]? additionalConversions = null,
		double? offsetBeforeScale = null)
	{
		Name = name;
		Expression = expression;
		AdditionalConversions = additionalConversions ?? [];
		OffsetBeforeScale = offsetBeforeScale;
	}

	public UnitDefinition(string name, UnitTerm[] terms) : this(name, new UnitExpression(terms)) { }

	public string Name { get; }
	public UnitExpression Expression { get; }
	public UnitConversion[] AdditionalConversions { get; }
	public double? OffsetBeforeScale { get; }
}
