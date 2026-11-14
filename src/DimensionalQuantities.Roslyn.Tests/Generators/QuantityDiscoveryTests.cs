using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Numerics;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.Roslyn.Generators;
using DimensionalQuantities.Units;
using DimensionalQuantities.TypeSystem;

namespace DimensionalQuantities.Roslyn.Tests.Generators;

[NotInParallel(nameof(QuantityDiscoveryTests))]
public sealed class QuantityDiscoveryTests
{
	[Test]
	public async Task TypeRequestsAddOnlyMissingQuantitiesWithoutInventingOperations()
	{
		QuantityGenerationPlan predefined = DefaultQuantityDiscovery.Discover();
		Dimension requested = new(Length: 1, ElectricCurrent: 1);

		QuantityGenerationPlan result = QuantityDiscovery.DiscoverWithPredefinedQuantities(predefined, [], [], [],
			[requested, requested, new(ElectricCurrent: 1), new(ElectricCurrent: 2), default]);

		using (Assert.Multiple())
		{
			await Assert.That((await Assert.That(result.AdditionalQuantities).HasSingleItem()).Dimension).IsEqualTo(requested);
			await Assert.That(result.GetQuantityName(requested)).IsEqualTo("LengthCurrent");
			await Assert.That(result.Operations).IsEmpty();
			await Assert.That(result.PowerOperations).IsEmpty();
			await Assert.That(result.PowerMethodOperations).IsEmpty();
		}
	}

	[Test]
	public async Task ReusesPredefinedQuantitiesAndOnlyAddsNewResults()
	{
		Dimension currentSquared = new(ElectricCurrent: 2);
		Dimension length = new(Length: 1);
		Dimension product = new(Length: 1, ElectricCurrent: 2);
		PlannedQuantity predefined = new(currentSquared, [new("CurrentSquared", [new(AtomicUnits.Amp, 2)])], false);
		ArithmeticOperation operation = new(ArithmeticOperationKind.Multiply, currentSquared, length, product);

		QuantityGenerationPlan predefinedPlan = new([], [predefined, predefined], [], [], []);

		QuantityGenerationPlan result = QuantityDiscovery.DiscoverWithPredefinedQuantities(predefinedPlan, [operation], [], []);

		using (Assert.Multiple())
		{
			await Assert.That((await Assert.That(result.KnownQuantities.Where(quantity => quantity.Dimension == currentSquared)).HasSingleItem())).IsEqualTo(predefined);
			await Assert.That((await Assert.That(result.AdditionalQuantities).HasSingleItem()).Dimension).IsEqualTo(product);
			await Assert.That(result.GetQuantityName(currentSquared)).IsEqualTo("CurrentSquared");
			await Assert.That((await Assert.That(result.Operations).HasSingleItem())).IsEqualTo(operation);
		}
	}

	[Test]
	public async Task ReusesPredefinedMethodsWhilePlanningNewConvenienceMethods()
	{
		Dimension current = new(ElectricCurrent: 1);
		Dimension squared = new(ElectricCurrent: 2);
		Dimension cubed = new(ElectricCurrent: 3);
		PlannedQuantity predefined = new(squared, [new("CurrentSquared", [new(AtomicUnits.Amp, 2)])], false);
		PowerMethodOperation square = new(current, 2, squared, "Squared");
		PowerMethodOperation cube = new(current, 3, cubed, "Cubed");

		QuantityGenerationPlan predefinedPlan = new([], [predefined], [], [], [square]);

		QuantityGenerationPlan result = QuantityDiscovery.DiscoverWithPredefinedQuantities(predefinedPlan, [], [], [square, cube]);

		using (Assert.Multiple())
		{
			await Assert.That((await Assert.That(result.PowerMethodOperations).HasSingleItem())).IsEqualTo(cube);
			await Assert.That((await Assert.That(result.AdditionalQuantities).HasSingleItem()).Dimension).IsEqualTo(cubed);
		}
	}

	[Test]
	[Arguments("Squared", 2, 1)]
	[Arguments("SquareRoot", 1, 2)]
	public async Task ConvenienceMethodsReuseExplicitCanonicalPowerOperations(string method, int numerator, int denominator)
	{
		Dimension current = new(ElectricCurrent: 1);
		Rational exponent = new(numerator, denominator);
		Dimension resultDimension = current.Pow(exponent);
		string exponentType = TypeSystemTypeNameFormatter.Format(TypeSystemNumberCodec.CreateType(exponent));
		PowerOperation expected = new(current, exponent, resultDimension, exponentType);
		PowerMethodOperation convenience = new(current, exponent, resultDimension, method);

		QuantityGenerationPlan result = QuantityDiscovery.Discover([], [expected], [convenience]);

		using (Assert.Multiple())
		{
			await Assert.That((await Assert.That(result.PowerOperations).HasSingleItem())).IsEqualTo(expected);
			await Assert.That((await Assert.That(result.PowerMethodOperations).HasSingleItem())).IsEqualTo(convenience);
		}
	}

