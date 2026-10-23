using System.Collections.Immutable;
using DimensionalQuantities.Dimensions;

namespace DimensionalQuantities.Roslyn.Discovery;

internal sealed class QuantityGenerationPlan
{
	private readonly ImmutableDictionary<Dimension, string> _names;

	public QuantityGenerationPlan(ImmutableArray<PlannedQuantity> knownQuantities, ImmutableArray<PlannedQuantity> additionalQuantities,
		ImmutableArray<ArithmeticOperation> operations, ImmutableArray<PowerOperation> powerOperations)
	{
		KnownQuantities = knownQuantities;
		AdditionalQuantities = additionalQuantities;
		Operations = operations;
		PowerOperations = powerOperations;
		_names = knownQuantities.Concat(additionalQuantities).ToImmutableDictionary(quantity => quantity.Dimension, quantity => quantity.Name);
	}

	public ImmutableArray<PlannedQuantity> KnownQuantities { get; }
	public ImmutableArray<PlannedQuantity> AdditionalQuantities { get; }
	public ImmutableArray<ArithmeticOperation> Operations { get; }
	public ImmutableArray<PowerOperation> PowerOperations { get; }

	public string GetQuantityName(Dimension dimension) => _names[dimension];
}
