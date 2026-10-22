using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generators;

public sealed partial class QuantityGeneratorTests
{
	[Theory]
	[InlineData("Length.FromMetres(2) * Length.FromMetres(3)", "Area", 6)]
	[InlineData("Current.FromAmps(2) * Current.FromAmps(3)", "CurrentSquared", 6)]
	[InlineData("Current.FromAmps(2) * Current.FromAmps(3) * Resistance.FromOhms(4)", "Power", 24)]
	[InlineData("Current.FromAmps(2) * (Current.FromAmps(3) * Resistance.FromOhms(4))", "Power", 24)]
	[InlineData("Voltage.FromVolts(12) / Current.FromAmps(3)", "Resistance", 4)]
	[InlineData("Current.FromAmps(3) * Time.FromSeconds(2)", "TimeCurrent", 6)]
	[InlineData("(Current.FromAmps(2) * Time.FromSeconds(3)) / Time.FromSeconds(3)", "Current", 2)]
	[InlineData("(Voltage.FromVolts(4) * Current.FromAmps(3)) / Current.FromAmps(2)", "Voltage", 6)]
	[InlineData("Current.FromAmps(3) * 2d", "Current", 6)]
	[InlineData("2 * Current.FromAmps(3)", "Current", 6)]
	[InlineData("Current.FromAmps(6) / 2f", "Current", 3)]
	[InlineData("2d / Time.FromSeconds(4)", "Frequency", 0.5)]
	[InlineData("2f / Current.FromAmps(4)", "InverseCurrent", 0.5)]
	[InlineData("(2 / Current.FromAmps(4)) * Time.FromSeconds(3)", "TimePerCurrent", 1.5)]
	[InlineData("(Current.FromAmps(2) * 2) * Current.FromAmps(3)", "CurrentSquared", 12)]
	[InlineData("2 * (Current.FromAmps(2) * Current.FromAmps(3))", "CurrentSquared", 12)]
	[InlineData("(Current.FromAmps(2) * Current.FromAmps(3)) / 2", "CurrentSquared", 3)]
	[InlineData("(2 + 3) * Current.FromAmps(2) * Current.FromAmps(3)", "CurrentSquared", 30)]
	[InlineData("(2 * 3) * Current.FromAmps(2) * Current.FromAmps(3)", "CurrentSquared", 36)]
	public void QuantityArithmeticProducesTheExpectedTypeAndSiMagnitude(string arithmetic, string resultType, double expected)
	{
		string expression = $"(({resultType})({arithmetic})).AsSiBaseUnit()";
		double result = Evaluate<double>(expression);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void QuantityArithmeticAcceptsTypedMethodParametersAndReturns()
	{
		const string source = """
			public static class Consumer
			{
				public static Area Multiply(Length left, Length right) => left * right;
				public static Voltage Divide(Power power, Current current) => power / current;
				public static InverseCurrent Invert(double numerator, Current current) => numerator / current;
			}
			""";

		Compile(source);
	}

	[Fact]
	public void RepeatedConsumerOperationIsEmittedOnce()
	{
		const string source = """
			public static class Consumer
			{
				public static Area First(Length left, Length right) => left * right;
				public static Area Second(Length left, Length right) => left * right;
			}
			""";
		CSharpCompilation compilation = Compile(source);
		INamedTypeSymbol extensions = compilation.GetTypeByMetadataName("DimensionalQuantities.Generated.ArithmeticExtensions")!;
		Assert.Single(extensions.GetMembers().OfType<IMethodSymbol>(), method => method.MetadataName == "op_Multiply");
	}


	[Fact]
	public void ArithmeticReadsFieldAndMethodReturnTypes()
	{
		const string source = """
			public static class Consumer
			{
				public static Current Input = Current.FromAmps(2);
				public static Resistance GetResistance() => Resistance.FromOhms(4);
				public static Power Calculate() => Input * Input * GetResistance();
			}
			""";
		Compile(source);
	}

	[Fact]
	public void DimensionPreservingScalarArithmeticUsesCoreOperatorsOnly()
	{
		const string source = """
			public static class Consumer
			{
				public static Current Scale(Current original, double factor) => factor * original / 2;
			}
			""";
		CSharpCompilation compilation = Compile(source);

		Assert.Null(compilation.GetTypeByMetadataName("DimensionalQuantities.Generated.ArithmeticExtensions"));
	}
}
