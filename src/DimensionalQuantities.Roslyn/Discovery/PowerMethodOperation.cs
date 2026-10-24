using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Numerics;
using DimensionalQuantities.TypeSystem;

namespace DimensionalQuantities.Roslyn.Discovery;

internal readonly record struct PowerMethodOperation(Dimension Operand, Rational Exponent, Dimension Result, string MethodName)
{
	public PowerOperation ToPowerOperation() => new(Operand, Exponent, Result,
		TypeSystemTypeNameFormatter.Format(TypeSystemNumberCodec.CreateType(Exponent)));
}
