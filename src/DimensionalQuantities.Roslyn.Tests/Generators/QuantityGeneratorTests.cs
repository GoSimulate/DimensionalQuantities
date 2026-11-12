using System.Text;
using DimensionalQuantities.TypeSystem;
using DimensionalQuantities.Roslyn.Generators;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.Roslyn.Tests.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generators;

public sealed class QuantityGeneratorTests
{

	[Fact]
	public async Task BaseAliasesHaveUnprefixedFactoriesAndAccessors()
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

		await new QuantityGeneratorTest
		{
			TestCode = source,
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
		}.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task SemanticPipelineResolvesPostInitialisationQuantityAliasesAndFactories()
	{
		const string source = """
			public static class Consumer
			{
				public static Length Use(Length input) => Length.FromMetres(2);
			}
			""";

		QuantityGeneratorTest resultTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			AdditionalGenerator = typeof(SemanticProbeGenerator),
			TestState =
			{
				Sources = { ("Consumer.cs", source) },
			},
		};
		await resultTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation result = resultTest.Compilation;
		INamedTypeSymbol probe = result.GetTypeByMetadataName("SemanticProbe")!;
		IFieldSymbol aliasResolved = Assert.IsAssignableFrom<IFieldSymbol>(Assert.Single(probe.GetMembers("AliasResolved")));
		IFieldSymbol factoryResolved = Assert.IsAssignableFrom<IFieldSymbol>(Assert.Single(probe.GetMembers("FactoryResolved")));

