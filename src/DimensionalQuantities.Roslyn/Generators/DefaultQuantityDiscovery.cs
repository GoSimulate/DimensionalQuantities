using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.Numerics;

namespace DimensionalQuantities.Roslyn.Generators;

internal static class DefaultQuantityDiscovery
{
	public static QuantityGenerationPlan Discover()
	{
		QuantityGenerationPlan basePlan = QuantityDiscovery.Discover([]);
		List<PowerMethodOperation> operations = [];
		foreach (KeyValuePair<string, Rational> method in PowerMethodDiscovery.SupportedMethods)
		{
			foreach (PlannedQuantity quantity in basePlan.KnownQuantities)
			{
				operations.Add(new(quantity.Dimension, method.Value, quantity.Dimension.Pow(method.Value), method.Key));
			}
		}

		return QuantityDiscovery.Discover([], [], operations);
	}
}
