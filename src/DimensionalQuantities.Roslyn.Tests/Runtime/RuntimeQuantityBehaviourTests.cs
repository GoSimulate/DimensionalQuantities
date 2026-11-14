using DimensionalQuantities.TypeSystem;
using Xunit;
using Length = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>>;
using Density = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<DimensionalQuantities.TypeSystem.Negative<DimensionalQuantities.TypeSystem.TerminalHex3>, DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>>;
using CurrentDensity = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<DimensionalQuantities.TypeSystem.Negative<DimensionalQuantities.TypeSystem.TerminalHex2>, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>>;

namespace DimensionalQuantities.Roslyn.Tests.Runtime;

public sealed class RuntimeQuantityBehaviourTests
{
	[Theory]
	[InlineData(0)]
	[InlineData(-2.5)]
	[InlineData(1.23456789)]
	public void SiFactoryStoresMagnitudeUnchanged(double magnitude)
	{
		Length value = Length.FromSiBaseUnit(magnitude);
		Assert.Equal(magnitude, value.AsSiBaseUnit());
	}

	[Fact]
	public void DefaultQuantityRepresentsZero() => Assert.Equal(0, default(Length).AsSiBaseUnit());

	[Theory]
	[InlineData("add", 9)]
	[InlineData("subtract", 3)]
	[InlineData("negate", -6)]
	[InlineData("multiplyLeft", 18)]
	[InlineData("multiplyRight", 18)]
	[InlineData("divide", 2)]
	public void SameDimensionArithmeticPreservesType(string operation, double expected)
	{
		Length left = Length.FromSiBaseUnit(6);
		Length right = Length.FromSiBaseUnit(3);
		object result = operation switch
		{
			"add" => left + right,
			"subtract" => left - right,
			"negate" => -left,
			"multiplyLeft" => 3 * left,
			"multiplyRight" => left * 3,
			_ => left / 3
		};
		Assert.Equal(expected, Assert.IsType<Length>(result).AsSiBaseUnit());
	}

	[Fact]
	public void SameDimensionDivisionReturnsDouble()
	{
		object result = Length.FromSiBaseUnit(6) / Length.FromSiBaseUnit(3);
		Assert.Equal(2, Assert.IsType<double>(result));
	}

	[Theory]
	[InlineData(-1, 0)]
	[InlineData(0, 0)]
	[InlineData(1.5, 1.5)]
	[InlineData(3, 3)]
	[InlineData(4, 3)]
	public void ClampRespectsInclusiveBounds(double value, double expected)
	{
		Length result = Length.Clamp(Length.FromSiBaseUnit(value), Length.FromSiBaseUnit(0), Length.FromSiBaseUnit(3));
		Assert.Equal(expected, result.AsSiBaseUnit());
	}

	[Fact]
	public void ClampRejectsReversedBounds() => Assert.Throws<ArgumentException>(() => Length.Clamp(default, Length.FromSiBaseUnit(3), default));

	[Theory]
	[InlineData("abs", 3)]
	[InlineData("min", -3)]
	[InlineData("max", 2)]
	public void MagnitudeHelpersPreserveDimension(string operation, double expected)
	{
		Length left = Length.FromSiBaseUnit(-3);
		Length right = Length.FromSiBaseUnit(2);
		object result = operation switch { "abs" => Length.Abs(left), "min" => Length.Min(left, right), _ => Length.Max(left, right) };
		Assert.Equal(expected, Assert.IsType<Length>(result).AsSiBaseUnit());
	}

	[Theory]
	[InlineData("<", -3, 2, true)]
	[InlineData(">", -3, 2, false)]
	[InlineData("<=", -3, 2, true)]
	[InlineData(">=", -3, 2, false)]
	[InlineData("==", -3, 2, false)]
	[InlineData("!=", -3, 2, true)]
	[InlineData("<", 2, 2, false)]
	[InlineData(">", 2, 2, false)]
	[InlineData("<=", 2, 2, true)]
	[InlineData(">=", 2, 2, true)]
	[InlineData("==", 2, 2, true)]
	[InlineData("!=", 2, 2, false)]
	public void ComparisonUsesSiMagnitude(string operation, double leftMagnitude, double rightMagnitude, bool expected)
	{
		Length left = Length.FromSiBaseUnit(leftMagnitude);
		Length right = Length.FromSiBaseUnit(rightMagnitude);
		bool result = operation switch { "<" => left < right, ">" => left > right, "<=" => left <= right, ">=" => left >= right, "==" => left == right, _ => left != right };
		Assert.Equal(expected, result);
	}

	[Fact]
	public void CurrentDensityWeightsLengthPrefixByNegativeExponent()
	{
		CurrentDensity value = CurrentDensity.FromArbitrarySiComponents(1, lengthPrefixExponent: -3);
		Assert.Multiple(
			() => Assert.Equal(1_000_000, value.AsSiBaseUnit()),
			() => Assert.Equal(1, value.AsArbitrarySiComponents(lengthPrefixExponent: -3)));
	}

	[Fact]
	public void DensityUsesGramBaselineAndCubedLength()
	{
		Density value = Density.FromArbitrarySiComponents(1, lengthPrefixExponent: -2, massPrefixExponent: 0);
		Assert.Equal(1000, value.AsSiBaseUnit(), 9);
	}

	[Fact]
	public void IrrelevantPrefixHasNoEffect()
	{
		Length value = Length.FromArbitrarySiComponents(2, timePrefixExponent: 12);
		Assert.Equal(2, value.AsSiBaseUnit());
	}

	[Fact]
	public void OverallScaleAddsToDimensionWeightedPrefix()
	{
		Length value = Length.FromArbitrarySiComponents(2, lengthPrefixExponent: -3, overallScaleExponent: 2);
		Assert.Equal(0.2, value.AsSiBaseUnit(), 12);
	}

	[Theory]
	[InlineData(-5)]
	[InlineData(0)]
	[InlineData(293.15)]
	public void AsDeltaPreservesStoredMagnitude(double magnitude)
	{
		Assert.Equal(magnitude, Length.FromSiBaseUnit(magnitude).AsDelta().AsSiBaseUnit());
	}
}
