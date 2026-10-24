namespace DimensionalQuantities.Units.Declarations;

/// <summary>Sets conversion options shared by the quantity associations on a unit declaration.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class UnitConversionAttribute : Attribute
{
	/// <summary>Offset added after scaling a magnitude into coherent SI units.</summary>
	public double Offset { get; set; }
}