		Assert.Multiple(
			() => Assert.Equal(true, aliasResolved.ConstantValue),
			() => Assert.Equal(true, factoryResolved.ConstantValue));
	}

	[Fact]
	public async Task CatalogueAliasesAreAvailableToConsumers()
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

		await new QuantityGeneratorTest
		{
			TestCode = source,
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
		}.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task QuantityArithmeticAcceptsTypedMethodParametersAndReturns()
	{
		const string source = """
			public static class Consumer
			{
				public static Area Multiply(Length left, Length right) => left * right;
				public static Voltage Divide(Power power, Current current) => power / current;
				public static InverseCurrent Invert(double numerator, Current current) => numerator / current;
			}
			""";

		await new QuantityGeneratorTest
		{
			TestCode = source,
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
		}.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task RepeatedConsumerOperationIsEmittedOnce()
	{
		const string source = """
			public static class Consumer
			{
				public static Area First(Length left, Length right) => left * right;
				public static Area Second(Length left, Length right) => left * right;
			}
			""";

		QuantityGeneratorTest compilationTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", source) },
			},
		};
		await compilationTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation compilation = compilationTest.Compilation;
		INamedTypeSymbol extensions = compilation.GetTypeByMetadataName("DimensionalQuantities.Generated.ArithmeticExtensions")!;
		Assert.Single(extensions.GetMembers().OfType<IMethodSymbol>(), method => method.MetadataName == "op_Multiply");
	}


	[Fact]
	public async Task ArithmeticReadsFieldAndMethodReturnTypes()
	{
		const string source = """
			public static class Consumer
			{
				public static Current Input = Current.FromAmps(2);
				public static Resistance GetResistance() => Resistance.FromSiBaseUnit(4);
				public static Power Calculate() => Input * Input * GetResistance();
			}
			""";
		await new QuantityGeneratorTest
		{
			TestCode = source,
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
		}.RunAsync(TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task DimensionPreservingScalarArithmeticUsesCoreOperatorsOnly()
	{
		const string source = """
			public static class Consumer
			{
				public static Current Scale(Current original, double factor) => factor * original / 2;
			}
			""";

		QuantityGeneratorTest compilationTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", source) },
			},
		};
		await compilationTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation compilation = compilationTest.Compilation;

		Assert.Null(compilation.GetTypeByMetadataName("DimensionalQuantities.Generated.ArithmeticExtensions"));
	}

	[Fact]
	public async Task SingleConsumerRequestsLengthCurrentArithmetic()
	{
		const string firstSource = """
			public static class Consumer
			{
				public static LengthCurrent Value() => Current.FromAmps(2) * Length.FromMetres(3);
			}
			""";

		QuantityGeneratorTest firstOutputTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", firstSource) },
			},
		};
		await firstOutputTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation firstOutput = firstOutputTest.Compilation;

		Assert.NotNull(firstOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions"));
	}

	[Fact]
	public async Task MultipleConsumersRequestBothProductQuantities()
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

		QuantityGeneratorTest addedOutputTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", firstSource), ("Added.cs", addedSource) },
			},
		};
		await addedOutputTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation addedOutput = addedOutputTest.Compilation;

		Assert.Multiple(
			() => Assert.NotNull(addedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions")),
			() => Assert.NotNull(addedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeExtensions")));
	}

	[Fact]
	public async Task DivisionAndProductRequestOnlyTheirOwnOperators()
	{
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
		Dimension length = new(Length: 1);
		Dimension time = new(Time: 1);
		Dimension lengthTime = new(Length: 1, Time: 1);
		Dimension voltage = new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -1);
		Dimension current = new(ElectricCurrent: 1);
		Dimension resistance = new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -2);
		ArithmeticOperation addedOperation = new(ArithmeticOperationKind.Multiply, length, time, lengthTime);
		ArithmeticOperation editedOperation = new(ArithmeticOperationKind.Divide, voltage, current, resistance);

		QuantityGeneratorTest editedOutputTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", editedSource), ("Added.cs", addedSource) },
			},
		};
		await editedOutputTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation editedOutput = editedOutputTest.Compilation;
		ArithmeticOperation[] editedOperators = ReadOperators(editedOutput);

		Assert.Multiple(
			() => Assert.Null(editedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions")),
			() => Assert.NotNull(editedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeExtensions")),
			() => Assert.Equal(2, editedOperators.Length),
			() => Assert.Contains(addedOperation, editedOperators),
			() => Assert.Contains(editedOperation, editedOperators));
	}

	[Fact]
	public async Task DivisionConsumerRequestsOnlyItsDivisionOperator()
	{
		const string editedSource = """
			public static class Consumer
			{
				public static Resistance Value() => Voltage.FromSiBaseUnit(12) / Current.FromAmps(3);
			}
			""";
		Dimension voltage = new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -1);
		Dimension current = new(ElectricCurrent: 1);
		Dimension resistance = new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -2);
		ArithmeticOperation editedOperation = new(ArithmeticOperationKind.Divide, voltage, current, resistance);

		QuantityGeneratorTest removedOutputTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", editedSource) },
			},
		};
		await removedOutputTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation removedOutput = removedOutputTest.Compilation;
		ArithmeticOperation[] removedOperators = ReadOperators(removedOutput);

		Assert.Multiple(
			() => Assert.Null(removedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions")),
			() => Assert.Null(removedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeExtensions")),
			() => Assert.Equal(editedOperation, Assert.Single(removedOperators)));
	}

	[Fact]
	public async Task EmptyConsumerHasNoDemandGeneratedArithmetic()
	{
		QuantityGeneratorTest emptyOutputTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", string.Empty) },
			},
		};
		await emptyOutputTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation emptyOutput = emptyOutputTest.Compilation;

		Assert.Multiple(
			() => Assert.Null(emptyOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.ArithmeticExtensions")),
			() => Assert.Null(emptyOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions")),
			() => Assert.Null(emptyOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeExtensions")));
	}

	[Fact]
	public async Task GeneratorIgnoresUnrelatedAndIncompleteArithmetic()
	{
		const string source = """
			public static class Consumer
			{
				public static int Number(int left, int right) => left * right;
				public static object Incomplete(Length length) => length * Missing;
			}
			""";

		QuantityGeneratorTest outputTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			CompilerDiagnostics = CompilerDiagnostics.None,
			TestState =
			{
				Sources = { ("Consumer.cs", source) },
			},
		};
		await outputTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation output = outputTest.Compilation;

		Assert.Null(output.GetTypeByMetadataName("DimensionalQuantities.Generated.ArithmeticExtensions"));
	}



	[Fact]
	public async Task InferredLengthCurrentLocalDeterminesDependentOperators()
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
		Dimension current = new(ElectricCurrent: 1);
		Dimension lengthCurrent = new(Length: 1, ElectricCurrent: 1);
		Dimension length = new(Length: 1);
		Dimension time = new(Time: 1);
		ArithmeticOperation[] originalExpected =
		[
			new(ArithmeticOperationKind.Multiply, current, length, lengthCurrent),
			new(ArithmeticOperationKind.Multiply, lengthCurrent, time, new(Length: 1, Time: 1, ElectricCurrent: 1))
		];

		QuantityGeneratorTest originalOutputTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", source) },
			},
		};
		await originalOutputTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation originalOutput = originalOutputTest.Compilation;

		Assert.Equal(originalExpected, ReadOperators(originalOutput));
	}



	[Fact]
	public async Task InferredAreaLocalDeterminesDependentOperators()
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
		Dimension length = new(Length: 1);
		Dimension area = new(Length: 2);
		Dimension time = new(Time: 1);
		ArithmeticOperation[] editedExpected =
		[
			new(ArithmeticOperationKind.Multiply, length, length, area),
			new(ArithmeticOperationKind.Multiply, area, time, new(Length: 2, Time: 1))
		];

		QuantityGeneratorTest editedOutputTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", editedSource) },
			},
		};
		await editedOutputTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation editedOutput = editedOutputTest.Compilation;

		Assert.Multiple(
			() => Assert.Equal(editedExpected, ReadOperators(editedOutput)),
			() => Assert.Null(editedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions")),
			() => Assert.Null(editedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeCurrentExtensions")));
	}



	[Fact]
	public async Task ConsumerWithoutArithmeticHasNoDemandGeneratedOperators()
	{
		const string removedSource = "public static class Consumer { public static double Value() => 0; }";

		QuantityGeneratorTest removedOutputTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", removedSource) },
			},
		};
		await removedOutputTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation removedOutput = removedOutputTest.Compilation;

		Assert.Multiple(
			() => Assert.Null(removedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.ArithmeticExtensions")),
			() => Assert.Null(removedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthSquaredTimeExtensions")));
	}


	[Theory]
	[InlineData("Current.FromAmps(2) + Current.FromMilliAmps(500)", "AsAmps")]
	[InlineData("Current.FromAmps(2) - Current.FromMilliAmps(500)", "AsAmps")]
	[InlineData("-Current.FromMilliAmps(500)", "AsAmps")]
	[InlineData("Energy.FromSiBaseUnit(2) + Torque.FromSiBaseUnit(3)", "AsSiBaseUnit")]
	public async Task SameDimensionArithmeticDoesNotGenerateOverloads(string expression, string accessor)
	{
		string source = $$"""
			public static class Consumer
			{
				public static double Value() => ({{expression}}).{{accessor}}();
			}
			""";

		QuantityGeneratorTest compilationTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", source) },
			},
		};
		await compilationTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation compilation = compilationTest.Compilation;

		Assert.Null(compilation.GetTypeByMetadataName("DimensionalQuantities.Generated.ArithmeticExtensions"));
	}

	[Theory]
	[InlineData("current + length")]
	[InlineData("current - length")]
	[InlineData("current + 2")]
	[InlineData("2 - current")]
	public async Task InvalidSameDimensionArithmeticRemainsACompilerError(string expression)
	{
		string source = $$"""
			public static class Consumer
			{
				public static object Value(Current current, Length length) => {{expression}};
			}
			""";

		QuantityGeneratorTest outputTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			CompilerDiagnostics = CompilerDiagnostics.None,
			TestState =
			{
				Sources = { ("Consumer.cs", source) },
			},
		};
		await outputTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation output = outputTest.Compilation;
		Diagnostic[] errors = output.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

		Assert.Equal("CS0019", Assert.Single(errors).Id);
	}

	[Fact]
	public async Task GeneratesEnglishAliasesOnlyWhenUsedByPowerCalls()
	{
		QuantityGeneratorTest unusedTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", "class Consumer { }") },
			},
		};
		await unusedTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation unused = unusedTest.Compilation;
		const string source = """
			using DimensionalQuantities.TypeSystem;
			class Consumer { object Value(Length value) => Power<NumberTwo>.Of(value); }
			""";

		QuantityGeneratorTest usedTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", source) },
			},
		};
		await usedTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation used = usedTest.Compilation;
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
	public async Task EnglishNamesOutsideSupportedPowerCallsRemainUnresolved(string expression)
	{
		string source = $$"""
			using DimensionalQuantities.TypeSystem;
			namespace Other { public struct Power<T> { public static object Of(object value) => value; } }
			class Consumer { object Value(Length value) => {{expression}}; }
			""";

		QuantityGeneratorTest outputTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			CompilerDiagnostics = CompilerDiagnostics.None,
			TestState =
			{
				Sources = { ("Consumer.cs", source) },
			},
		};
		await outputTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation output = outputTest.Compilation;
		string[] aliases = output.SyntaxTrees.SelectMany(tree => tree.GetCompilationUnitRoot(TestContext.Current.CancellationToken).Usings)
			.Where(directive => directive.Alias is not null).Select(directive => directive.Alias!.Name.Identifier.ValueText).ToArray();

		Assert.Multiple(
			() => Assert.DoesNotContain("NumberTwo", aliases),
			() => Assert.Contains(output.GetDiagnostics(TestContext.Current.CancellationToken), diagnostic => diagnostic.Id == "CS0246" && diagnostic.GetMessage().Contains("NumberTwo", StringComparison.Ordinal)));
	}

	[Fact]
	public async Task EnglishPowerCallRequestsItsAlias()
	{
		const string source = """
			using DimensionalQuantities.TypeSystem;
			class Consumer { object Value(Length value) => Power<NumberTwo>.Of(value); }
			""";

		QuantityGeneratorTest originalTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", source) },
			},
		};
		await originalTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation original = originalTest.Compilation;
		string[] originalNames = original.SyntaxTrees.SelectMany(tree => tree.GetCompilationUnitRoot(TestContext.Current.CancellationToken).Usings)
			.Where(directive => directive.Alias is not null).Select(directive => directive.Alias!.Name.Identifier.ValueText).ToArray();

		Assert.Contains("NumberTwo", originalNames);
	}

	[Fact]
	public async Task ConsumerWithoutPowerCallsHasNoEnglishAliases()
	{
		QuantityGeneratorTest removedTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", string.Empty) },
			},
		};
		await removedTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation removed = removedTest.Compilation;
		string[] remainingNames = removed.SyntaxTrees.SelectMany(tree => tree.GetCompilationUnitRoot(TestContext.Current.CancellationToken).Usings)
			.Where(directive => directive.Alias is not null).Select(directive => directive.Alias!.Name.Identifier.ValueText).ToArray();

		Assert.DoesNotContain("NumberTwo", remainingNames);
	}

	[Fact]
	public async Task ExplicitAliasInAnotherFileReservesTheName()
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

		QuantityGeneratorTest outputTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			CompilerDiagnostics = CompilerDiagnostics.None,
			TestState =
			{
				Sources = { ("Consumer.cs", generatedAliasSource), ("Second.cs", explicitAliasSource) },
			},
		};
		await outputTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation output = outputTest.Compilation;
		Diagnostic[] errors = output.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

		Assert.Multiple(
			() => Assert.Contains(errors, diagnostic => diagnostic.Id == "CS0246" && diagnostic.GetMessage().Contains("NumberTwo", StringComparison.Ordinal)),
			() => Assert.DoesNotContain(errors, diagnostic => diagnostic.Location.SourceTree?.FilePath == "Second.cs"),
			() => Assert.DoesNotContain(output.SyntaxTrees.SelectMany(tree => tree.GetCompilationUnitRoot(TestContext.Current.CancellationToken).Usings),
				directive => directive.Alias?.Name.Identifier.ValueText == "NumberTwo" && directive.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword)));
	}

	[Theory]
	[InlineData("Two")]
	[InlineData("NumberNotANumber", "DQ0007")]
	public async Task UnsupportedExponentNamesRemainCompilerErrors(string name, params string[] expectedDiagnosticIds)
	{
		string source = $$"""
			using DimensionalQuantities.TypeSystem;
			public static class Consumer
			{
				public static object Value(Length length) => Power<{{name}}>.Of(length);
			}
			""";

		QuantityGeneratorTest outputTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			CompilerDiagnostics = CompilerDiagnostics.None,
			TestState =
			{
				Sources = { ("Consumer.cs", source) },
			},
		};
		if (expectedDiagnosticIds.Length != 0)
		{
			outputTest.ExpectedDiagnostics.Add(new DiagnosticResult("DQ0007", DiagnosticSeverity.Error)
				.WithSpan("Consumer.cs", 4, 53, 4, 53 + name.Length).WithArguments(name));
		}
		await outputTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation output = outputTest.Compilation;

		Assert.Contains(output.GetDiagnostics(TestContext.Current.CancellationToken), diagnostic => diagnostic.Id == "CS0246" && diagnostic.GetMessage().Contains(name, StringComparison.Ordinal));
	}

	[Theory]
	[InlineData("TerminalHex2")]
	[InlineData("NumberTwo")]
	public async Task SquaredProductRequestsSquaredQuantity(string originalExponent)
	{
		string source = $$"""
			using DimensionalQuantities.TypeSystem;
			public static class Consumer
			{
				public static object Value(Current current) => Power<{{originalExponent}}>.Of(current * Length.FromMetres(1));
			}
			""";

		QuantityGeneratorTest originalOutputTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", source) },
			},
		};
		await originalOutputTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation originalOutput = originalOutputTest.Compilation;

		Assert.NotNull(originalOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthSquaredCurrentSquaredExtensions"));
	}

	[Theory]
	[InlineData("TerminalHex2", "TerminalHex3")]
	[InlineData("NumberTwo", "NumberThree")]
	public async Task CubedProductRequestsOnlyItsCubedQuantity(string originalExponent, string editedExponent)
	{
		string source = $$"""
			using DimensionalQuantities.TypeSystem;
			public static class Consumer
			{
				public static object Value(Current current) => Power<{{originalExponent}}>.Of(current * Length.FromMetres(1));
			}
			""";

		QuantityGeneratorTest editedOutputTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", source.Replace(originalExponent, editedExponent, StringComparison.Ordinal)) },
			},
		};
		await editedOutputTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation editedOutput = editedOutputTest.Compilation;

		Assert.Multiple(
			() => Assert.Null(editedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthSquaredCurrentSquaredExtensions")),
			() => Assert.NotNull(editedOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCubedCurrentCubedExtensions")));
	}

	[Fact]
	public async Task ConsumerWithoutPowerCallsHasNoDemandGeneratedPowerQuantity()
	{
		QuantityGeneratorTest emptyOutputTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", string.Empty) },
			},
		};
		await emptyOutputTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation emptyOutput = emptyOutputTest.Compilation;

		Assert.Null(emptyOutput.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCubedCurrentCubedExtensions"));
	}

	[Fact]
	public async Task GeneratesOnlyTheRequestedIntermediateConvenienceOverloadOnce()
	{
		const string source = """
			class Consumer
			{
				object First(Length length, Current current) => (length * current).Squared();
				object Second(Length length, Current current) => (length * current).Squared();
			}
			""";

		QuantityGeneratorTest outputTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", source) },
			},
		};
		await outputTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation output = outputTest.Compilation;
		string[] convenienceNames = ["Squared", "Cubed", "Pow4", "Pow5", "Pow6", "SquareRoot", "CubeRoot", "Root4", "Root5", "Root6"];
		string[] result = ReadConvenienceMethods(output, new(Length: 1, ElectricCurrent: 1)).Where(convenienceNames.Contains).ToArray();
		string[] expected = ["Squared"];

		Assert.Equal(expected, result);
	}

	[Fact]
	public async Task BaseAndCatalogueHelpersAreAvailableWithoutConsumerCode()
	{
		QuantityGeneratorTest resultTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", string.Empty) },
			},
		};
		await resultTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation result = resultTest.Compilation;
		string[] expected = ["Squared", "Cubed", "Pow4", "Pow5", "Pow6", "SquareRoot", "CubeRoot", "Root4", "Root5", "Root6"];
		Dimension[] dimensions = QuantityDiscovery.Discover([]).KnownQuantities.Select(quantity => quantity.Dimension).ToArray();

		Assert.All(dimensions, dimension => Assert.Equal(expected.Order(), ReadConvenienceMethods(result, dimension).Where(expected.Contains).Order()));
	}

	[Fact]
	public async Task CatalogueConvenienceMethodsAreBoundBeforeDemandGeneration()
	{
		const string source = """
			public static class Consumer
			{
				public static object Value(Voltage voltage) => voltage.Squared();
			}
			""";

		QuantityGeneratorTest resultTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			AdditionalGenerator = typeof(SemanticProbeGenerator),
			TestState =
			{
				Sources = { ("Consumer.cs", source) },
			},
		};
		await resultTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation result = resultTest.Compilation;
		INamedTypeSymbol probe = result.GetTypeByMetadataName("SemanticProbe")!;
		IFieldSymbol resolved = Assert.IsAssignableFrom<IFieldSymbol>(Assert.Single(probe.GetMembers("FactoryResolved")));

		Assert.Equal(true, resolved.ConstantValue);
	}

	[Theory]
	[InlineData("(3d).Squared()", "CS1929")]
	[InlineData("Length.FromMetres(3).Pow7()", "CS1061")]
	public async Task UnsupportedConvenienceCallsRemainCompilerErrors(string expression, string diagnosticId)
	{
		string source = $$"""class Consumer { object Value() => {{expression}}; }""";

		QuantityGeneratorTest outputTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			CompilerDiagnostics = CompilerDiagnostics.None,
			TestState =
			{
				Sources = { ("Consumer.cs", source) },
			},
		};
		await outputTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation output = outputTest.Compilation;

		Assert.Contains(output.GetDiagnostics(TestContext.Current.CancellationToken), diagnostic => diagnostic.Id == diagnosticId);
	}

	[Fact]
	public async Task IntermediateConvenienceCallRequestsItsMethod()
	{
		const string source = "class Consumer { object Value(Length length, Current current) => (length * current).Squared(); }";

		QuantityGeneratorTest originalTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", source) },
			},
		};
		await originalTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation original = originalTest.Compilation;
		string[] originalMethods = ReadConvenienceMethods(original, new(Length: 1, ElectricCurrent: 1));

		Assert.Contains("Squared", originalMethods);
	}

	[Fact]
	public async Task ConsumerWithoutConvenienceCallsHasNoIntermediateMethods()
	{
		QuantityGeneratorTest removedTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", string.Empty) },
			},
		};
		await removedTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation removed = removedTest.Compilation;
		string[] remainingMethods = ReadConvenienceMethods(removed, new(Length: 1, ElectricCurrent: 1));

		Assert.DoesNotContain("Squared", remainingMethods);
	}



	[Fact]
	public async Task ExplicitLengthCurrentLocalSupportsDependentArithmetic()
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

		QuantityGeneratorTest initial1Test = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", source) },
			},
		};
		await initial1Test.RunAsync(TestContext.Current.CancellationToken);
	}



	[Fact]
	public async Task ExplicitLengthTimeLocalDeterminesDependentArithmetic()
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

		QuantityGeneratorTest resultTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", edited) },
			},
		};
		await resultTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation result = resultTest.Compilation;

		Assert.Multiple(
			() => Assert.Null(result.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions")),
			() => Assert.Null(result.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeCurrentExtensions")),
			() => Assert.NotNull(result.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeSquaredExtensions")));
	}


	[Fact]
	public async Task LengthCurrentConditionalLocalSupportsDependentArithmetic()
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

		QuantityGeneratorTest initial1Test = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", source) },
			},
		};
		await initial1Test.RunAsync(TestContext.Current.CancellationToken);
	}


	[Fact]
	public async Task LengthTimeConditionalLocalDeterminesDependentArithmetic()
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

		QuantityGeneratorTest resultTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", edited) },
			},
		};
		await resultTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation result = resultTest.Compilation;

		Assert.Multiple(
			() => Assert.Null(result.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeCurrentExtensions")),
			() => Assert.NotNull(result.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeSquaredExtensions")));
	}


	[Fact]
	public async Task LengthCurrentMemberSupportsDependentArithmetic()
	{
		const string source = "class Inputs { public static LengthCurrent Stored => Length.FromMetres(2) * Current.FromAmps(3); }";
		const string consumer = "class Consumer { object Value(Time time) => Inputs.Stored * time; }";

		QuantityGeneratorTest initial1Test = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", consumer), ("Inputs.cs", source) },
			},
		};
		await initial1Test.RunAsync(TestContext.Current.CancellationToken);
	}


	[Fact]
	public async Task LengthTimeMemberDeterminesDependentArithmetic()
	{
		const string source = "class Inputs { public static LengthCurrent Stored => Length.FromMetres(2) * Current.FromAmps(3); }";
		const string consumer = "class Consumer { object Value(Time time) => Inputs.Stored * time; }";
		string edited = source.Replace("LengthCurrent", "LengthTime", StringComparison.Ordinal).Replace("Current.FromAmps", "Time.FromSeconds", StringComparison.Ordinal);

		QuantityGeneratorTest resultTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", consumer), ("Inputs.cs", edited) },
			},
		};
		await resultTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation result = resultTest.Compilation;

		Assert.Multiple(
			() => Assert.Null(result.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeCurrentExtensions")),
			() => Assert.NotNull(result.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeSquaredExtensions")));
	}


	[Fact]
	public async Task LengthCurrentMethodSupportsConsumerArithmetic()
	{
		const string source = "class Inputs { public static LengthCurrent Make() => Length.FromMetres(2) * Current.FromAmps(3); }";
		const string consumer = "class Consumer { object Value(Time time) => Inputs.Make() * time; }";

		QuantityGeneratorTest initial1Test = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", consumer), ("Inputs.cs", source) },
			},
		};
		await initial1Test.RunAsync(TestContext.Current.CancellationToken);
	}


	[Fact]
	public async Task LengthTimeMethodDeterminesConsumerArithmetic()
	{
		const string source = "class Inputs { public static LengthCurrent Make() => Length.FromMetres(2) * Current.FromAmps(3); }";
		const string consumer = "class Consumer { object Value(Time time) => Inputs.Make() * time; }";
		string edited = source.Replace("LengthCurrent", "LengthTime", StringComparison.Ordinal).Replace("Current.FromAmps", "Time.FromSeconds", StringComparison.Ordinal);

		QuantityGeneratorTest resultTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", consumer), ("Inputs.cs", edited) },
			},
		};
		await resultTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation result = resultTest.Compilation;

		Assert.Multiple(
			() => Assert.Null(result.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeCurrentExtensions")),
			() => Assert.NotNull(result.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeSquaredExtensions")));
	}


	[Fact]
	public async Task TypeOnlyDeclarationsGenerateQuantitiesInPublicApisAndCollections()
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

		QuantityGeneratorTest resultTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", source) },
			},
		};
		await resultTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation result = resultTest.Compilation;

		Assert.NotNull(result.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions"));
	}






	[Theory]
	[InlineData("public class LengthCurrent { }")]
	[InlineData("using LengthCurrent = System.Int32;")]
	public async Task ExistingTypesAndAliasesAreNotReplacedByQuantityNames(string declaration)
	{
		string source = $$"""{{declaration}} class Consumer { object Value(LengthCurrent value) => value; }""";

		QuantityGeneratorTest resultTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", source) },
			},
		};
		await resultTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation result = resultTest.Compilation;

		Assert.Null(result.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions"));
	}

	[Fact]
	public async Task QuantityLookingVariableNamesDoNotRequestTypes()
	{
		const string source = "class Consumer { int Value() { int LengthCurrent = 7; return LengthCurrent; } }";

		QuantityGeneratorTest resultTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", source) },
			},
		};
		await resultTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation result = resultTest.Compilation;

		Assert.Null(result.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions"));
	}

	[Fact]
	public async Task TypeNameDiscoveryPreservesExplicitAliasesInOtherFiles()
	{
		const string source = "class First { public LengthCurrent Value; }";
		const string aliasSource = "using LengthCurrent = System.Int32; class Second { public int Value(LengthCurrent value) => value; }";

		QuantityGeneratorTest resultTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			CompilerDiagnostics = CompilerDiagnostics.None,
			TestState =
			{
				Sources = { ("Consumer.cs", source), ("Alias.cs", aliasSource) },
			},
		};
		await resultTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation result = resultTest.Compilation;
		Diagnostic[] errors = result.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

		Assert.Multiple(
			() => Assert.Contains(errors, error => error.Id == "CS0246" && error.Location.SourceTree?.FilePath == "Consumer.cs"),
			() => Assert.DoesNotContain(errors, error => error.Location.SourceTree?.FilePath == "Alias.cs"));
	}

	[Fact]
	public async Task TypeOnlyDeclarationsRequestLocalConversions()
	{
		const string source = "class Consumer { public LengthCurrent Identity(LengthCurrent value) => value; }";

		QuantityGeneratorTest originalTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", source) },
			},
		};
		await originalTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation original = originalTest.Compilation;

		Assert.NotNull(original.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions"));
	}

	[Fact]
	public async Task ConsumerWithoutTypeDeclarationsHasNoDemandGeneratedQuantities()
	{
		QuantityGeneratorTest resultTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", string.Empty) },
			},
		};
		await resultTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation result = resultTest.Compilation;

		Assert.Null(result.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions"));
	}

	[Theory]
	[InlineData("Library.Create()")]
	[InlineData("Library.Field")]
	[InlineData("Library.Property")]
	[InlineData("Library.Values()[0]")]
	[InlineData("Library.Measure().Value")]
	public async Task ReferencedQuantityValuesGetLocalConversionsWithoutArithmetic(string expression)
	{
		PortableExecutableReference library = await CreateLibraryReference("""
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

		QuantityGeneratorTest resultTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", $"public class Consumer {{ public double Value() => {expression}.AsPrefixedUnits(3, 0); }}") },
				AdditionalReferences = { library },
			},
		};
		await resultTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation result = resultTest.Compilation;

		Assert.NotNull(result.Assembly.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions"));
	}

	[Fact]
	public async Task ReferencedCollectionTypesRequestTheirNestedQuantity()
	{
		PortableExecutableReference library = await CreateLibraryReference("public static class Library { public static System.Collections.Generic.List<LengthCurrent[]> Values() => new(); }");

		QuantityGeneratorTest resultTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", "public class Consumer { public object Value() => Library.Values(); }") },
				AdditionalReferences = { library },
			},
		};
		await resultTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation result = resultTest.Compilation;

		Assert.NotNull(result.Assembly.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions"));
	}

	[Fact]
	public async Task ReferencedQuantityUsageRequestsLocalConversions()
	{
		PortableExecutableReference library = await CreateLibraryReference("public static class Library { public static LengthCurrent Create() => default; }");

		QuantityGeneratorTest originalTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", "public class Consumer { public object Value() => Library.Create(); }") },
				AdditionalReferences = { library },
			},
		};
		await originalTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation original = originalTest.Compilation;

		Assert.NotNull(original.Assembly.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions"));
	}

	[Fact]
	public async Task UnusedReferencedQuantityHasNoLocalConversions()
	{
		PortableExecutableReference library = await CreateLibraryReference("public static class Library { public static LengthCurrent Create() => default; }");

		QuantityGeneratorTest resultTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", string.Empty) },
				AdditionalReferences = { library },
			},
		};
		await resultTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation result = resultTest.Compilation;

		Assert.Null(result.Assembly.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions"));
	}

	[Fact]
	public async Task ReferencedClassesWithQuantityNamesAreNotTreatedAsQuantities()
	{
		PortableExecutableReference library = await CreateLibraryReference("public class LengthCurrent { } public static class Library { public static LengthCurrent Create() => new(); }");

		QuantityGeneratorTest resultTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", "public class Consumer { public object Value() => Library.Create(); }") },
				AdditionalReferences = { library },
			},
		};
		await resultTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation result = resultTest.Compilation;

		Assert.Null(result.Assembly.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentExtensions"));
	}

	[Fact]
	public async Task MetadataDiscoveryPreservesExplicitConsumerAliases()
	{
		PortableExecutableReference library = await CreateLibraryReference("public static class Library { public static LengthCurrent Create() => default; }");

		QuantityGeneratorTest resultTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", "using LengthCurrent = System.Int32; public class Consumer { public object Value() => Library.Create(); public LengthCurrent Count => 1; }") },
				AdditionalReferences = { library },
			},
		};
		await resultTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation result = resultTest.Compilation;
	}

	[Theory]
	[InlineData("public class LengthCurrent { public int Marker { get; init; } }", "LengthCurrent", "new() { Marker = 7 }")]
	[InlineData("namespace LengthCurrent { public class Item { } }", "LengthCurrent.Item", "new()")]
	public async Task MetadataDiscoveryPreservesExistingConsumerTypesAndNamespaces(string declaration, string type, string value)
	{
		PortableExecutableReference library = await CreateLibraryReference("public static class Library { public static LengthCurrent Create() => default; }");
		string source = $$"""{{declaration}} public class Consumer { public object Value() => Library.Create(); public {{type}} Item => {{value}}; }""";

		QuantityGeneratorTest resultTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", source) },
				AdditionalReferences = { library },
			},
		};
		await resultTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation result = resultTest.Compilation;
	}

	[Fact]
	public async Task ReferencedGenericResultsGetConversionsAfterTheirArgumentsResolve()
	{
		PortableExecutableReference library = await CreateLibraryReference("public static class Library { public static LengthTime Convert<T>(T value) => default; }");
		const string source = """
			public static class Consumer
			{
				public static double Value(Length length, Current current) => Library.Convert(length * current).AsPrefixedUnits(0, 0);
			}
			""";

		QuantityGeneratorTest resultTest = new()
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestState =
			{
				Sources = { ("Consumer.cs", source) },
				AdditionalReferences = { library },
			},
		};
		await resultTest.RunAsync(TestContext.Current.CancellationToken);
		Compilation result = resultTest.Compilation;

		Assert.NotNull(result.Assembly.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeExtensions"));
	}

	private static async Task<PortableExecutableReference> CreateLibraryReference(string source)
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = source,
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		using MemoryStream stream = new();
		Microsoft.CodeAnalysis.Emit.EmitResult emitted = test.Compilation.WithAssemblyName("ReferencedQuantities").Emit(stream, cancellationToken: TestContext.Current.CancellationToken);
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
