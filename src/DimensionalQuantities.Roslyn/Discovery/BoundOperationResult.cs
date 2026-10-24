using System.Collections.Immutable;

namespace DimensionalQuantities.Roslyn.Discovery;

internal readonly record struct BoundOperationResult(ImmutableArray<ArithmeticOperation> ArithmeticOperations,
	ImmutableArray<PowerOperation> PowerOperations, ImmutableArray<PowerMethodOperation> PowerMethodOperations);
