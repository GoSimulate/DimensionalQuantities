using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Numerics;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.Roslyn.Generators;
using DimensionalQuantities.Units;
using DimensionalQuantities.TypeSystem;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generators;

public sealed class QuantityDiscoveryTests
{
	[Fact]
	public void TypeRequestsAddOnlyMissingQuantitiesWithoutInventingOperations()
	{
		QuantityGenerationPlan predefined = DefaultQuantityDiscovery.Discover();
		Dimension requested = new(Length: 1, ElectricCurrent: 1);

		QuantityGenerationPlan result = QuantityDiscovery.DiscoverWithPredefinedQuantities(predefined, [], [], [],
			[requested, requested, new(ElectricCurrent: 1), new(ElectricCurrent: 2), default]);

		Assert.Multiple(
			() => Assert.Equal(requested, Assert.Single(result.AdditionalQuantities).Dimension),
			() => Assert.Equal("LengthCurrent", result.GetQuantityName(requested)),
			() => Assert.Empty(result.Operations),
			() => Assert.Empty(result.PowerOperations),
			() => Assert.Empty(result.PowerMethodOperations));
	}

	[Fact]
	public void ReusesPredefinedQuantitiesAndOnlyAddsNewResults()
	{
		Dimension currentSquared = new(ElectricCurrent: 2);
		Dimension length = new(Length: 1);
		Dimension product = new(Length: 1, ElectricCurrent: 2);
		PlannedQuantity predefined = new(currentSquared, [new("CurrentSquared", [new(AtomicUnits.Amp, 2)])], false);
		ArithmeticOperation operation = new(ArithmeticOperationKind.Multiply, currentSquared, length, product);

		QuantityGenerationPlan predefinedPlan = new([], [predefined, predefined], [], [], []);

		QuantityGenerationPlan result = QuantityDiscovery.DiscoverWithPredefinedQuantities(predefinedPlan, [operation], [], []);

		Assert.Multiple(
			() => Assert.Equal(predefined, Assert.Single(result.KnownQuantities.Where(quantity => quantity.Dimension == currentSquared))),
			() => Assert.Equal(product, Assert.Single(result.AdditionalQuantities).Dimension),
			() => Assert.Equal("CurrentSquared", result.GetQuantityName(currentSquared)),
			() => Assert.Equal(operation, Assert.Single(result.Operations)));
	}

	[Fact]
	public void ReusesPredefinedMethodsWhilePlanningNewConvenienceMethods()
	{
		Dimension current = new(ElectricCurrent: 1);
		Dimension squared = new(ElectricCurrent: 2);
		Dimension cubed = new(ElectricCurrent: 3);
		PlannedQuantity predefined = new(squared, [new("CurrentSquared", [new(AtomicUnits.Amp, 2)])], false);
		PowerMethodOperation square = new(current, 2, squared, "Squared");
		PowerMethodOperation cube = new(current, 3, cubed, "Cubed");

		QuantityGenerationPlan predefinedPlan = new([], [predefined], [], [], [square]);

		QuantityGenerationPlan result = QuantityDiscovery.DiscoverWithPredefinedQuantities(predefinedPlan, [], [], [square, cube]);

		Assert.Multiple(
			() => Assert.Equal(cube, Assert.Single(result.PowerMethodOperations)),
			() => Assert.Equal(cubed, Assert.Single(result.AdditionalQuantities).Dimension));
	}

	[Theory]
	[InlineData("Squared", 2, 1)]
	[InlineData("SquareRoot", 1, 2)]
	public void ConvenienceMethodsReuseExplicitCanonicalPowerOperations(string method, int numerator, int denominator)
	{
		Dimension current = new(ElectricCurrent: 1);
		Rational exponent = new(numerator, denominator);
		Dimension resultDimension = current.Pow(exponent);
		string exponentType = TypeSystemTypeNameFormatter.Format(TypeSystemNumberCodec.CreateType(exponent));
		PowerOperation expected = new(current, exponent, resultDimension, exponentType);
		PowerMethodOperation convenience = new(current, exponent, resultDimension, method);

		QuantityGenerationPlan result = QuantityDiscovery.Discover([], [expected], [convenience]);

		Assert.Multiple(
			() => Assert.Equal(expected, Assert.Single(result.PowerOperations)),
			() => Assert.Equal(convenience, Assert.Single(result.PowerMethodOperations)));
	}

	[Fact]
	public void ConvenienceMethodAddsCanonicalPowerAlongsideAnExplicitAlternativeEncoding()
	{
		Dimension current = new(ElectricCurrent: 1);
		Dimension squared = new(ElectricCurrent: 2);
		string alternativeType = TypeSystemTypeNameFormatter.Format(typeof(Fraction<TerminalHex4, TerminalHex2>));
		string canonicalType = TypeSystemTypeNameFormatter.Format(typeof(TerminalHex2));
		PowerOperation alternative = new(current, 2, squared, alternativeType);
		PowerOperation canonical = new(current, 2, squared, canonicalType);
		PowerMethodOperation convenience = new(current, 2, squared, "Squared");

		QuantityGenerationPlan result = QuantityDiscovery.Discover([], [alternative], [convenience]);

		Assert.Multiple(
			() => Assert.Equal(2, result.PowerOperations.Length),
			() => Assert.Contains(alternative, result.PowerOperations),
			() => Assert.Contains(canonical, result.PowerOperations));
	}

