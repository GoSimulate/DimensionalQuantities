using JetBrains.Annotations;

namespace DimensionalQuantities.TypeSystem;

/// <summary>A quantity whose dimension is encoded in its type and whose magnitude is stored in SI units.</summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public readonly record struct Quantity<TDimension>
{
	/// <summary>Creates a quantity from a magnitude expressed in coherent SI base units for its dimension.</summary>
	public static Quantity<TDimension> FromSiBaseUnit(double value) => new(value);

	internal Quantity(double valueInSiBaseUnit)
	{
		ValueInSiBaseUnit = valueInSiBaseUnit;
	}

	internal double ValueInSiBaseUnit { get; }

	/// <summary>Returns the magnitude in coherent SI base units for its dimension.</summary>
	public double AsSiBaseUnit() => ValueInSiBaseUnit;
}
