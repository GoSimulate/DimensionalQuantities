namespace DimensionalQuantities.Numerics;

internal sealed class RationalNumberCantHaveComplexComponentsException : Exception
{
	public RationalNumberCantHaveComplexComponentsException() : base("A rational number must have integer numerator and denominator components. A value with a nonzero imaginary part cannot be represented as a rational number.")
	{
	}
}
