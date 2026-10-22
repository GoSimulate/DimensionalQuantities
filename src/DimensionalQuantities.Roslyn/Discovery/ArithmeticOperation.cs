using DimensionalQuantities.Dimensions;

namespace DimensionalQuantities.Roslyn.Discovery;

internal readonly record struct ArithmeticOperation(ArithmeticOperationKind Kind, Dimension Left, Dimension Right, Dimension Result);
