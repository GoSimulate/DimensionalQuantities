using Microsoft.Build.Utilities.ProjectCreation;
using Xunit;

namespace DimensionalQuantities.IntegrationTests.Packaging;

public sealed partial class PackagedGeneratorTests
{

	[Theory]
	[InlineData("Length.FromMetres(2) * Length.FromMetres(3)", "Area", 6)]
	[InlineData("Current.FromAmps(2) * Current.FromAmps(3)", "CurrentSquared", 6)]
	[InlineData("Current.FromAmps(2) * Current.FromAmps(3) * Resistance.FromSiBaseUnit(4)", "Power", 24)]
	[InlineData("Current.FromAmps(2) * (Current.FromAmps(3) * Resistance.FromSiBaseUnit(4))", "Power", 24)]
	[InlineData("Voltage.FromSiBaseUnit(12) / Current.FromAmps(3)", "Resistance", 4)]
	[InlineData("Current.FromAmps(3) * Time.FromSeconds(2)", "TimeCurrent", 6)]
	[InlineData("(Current.FromAmps(2) * Time.FromSeconds(3)) / Time.FromSeconds(3)", "Current", 2)]
	[InlineData("(Voltage.FromSiBaseUnit(4) * Current.FromAmps(3)) / Current.FromAmps(2)", "Voltage", 6)]
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


