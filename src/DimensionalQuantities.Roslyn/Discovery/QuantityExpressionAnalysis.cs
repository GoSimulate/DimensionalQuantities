using System.Collections.Immutable;

namespace DimensionalQuantities.Roslyn.Discovery;

internal readonly record struct QuantityExpressionAnalysis(
	ImmutableArray<ArithmeticOperation> ArithmeticOperations,
	ImmutableArray<PowerOperation> PowerOperations);
