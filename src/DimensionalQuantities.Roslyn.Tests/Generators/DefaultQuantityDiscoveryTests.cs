using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Numerics;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.Roslyn.Generators;

namespace DimensionalQuantities.Roslyn.Tests.Generators;

[NotInParallel(nameof(DefaultQuantityDiscoveryTests))]
public sealed class DefaultQuantityDiscoveryTests
{
	[Test]
	[Arguments("Squared", 2, 1)]
	[Arguments("Cubed", 3, 1)]
	[Arguments("Pow4", 4, 1)]
	[Arguments("Pow5", 5, 1)]
	[Arguments("Pow6", 6, 1)]
	[Arguments("SquareRoot", 1, 2)]
	[Arguments("CubeRoot", 1, 3)]
	[Arguments("Root4", 1, 4)]
	[Arguments("Root5", 1, 5)]
	[Arguments("Root6", 1, 6)]
	public async Task PlansEachConvenienceMethodForEveryBaseAndCatalogueDimension(string method, int numerator, int denominator)
	{
		Rational exponent = new(numerator, denominator);
		Dimension[] dimensions = QuantityDiscovery.Discover([]).KnownQuantities.Select(quantity => quantity.Dimension).ToArray();
		PowerMethodOperation[] expected = dimensions.Select(dimension => new PowerMethodOperation(dimension, exponent, dimension.Pow(exponent), method)).ToArray();

		QuantityGenerationPlan plan = DefaultQuantityDiscovery.Discover();
		PowerMethodOperation[] result = plan.PowerMethodOperations.Where(operation => operation.MethodName == method).ToArray();

		await Assert.That(result).IsEquivalentTo(expected, TUnit.Assertions.Enums.CollectionOrdering.Matching);
	}

	[Test]
	public async Task PlansReturnTypesWithoutRecursivelyAddingTheirConvenienceMethods()
	{
		QuantityGenerationPlan result = DefaultQuantityDiscovery.Discover();
		Dimension[] originalDimensions = result.KnownQuantities.Select(quantity => quantity.Dimension).ToArray();
		Dimension[] allDimensions = result.KnownQuantities.Concat(result.AdditionalQuantities).Select(quantity => quantity.Dimension).ToArray();

		using (Assert.Multiple())
		{
			await Assert.That(result.PowerMethodOperations.Length).IsEqualTo(originalDimensions.Length * 10);
			foreach (PowerMethodOperation operation in result.PowerMethodOperations)
			{
				await Assert.That(originalDimensions).Contains(operation.Operand);
			}
			foreach (PowerMethodOperation operation in result.PowerMethodOperations)
			{
				await Assert.That(allDimensions).Contains(operation.Result);
			}
			await Assert.That(allDimensions.Distinct().Count()).IsEqualTo(allDimensions.Length);
			await Assert.That(result.AdditionalQuantities).IsNotEmpty();
		}
	}
}