	[Test]
	public async Task ConvenienceMethodAddsCanonicalPowerAlongsideAnExplicitAlternativeEncoding()
	{
		Dimension current = new(ElectricCurrent: 1);
		Dimension squared = new(ElectricCurrent: 2);
		string alternativeType = TypeSystemTypeNameFormatter.Format(typeof(Fraction<TerminalHex4, TerminalHex2>));
		string canonicalType = TypeSystemTypeNameFormatter.Format(typeof(TerminalHex2));
		PowerOperation alternative = new(current, 2, squared, alternativeType);
		PowerOperation canonical = new(current, 2, squared, canonicalType);
		PowerMethodOperation convenience = new(current, 2, squared, "Squared");

		QuantityGenerationPlan result = QuantityDiscovery.Discover([], [alternative], [convenience]);

		using (Assert.Multiple())
		{
			await Assert.That(result.PowerOperations.Length).IsEqualTo(2);
			await Assert.That(result.PowerOperations).Contains(alternative);
			await Assert.That(result.PowerOperations).Contains(canonical);
		}
	}

	[Test]
	public async Task ConvenienceMethodsDeduplicateByOperandAndNameAndPlanTheirDimensions()
	{
		Dimension current = new(ElectricCurrent: 1);
		Dimension squared = new(ElectricCurrent: 2);
		Dimension cubed = new(ElectricCurrent: 3);
		PowerMethodOperation square = new(current, 2, squared, "Squared");
		PowerMethodOperation cube = new(current, 3, cubed, "Cubed");
		PowerMethodOperation squareAgain = new(squared, 2, new(ElectricCurrent: 4), "Squared");
		PowerMethodOperation[] expected = [square, cube, squareAgain];

		QuantityGenerationPlan result = QuantityDiscovery.Discover([], [], [square, square, cube, squareAgain]);

		using (Assert.Multiple())
		{
			await Assert.That(result.PowerMethodOperations).IsEquivalentTo(expected, TUnit.Assertions.Enums.CollectionOrdering.Matching);
			await Assert.That(result.GetQuantityName(squared)).IsEqualTo("CurrentSquared");
			await Assert.That(result.GetQuantityName(cubed)).IsEqualTo("CurrentCubed");
			await Assert.That(result.AdditionalQuantities.Length).IsEqualTo(3);
		}
	}

	[Test]
	public async Task PlanKeepsCatalogueNameAndDeduplicatesOrderedOperations()
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

		using (Assert.Multiple())
		{
			await Assert.That(plan.Operations).IsEquivalentTo(expected, TUnit.Assertions.Enums.CollectionOrdering.Matching);
			await Assert.That(plan.GetQuantityName(power)).IsEqualTo("Power");
			await Assert.That(plan.GetQuantityName(energy)).IsEqualTo("Energy");
			await Assert.That(plan.GetQuantityName(currentSquared)).IsEqualTo("CurrentSquared");
			await Assert.That(plan.AdditionalQuantities).HasSingleItem();
		}
	}

	[Test]
	public async Task CancellingProductPlansOperandsWithoutADimensionlessQuantity()
	{
		Dimension current = new(ElectricCurrent: 1);
		Dimension inverse = new(ElectricCurrent: -1);
		ArithmeticOperation operation = new(ArithmeticOperationKind.Multiply, current, inverse, default);
		QuantityGenerationPlan result = QuantityDiscovery.Discover([operation, operation]);

		using (Assert.Multiple())
		{
			await Assert.That((await Assert.That(result.Operations).HasSingleItem())).IsEqualTo(operation);
			await Assert.That((await Assert.That(result.AdditionalQuantities).HasSingleItem()).Dimension).IsEqualTo(inverse);
			await Assert.That(result.KnownQuantities.Any(quantity => quantity.Dimension == default)).IsFalse();
		}
	}

	[Test]
	public async Task EquivalentExponentsKeepTheirOverloadsAndShareOneResultType()
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

		using (Assert.Multiple())
		{
			await Assert.That(result.PowerOperations).IsEquivalentTo(expected, TUnit.Assertions.Enums.CollectionOrdering.Matching);
			await Assert.That((await Assert.That(result.AdditionalQuantities).HasSingleItem()).Dimension).IsEqualTo(rootCurrent);
			await Assert.That(result.Operations).IsEmpty();
		}
	}

	[Test]
	public async Task UnknownQuantityUsesSiComponentPowersAndBasePrefixes()
	{
		Dimension mass = new(Mass: 1);
		Dimension massSquared = new(Mass: 2);
		ArithmeticOperation operation = new(ArithmeticOperationKind.Multiply, mass, mass, massSquared);
		QuantityGenerationPlan plan = QuantityDiscovery.Discover([operation]);
		PlannedQuantity quantity = await Assert.That(plan.AdditionalQuantities).HasSingleItem();
		UnitDefinition unit = (await Assert.That(quantity.Units).HasSingleItem());
		UnitTerm term = (await Assert.That(unit.Expression.Terms).HasSingleItem());

		using (Assert.Multiple())
		{
			await Assert.That(quantity.Dimension).IsEqualTo(massSquared);
			await Assert.That(quantity.IncludeNamedConversions).IsFalse();
			await Assert.That(unit.Name).IsEqualTo("MassSquared");
			await Assert.That(term.Power).IsEqualTo(2);
			await Assert.That(term.PrefixExponent).IsEqualTo(SiBasePrefixExponents.Mass);
		}
	}
}
