using JetBrains.Annotations;

namespace DimensionalQuantities.TypeSystem;

/// <summary>Encodes the seven SI exponents using shared canonical exponent types.</summary>
[UsedImplicitly]
public readonly struct Dimension<TLength, TMass, TTime, TElectricCurrent, TTemperature, TAmountOfSubstance, TLuminousIntensity>;
