using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Numerics;

namespace DimensionalQuantities.Roslyn.Discovery;

internal readonly record struct PowerMethodOperation(Dimension Operand, Rational Exponent, Dimension Result, string MethodName);
