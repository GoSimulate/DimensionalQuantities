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
	private static readonly CSharpParseOptions _parseOptions = new(LanguageVersion.CSharp14);

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
					Temperature temperature = Temperature.FromKelvin(1);
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

	[Fact]
	public void GeneratorRefreshesArithmeticAfterAddingEditingAndRemovingConsumerTrees()
	{
		const string firstSource = """
			public static class Consumer
			{
				public static CurrentSquared Value() => Current.FromAmps(2) * Current.FromAmps(3);
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
				public static Resistance Value() => Voltage.FromVolts(12) / Current.FromAmps(3);
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
			() => Assert.NotNull(firstOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.CurrentSquaredExtensions")));

		CSharpCompilation addedInput = initialInput.AddSyntaxTrees(addedTree);
		driver = driver.RunGeneratorsAndUpdateCompilation(addedInput, out Compilation addedOutput, out ImmutableArray<Diagnostic> addedDiagnostics,
			TestContext.Current.CancellationToken);
		Assert.Multiple(
			() => Assert.Empty(addedDiagnostics),
			() => Assert.Empty(addedOutput.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)),
			() => Assert.NotNull(addedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.CurrentSquaredExtensions")),
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
			() => Assert.Null(editedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.CurrentSquaredExtensions")),
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
			() => Assert.Null(removedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.CurrentSquaredExtensions")),
			() => Assert.Null(removedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeExtensions")),
			() => Assert.Equal(editedOperation, Assert.Single(removedOperators)));

		CSharpCompilation emptyInput = removedInput.RemoveSyntaxTrees(editedTree);
		driver.RunGeneratorsAndUpdateCompilation(emptyInput, out Compilation emptyOutput, out ImmutableArray<Diagnostic> emptyDiagnostics,
			TestContext.Current.CancellationToken);
		Assert.Multiple(
			() => Assert.Empty(emptyDiagnostics),
			() => Assert.Empty(emptyOutput.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)),
			() => Assert.Null(emptyOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.ArithmeticExtensions")),
			() => Assert.Null(emptyOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.CurrentSquaredExtensions")),
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

	[Theory]
	[InlineData("var squared = current * current; var heating = squared * resistance;", "((Power)heating).AsWatts()", 36)]
	[InlineData("var squared = current * current; var copy = squared; var heating = copy * resistance; var energy = heating * time;", "((Energy)energy).AsJoules()", 72)]
	[InlineData("var squared = (2 * current) * current / 2; var heating = squared * resistance;", "((Power)heating).AsWatts()", 36)]
	[InlineData("var squared = current * current; var fourth = squared * squared;", "fourth.AsSiBaseUnit()", 81)]
	public void ArithmeticThroughLocalInitialisersCompilesAndExecutes(string declarations, string expression, double expected)
	{
		string source = $$"""
			public static class Consumer
			{
				public static double Value()
				{
					Current current = Current.FromAmps(3);
					Resistance resistance = Resistance.FromOhms(4);
					Time time = Time.FromSeconds(2);
					{{declarations}}
					return {{expression}};
				}
			}
			""";
		double result = EvaluateSource<double>(source);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void IdenticallyNamedLocalsInSeparateScopesRetainTheirOwnDimensions()
	{
		const string source = """
			public static class Consumer
			{
				public static double Value()
				{
					Current current = Current.FromAmps(3);
					Resistance resistance = Resistance.FromOhms(4);
					Time time = Time.FromSeconds(2);
					double first;
					double second;
					{
						var intermediate = current * current;
						var result = intermediate * resistance;
						first = result.AsWatts();
					}
					{
						var intermediate = time * time;
						var result = intermediate * current;
						second = result.AsSiBaseUnit();
					}
					return first + second;
				}
			}
			""";
		double result = EvaluateSource<double>(source);
		double expected = 48;

		Assert.Equal(expected, result);
	}

	[Fact]
	public void LocalInitialiserEditsRefreshDependentOperatorsAndRemoveObsoleteOutput()
	{
		const string source = """
			public static class Consumer
			{
				public static double Value(Current current, Length length, Time time)
				{
					var product = current * current;
					var result = product * time;
					return result.AsSiBaseUnit();
				}
			}
			""";
		string editedSource = source.Replace("current * current", "length * length", StringComparison.Ordinal);
		const string removedSource = "public static class Consumer { public static double Value() => 0; }";
		CSharpCompilation input = CreateInputCompilation(source);
		SyntaxTree originalTree = Assert.Single(input.SyntaxTrees);
		SyntaxTree editedTree = CSharpSyntaxTree.ParseText(editedSource, _parseOptions, path: "Consumer.cs", cancellationToken: TestContext.Current.CancellationToken);
		SyntaxTree removedTree = CSharpSyntaxTree.ParseText(removedSource, _parseOptions, path: "Consumer.cs", cancellationToken: TestContext.Current.CancellationToken);
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: _parseOptions);
		Dimension current = new(ElectricCurrent: 1);
		Dimension squaredCurrent = new(ElectricCurrent: 2);
		Dimension length = new(Length: 1);
		Dimension area = new(Length: 2);
		Dimension time = new(Time: 1);
		ArithmeticOperation[] originalExpected =
		[
			new(ArithmeticOperationKind.Multiply, current, current, squaredCurrent),
			new(ArithmeticOperationKind.Multiply, squaredCurrent, time, new(Time: 1, ElectricCurrent: 2))
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
			() => Assert.Null(editedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.CurrentSquaredExtensions")),
			() => Assert.Null(editedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.TimeCurrentSquaredExtensions")));

		CSharpCompilation removedInput = editedInput.ReplaceSyntaxTree(editedTree, removedTree);
		driver.RunGeneratorsAndUpdateCompilation(removedInput, out Compilation removedOutput, out ImmutableArray<Diagnostic> removedDiagnostics, TestContext.Current.CancellationToken);
		Assert.Multiple(
			() => Assert.Empty(removedDiagnostics),
			() => Assert.Empty(removedOutput.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)),
			() => Assert.Null(removedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.ArithmeticExtensions")),
			() => Assert.Null(removedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthSquaredTimeExtensions")));
	}

	[Fact]
	public void CyclicLocalInitialisersDoNotCrashTheGenerator()
	{
		const string source = """
			public static class Consumer
			{
				public static object Value(Current current)
				{
					var first = second * current;
					var second = first * current;
					return second;
				}
			}
			""";
		CSharpCompilation input = CreateInputCompilation(source);
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: _parseOptions);
		driver.RunGeneratorsAndUpdateCompilation(input, out Compilation output, out ImmutableArray<Diagnostic> diagnostics, TestContext.Current.CancellationToken);

		Assert.Multiple(
			() => Assert.Empty(diagnostics),
			() => Assert.Null(output.GetTypeByMetadataName("DimensionalQuantities.Generated.ArithmeticExtensions")));
	}

	[Theory]
	[InlineData("Current.FromAmps(2) + Current.FromMilliAmps(500)", "AsAmps", 2.5)]
	[InlineData("Current.FromAmps(2) - Current.FromMilliAmps(500)", "AsAmps", 1.5)]
	[InlineData("-Current.FromMilliAmps(500)", "AsAmps", -0.5)]
	[InlineData("Energy.FromJoules(2) + Torque.FromNewtonMetres(3)", "AsJoules", 5)]
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
					Resistance resistance = Resistance.FromOhms(4);
					var firstSquared = first * first;
					var secondSquared = second * second;
					var combined = {{expression}};
					var heating = combined * resistance;
					return ((Power)heating).AsWatts();
				}
			}
			""";
		double result = EvaluateSource<double>(source);

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("((Current.FromAmps(3) * Current.FromAmps(3) + Current.FromAmps(2) * Current.FromAmps(2)) * Resistance.FromOhms(4)).AsWatts()", 52)]
	[InlineData("((Current.FromAmps(3) * Current.FromAmps(3) - Current.FromAmps(2) * Current.FromAmps(2)) * Resistance.FromOhms(4)).AsWatts()", 20)]
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
	[InlineData("Energy.FromJoules(6) / Torque.FromNewtonMetres(2)", 3)]
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
					Voltage voltage = root * Resistance.FromOhms(4);
					Current nested = Power<NumberHalf>.Of(Power<NumberTwo>.Of(root));
					return voltage.AsVolts() + nested.AsAmps();
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
					Length first = Power<NumberHalf>.Of(Area.FromSquareMetres(9));
					Length second = Power<NumberTwoQuarters>.Of(Area.FromSquareMetres(9));
					Length third = Power<Fraction<TerminalHex1, TerminalHex2>>.Of(Area.FromSquareMetres(9));
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
	[InlineData("var squared = Power<TerminalHex2>.Of(current); var result = squared * resistance;", "((Power)result).AsWatts()", 36)]
	[InlineData("var squared = current * current; var result = Power<Fraction<TerminalHex1, TerminalHex2>>.Of(squared);", "((Current)result).AsAmps()", 3)]
	[InlineData("var squared = Power<TerminalHex2>.Of(current); var copy = squared; var result = Power<Fraction<TerminalHex1, TerminalHex2>>.Of(copy);", "((Current)result).AsAmps()", 3)]
	[InlineData("var result = Power<TerminalHex2>.Of(current * resistance);", "result.AsSiBaseUnit()", 144)]
	[InlineData("var result = Power<Fraction<TerminalHex1, TerminalHex2>>.Of(Power<TerminalHex2>.Of(current));", "((Current)result).AsAmps()", 3)]
	[InlineData("var identity = Power<TerminalHex0>.Of(current); var result = identity * current * resistance;", "((Voltage)result).AsVolts()", 12)]
	public void PowersWorkWithArithmeticAndLocalVariables(string declarations, string expression, double expected)
	{
		string source = $$"""
			using DimensionalQuantities.TypeSystem;
			public static class Consumer
			{
				public static double Value()
				{
					Current current = Current.FromAmps(3);
					Resistance resistance = Resistance.FromOhms(4);
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
	[InlineData("NumberNotANumber")]
	public void UnsupportedExponentNamesRemainCompilerErrors(string name)
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
			() => Assert.Empty(diagnostics),
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
				public static object Value(Current current) => Power<{{originalExponent}}>.Of(current);
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
			() => Assert.NotNull(originalOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.CurrentSquaredExtensions")));

		CSharpCompilation editedInput = input.ReplaceSyntaxTree(originalTree, editedTree);
		driver = driver.RunGeneratorsAndUpdateCompilation(editedInput, out Compilation editedOutput, out ImmutableArray<Diagnostic> editedDiagnostics, TestContext.Current.CancellationToken);
		Assert.Multiple(
			() => Assert.Empty(editedDiagnostics),
			() => Assert.Empty(editedOutput.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)),
			() => Assert.Null(editedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.CurrentSquaredExtensions")),
			() => Assert.NotNull(editedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.CurrentCubedExtensions")));

		CSharpCompilation emptyInput = editedInput.RemoveSyntaxTrees(editedTree);
		driver.RunGeneratorsAndUpdateCompilation(emptyInput, out Compilation emptyOutput, out ImmutableArray<Diagnostic> emptyDiagnostics, TestContext.Current.CancellationToken);
		Assert.Multiple(
			() => Assert.Empty(emptyDiagnostics),
			() => Assert.Empty(emptyOutput.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)),
			() => Assert.Null(emptyOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.CurrentCubedExtensions")));
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