	[Theory]
	[InlineData("Current.FromAmps(2) + Current.FromMilliAmps(500)", "AsAmps", 2.5)]
	[InlineData("Current.FromAmps(2) - Current.FromMilliAmps(500)", "AsAmps", 1.5)]
	[InlineData("-Current.FromMilliAmps(500)", "AsAmps", -0.5)]
	[InlineData("Energy.FromSiBaseUnit(2) + Torque.FromSiBaseUnit(3)", "AsSiBaseUnit", 5)]
	public void SameDimensionArithmeticUsesCoreOperatorsWithoutGeneratedOverloads(string expression, string accessor, double expected)
	{
		string source = $$"""
			public static class Consumer
			{
				public static double Value() => ({{expression}}).{{accessor}}();
			}
			""";
		double result = EvaluateSource<double>(source);

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("firstSquared + secondSquared", 52)]
	[InlineData("firstSquared - secondSquared", 20)]
	[InlineData("-firstSquared", -36)]
	public void SameDimensionArithmeticThroughInferredLocalsCompilesAndExecutes(string expression, double expected)
	{
		string source = $$"""
			public static class Consumer
			{
				public static double Value()
				{
					Current first = Current.FromAmps(3);
					Current second = Current.FromAmps(2);
					Resistance resistance = Resistance.FromSiBaseUnit(4);
					var firstSquared = first * first;
					var secondSquared = second * second;
					var combined = {{expression}};
					var heating = combined * resistance;
					return ((Power)heating).AsSiBaseUnit();
				}
			}
			""";
		double result = EvaluateSource<double>(source);

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("((Current.FromAmps(3) * Current.FromAmps(3) + Current.FromAmps(2) * Current.FromAmps(2)) * Resistance.FromSiBaseUnit(4)).AsSiBaseUnit()", 52)]
	[InlineData("((Current.FromAmps(3) * Current.FromAmps(3) - Current.FromAmps(2) * Current.FromAmps(2)) * Resistance.FromSiBaseUnit(4)).AsSiBaseUnit()", 20)]
	[InlineData("(-(Current.FromAmps(3) * Current.FromAmps(3))).AsSiBaseUnit()", -9)]
	public void SameDimensionArithmeticAroundNestedProductsCompilesAndExecutes(string expression, double expected)
	{
		double result = Evaluate<double>(expression);

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("Length.FromMetres(3) / Length.FromMilliMetres(500)", 6)]
	[InlineData("Energy.FromSiBaseUnit(6) / Torque.FromSiBaseUnit(2)", 3)]
	[InlineData("Current.FromAmps(3) * (2 / Current.FromAmps(3))", 2)]
	[InlineData("(2 / Current.FromAmps(3)) * Current.FromAmps(3)", 2)]
	[InlineData("(Current.FromAmps(3) * Current.FromAmps(3)) / (Current.FromAmps(2) * Current.FromAmps(2))", 2.25)]
	[InlineData("(Length.FromMetres(3) / Length.FromMetres(1)) + (Current.FromAmps(4) / Current.FromAmps(2))", 5)]
	public void CancelledDimensionsCompileAndExecuteAsDouble(string expression, double expected)
	{
		double result = Evaluate<double>(expression);

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("var ratio = first / second;", "(ratio * time).AsSeconds()", 6)]
	[InlineData("var firstSquared = first * first; var secondSquared = second * second; var ratio = firstSquared / secondSquared;", "(ratio * time).AsSeconds()", 9)]
	[InlineData("var inverse = 2 / first; var ratio = first * inverse; var copy = ratio;", "(copy * time).AsSeconds()", 8)]
	[InlineData("var inverse = 2 / first; var ratio = first * inverse;", "(time / ratio).AsSeconds()", 2)]
	[InlineData("var inverse = 2 / first; var ratio = first * inverse; var scaled = (ratio + 1) * first;", "(scaled * time).AsSiBaseUnit()", 36)]
	[InlineData("var ratio = first / second; var inverseTime = ratio / time;", "inverseTime.AsSiBaseUnit()", 0.375)]
	public void CancelledLocalsCanBeUsedAsScalarOperands(string declarations, string expression, double expected)
	{
		string source = $$"""
			public static class Consumer
			{
				public static double Value()
				{
					Current first = Current.FromAmps(3);
					Current second = Current.FromAmps(2);
					Time time = Time.FromSeconds(4);
					{{declarations}}
					return {{expression}};
				}
			}
			""";
		double result = EvaluateSource<double>(source);

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("TerminalHex2", 3, 9)]
	[InlineData("TerminalHex3", 3, 27)]
	[InlineData("TerminalHex1", 3, 3)]
	[InlineData("Hex<TerminalHex0, TerminalHex1>", 2, 65536)]
	[InlineData("Negative<TerminalHex2>", 2, 0.25)]
	[InlineData("Fraction<TerminalHex1, TerminalHex2>", 9, 3)]
	[InlineData("Fraction<TerminalHex2, TerminalHex4>", 9, 3)]
	[InlineData("Fraction<TerminalHex1, TerminalHex3>", -8, -2)]
	[InlineData("Fraction<TerminalHex2, TerminalHex3>", -8, 4)]
	[InlineData("Fraction<Negative<TerminalHex1>, TerminalHex3>", -8, -0.5)]
	public void EncodedPowersCompileAndExecute(string exponent, double value, double expected)
	{
		string source = $$"""
			using DimensionalQuantities.TypeSystem;
			public static class Consumer
			{
				public static double Value() => Power<{{exponent}}>.Of(Length.FromMetres({{value.ToString(System.Globalization.CultureInfo.InvariantCulture)}})).AsSiBaseUnit();
			}
			""";
		double result = EvaluateSource<double>(source);

		Assert.Equal(expected, result, 12);
	}

	[Theory]
	[InlineData("NumberTwo", 3, 9)]
	[InlineData("NumberSeventeen", 2, 131072)]
	[InlineData("NumberMinusTwo", 2, 0.25)]
	[InlineData("NumberHalf", 9, 3)]
	[InlineData("NumberTwoQuarters", 9, 3)]
	[InlineData("NumberTwoOverThree", -8, 4)]
	[InlineData("NumberMinusOneOverThree", -8, -0.5)]
	[InlineData("NumberMillionth", 1, 1)]
	public void EnglishPowersCompileAndExecute(string exponent, double value, double expected)
	{
		string source = $$"""
			using DimensionalQuantities.TypeSystem;
			public static class Consumer
			{
				public static double Value() => Power<{{exponent}}>.Of(Length.FromMetres({{value.ToString(System.Globalization.CultureInfo.InvariantCulture)}})).AsSiBaseUnit();
			}
			""";
		double result = EvaluateSource<double>(source);

		Assert.Equal(expected, result, 12);
	}

