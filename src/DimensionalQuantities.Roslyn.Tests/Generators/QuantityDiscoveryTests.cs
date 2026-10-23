using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Numerics;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.Roslyn.Generators;
using DimensionalQuantities.Units;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generators;

public sealed class QuantityDiscoveryTests
{
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
