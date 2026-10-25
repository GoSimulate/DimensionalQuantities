namespace DimensionalQuantities.Units.Declarations;

/// <summary>Adds a separately declared unit expression for a quantity.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class AdditionalUnitAttribute : Attribute
{
	public AdditionalUnitAttribute(Type unitType) => UnitType = unitType;

	public Type UnitType { get; }
}
