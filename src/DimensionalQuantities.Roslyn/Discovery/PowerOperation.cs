using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Numerics;

namespace DimensionalQuantities.Roslyn.Discovery;

internal readonly record struct PowerOperation(Dimension Operand, Rational Exponent, Dimension Result, string ExponentTypeName);
