using DimensionalQuantities.Numerics;

namespace DimensionalQuantities.DimensionAnalysis;

internal readonly record struct DimensionComponent(Rational Exponent, UnitDefinition Unit);
