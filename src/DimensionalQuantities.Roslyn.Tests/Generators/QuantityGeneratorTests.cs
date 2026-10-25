using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.Roslyn.Generators;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generators;

public sealed class QuantityGeneratorTests
{
	private static readonly CSharpParseOptions _parseOptions = new(LanguageVersion.Preview);

	[Fact]
	public void BaseAliasesHaveUnprefixedFactoriesAndAccessors()
	{
		const string source = """
			public static class Consumer
			{
				public static double UseBaseQuantities()
				{
					Length length = Length.FromMetres(1);
					Mass mass = Mass.FromGrams(1000);
					Time time = Time.FromSeconds(1);
					Current current = Current.FromAmps(1);
					AbsoluteTemperature temperature = AbsoluteTemperature.FromKelvin(1);
					AmountOfSubstance amount = AmountOfSubstance.FromMoles(1);
					LuminousIntensity intensity = LuminousIntensity.FromCandelas(1);
					return length.AsMetres() + mass.AsGrams() + time.AsSeconds() + current.AsAmps()
						+ temperature.AsKelvin() + amount.AsMoles() + intensity.AsCandelas();
				}
			}
			""";

		Compile(source);
	}

	[Fact]
	public void SemanticPipelineResolvesPostInitialisationQuantityAliasesAndFactories()
	{
		const string source = """
			public static class Consumer
			{
				public static Length Use(Length input) => Length.FromMetres(2);
			}
			""";
		CSharpCompilation result = Compile(source, new SemanticProbeGenerator());
		INamedTypeSymbol probe = result.GetTypeByMetadataName("SemanticProbe")!;
		IFieldSymbol aliasResolved = Assert.IsAssignableFrom<IFieldSymbol>(Assert.Single(probe.GetMembers("AliasResolved")));
		IFieldSymbol factoryResolved = Assert.IsAssignableFrom<IFieldSymbol>(Assert.Single(probe.GetMembers("FactoryResolved")));

		Assert.Multiple(
			() => Assert.Equal(true, aliasResolved.ConstantValue),
			() => Assert.Equal(true, factoryResolved.ConstantValue));
	}

	[Fact]
	public void CatalogueAliasesAreAvailableToConsumers()
	{
		const string source = """
			public static class Consumer
			{
				public static global::System.Type[] Types() =>
				[
					typeof(Area), typeof(Volume), typeof(Frequency), typeof(AngularVelocity), typeof(Density),
					typeof(Pressure), typeof(Stress), typeof(YoungsModulus), typeof(Energy), typeof(Torque),
					typeof(Power), typeof(SpecificPower), typeof(SpecificPowerLoss), typeof(TemperatureCoefficient),
					typeof(PowerDensity), typeof(VolumetricPowerLoss), typeof(Voltage), typeof(Resistance),
					typeof(Inductance), typeof(MagneticFlux), typeof(FluxLinkage), typeof(MagneticFluxDensity),
					typeof(MagneticPolarisation), typeof(RemanentFluxDensity), typeof(MagneticFieldStrength),
					typeof(CoerciveFieldStrength), typeof(CurrentDensity), typeof(Permeability), typeof(Resistivity),
					typeof(Conductivity), typeof(MagneticReluctivity), typeof(MagneticVectorPotential)
				];
			}
			""";

		Compile(source);
	}

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
				public static Resistance GetResistance() => Resistance.FromSiBaseUnit(4);
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

	[Fact]
	public void GeneratorRefreshesArithmeticAfterAddingEditingAndRemovingConsumerTrees()
	{
		const string firstSource = """
			public static class Consumer
			{
				public static LengthCurrent Value() => Current.FromAmps(2) * Length.FromMetres(3);
			}
			""";
		const string addedSource = """
			public static class Added
			{
				public static LengthTime Value() => Length.FromMetres(2) * Time.FromSeconds(3);
			}
			""";
		const string editedSource = """
			public static class Consumer
			{
				public static Resistance Value() => Voltage.FromSiBaseUnit(12) / Current.FromAmps(3);
			}
			""";
		CSharpCompilation initialInput = CreateInputCompilation(firstSource);
		SyntaxTree initialTree = Assert.Single(initialInput.SyntaxTrees);
		SyntaxTree addedTree = CSharpSyntaxTree.ParseText(addedSource, _parseOptions, path: "Added.cs", cancellationToken: TestContext.Current.CancellationToken);
		SyntaxTree editedTree = CSharpSyntaxTree.ParseText(editedSource, _parseOptions, path: "Consumer.cs", cancellationToken: TestContext.Current.CancellationToken);
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: _parseOptions);

