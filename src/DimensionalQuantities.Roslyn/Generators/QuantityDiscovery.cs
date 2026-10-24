using System.Collections.Immutable;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Naming;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.Units;

namespace DimensionalQuantities.Roslyn.Generators;

internal static class QuantityDiscovery
{
	public static QuantityGenerationPlan Discover(IEnumerable<ArithmeticOperation> operations) => Discover(operations, [], []);

	public static QuantityGenerationPlan Discover(IEnumerable<ArithmeticOperation> operations, IEnumerable<PowerOperation> powerOperations) => Discover(operations, powerOperations, []);

	public static QuantityGenerationPlan Discover(IEnumerable<ArithmeticOperation> operations, IEnumerable<PowerOperation> powerOperations,
		IEnumerable<PowerMethodOperation> powerMethodOperations)
		=> Discover([], [], [], operations, powerOperations, powerMethodOperations, []);

	public static QuantityGenerationPlan DiscoverWithPredefinedQuantities(QuantityGenerationPlan predefinedPlan,
		IEnumerable<ArithmeticOperation> operations,
		IEnumerable<PowerOperation> powerOperations, IEnumerable<PowerMethodOperation> powerMethodOperations)
		=> DiscoverWithPredefinedQuantities(predefinedPlan, operations, powerOperations, powerMethodOperations, []);

	public static QuantityGenerationPlan DiscoverWithPredefinedQuantities(QuantityGenerationPlan predefinedPlan,
		IEnumerable<ArithmeticOperation> operations, IEnumerable<PowerOperation> powerOperations,
		IEnumerable<PowerMethodOperation> powerMethodOperations, IEnumerable<Dimension> requestedDimensions)
		=> Discover(predefinedPlan.AdditionalQuantities, predefinedPlan.PowerOperations, predefinedPlan.PowerMethodOperations,
			operations, powerOperations, powerMethodOperations, requestedDimensions);

	private static QuantityGenerationPlan Discover(IReadOnlyList<PlannedQuantity> predefinedQuantities,
		IReadOnlyList<PowerOperation> predefinedPowers, IReadOnlyList<PowerMethodOperation> predefinedPowerMethods,
		IEnumerable<ArithmeticOperation> operations,
		IEnumerable<PowerOperation> powerOperations, IEnumerable<PowerMethodOperation> powerMethodOperations,
		IEnumerable<Dimension> requestedDimensions)
	{
		List<PlannedQuantity> knownQuantities = [];
		List<PlannedQuantity> additionalQuantities = [];
		HashSet<Dimension> dimensions = [];
		HashSet<(ArithmeticOperationKind Kind, Dimension Left, Dimension Right)> seenOperations = [];
		List<ArithmeticOperation> orderedOperations = [];
		HashSet<(Dimension Operand, string ExponentTypeName)> seenPowers = new(predefinedPowers
			.Select(operation => (operation.Operand, operation.ExponentTypeName)));
		List<PowerOperation> orderedPowers = [];
		HashSet<(Dimension Operand, string MethodName)> seenPowerMethods = new(predefinedPowerMethods
			.Select(operation => (operation.Operand, operation.MethodName)));
		List<PowerMethodOperation> orderedPowerMethods = [];
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

		foreach (KeyValuePair<Dimension, string[]> entry in EngineeringQuantityAliases.Entries)
		{
			knownQuantities.Add(new(entry.Key, [.. entry.Value], [], false));
			dimensions.Add(entry.Key);
		}

		foreach (PlannedQuantity quantity in predefinedQuantities)
		{
			if (dimensions.Add(quantity.Dimension))
			{
				knownQuantities.Add(quantity);
			}
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

		foreach (PowerOperation operation in powerOperations)
		{
			AddPower(operation);
		}

		foreach (PowerMethodOperation operation in powerMethodOperations)
		{
			if (!seenPowerMethods.Add((operation.Operand, operation.MethodName)))
			{
				continue;
			}

			orderedPowerMethods.Add(operation);
			AddUnknown(operation.Operand);
			AddUnknown(operation.Result);
			AddPower(operation.ToPowerOperation());
		}

		foreach (Dimension dimension in requestedDimensions)
		{
			AddUnknown(dimension);
		}

		return new(knownQuantities.ToImmutableArray(), additionalQuantities.ToImmutableArray(), orderedOperations.ToImmutableArray(), orderedPowers.ToImmutableArray(),
			orderedPowerMethods.ToImmutableArray());

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

		void AddPower(PowerOperation operation)
		{
			if (!seenPowers.Add((operation.Operand, operation.ExponentTypeName)))
			{
				return;
			}

			orderedPowers.Add(operation);
			AddUnknown(operation.Operand);
			AddUnknown(operation.Result);
		}
	}
}
