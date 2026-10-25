namespace DimensionalQuantities.Units;

internal readonly record struct UnitConversion(UnitExpression Expression, double? OffsetBeforeScale = null);
