using DimensionalQuantities.Dimensions;

namespace DimensionalQuantities.Roslyn.Discovery;

internal readonly record struct QuantityOperand(QuantityOperandKind Kind, Dimension Dimension = default);
