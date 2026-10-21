using DimensionalQuantities.Numerics;

namespace DimensionalQuantities.DimensionAnalysis;

internal readonly record struct DimensionComponent(Rational Exponent, string QuantityName, string UnitNameSingular, string UnitNamePlural);
