using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.TypeSystem;

namespace DimensionalQuantities.Roslyn.Generators;

internal static class QuantityTypeNameFormatter
{
	public static string Format(Dimension dimension)
	{
		Type[] exponents =
		[
			TypeSystemNumberCodec.CreateType(dimension.Length),
			TypeSystemNumberCodec.CreateType(dimension.Mass),
			TypeSystemNumberCodec.CreateType(dimension.Time),
			TypeSystemNumberCodec.CreateType(dimension.ElectricCurrent),
			TypeSystemNumberCodec.CreateType(dimension.Temperature),
			TypeSystemNumberCodec.CreateType(dimension.AmountOfSubstance),
			TypeSystemNumberCodec.CreateType(dimension.LuminousIntensity)
		];
		return $"global::DimensionalQuantities.TypeSystem.Quantity<global::DimensionalQuantities.TypeSystem.Dimension<{string.Join(", ", exponents.Select(TypeSystemTypeNameFormatter.Format))}>>";
	}
}