		driver = driver.RunGeneratorsAndUpdateCompilation(initialInput, out Compilation firstOutput, out ImmutableArray<Diagnostic> firstDiagnostics,
			TestContext.Current.CancellationToken);
		Assert.Multiple(
			() => Assert.Empty(firstDiagnostics),
			() => Assert.Empty(firstOutput.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)),
			() => Assert.NotNull(firstOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions")));

		CSharpCompilation addedInput = initialInput.AddSyntaxTrees(addedTree);
		driver = driver.RunGeneratorsAndUpdateCompilation(addedInput, out Compilation addedOutput, out ImmutableArray<Diagnostic> addedDiagnostics,
			TestContext.Current.CancellationToken);
		Assert.Multiple(
			() => Assert.Empty(addedDiagnostics),
			() => Assert.Empty(addedOutput.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)),
			() => Assert.NotNull(addedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions")),
			() => Assert.NotNull(addedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeExtensions")));

		CSharpCompilation editedInput = addedInput.ReplaceSyntaxTree(initialTree, editedTree);
		driver = driver.RunGeneratorsAndUpdateCompilation(editedInput, out Compilation editedOutput, out ImmutableArray<Diagnostic> editedDiagnostics,
			TestContext.Current.CancellationToken);
		Dimension length = new(Length: 1);
		Dimension time = new(Time: 1);
		Dimension lengthTime = new(Length: 1, Time: 1);
		Dimension voltage = new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -1);
		Dimension current = new(ElectricCurrent: 1);
		Dimension resistance = new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -2);
		ArithmeticOperation addedOperation = new(ArithmeticOperationKind.Multiply, length, time, lengthTime);
		ArithmeticOperation editedOperation = new(ArithmeticOperationKind.Divide, voltage, current, resistance);
		ArithmeticOperation[] editedOperators = ReadOperators(editedOutput);
		Assert.Multiple(
			() => Assert.Empty(editedDiagnostics),
			() => Assert.Empty(editedOutput.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)),
			() => Assert.Null(editedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions")),
			() => Assert.NotNull(editedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeExtensions")),
			() => Assert.Equal(2, editedOperators.Length),
			() => Assert.Contains(addedOperation, editedOperators),
			() => Assert.Contains(editedOperation, editedOperators));

		CSharpCompilation removedInput = editedInput.RemoveSyntaxTrees(addedTree);
		driver = driver.RunGeneratorsAndUpdateCompilation(removedInput, out Compilation removedOutput, out ImmutableArray<Diagnostic> removedDiagnostics,
			TestContext.Current.CancellationToken);
		ArithmeticOperation[] removedOperators = ReadOperators(removedOutput);
		Assert.Multiple(
			() => Assert.Empty(removedDiagnostics),
			() => Assert.Empty(removedOutput.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)),
			() => Assert.Null(removedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions")),
			() => Assert.Null(removedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeExtensions")),
			() => Assert.Equal(editedOperation, Assert.Single(removedOperators)));

		CSharpCompilation emptyInput = removedInput.RemoveSyntaxTrees(editedTree);
		driver.RunGeneratorsAndUpdateCompilation(emptyInput, out Compilation emptyOutput, out ImmutableArray<Diagnostic> emptyDiagnostics,
			TestContext.Current.CancellationToken);
		Assert.Multiple(
			() => Assert.Empty(emptyDiagnostics),
			() => Assert.Empty(emptyOutput.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)),
			() => Assert.Null(emptyOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.ArithmeticExtensions")),
			() => Assert.Null(emptyOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions")),
			() => Assert.Null(emptyOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeExtensions")));
	}

	[Fact]
	public void GeneratorIgnoresUnrelatedAndIncompleteArithmetic()
	{
		const string source = """
			public static class Consumer
			{
				public static int Number(int left, int right) => left * right;
				public static object Incomplete(Length length) => length * Missing;
			}
			""";
		CSharpCompilation input = CreateInputCompilation(source);
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: _parseOptions);
		driver.RunGeneratorsAndUpdateCompilation(input, out Compilation output, out ImmutableArray<Diagnostic> generatorDiagnostics,
			TestContext.Current.CancellationToken);

		Assert.Multiple(
			() => Assert.Empty(generatorDiagnostics),
			() => Assert.Null(output.GetTypeByMetadataName("DimensionalQuantities.Generated.ArithmeticExtensions")));
	}



	[Fact]
	public void LocalInitialiserEditsRefreshDependentOperatorsAndRemoveObsoleteOutput()
	{
		const string source = """
			public static class Consumer
			{
				public static double Value(Current current, Length length, Time time)
				{
					var product = current * length;
					var result = product * time;
					return result.AsSiBaseUnit();
				}
			}
			""";
		string editedSource = source.Replace("current * length", "length * length", StringComparison.Ordinal);
		const string removedSource = "public static class Consumer { public static double Value() => 0; }";
		CSharpCompilation input = CreateInputCompilation(source);
		SyntaxTree originalTree = Assert.Single(input.SyntaxTrees);
		SyntaxTree editedTree = CSharpSyntaxTree.ParseText(editedSource, _parseOptions, path: "Consumer.cs", cancellationToken: TestContext.Current.CancellationToken);
		SyntaxTree removedTree = CSharpSyntaxTree.ParseText(removedSource, _parseOptions, path: "Consumer.cs", cancellationToken: TestContext.Current.CancellationToken);
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: _parseOptions);
		Dimension current = new(ElectricCurrent: 1);
		Dimension lengthCurrent = new(Length: 1, ElectricCurrent: 1);
		Dimension length = new(Length: 1);
		Dimension area = new(Length: 2);
		Dimension time = new(Time: 1);
		ArithmeticOperation[] originalExpected =
		[
			new(ArithmeticOperationKind.Multiply, current, length, lengthCurrent),
			new(ArithmeticOperationKind.Multiply, lengthCurrent, time, new(Length: 1, Time: 1, ElectricCurrent: 1))
		];
		ArithmeticOperation[] editedExpected =
		[
			new(ArithmeticOperationKind.Multiply, length, length, area),
			new(ArithmeticOperationKind.Multiply, area, time, new(Length: 2, Time: 1))
		];

		driver = driver.RunGeneratorsAndUpdateCompilation(input, out Compilation originalOutput, out ImmutableArray<Diagnostic> originalDiagnostics, TestContext.Current.CancellationToken);
		Assert.Multiple(
			() => Assert.Empty(originalDiagnostics),
			() => Assert.Empty(originalOutput.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)),
			() => Assert.Equal(originalExpected, ReadOperators(originalOutput)));

		CSharpCompilation editedInput = input.ReplaceSyntaxTree(originalTree, editedTree);
		driver = driver.RunGeneratorsAndUpdateCompilation(editedInput, out Compilation editedOutput, out ImmutableArray<Diagnostic> editedDiagnostics, TestContext.Current.CancellationToken);
		Assert.Multiple(
			() => Assert.Empty(editedDiagnostics),
			() => Assert.Empty(editedOutput.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)),
			() => Assert.Equal(editedExpected, ReadOperators(editedOutput)),
			() => Assert.Null(editedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions")),
			() => Assert.Null(editedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeCurrentExtensions")));

		CSharpCompilation removedInput = editedInput.ReplaceSyntaxTree(editedTree, removedTree);
		driver.RunGeneratorsAndUpdateCompilation(removedInput, out Compilation removedOutput, out ImmutableArray<Diagnostic> removedDiagnostics, TestContext.Current.CancellationToken);
		Assert.Multiple(
			() => Assert.Empty(removedDiagnostics),
			() => Assert.Empty(removedOutput.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)),
			() => Assert.Null(removedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.ArithmeticExtensions")),
			() => Assert.Null(removedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthSquaredTimeExtensions")));
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
		CSharpCompilation compilation = Compile(source);

		Assert.Multiple(
			() => Assert.Equal(expected, result),
			() => Assert.Null(compilation.GetTypeByMetadataName("DimensionalQuantities.Generated.ArithmeticExtensions")));
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
	[InlineData("current + length")]
	[InlineData("current - length")]
	[InlineData("current + 2")]
	[InlineData("2 - current")]
	public void InvalidSameDimensionArithmeticRemainsACompilerError(string expression)
	{
		string source = $$"""
			public static class Consumer
			{
				public static object Value(Current current, Length length) => {{expression}};
			}
			""";
		CSharpCompilation input = CreateInputCompilation(source);
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: _parseOptions);
		driver.RunGeneratorsAndUpdateCompilation(input, out Compilation output, out ImmutableArray<Diagnostic> diagnostics, TestContext.Current.CancellationToken);
		Diagnostic[] errors = output.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

		Assert.Multiple(
			() => Assert.Empty(diagnostics),
			() => Assert.Equal("CS0019", Assert.Single(errors).Id));
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
	public void GeneratesEnglishAliasesOnlyWhenUsedByPowerCalls()
	{
		CSharpCompilation unused = Compile("class Consumer { }");
		const string source = """
			using DimensionalQuantities.TypeSystem;
			class Consumer { object Value(Length value) => Power<NumberTwo>.Of(value); }
			""";
		CSharpCompilation used = Compile(source);
		string[] unusedNames = unused.SyntaxTrees.SelectMany(tree => tree.GetCompilationUnitRoot(TestContext.Current.CancellationToken).Usings)
			.Where(directive => directive.Alias is not null).Select(directive => directive.Alias!.Name.Identifier.ValueText).ToArray();
		string[] usedNames = used.SyntaxTrees.SelectMany(tree => tree.GetCompilationUnitRoot(TestContext.Current.CancellationToken).Usings)
			.Where(directive => directive.Alias is not null).Select(directive => directive.Alias!.Name.Identifier.ValueText).ToArray();
		string[] result = usedNames.Except(unusedNames).ToArray();
		string[] expected = ["NumberTwo"];

		Assert.Multiple(
			() => Assert.DoesNotContain("NumberTwo", unusedNames),
			() => Assert.DoesNotContain("NumberHalf", unusedNames),
			() => Assert.Equal(expected, result));
	}

	[Theory]
	[InlineData("typeof(NumberTwo)")]
	[InlineData("Power<NumberTwo>.Other(value)")]
	[InlineData("Other.Power<NumberTwo>.Of(value)")]
	public void EnglishNamesOutsideSupportedPowerCallsRemainUnresolved(string expression)
	{
		string source = $$"""
			using DimensionalQuantities.TypeSystem;
			namespace Other { public struct Power<T> { public static object Of(object value) => value; } }
			class Consumer { object Value(Length value) => {{expression}}; }
			""";
		CSharpCompilation input = CreateInputCompilation(source);
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: _parseOptions);
		driver.RunGeneratorsAndUpdateCompilation(input, out Compilation output, out ImmutableArray<Diagnostic> diagnostics, TestContext.Current.CancellationToken);
		string[] aliases = output.SyntaxTrees.SelectMany(tree => tree.GetCompilationUnitRoot(TestContext.Current.CancellationToken).Usings)
			.Where(directive => directive.Alias is not null).Select(directive => directive.Alias!.Name.Identifier.ValueText).ToArray();

		Assert.Multiple(
			() => Assert.Empty(diagnostics),
			() => Assert.DoesNotContain("NumberTwo", aliases),
			() => Assert.Contains(output.GetDiagnostics(TestContext.Current.CancellationToken), diagnostic => diagnostic.Id == "CS0246" && diagnostic.GetMessage().Contains("NumberTwo", StringComparison.Ordinal)));
	}

	[Fact]
	public void RemovingEnglishPowerCallsRemovesTheirAliases()
	{
		const string source = """
			using DimensionalQuantities.TypeSystem;
			class Consumer { object Value(Length value) => Power<NumberTwo>.Of(value); }
			""";
		CSharpCompilation input = CreateInputCompilation(source);
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: _parseOptions);
		driver = driver.RunGeneratorsAndUpdateCompilation(input, out Compilation original, out _, TestContext.Current.CancellationToken);
		CSharpCompilation emptyInput = input.RemoveAllSyntaxTrees();
		driver.RunGeneratorsAndUpdateCompilation(emptyInput, out Compilation removed, out ImmutableArray<Diagnostic> diagnostics, TestContext.Current.CancellationToken);
		string[] originalNames = original.SyntaxTrees.SelectMany(tree => tree.GetCompilationUnitRoot(TestContext.Current.CancellationToken).Usings)
			.Where(directive => directive.Alias is not null).Select(directive => directive.Alias!.Name.Identifier.ValueText).ToArray();
		string[] remainingNames = removed.SyntaxTrees.SelectMany(tree => tree.GetCompilationUnitRoot(TestContext.Current.CancellationToken).Usings)
			.Where(directive => directive.Alias is not null).Select(directive => directive.Alias!.Name.Identifier.ValueText).ToArray();

		Assert.Multiple(
			() => Assert.Contains("NumberTwo", originalNames),
			() => Assert.DoesNotContain("NumberTwo", remainingNames),
			() => Assert.Empty(diagnostics),
			() => Assert.Empty(removed.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)));
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
	public void ExplicitAliasInAnotherFileReservesTheName()
	{
		const string generatedAliasSource = """
			using DimensionalQuantities.TypeSystem;
			class First { public Area Value(Length value) => Power<NumberTwo>.Of(value); }
			""";
		const string explicitAliasSource = """
			using DimensionalQuantities.TypeSystem;
			using NumberTwo = DimensionalQuantities.TypeSystem.TerminalHex3;
			class Second { public Volume Value(Length value) => Power<NumberTwo>.Of(value); }
			""";
		CSharpCompilation input = CreateInputCompilation(generatedAliasSource).AddSyntaxTrees(
			CSharpSyntaxTree.ParseText(explicitAliasSource, _parseOptions, path: "Second.cs", cancellationToken: TestContext.Current.CancellationToken));
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: _parseOptions);
		driver.RunGeneratorsAndUpdateCompilation(input, out Compilation output, out ImmutableArray<Diagnostic> diagnostics, TestContext.Current.CancellationToken);
		Diagnostic[] errors = output.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

		Assert.Multiple(
			() => Assert.Empty(diagnostics),
			() => Assert.Contains(errors, diagnostic => diagnostic.Id == "CS0246" && diagnostic.GetMessage().Contains("NumberTwo", StringComparison.Ordinal)),
			() => Assert.DoesNotContain(errors, diagnostic => diagnostic.Location.SourceTree?.FilePath == "Second.cs"),
			() => Assert.DoesNotContain(output.SyntaxTrees.SelectMany(tree => tree.GetCompilationUnitRoot(TestContext.Current.CancellationToken).Usings),
				directive => directive.Alias?.Name.Identifier.ValueText == "NumberTwo" && directive.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword)));
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
	[InlineData("Two")]
	[InlineData("NumberNotANumber", "DQ0007")]
	public void UnsupportedExponentNamesRemainCompilerErrors(string name, params string[] expectedDiagnosticIds)
	{
		string source = $$"""
			using DimensionalQuantities.TypeSystem;
			public static class Consumer
			{
				public static object Value(Length length) => Power<{{name}}>.Of(length);
			}
			""";
		CSharpCompilation input = CreateInputCompilation(source);
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: _parseOptions);
		driver.RunGeneratorsAndUpdateCompilation(input, out Compilation output, out ImmutableArray<Diagnostic> diagnostics, TestContext.Current.CancellationToken);

		Assert.Multiple(
			() => Assert.Equal(expectedDiagnosticIds, diagnostics.Select(diagnostic => diagnostic.Id)),
			() => Assert.Contains(output.GetDiagnostics(TestContext.Current.CancellationToken), diagnostic => diagnostic.Id == "CS0246" && diagnostic.GetMessage().Contains(name, StringComparison.Ordinal)));
	}

	[Theory]
	[InlineData("TerminalHex2", "TerminalHex3")]
	[InlineData("NumberTwo", "NumberThree")]
	public void EditingAndRemovingPowerCallsRemovesObsoleteGeneratedTypes(string originalExponent, string editedExponent)
	{
		string source = $$"""
			using DimensionalQuantities.TypeSystem;
			public static class Consumer
			{
				public static object Value(Current current) => Power<{{originalExponent}}>.Of(current * Length.FromMetres(1));
			}
			""";
		CSharpCompilation input = CreateInputCompilation(source);
		SyntaxTree originalTree = input.SyntaxTrees.Single();
		SyntaxTree editedTree = CSharpSyntaxTree.ParseText(source.Replace(originalExponent, editedExponent, StringComparison.Ordinal), _parseOptions, path: "Consumer.cs", cancellationToken: TestContext.Current.CancellationToken);
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: _parseOptions);
		driver = driver.RunGeneratorsAndUpdateCompilation(input, out Compilation originalOutput, out ImmutableArray<Diagnostic> originalDiagnostics, TestContext.Current.CancellationToken);

		Assert.Multiple(
			() => Assert.Empty(originalDiagnostics),
			() => Assert.Empty(originalOutput.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)),
			() => Assert.NotNull(originalOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthSquaredCurrentSquaredExtensions")));

		CSharpCompilation editedInput = input.ReplaceSyntaxTree(originalTree, editedTree);
		driver = driver.RunGeneratorsAndUpdateCompilation(editedInput, out Compilation editedOutput, out ImmutableArray<Diagnostic> editedDiagnostics, TestContext.Current.CancellationToken);
		Assert.Multiple(
			() => Assert.Empty(editedDiagnostics),
			() => Assert.Empty(editedOutput.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)),
			() => Assert.Null(editedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthSquaredCurrentSquaredExtensions")),
			() => Assert.NotNull(editedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCubedCurrentCubedExtensions")));

		CSharpCompilation emptyInput = editedInput.RemoveSyntaxTrees(editedTree);
		driver.RunGeneratorsAndUpdateCompilation(emptyInput, out Compilation emptyOutput, out ImmutableArray<Diagnostic> emptyDiagnostics, TestContext.Current.CancellationToken);
		Assert.Multiple(
			() => Assert.Empty(emptyDiagnostics),
			() => Assert.Empty(emptyOutput.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)),
			() => Assert.Null(emptyOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCubedCurrentCubedExtensions")));
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
	public void GeneratesOnlyTheRequestedIntermediateConvenienceOverloadOnce()
	{
		const string source = """
			class Consumer
			{
				object First(Length length, Current current) => (length * current).Squared();
				object Second(Length length, Current current) => (length * current).Squared();
			}
			""";
		CSharpCompilation output = Compile(source);
		string[] convenienceNames = ["Squared", "Cubed", "Pow4", "Pow5", "Pow6", "SquareRoot", "CubeRoot", "Root4", "Root5", "Root6"];
		string[] result = ReadConvenienceMethods(output, new(Length: 1, ElectricCurrent: 1)).Where(convenienceNames.Contains).ToArray();
		string[] expected = ["Squared"];

		Assert.Equal(expected, result);
	}

	[Fact]
	public void BaseAndCatalogueHelpersAreAvailableWithoutConsumerCode()
	{
		CSharpCompilation result = Compile(string.Empty);
		string[] expected = ["Squared", "Cubed", "Pow4", "Pow5", "Pow6", "SquareRoot", "CubeRoot", "Root4", "Root5", "Root6"];
		Dimension[] dimensions = QuantityDiscovery.Discover([]).KnownQuantities.Select(quantity => quantity.Dimension).ToArray();

		Assert.All(dimensions, dimension => Assert.Equal(expected.Order(), ReadConvenienceMethods(result, dimension).Where(expected.Contains).Order()));
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
	public void CatalogueConvenienceMethodsAreBoundBeforeDemandGeneration()
	{
		const string source = """
			public static class Consumer
			{
				public static object Value(Voltage voltage) => voltage.Squared();
			}
			""";
		CSharpCompilation result = Compile(source, new SemanticProbeGenerator());
		INamedTypeSymbol probe = result.GetTypeByMetadataName("SemanticProbe")!;
		IFieldSymbol resolved = Assert.IsAssignableFrom<IFieldSymbol>(Assert.Single(probe.GetMembers("FactoryResolved")));

		Assert.Equal(true, resolved.ConstantValue);
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
	[InlineData("(3d).Squared()", "CS1929")]
	[InlineData("Length.FromMetres(3).Pow7()", "CS1061")]
	public void UnsupportedConvenienceCallsRemainCompilerErrors(string expression, string diagnosticId)
	{
		string source = $$"""class Consumer { object Value() => {{expression}}; }""";
		CSharpCompilation input = CreateInputCompilation(source);
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: _parseOptions);
		driver.RunGeneratorsAndUpdateCompilation(input, out Compilation output, out ImmutableArray<Diagnostic> diagnostics, TestContext.Current.CancellationToken);

		Assert.Multiple(
			() => Assert.Empty(diagnostics),
			() => Assert.Contains(output.GetDiagnostics(TestContext.Current.CancellationToken), diagnostic => diagnostic.Id == diagnosticId));
	}

	[Fact]
	public void RemovingIntermediateConvenienceCallsRemovesTheirGeneratedMethods()
	{
		const string source = "class Consumer { object Value(Length length, Current current) => (length * current).Squared(); }";
		CSharpCompilation input = CreateInputCompilation(source);
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: _parseOptions);
		driver = driver.RunGeneratorsAndUpdateCompilation(input, out Compilation original, out _, TestContext.Current.CancellationToken);
		driver.RunGeneratorsAndUpdateCompilation(input.RemoveAllSyntaxTrees(), out Compilation removed, out ImmutableArray<Diagnostic> diagnostics, TestContext.Current.CancellationToken);
		string[] originalMethods = ReadConvenienceMethods(original, new(Length: 1, ElectricCurrent: 1));
		string[] remainingMethods = ReadConvenienceMethods(removed, new(Length: 1, ElectricCurrent: 1));

		Assert.Multiple(
			() => Assert.Contains("Squared", originalMethods),
			() => Assert.DoesNotContain("Squared", remainingMethods),
			() => Assert.Empty(diagnostics));
	}



	[Fact]
	public void EditingExplicitLocalUpdatesItsDependentArithmetic()
	{
		const string source = """
			class Consumer
			{
				object Value(Length length, Current current, Time time)
				{
					LengthCurrent value = length * current;
					return value * time;
				}
			}
			""";
		string edited = source.Replace("LengthCurrent value = length * current", "LengthTime value = length * time", StringComparison.Ordinal);
		CSharpCompilation input = CreateInputCompilation(source);
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: _parseOptions);
		driver = driver.RunGeneratorsAndUpdateCompilation(input, out _, out _, TestContext.Current.CancellationToken);
		CSharpCompilation changed = input.ReplaceSyntaxTree(input.SyntaxTrees.Single(), CSharpSyntaxTree.ParseText(edited, _parseOptions, path: "Consumer.cs", cancellationToken: TestContext.Current.CancellationToken));
		driver.RunGeneratorsAndUpdateCompilation(changed, out Compilation result, out ImmutableArray<Diagnostic> diagnostics, TestContext.Current.CancellationToken);

		Assert.Multiple(
			() => Assert.Empty(diagnostics),
			() => Assert.Empty(result.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)),
			() => Assert.Null(result.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions")),
			() => Assert.Null(result.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeCurrentExtensions")),
			() => Assert.NotNull(result.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeSquaredExtensions")));
	}


	[Fact]
	public void EditingConditionalBranchesRemovesObsoleteDependentOperations()
	{
		const string source = """
			class Consumer
			{
				object Value(bool choose, Length length, Current current, Time time)
				{
					var product = choose ? length * current : length * current;
					return product * time;
				}
			}
			""";
		string edited = source.Replace("length * current", "length * time", StringComparison.Ordinal);
		CSharpCompilation input = CreateInputCompilation(source);
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: _parseOptions);
		driver = driver.RunGeneratorsAndUpdateCompilation(input, out _, out _, TestContext.Current.CancellationToken);
		SyntaxTree replacement = CSharpSyntaxTree.ParseText(edited, _parseOptions, path: "Consumer.cs", cancellationToken: TestContext.Current.CancellationToken);
		driver.RunGeneratorsAndUpdateCompilation(input.ReplaceSyntaxTree(input.SyntaxTrees.Single(), replacement), out Compilation result, out ImmutableArray<Diagnostic> diagnostics, TestContext.Current.CancellationToken);

		Assert.Multiple(
			() => Assert.Empty(diagnostics),
			() => Assert.Empty(result.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)),
			() => Assert.Null(result.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeCurrentExtensions")),
			() => Assert.NotNull(result.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeSquaredExtensions")));
	}


	[Fact]
	public void EditingMemberDefinitionRemovesObsoleteDependentOperations()
	{
		const string source = "class Inputs { public static LengthCurrent Stored => Length.FromMetres(2) * Current.FromAmps(3); }";
		const string consumer = "class Consumer { object Value(Time time) => Inputs.Stored * time; }";
		string edited = source.Replace("LengthCurrent", "LengthTime", StringComparison.Ordinal).Replace("Current.FromAmps", "Time.FromSeconds", StringComparison.Ordinal);
		SyntaxTree memberTree = CSharpSyntaxTree.ParseText(source, _parseOptions, path: "Inputs.cs", cancellationToken: TestContext.Current.CancellationToken);
		CSharpCompilation input = CreateInputCompilation(consumer).AddSyntaxTrees(memberTree);
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: _parseOptions);
		driver = driver.RunGeneratorsAndUpdateCompilation(input, out _, out _, TestContext.Current.CancellationToken);
		SyntaxTree replacement = CSharpSyntaxTree.ParseText(edited, _parseOptions, path: "Inputs.cs", cancellationToken: TestContext.Current.CancellationToken);
		driver.RunGeneratorsAndUpdateCompilation(input.ReplaceSyntaxTree(memberTree, replacement), out Compilation result, out ImmutableArray<Diagnostic> diagnostics, TestContext.Current.CancellationToken);

		Assert.Multiple(
			() => Assert.Empty(diagnostics),
			() => Assert.Empty(result.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)),
			() => Assert.Null(result.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeCurrentExtensions")),
			() => Assert.NotNull(result.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeSquaredExtensions")));
	}


	[Fact]
	public void EditingMethodDefinitionRefreshesUnchangedConsumers()
	{
		const string source = "class Inputs { public static LengthCurrent Make() => Length.FromMetres(2) * Current.FromAmps(3); }";
		const string consumer = "class Consumer { object Value(Time time) => Inputs.Make() * time; }";
		string edited = source.Replace("LengthCurrent", "LengthTime", StringComparison.Ordinal).Replace("Current.FromAmps", "Time.FromSeconds", StringComparison.Ordinal);
		SyntaxTree memberTree = CSharpSyntaxTree.ParseText(source, _parseOptions, path: "Inputs.cs", cancellationToken: TestContext.Current.CancellationToken);
		CSharpCompilation input = CreateInputCompilation(consumer).AddSyntaxTrees(memberTree);
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: _parseOptions);
		driver = driver.RunGeneratorsAndUpdateCompilation(input, out _, out _, TestContext.Current.CancellationToken);
		SyntaxTree replacement = CSharpSyntaxTree.ParseText(edited, _parseOptions, path: "Inputs.cs", cancellationToken: TestContext.Current.CancellationToken);
		driver.RunGeneratorsAndUpdateCompilation(input.ReplaceSyntaxTree(memberTree, replacement), out Compilation result, out ImmutableArray<Diagnostic> diagnostics, TestContext.Current.CancellationToken);

		Assert.Multiple(
			() => Assert.Empty(diagnostics),
			() => Assert.Empty(result.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)),
			() => Assert.Null(result.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeCurrentExtensions")),
			() => Assert.NotNull(result.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeSquaredExtensions")));
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
	public void TypeOnlyDeclarationsGenerateQuantitiesInPublicApisAndCollections()
	{
		const string source = """
			public record Measurement(LengthCurrent Value);
			public class Consumer
			{
				public LengthCurrent Field;
				public LengthCurrent Property { get; set; }
				public System.Collections.Generic.List<LengthCurrent> Values { get; } = new();
				public LengthCurrent[] Copy(LengthCurrent[] values) => values;
				public LengthCurrent Identity(LengthCurrent value) => value;
			}
			""";
		CSharpCompilation result = Compile(source);

		Assert.NotNull(result.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions"));
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






	[Theory]
	[InlineData("public class LengthCurrent { }")]
	[InlineData("using LengthCurrent = System.Int32;")]
	public void ExistingTypesAndAliasesAreNotReplacedByQuantityNames(string declaration)
	{
		string source = $$"""{{declaration}} class Consumer { object Value(LengthCurrent value) => value; }""";
		CSharpCompilation result = Compile(source);

		Assert.Null(result.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions"));
	}

	[Fact]
	public void QuantityLookingVariableNamesDoNotRequestTypes()
	{
		const string source = "class Consumer { int Value() { int LengthCurrent = 7; return LengthCurrent; } }";
		CSharpCompilation result = Compile(source);

		Assert.Null(result.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions"));
	}

	[Fact]
	public void TypeNameDiscoveryPreservesExplicitAliasesInOtherFiles()
	{
		const string source = "class First { public LengthCurrent Value; }";
		const string aliasSource = "using LengthCurrent = System.Int32; class Second { public int Value(LengthCurrent value) => value; }";
		CSharpCompilation input = CreateInputCompilation(source).AddSyntaxTrees(CSharpSyntaxTree.ParseText(aliasSource, _parseOptions, path: "Alias.cs", cancellationToken: TestContext.Current.CancellationToken));
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: _parseOptions);
		driver.RunGeneratorsAndUpdateCompilation(input, out Compilation result, out ImmutableArray<Diagnostic> diagnostics, TestContext.Current.CancellationToken);
		Diagnostic[] errors = result.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

		Assert.Multiple(
			() => Assert.Empty(diagnostics),
			() => Assert.Contains(errors, error => error.Id == "CS0246" && error.Location.SourceTree?.FilePath == "Consumer.cs"),
			() => Assert.DoesNotContain(errors, error => error.Location.SourceTree?.FilePath == "Alias.cs"));
	}

	[Fact]
	public void RemovingTypeOnlyDeclarationsRemovesTheirQuantities()
	{
		const string source = "class Consumer { public LengthCurrent Identity(LengthCurrent value) => value; }";
		CSharpCompilation input = CreateInputCompilation(source);
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: _parseOptions);
		driver = driver.RunGeneratorsAndUpdateCompilation(input, out Compilation original, out _, TestContext.Current.CancellationToken);
		driver.RunGeneratorsAndUpdateCompilation(input.RemoveAllSyntaxTrees(), out Compilation result, out ImmutableArray<Diagnostic> diagnostics, TestContext.Current.CancellationToken);

		Assert.Multiple(
			() => Assert.NotNull(original.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions")),
			() => Assert.Null(result.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions")),
			() => Assert.Empty(diagnostics));
	}

	[Theory]
	[InlineData("Library.Create()")]
	[InlineData("Library.Field")]
	[InlineData("Library.Property")]
	[InlineData("Library.Values()[0]")]
	[InlineData("Library.Measure().Value")]
	public void ReferencedQuantityValuesGetLocalConversionsWithoutArithmetic(string expression)
	{
		PortableExecutableReference library = CreateLibraryReference("""
			public static class Library
			{
				public static LengthCurrent Create() => Length.FromMetres(2) * Current.FromAmps(3);
				public static LengthCurrent Field;
				public static LengthCurrent Property => Create();
				public static System.Collections.Generic.List<LengthCurrent> Values() => new() { Create() };
				public static Measurement Measure() => new();
			}
			public sealed class Measurement { public LengthCurrent Value => Library.Create(); }
			""");
		CSharpCompilation input = CreateInputCompilation($"public class Consumer {{ public double Value() => {expression}.AsPrefixedUnits(3, 0); }}").AddReferences(library);
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: _parseOptions);

		driver.RunGeneratorsAndUpdateCompilation(input, out Compilation result, out ImmutableArray<Diagnostic> diagnostics, TestContext.Current.CancellationToken);
		Diagnostic[] errors = result.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

		Assert.Multiple(
			() => Assert.Empty(diagnostics),
			() => Assert.Empty(errors),
			() => Assert.NotNull(result.Assembly.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions")));
	}

	[Fact]
	public void ReferencedCollectionTypesRequestTheirNestedQuantity()
	{
		PortableExecutableReference library = CreateLibraryReference("public static class Library { public static System.Collections.Generic.List<LengthCurrent[]> Values() => new(); }");
		CSharpCompilation input = CreateInputCompilation("public class Consumer { public object Value() => Library.Values(); }").AddReferences(library);
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: _parseOptions);

		driver.RunGeneratorsAndUpdateCompilation(input, out Compilation result, out ImmutableArray<Diagnostic> diagnostics, TestContext.Current.CancellationToken);

		Assert.Multiple(
			() => Assert.Empty(diagnostics),
			() => Assert.NotNull(result.Assembly.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions")));
	}

	[Fact]
	public void RemovingMetadataUsageRemovesItsLocallyGeneratedConversions()
	{
		PortableExecutableReference library = CreateLibraryReference("public static class Library { public static LengthCurrent Create() => default; }");
		CSharpCompilation input = CreateInputCompilation("public class Consumer { public object Value() => Library.Create(); }").AddReferences(library);
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: _parseOptions);
		driver = driver.RunGeneratorsAndUpdateCompilation(input, out Compilation original, out _, TestContext.Current.CancellationToken);

		driver.RunGeneratorsAndUpdateCompilation(input.RemoveAllSyntaxTrees(), out Compilation result, out ImmutableArray<Diagnostic> diagnostics, TestContext.Current.CancellationToken);

		Assert.Multiple(
			() => Assert.NotNull(original.Assembly.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions")),
			() => Assert.Null(result.Assembly.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions")),
			() => Assert.Empty(diagnostics));
	}

	[Fact]
	public void ReferencedClassesWithQuantityNamesAreNotTreatedAsQuantities()
	{
		PortableExecutableReference library = CreateLibraryReference("public class LengthCurrent { } public static class Library { public static LengthCurrent Create() => new(); }");
		CSharpCompilation input = CreateInputCompilation("public class Consumer { public object Value() => Library.Create(); }").AddReferences(library);
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: _parseOptions);

		driver.RunGeneratorsAndUpdateCompilation(input, out Compilation result, out ImmutableArray<Diagnostic> diagnostics, TestContext.Current.CancellationToken);

		Assert.Multiple(
			() => Assert.Empty(diagnostics),
			() => Assert.Null(result.Assembly.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions")));
	}

	[Fact]
	public void MetadataDiscoveryPreservesExplicitConsumerAliases()
	{
		PortableExecutableReference library = CreateLibraryReference("public static class Library { public static LengthCurrent Create() => default; }");
		CSharpCompilation input = CreateInputCompilation("using LengthCurrent = System.Int32; public class Consumer { public object Value() => Library.Create(); public LengthCurrent Count => 1; }").AddReferences(library);
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: _parseOptions);

		driver.RunGeneratorsAndUpdateCompilation(input, out Compilation result, out ImmutableArray<Diagnostic> diagnostics, TestContext.Current.CancellationToken);
		Diagnostic[] errors = result.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

		Assert.Multiple(
			() => Assert.Empty(diagnostics),
			() => Assert.Empty(errors));
	}

	[Theory]
	[InlineData("public class LengthCurrent { public int Marker { get; init; } }", "LengthCurrent", "new() { Marker = 7 }")]
	[InlineData("namespace LengthCurrent { public class Item { } }", "LengthCurrent.Item", "new()")]
	public void MetadataDiscoveryPreservesExistingConsumerTypesAndNamespaces(string declaration, string type, string value)
	{
		PortableExecutableReference library = CreateLibraryReference("public static class Library { public static LengthCurrent Create() => default; }");
		string source = $$"""{{declaration}} public class Consumer { public object Value() => Library.Create(); public {{type}} Item => {{value}}; }""";
		CSharpCompilation input = CreateInputCompilation(source).AddReferences(library);
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: _parseOptions);

		driver.RunGeneratorsAndUpdateCompilation(input, out Compilation result, out ImmutableArray<Diagnostic> diagnostics, TestContext.Current.CancellationToken);
		Diagnostic[] errors = result.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

		Assert.Multiple(
			() => Assert.Empty(diagnostics),
			() => Assert.Empty(errors));
	}

	[Fact]
	public void ReferencedGenericResultsGetConversionsAfterTheirArgumentsResolve()
	{
		PortableExecutableReference library = CreateLibraryReference("public static class Library { public static LengthTime Convert<T>(T value) => default; }");
		const string source = """
			public static class Consumer
			{
				public static double Value(Length length, Current current) => Library.Convert(length * current).AsPrefixedUnits(0, 0);
			}
			""";
		CSharpCompilation input = CreateInputCompilation(source).AddReferences(library);
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: _parseOptions);

		driver.RunGeneratorsAndUpdateCompilation(input, out Compilation result, out ImmutableArray<Diagnostic> diagnostics, TestContext.Current.CancellationToken);
		Diagnostic[] errors = result.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

		Assert.Multiple(
			() => Assert.Empty(diagnostics),
			() => Assert.Empty(errors),
			() => Assert.NotNull(result.Assembly.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeExtensions")));
	}

	private static PortableExecutableReference CreateLibraryReference(string source)
	{
		CSharpCompilation library = Compile(source).WithAssemblyName("ReferencedQuantities");
		using MemoryStream stream = new();
		EmitResult emitted = library.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);
		if (!emitted.Success)
		{
			throw new InvalidOperationException(string.Join(Environment.NewLine, emitted.Diagnostics));
		}
		return MetadataReference.CreateFromImage(stream.ToArray());
	}

	private static string[] ReadConvenienceMethods(Compilation compilation, Dimension dimension)
	{
		QuantityTypeReader reader = new(compilation);
		return compilation.SyntaxTrees.SelectMany(tree => tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<ExtensionBlockDeclarationSyntax>())
			.Where(block => block.ParameterList is { Parameters.Count: 1 } parameters && parameters.Parameters[0].Type is { } type
				&& reader.TryRead(compilation.GetSemanticModel(block.SyntaxTree).GetTypeInfo(type).Type, out Dimension receiver, TestContext.Current.CancellationToken)
				&& receiver == dimension)
			.SelectMany(block => block.Members.OfType<MethodDeclarationSyntax>())
			.Where(method => method.ParameterList.Parameters.Count == 0 && !method.Modifiers.Any(SyntaxKind.StaticKeyword))
			.Select(method => method.Identifier.ValueText).ToArray();
	}

	private static T Evaluate<T>(string expression)
	{
		string source = $$"""
			public static class Consumer
			{
				public static global::{{typeof(T).FullName}} Value() => {{expression}};
			}
			""";
		return EvaluateSource<T>(source);
	}

	private static T EvaluateSource<T>(string source)
	{
		CSharpCompilation compilation = Compile(source);
		using MemoryStream assemblyBytes = new();
		EmitResult emitResult = compilation.Emit(assemblyBytes, cancellationToken: TestContext.Current.CancellationToken);
		Assert.True(emitResult.Success, string.Join(Environment.NewLine, emitResult.Diagnostics));
		Assembly assembly = Assembly.Load(assemblyBytes.ToArray());
		MethodInfo method = assembly.GetType("Consumer")!.GetMethod("Value")!;
		return Assert.IsType<T>(method.Invoke(null, null));
	}

	private static CSharpCompilation Compile(string source, IIncrementalGenerator? additionalGenerator = null)
	{
		CSharpCompilation input = CreateInputCompilation(source);
		ISourceGenerator[] generators = additionalGenerator is null
			? [new QuantityGenerator().AsSourceGenerator()]
			: [new QuantityGenerator().AsSourceGenerator(), additionalGenerator.AsSourceGenerator()];
		GeneratorDriver driver = CSharpGeneratorDriver.Create(generators, parseOptions: _parseOptions);
		driver.RunGeneratorsAndUpdateCompilation(input, out Compilation output, out ImmutableArray<Diagnostic> generatorDiagnostics);
		Diagnostic[] compilationErrors = output.GetDiagnostics(TestContext.Current.CancellationToken)
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
		Assert.Multiple(
			() => Assert.True(generatorDiagnostics.IsEmpty, string.Join(Environment.NewLine, generatorDiagnostics)),
			() => Assert.True(compilationErrors.Length == 0, string.Join(Environment.NewLine, compilationErrors)));
		return (CSharpCompilation)output;
	}

	private static CSharpCompilation CreateInputCompilation(string source)
	{
		string[] platformAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = platformAssemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		return CSharpCompilation.Create(
			"GeneratedQuantityConsumer",
			[CSharpSyntaxTree.ParseText(source, _parseOptions, path: "Consumer.cs")],
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
	}

	private sealed class SemanticProbeGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			IncrementalValuesProvider<(bool IsInvocation, bool Resolved)> observed = context.SyntaxProvider.CreateSyntaxProvider(
				static (node, _) => node.SyntaxTree.FilePath == "Consumer.cs" && node is (ParameterSyntax or InvocationExpressionSyntax),
				static (syntaxContext, _) =>
				{
					bool isInvocation = syntaxContext.Node is InvocationExpressionSyntax;
					ITypeSymbol? type = syntaxContext.Node switch
					{
						ParameterSyntax parameter => (syntaxContext.SemanticModel.GetDeclaredSymbol(parameter) as IParameterSymbol)?.Type,
						InvocationExpressionSyntax invocation => (syntaxContext.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol)?.ReturnType,
						_ => null
					};
					INamedTypeSymbol? quantity = syntaxContext.SemanticModel.Compilation.GetTypeByMetadataName(typeof(Quantity<>).FullName!);
					bool resolved = type is INamedTypeSymbol named && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, quantity);
					return (isInvocation, resolved);
				});
			context.RegisterSourceOutput(observed.Collect(), static (output, symbols) =>
			{
				bool aliasResolved = symbols.Any(symbol => !symbol.IsInvocation && symbol.Resolved);
				bool factoryResolved = symbols.Any(symbol => symbol.IsInvocation && symbol.Resolved);
				string source = $"internal static class SemanticProbe {{ public const bool AliasResolved = {aliasResolved.ToString().ToLowerInvariant()}; public const bool FactoryResolved = {factoryResolved.ToString().ToLowerInvariant()}; }}";
				output.AddSource("SemanticProbe.g.cs", SourceText.From(source, Encoding.UTF8));
			});
		}
	}

	private static ArithmeticOperation[] ReadOperators(Compilation compilation)
	{
		INamedTypeSymbol extensions = compilation.GetTypeByMetadataName("DimensionalQuantities.Generated.ArithmeticExtensions")!;
		QuantityTypeReader reader = new(compilation);
		return extensions.GetMembers().OfType<IMethodSymbol>()
			.Where(method => method.MetadataName is "op_Multiply" or "op_Division")
			.Select(method =>
			{
				if (!reader.TryRead(method.Parameters[0].Type, out Dimension left, TestContext.Current.CancellationToken)
					|| !reader.TryRead(method.Parameters[1].Type, out Dimension right, TestContext.Current.CancellationToken)
					|| !reader.TryRead(method.ReturnType, out Dimension result, TestContext.Current.CancellationToken))
				{
					throw new InvalidOperationException("Generated operator has an unreadable quantity signature.");
				}

				ArithmeticOperationKind kind = method.MetadataName == "op_Multiply" ? ArithmeticOperationKind.Multiply : ArithmeticOperationKind.Divide;
				return new ArithmeticOperation(kind, left, right, result);
			}).ToArray();
	}
}
