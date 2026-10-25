namespace DimensionalQuantities.Units.Declarations;

/// <summary>Defines an offset applied before scaling a unit value to coherent SI base units.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class AffineUnitAttribute : Attribute
{
	public AffineUnitAttribute(double offsetBeforeScale) => OffsetBeforeScale = offsetBeforeScale;

	public double OffsetBeforeScale { get; }
}