	[Fact]
	public void EnglishPowersWorkWithNestedCallsArithmeticAndLocals()
	{
		const string source = """
			using DimensionalQuantities.TypeSystem;
			public static class Consumer
			{
				public static double Value()
				{
					var squared = Power<NumberTwo>.Of(Current.FromAmps(3));
					var root = Power<NumberHalf>.Of(squared);
					Voltage voltage = root * Resistance.FromSiBaseUnit(4);
					Current nested = Power<NumberHalf>.Of(Power<NumberTwo>.Of(root));
					return voltage.AsSiBaseUnit() + nested.AsAmps();
				}
			}
			""";
		double result = EvaluateSource<double>(source);

		Assert.Equal(15, result);
	}

	[Fact]
	public void EnglishLookingUserAliasKeepsItsDeclaredExponent()
	{
		const string source = """
			using DimensionalQuantities.TypeSystem;
			using NumberTwo = DimensionalQuantities.TypeSystem.TerminalHex3;
			public static class Consumer
			{
				public static double Value() => Power<NumberTwo>.Of(Length.FromMetres(3)).AsSiBaseUnit();
			}
			""";
		double result = EvaluateSource<double>(source);

		Assert.Equal(27, result);
	}

	[Fact]
	public void EquivalentEnglishNamesShareTheEncodedTypeAndPowerOverload()
	{
		const string source = """
			using DimensionalQuantities.TypeSystem;
			public static class Consumer
			{
				public static bool Value()
				{
					Length first = Power<NumberHalf>.Of(Area.FromSiBaseUnit(9));
					Length second = Power<NumberTwoQuarters>.Of(Area.FromSiBaseUnit(9));
					Length third = Power<Fraction<TerminalHex1, TerminalHex2>>.Of(Area.FromSiBaseUnit(9));
					return first == second && second == third && typeof(NumberHalf) == typeof(NumberTwoQuarters)
						&& typeof(NumberHalf) == typeof(Fraction<TerminalHex1, TerminalHex2>);
				}
			}
			""";

		Assert.True(EvaluateSource<bool>(source));
	}

	[Fact]
	public void EnglishZerothPowerReturnsDouble()
	{
		const string source = """
			using DimensionalQuantities.TypeSystem;
			public static class Consumer
			{
				public static double Value() => Power<NumberZero>.Of(Length.FromMetres(3));
			}
			""";

		Assert.Equal(1, EvaluateSource<double>(source));
	}

	[Fact]
	public void PowersReturnExistingQuantityTypesWhenCalledThroughAliases()
	{
		const string source = """
			using DimensionalQuantities.TypeSystem;
			using Two = DimensionalQuantities.TypeSystem.TerminalHex2;
			using Half = DimensionalQuantities.TypeSystem.Fraction<DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.TerminalHex2>;
			using Squaring = DimensionalQuantities.TypeSystem.Power<DimensionalQuantities.TypeSystem.TerminalHex2>;
			public static class Consumer
			{
				public static double Value()
				{
					Length length = Length.FromMetres(3);
					Area first = Power<Two>.Of(length);
					Area second = Squaring.Of(length);
					Length root = Power<Half>.Of(first);
					Length equivalent = Power<Fraction<TerminalHex2, TerminalHex4>>.Of(second);
					Volume volume = Power<TerminalHex3>.Of(length);
					return first.AsSiBaseUnit() + second.AsSiBaseUnit() + root.AsSiBaseUnit() + equivalent.AsSiBaseUnit() + volume.AsSiBaseUnit();
				}
			}
			""";
		double result = EvaluateSource<double>(source);
		double expected = 51;

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("var squared = Power<TerminalHex2>.Of(current); var result = squared * resistance;", "((Power)result).AsSiBaseUnit()", 36)]
	[InlineData("var squared = current * current; var result = Power<Fraction<TerminalHex1, TerminalHex2>>.Of(squared);", "((Current)result).AsAmps()", 3)]
	[InlineData("var squared = Power<TerminalHex2>.Of(current); var copy = squared; var result = Power<Fraction<TerminalHex1, TerminalHex2>>.Of(copy);", "((Current)result).AsAmps()", 3)]
	[InlineData("var result = Power<TerminalHex2>.Of(current * resistance);", "result.AsSiBaseUnit()", 144)]
	[InlineData("var result = Power<Fraction<TerminalHex1, TerminalHex2>>.Of(Power<TerminalHex2>.Of(current));", "((Current)result).AsAmps()", 3)]
	[InlineData("var identity = Power<TerminalHex0>.Of(current); var result = identity * current * resistance;", "((Voltage)result).AsSiBaseUnit()", 12)]
	public void PowersWorkWithArithmeticAndLocalVariables(string declarations, string expression, double expected)
	{
		string source = $$"""
			using DimensionalQuantities.TypeSystem;
			public static class Consumer
			{
				public static double Value()
				{
					Current current = Current.FromAmps(3);
					Resistance resistance = Resistance.FromSiBaseUnit(4);
					{{declarations}}
					return {{expression}};
				}
			}
			""";
		double result = EvaluateSource<double>(source);

		Assert.Equal(expected, result, 12);
	}

