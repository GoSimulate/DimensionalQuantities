using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Numerics;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.Roslyn.Generators;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Emission;

public sealed class PowerSourceEmitterTests
{
	[Theory]
	[InlineData(1, 2, 1, "TerminalHex2", "Length", "Area", 3, 9)]
	[InlineData(1, 3, 1, "TerminalHex3", "Length", "Volume", 3, 27)]
	[InlineData(2, 1, 2, "Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex2>", "Area", "Length", 9, 3)]
	[InlineData(1, 0, 1, "TerminalHex0", "Length", "double", 3, 1)]
	public void PowersReturnTheExpectedQuantityAndMagnitude(int lengthPower, int numerator, int denominator,
		string exponentType, string operandName, string resultName, double value, double expected)
	{
		Dimension operand = new(Length: lengthPower);
		Rational exponent = new(numerator, denominator);
		Dimension result = new(Length: new Rational(lengthPower * numerator, denominator));
		PlannedQuantity input = new(operand, [operandName], [], false);
		QuantityGenerationPlan plan = new([input], result == default ? [] : [new(result, [resultName], [], false)], [],
			[new(operand, exponent, result, "global::DimensionalQuantities.TypeSystem." + exponentType)], []);
		string output = result == default ? "output" : "output.AsSiBaseUnit()";
		CSharpCompilation compilation = QuantitySourceEmitterTests.Compile(
			$$"""
			using DimensionalQuantities.TypeSystem;
			public static class Consumer
			{
				public static double Value()
				{
					{{resultName}} output = Power<{{exponentType}}>.Of({{operandName}}.FromSiBaseUnit({{value}}));
					return {{output}};
				}
			}
			""", QuantitySourceEmitter.Emit(plan.KnownQuantities), QuantitySourceEmitter.Emit(plan.AdditionalQuantities), PowerSourceEmitter.Emit(plan));
		Assert.Equal(expected, QuantitySourceEmitterTests.Execute<double>(compilation));
	}
}
