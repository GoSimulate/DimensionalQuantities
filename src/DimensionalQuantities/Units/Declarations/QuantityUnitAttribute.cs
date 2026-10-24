namespace DimensionalQuantities.Units.Declarations;

/// <summary>Associates a unit declaration with a quantity type.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class QuantityUnitAttribute : Attribute
{
	public QuantityUnitAttribute(Type quantityType) => QuantityType = quantityType;

	public Type QuantityType { get; }

	/// <summary>Offset added after scaling a magnitude into coherent SI units.</summary>
	public double Offset { get; set; }
}
