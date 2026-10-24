namespace DimensionalQuantities.Units.Declarations;

/// <summary>Associates a unit declaration with a quantity type.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class QuantityUnitAttribute : Attribute
{
	public QuantityUnitAttribute(Type quantityType) => QuantityType = quantityType;

	public Type QuantityType { get; }
}