	[Fact]
	public void ConvenienceMethodsDeduplicateByOperandAndNameAndPlanTheirDimensions()
	{
		Dimension current = new(ElectricCurrent: 1);
		Dimension squared = new(ElectricCurrent: 2);
		Dimension cubed = new(ElectricCurrent: 3);
		PowerMethodOperation square = new(current, 2, squared, "Squared");
		PowerMethodOperation cube = new(current, 3, cubed, "Cubed");
		PowerMethodOperation squareAgain = new(squared, 2, new(ElectricCurrent: 4), "Squared");
		PowerMethodOperation[] expected = [square, cube, squareAgain];

		QuantityGenerationPlan result = QuantityDiscovery.Discover([], [], [square, square, cube, squareAgain]);

		Assert.Multiple(
			() => Assert.Equal(expected, result.PowerMethodOperations),
			() => Assert.Equal("CurrentSquared", result.GetQuantityName(squared)),
			() => Assert.Equal("CurrentCubed", result.GetQuantityName(cubed)),
			() => Assert.Equal(3, result.AdditionalQuantities.Length));
	}

	[Fact]
	public void PlanKeepsCatalogueNameAndDeduplicatesOrderedOperations()
	{
		Dimension current = new(ElectricCurrent: 1);
		Dimension currentSquared = new(ElectricCurrent: 2);
		Dimension resistance = new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -2);
		Dimension power = new(Length: 2, Mass: 1, Time: -3);
		Dimension energy = new(Length: 2, Mass: 1, Time: -2);
		ArithmeticOperation square = new(ArithmeticOperationKind.Multiply, current, current, currentSquared);
		ArithmeticOperation heat = new(ArithmeticOperationKind.Multiply, currentSquared, resistance, power);
		ArithmeticOperation[] operations = [square, square, heat];
		QuantityGenerationPlan plan = QuantityDiscovery.Discover(operations);
		ArithmeticOperation[] expected = [square, heat];

		Assert.Multiple(
			() => Assert.Equal(expected, plan.Operations),
			() => Assert.Equal("Power", plan.GetQuantityName(power)),
			() => Assert.Equal("Energy", plan.GetQuantityName(energy)),
			() => Assert.Equal("CurrentSquared", plan.GetQuantityName(currentSquared)),
			() => Assert.Single(plan.AdditionalQuantities));
	}

	[Fact]
	public void CancellingProductPlansOperandsWithoutADimensionlessQuantity()
	{
		Dimension current = new(ElectricCurrent: 1);
		Dimension inverse = new(ElectricCurrent: -1);
		ArithmeticOperation operation = new(ArithmeticOperationKind.Multiply, current, inverse, default);
		QuantityGenerationPlan result = QuantityDiscovery.Discover([operation, operation]);

		Assert.Multiple(
			() => Assert.Equal(operation, Assert.Single(result.Operations)),
			() => Assert.Equal(inverse, Assert.Single(result.AdditionalQuantities).Dimension),
			() => Assert.DoesNotContain(result.KnownQuantities, quantity => quantity.Dimension == default));
	}

	[Fact]
	public void EquivalentExponentsKeepTheirOverloadsAndShareOneResultType()
	{
		Dimension current = new(ElectricCurrent: 1);
		Dimension rootCurrent = new(ElectricCurrent: new Rational(1, 2));
		PowerOperation half = new(current, new Rational(1, 2), rootCurrent,
			"global::DimensionalQuantities.TypeSystem.Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex2>");
		PowerOperation twoQuarters = half with
		{
			ExponentTypeName = "global::DimensionalQuantities.TypeSystem.Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex2, global::DimensionalQuantities.TypeSystem.TerminalHex4>"
		};
		PowerOperation zero = new(current, Rational.Zero, default, "global::DimensionalQuantities.TypeSystem.TerminalHex0");
		QuantityGenerationPlan result = QuantityDiscovery.Discover([], [half, half, twoQuarters, zero]);
		PowerOperation[] expected = [half, twoQuarters, zero];

		Assert.Multiple(
			() => Assert.Equal(expected, result.PowerOperations),
			() => Assert.Equal(rootCurrent, Assert.Single(result.AdditionalQuantities).Dimension),
			() => Assert.Empty(result.Operations));
	}

	[Fact]
	public void UnknownQuantityUsesSiComponentPowersAndBasePrefixes()
	{
		Dimension mass = new(Mass: 1);
		Dimension massSquared = new(Mass: 2);
		ArithmeticOperation operation = new(ArithmeticOperationKind.Multiply, mass, mass, massSquared);
		QuantityGenerationPlan plan = QuantityDiscovery.Discover([operation]);
		PlannedQuantity quantity = Assert.Single(plan.AdditionalQuantities);
		UnitDefinition unit = Assert.Single(quantity.Units);
		UnitTerm term = Assert.Single(unit.Expression.Terms);

		Assert.Multiple(
			() => Assert.Equal(massSquared, quantity.Dimension),
			() => Assert.False(quantity.IncludeNamedConversions),
			() => Assert.Equal("MassSquared", unit.QuantityName),
			() => Assert.Equal(2, term.Power),
			() => Assert.Equal(SiBasePrefixExponents.Mass, term.PrefixExponent));
	}
}
