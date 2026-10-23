using JetBrains.Annotations;

namespace DimensionalQuantities.TypeSystem;

/// <summary>Specifies the exponent type used by generated <c>Of(quantity)</c> methods.</summary>
[UsedImplicitly]
public readonly struct Power<TExponent> where TExponent : struct, ITypeSystemNumber;
