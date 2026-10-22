using DimensionalQuantities.Numerics;
using DimensionalQuantities.Units;

namespace DimensionalQuantities.Dimensions;

internal readonly record struct DimensionComponent(Rational Exponent, UnitDefinition Unit);
