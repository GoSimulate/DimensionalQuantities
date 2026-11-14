using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.Roslyn.Generators;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Emission;

public sealed class ArithmeticSourceEmitterTests
{
	[Theory]
	[InlineData("(Current.FromSiBaseUnit(2) * Current.FromSiBaseUnit(3) * Resistance.FromSiBaseUnit(4)).AsSiBaseUnit()", 24)]
	[InlineData("(Current.FromSiBaseUnit(2) * (Current.FromSiBaseUnit(3) * Resistance.FromSiBaseUnit(4))).AsSiBaseUnit()", 24)]
	[InlineData("(Voltage.FromSiBaseUnit(12) / Current.FromSiBaseUnit(3)).AsSiBaseUnit()", 4)]
	public void ExplicitPlanEmitsUsableArithmetic(string expression, double expected)
	{
		Dimension current = new(ElectricCurrent: 1);
		Dimension squared = new(ElectricCurrent: 2);
		Dimension resistance = new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -2);
		Dimension voltage = new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -1);
		Dimension power = new(Length: 2, Mass: 1, Time: -3);
		QuantityGenerationPlan plan = new(
			[new(current, ["Current"], [], false), new(squared, ["CurrentSquared"], [], false),
				new(resistance, ["Resistance"], [], false), new(voltage, ["Voltage"], [], false), new(power, ["Power"], [], false)], [],
			[new(ArithmeticOperationKind.Multiply, current, current, squared),
				new(ArithmeticOperationKind.Multiply, squared, resistance, power),
				new(ArithmeticOperationKind.Multiply, current, resistance, voltage),
				new(ArithmeticOperationKind.Multiply, current, voltage, power),
				new(ArithmeticOperationKind.Divide, voltage, current, resistance)], [], []);
		CSharpCompilation compilation = QuantitySourceEmitterTests.Compile(
			$$"""
			public static class Consumer
			{
				public static double Value() => {{expression}};
			}
			""", QuantitySourceEmitter.Emit(plan.KnownQuantities), ArithmeticSourceEmitter.Emit(plan));
		Assert.Equal(expected, QuantitySourceEmitterTests.Execute<double>(compilation));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void CancellingProductEmitsAnOperatorReturningDouble(bool inverseFirst)
	{
		Dimension current = new(ElectricCurrent: 1);
		Dimension inverse = new(ElectricCurrent: -1);
		QuantityGenerationPlan plan = new(
			[new(current, ["Current"], [], false), new(inverse, ["InverseCurrent"], [], false)], [],
			[inverseFirst ? new(ArithmeticOperationKind.Multiply, inverse, current, default)
				: new(ArithmeticOperationKind.Multiply, current, inverse, default)], [], []);
		string left = inverseFirst ? "InverseCurrent" : "Current";
		string right = inverseFirst ? "Current" : "InverseCurrent";
		CSharpCompilation compilation = QuantitySourceEmitterTests.Compile(
			$$"""
			public static class Consumer
			{
				public static double Value() => {{left}}.FromSiBaseUnit(3) * {{right}}.FromSiBaseUnit(2);
			}
			""", QuantitySourceEmitter.Emit(plan.KnownQuantities), ArithmeticSourceEmitter.Emit(plan));
		Assert.Equal(6, QuantitySourceEmitterTests.Execute<double>(compilation));
	}
}
