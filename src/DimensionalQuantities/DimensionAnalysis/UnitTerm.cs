namespace DimensionalQuantities.DimensionAnalysis;

internal readonly record struct UnitTerm(AtomicUnit Unit, int Power = 1, int PrefixExponent = 0);
