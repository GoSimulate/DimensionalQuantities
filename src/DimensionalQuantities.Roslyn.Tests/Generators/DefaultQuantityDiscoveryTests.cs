using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Numerics;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.Roslyn.Generators;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generators;

public sealed class DefaultQuantityDiscoveryTests
{
	[Theory]
	[InlineData("Squared", 2, 1)]
	[InlineData("Cubed", 3, 1)]
	[InlineData("Pow4", 4, 1)]
	[InlineData("Pow5", 5, 1)]
	[InlineData("Pow6", 6, 1)]
	[InlineData("SquareRoot", 1, 2)]
	[InlineData("CubeRoot", 1, 3)]
	[InlineData("Root4", 1, 4)]
	[InlineData("Root5", 1, 5)]
	[InlineData("Root6", 1, 6)]
	public void PlansEachConvenienceMethodForEveryBaseAndCatalogueDimension(string method, int numerator, int denominator)
	{
		Rational exponent = new(numerator, denominator);
		Dimension[] dimensions = QuantityDiscovery.Discover([]).KnownQuantities.Select(quantity => quantity.Dimension).ToArray();
		PowerMethodOperation[] expected = dimensions.Select(dimension => new PowerMethodOperation(dimension, exponent, dimension.Pow(exponent), method)).ToArray();

		QuantityGenerationPlan plan = DefaultQuantityDiscovery.Discover();
		PowerMethodOperation[] result = plan.PowerMethodOperations.Where(operation => operation.MethodName == method).ToArray();

		Assert.Equal(expected, result);
	}

	[Fact]
	public void PlansReturnTypesWithoutRecursivelyAddingTheirConvenienceMethods()
	{
		QuantityGenerationPlan result = DefaultQuantityDiscovery.Discover();
		Dimension[] originalDimensions = result.KnownQuantities.Select(quantity => quantity.Dimension).ToArray();
		Dimension[] allDimensions = result.KnownQuantities.Concat(result.AdditionalQuantities).Select(quantity => quantity.Dimension).ToArray();

		Assert.Multiple(
			() => Assert.Equal(originalDimensions.Length * 10, result.PowerMethodOperations.Length),
			() => Assert.All(result.PowerMethodOperations, operation => Assert.Contains(operation.Operand, originalDimensions)),
			() => Assert.All(result.PowerMethodOperations, operation => Assert.Contains(operation.Result, allDimensions)),
			() => Assert.Equal(allDimensions.Length, allDimensions.Distinct().Count()),
			() => Assert.NotEmpty(result.AdditionalQuantities));
	}
}
