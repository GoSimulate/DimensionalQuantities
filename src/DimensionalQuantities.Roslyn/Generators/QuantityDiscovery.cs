using DimensionalQuantities.DimensionAnalysis;

namespace DimensionalQuantities.Roslyn.Generators;

internal static class QuantityDiscovery
{
	public static IReadOnlyDictionary<Dimension, UnitDefinition[]> Discover()
	{
		Dictionary<Dimension, UnitDefinition[]> quantities = new();
		Dimension[] baseDimensions =
		[
			new(Length: 1),
			new(Mass: 1),
			new(Time: 1),
			new(ElectricCurrent: 1),
			new(Temperature: 1),
			new(AmountOfSubstance: 1),
			new(LuminousIntensity: 1)
		];

		foreach (Dimension dimension in baseDimensions)
		{
			quantities.Add(dimension, [dimension.GetComponents().Single().Unit]);
		}

		foreach (KeyValuePair<Dimension, UnitDefinition[]> entry in EngineeringCatalogue.Entries)
		{
			quantities.Add(entry.Key, entry.Value);
		}

		return quantities;
	}
}
