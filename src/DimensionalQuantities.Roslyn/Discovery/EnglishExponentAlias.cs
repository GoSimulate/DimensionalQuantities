using DimensionalQuantities.Numerics;

namespace DimensionalQuantities.Roslyn.Discovery;

internal readonly record struct EnglishExponentAlias(string Name, Rational Value, string EncodedTypeName);