	[Fact]
	public void ZerothPowerReturnsDouble()
	{
		const string source = """
			using DimensionalQuantities.TypeSystem;
			public static class Consumer
			{
				public static double Value() => Power<TerminalHex0>.Of(Length.FromMetres(3));
			}
			""";
		double result = EvaluateSource<double>(source);

		Assert.Equal(1, result);
	}

	[Theory]
	[InlineData("Squared", 3, 9)]
	[InlineData("Cubed", 3, 27)]
	[InlineData("Pow4", 2, 16)]
	[InlineData("Pow5", 2, 32)]
	[InlineData("Pow6", 2, 64)]
	[InlineData("SquareRoot", 9, 3)]
	[InlineData("CubeRoot", 27, 3)]
	[InlineData("Root4", 16, 2)]
	[InlineData("Root5", 32, 2)]
	[InlineData("Root6", 64, 2)]
	public void ConveniencePowersCompileAndExecute(string method, double value, double expected)
	{
		string source = $$"""
			public static class Consumer
			{
				public static double Value() => Length.FromMetres({{value.ToString(System.Globalization.CultureInfo.InvariantCulture)}}).{{method}}().AsSiBaseUnit();
			}
			""";
		double result = EvaluateSource<double>(source);

		Assert.Equal(expected, result, 12);
	}

	[Fact]
	public void ConveniencePowersShareQuantityTypesWithGenericPowersAndArithmetic()
	{
		const string source = """
			using DimensionalQuantities.TypeSystem;
			public static class Consumer
			{
				public static double Value()
				{
					Length length = Length.FromMetres(3);
					Area area = length.Squared();
					Volume volume = length.Cubed();
					Length root = area.SquareRoot();
					Length cubeRoot = volume.CubeRoot();
					var fourthPower = length.Pow4();
					Length fourthRoot = fourthPower.Root4();
					Area fromGeneric = Power<NumberTwo>.Of(root);
					Area fromProduct = root * cubeRoot;
					return area.AsSiBaseUnit() + volume.AsSiBaseUnit() + fourthRoot.AsMetres()
						+ fromGeneric.AsSiBaseUnit() + fromProduct.AsSiBaseUnit();
				}
			}
			""";
		double result = EvaluateSource<double>(source);

		Assert.Equal(57, result, 12);
	}

	[Theory]
	[InlineData("current.Squared().SquareRoot() * resistance", 12)]
	[InlineData("Power<NumberHalf>.Of(current.Squared()) * resistance", 12)]
	[InlineData("Power<NumberTwo>.Of(current).SquareRoot() * resistance", 12)]
	[InlineData("(current * resistance).Squared().SquareRoot()", 12)]
	public void ConveniencePowersWorkInsideNestedExpressions(string expression, double expected)
	{
		string source = $$"""
			using DimensionalQuantities.TypeSystem;
			public static class Consumer
			{
				public static double Value()
				{
					Current current = Current.FromAmps(3);
					Resistance resistance = Resistance.FromSiBaseUnit(4);
					Voltage voltage = {{expression}};
					return voltage.AsSiBaseUnit();
				}
			}
			""";
		double result = EvaluateSource<double>(source);

		Assert.Equal(expected, result, 12);
	}

