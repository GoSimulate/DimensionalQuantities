using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Numerics;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.Roslyn.Generators;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Planning;

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
		Dimension[] dimensions = PredefinedDimensions();
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

	[Theory]
	[InlineData("Length", 1, 0, 0, 0, 0, 0, 0)]
	[InlineData("Mass", 0, 1, 0, 0, 0, 0, 0)]
	[InlineData("Time", 0, 0, 1, 0, 0, 0, 0)]
	[InlineData("Current", 0, 0, 0, 1, 0, 0, 0)]
	[InlineData("Temperature", 0, 0, 0, 0, 1, 0, 0)]
	[InlineData("AmountOfSubstance", 0, 0, 0, 0, 0, 1, 0)]
	[InlineData("LuminousIntensity", 0, 0, 0, 0, 0, 0, 1)]
	[InlineData("Area", 2, 0, 0, 0, 0, 0, 0)]
	[InlineData("Volume", 3, 0, 0, 0, 0, 0, 0)]
	[InlineData("Speed", 1, 0, -1, 0, 0, 0, 0)]
	[InlineData("Acceleration", 1, 0, -2, 0, 0, 0, 0)]
	[InlineData("Frequency", 0, 0, -1, 0, 0, 0, 0)]
	[InlineData("AngularVelocity", 0, 0, -1, 0, 0, 0, 0)]
	[InlineData("Force", 1, 1, -2, 0, 0, 0, 0)]
	[InlineData("Density", -3, 1, 0, 0, 0, 0, 0)]
	[InlineData("Pressure", -1, 1, -2, 0, 0, 0, 0)]
	[InlineData("Stress", -1, 1, -2, 0, 0, 0, 0)]
	[InlineData("YoungsModulus", -1, 1, -2, 0, 0, 0, 0)]
	[InlineData("Energy", 2, 1, -2, 0, 0, 0, 0)]
	[InlineData("Torque", 2, 1, -2, 0, 0, 0, 0)]
	[InlineData("Power", 2, 1, -3, 0, 0, 0, 0)]
	[InlineData("SpecificPower", 2, 0, -3, 0, 0, 0, 0)]
	[InlineData("SpecificPowerLoss", 2, 0, -3, 0, 0, 0, 0)]
	[InlineData("TemperatureCoefficient", 0, 0, 0, 0, -1, 0, 0)]
	[InlineData("PowerDensity", -1, 1, -3, 0, 0, 0, 0)]
	[InlineData("VolumetricPowerLoss", -1, 1, -3, 0, 0, 0, 0)]
	[InlineData("ElectricCharge", 0, 0, 1, 1, 0, 0, 0)]
	[InlineData("Voltage", 2, 1, -3, -1, 0, 0, 0)]
	[InlineData("Capacitance", -2, -1, 4, 2, 0, 0, 0)]
	[InlineData("Resistance", 2, 1, -3, -2, 0, 0, 0)]
	[InlineData("Inductance", 2, 1, -2, -2, 0, 0, 0)]
	[InlineData("MagneticFlux", 2, 1, -2, -1, 0, 0, 0)]
	[InlineData("FluxLinkage", 2, 1, -2, -1, 0, 0, 0)]
	[InlineData("MagneticFluxDensity", 0, 1, -2, -1, 0, 0, 0)]
	[InlineData("MagneticPolarisation", 0, 1, -2, -1, 0, 0, 0)]
	[InlineData("RemanentFluxDensity", 0, 1, -2, -1, 0, 0, 0)]
	[InlineData("MagneticFieldStrength", -1, 0, 0, 1, 0, 0, 0)]
	[InlineData("CoerciveFieldStrength", -1, 0, 0, 1, 0, 0, 0)]
	[InlineData("CurrentDensity", -2, 0, 0, 1, 0, 0, 0)]
	[InlineData("Permeability", 1, 1, -2, -2, 0, 0, 0)]
	[InlineData("Resistivity", 3, 1, -3, -2, 0, 0, 0)]
	[InlineData("Conductivity", -3, -1, 3, 2, 0, 0, 0)]
	[InlineData("MagneticReluctivity", -1, -1, 2, 2, 0, 0, 0)]
	[InlineData("MagneticVectorPotential", 1, 1, -2, -1, 0, 0, 0)]
	public void DefaultPlanIncludesEachAliasWithItsIndependentDimension(string alias, int length, int mass, int time,
		int current, int temperature, int amount, int intensity)
	{
		Dimension expected = new(length, mass, time, current, temperature, amount, intensity);

		QuantityGenerationPlan plan = DefaultQuantityDiscovery.Discover();
		PlannedQuantity quantity = Assert.Single(plan.KnownQuantities.Concat(plan.AdditionalQuantities), quantity => quantity.Aliases.Contains(alias));

		Assert.Equal(expected, quantity.Dimension);
	}

	private static Dimension[] PredefinedDimensions() =>
	[
		new(Length: 1),
		new(Mass: 1),
		new(Time: 1),
		new(ElectricCurrent: 1),
		new(Temperature: 1),
		new(AmountOfSubstance: 1),
		new(LuminousIntensity: 1),
		new(Length: 2),
		new(Length: 3),
		new(Length: 1, Time: -1),
		new(Length: 1, Time: -2),
		new(Time: -1),
		new(Length: 1, Mass: 1, Time: -2),
		new(Length: -3, Mass: 1),
		new(Length: -1, Mass: 1, Time: -2),
		new(Length: 2, Mass: 1, Time: -2),
		new(Length: 2, Mass: 1, Time: -3),
		new(Length: 2, Time: -3),
		new(Temperature: -1),
		new(Length: -1, Mass: 1, Time: -3),
		new(Time: 1, ElectricCurrent: 1),
		new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -1),
		new(Length: -2, Mass: -1, Time: 4, ElectricCurrent: 2),
		new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -2),
		new(Length: 2, Mass: 1, Time: -2, ElectricCurrent: -2),
		new(Length: 2, Mass: 1, Time: -2, ElectricCurrent: -1),
		new(Mass: 1, Time: -2, ElectricCurrent: -1),
		new(Length: -1, ElectricCurrent: 1),
		new(Length: -2, ElectricCurrent: 1),
		new(Length: 1, Mass: 1, Time: -2, ElectricCurrent: -2),
		new(Length: 3, Mass: 1, Time: -3, ElectricCurrent: -2),
		new(Length: -3, Mass: -1, Time: 3, ElectricCurrent: 2),
		new(Length: -1, Mass: -1, Time: 2, ElectricCurrent: 2),
		new(Length: 1, Mass: 1, Time: -2, ElectricCurrent: -1)
	];
}
