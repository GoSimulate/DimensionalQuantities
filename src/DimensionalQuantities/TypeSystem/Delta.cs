namespace DimensionalQuantities.TypeSystem;

/// <summary>A quantity interpreted as a difference, retaining its magnitude in coherent SI base units.</summary>
public readonly struct Delta<TQuantity>
{
	private readonly double _valueInSiBaseUnit;

	internal Delta(double valueInSiBaseUnit) => _valueInSiBaseUnit = valueInSiBaseUnit;

	/// <summary>Returns the difference in coherent SI base units for its quantity dimension.</summary>
	public double AsSiBaseUnit() => _valueInSiBaseUnit;
}
