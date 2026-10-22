using System.Collections.Immutable;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Naming;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.Units;

namespace DimensionalQuantities.Roslyn.Generators;

internal static class QuantityDiscovery
{
	public static QuantityGenerationPlan Discover(IEnumerable<ArithmeticOperation> operations)
	{
		List<PlannedQuantity> knownQuantities = [];
		List<PlannedQuantity> additionalQuantities = [];
		HashSet<Dimension> dimensions = [];
		HashSet<(ArithmeticOperationKind Kind, Dimension Left, Dimension Right)> seenOperations = [];
		List<ArithmeticOperation> orderedOperations = [];
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
			knownQuantities.Add(new(dimension, [dimension.GetComponents().Single().Unit], true));
			dimensions.Add(dimension);
		}

		foreach (KeyValuePair<Dimension, UnitDefinition[]> entry in EngineeringCatalogue.Entries)
		{
			knownQuantities.Add(new(entry.Key, [.. entry.Value], true));
			dimensions.Add(entry.Key);
		}

		foreach (ArithmeticOperation operation in operations)
		{
			if (!seenOperations.Add((operation.Kind, operation.Left, operation.Right)))
			{
				continue;
			}

			orderedOperations.Add(operation);
			AddUnknown(operation.Left);
			AddUnknown(operation.Right);
			AddUnknown(operation.Result);
		}

		return new(knownQuantities.ToImmutableArray(), additionalQuantities.ToImmutableArray(), orderedOperations.ToImmutableArray());

		void AddUnknown(Dimension dimension)
		{
			if (dimension == default || !dimensions.Add(dimension))
			{
				return;
			}

			UnitTerm[] terms = dimension.GetComponents()
				.Select(component => component.Unit.Expression.Terms.Single() with { Power = component.Exponent }).ToArray();
			UnitDefinition unit = new(DimensionNameGenerator.GetQuantityName(dimension), terms);
			additionalQuantities.Add(new(dimension, [unit], false));
		}
	}
}