	[Fact]
	public void InferredCatalogueQuantityReusesItsDefaultConvenienceMethod()
	{
		const string source = """
			public static class Consumer
			{
				public static double Value()
				{
					Length length = Length.FromMetres(3);
					var area = length * length;
					return area.Squared().AsSiBaseUnit();
				}
			}
			""";
		double result = EvaluateSource<double>(source);

		Assert.Equal(81, result);
	}

	[Fact]
	public void ExistingQuantityExtensionMethodKeepsItsBehaviour()
	{
		const string source = """
			public static class UserMethods { public static double Squared(this Length value) => 42; }
			public static class Consumer { public static double Value() => Length.FromMetres(3).Squared(); }
			""";
		double result = EvaluateSource<double>(source);

		Assert.Equal(42, result);
	}

	[Fact]
	public void ConvenienceMethodCoexistsWithAnExistingOverload()
	{
		const string source = """
			public static class UserMethods { public static double Squared(this Length value, int factor) => value.AsMetres() * factor; }
			public static class Consumer
			{
				public static double[] Value()
				{
					Length length = Length.FromMetres(3);
					return [length.Squared().AsSiBaseUnit(), length.Squared(4)];
				}
			}
			""";
		double[] result = EvaluateSource<double[]>(source);
		double[] expected = [9, 12];

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData(false, 216)]
	[InlineData(true, 72)]
	public void WindingLossHelpersPreserveUnitsAndRoundTripResistance(bool delta, double expectedLoss)
	{
		string source = $$"""
			public static class Consumer
			{
				private static Current PhaseCurrent(Current line, bool delta) => delta ? line / System.Math.Sqrt(3) : line;
				private static Power Loss(Resistance resistance, Current line, bool delta)
				{
					Current phase = PhaseCurrent(line, delta);
					CurrentSquared squared = phase.Squared();
					return 3 * squared * resistance;
				}
				private static Resistance Recover(Power loss, Current line, bool delta)
				{
					Current phase = PhaseCurrent(line, delta);
					CurrentSquared denominator = 3 * phase.Squared();
					return loss / denominator;
				}
				public static double[] Value()
				{
					Current line = Current.FromAmps(12);
					Resistance resistance = Resistance.FromSiBaseUnit(0.5);
					Power loss = Loss(resistance, line, {{delta.ToString().ToLowerInvariant()}});
					Resistance recovered = Recover(loss, line, {{delta.ToString().ToLowerInvariant()}});
					return [loss.AsSiBaseUnit(), recovered.AsSiBaseUnit()];
				}
			}
			""";
		double[] result = EvaluateSource<double[]>(source);

		Assert.Multiple(
			() => Assert.Equal(expectedLoss, result[0], 12),
			() => Assert.Equal(0.5, result[1], 12));
	}

	[Fact]
	public void TypeOnlyParameterSuppliesDimensionsToArithmeticWithoutAnInitialiser()
	{
		const string source = """
			public static class Consumer
			{
				private static LengthTimeCurrent Multiply(LengthCurrent input, Time time) => input * time;
				public static double Value() => Multiply(LengthCurrent.FromSiBaseUnit(6), Time.FromSeconds(4)).AsSiBaseUnit();
			}
			""";
		double result = EvaluateSource<double>(source);

		Assert.Equal(24, result);
	}

	private T Evaluate<T>(string expression)
	{
		string source = $$"""
			public static class Consumer
			{
				public static global::{{typeof(T).FullName}} Value() => {{expression}};
			}
			""";
		return EvaluateSource<T>(source);
	}

	private T EvaluateSource<T>(string source)
	{
		using PackageRepository repository = PackageRepository.Create(_scratch, new Uri("https://api.nuget.org/v3/index.json"));
		Package core = PackCore(repository);
		ProjectCreator consumer = CreateProject("GeneratedConsumer", source, core);
		Build(consumer);
		string output = OutputDirectory("GeneratedConsumer");
		using PackageAssemblyContext context = new(output);
		Type type = context.LoadFromAssemblyPath(Path.Combine(output, "GeneratedConsumer.dll")).GetType("Consumer", throwOnError: true)!;
		return Assert.IsType<T>(type.GetMethod("Value")!.Invoke(null, null));
	}
}
