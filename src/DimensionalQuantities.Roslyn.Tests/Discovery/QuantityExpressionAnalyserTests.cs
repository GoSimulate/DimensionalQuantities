using System.Collections.Immutable;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Numerics;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.Roslyn.Generators;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Discovery;

public sealed class QuantityExpressionAnalyserTests
{
	private static readonly CSharpParseOptions _parseOptions = new(LanguageVersion.CSharp14);

	[Theory]
	[InlineData("a * b", true)]
	[InlineData("a / b", false)]
	public void DiscoveryReadsQuantityOperandsFromConsumerSymbols(string expression, bool multiply)
	{
		string source = $$"""
			public static class Consumer
			{
				public static object Test(Length a, Time b) => {{expression}};
			}
			""";
		CSharpCompilation compilation = Compile(source);
		ImmutableArray<ArithmeticOperation> result = Analyse(compilation);
		ArithmeticOperation operation = Assert.Single(result);
		ArithmeticOperationKind expectedKind = multiply ? ArithmeticOperationKind.Multiply : ArithmeticOperationKind.Divide;
		Dimension expectedLeft = new(Length: 1);
		Dimension expectedRight = new(Time: 1);
		Dimension expectedResult = new(Length: 1, Time: multiply ? 1 : -1);
		ArithmeticOperation expected = new(expectedKind, expectedLeft, expectedRight, expectedResult);

		Assert.Equal(expected, operation);
	}

	[Fact]
	public void DiscoveryFindsLeftNestedOperationsBottomUp()
	{
		const string source = """
			public static class Consumer
			{
				public static object Test(Current a, Current b, Resistance r) => (a * b) * r;
			}
			""";
		CSharpCompilation compilation = Compile(source);
		ImmutableArray<ArithmeticOperation> result = Analyse(compilation);
		Dimension current = new(ElectricCurrent: 1);
		Dimension resistance = new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -2);
		Dimension currentSquared = new(ElectricCurrent: 2);
		Dimension power = new(Length: 2, Mass: 1, Time: -3);
		ArithmeticOperation[] expected =
		[
			new(ArithmeticOperationKind.Multiply, current, current, currentSquared),
			new(ArithmeticOperationKind.Multiply, currentSquared, resistance, power)
		];

		Assert.Equal(expected, result);
	}

	[Fact]
	public void DiscoveryFindsRightNestedOperationsBottomUp()
	{
		const string source = """
			public static class Consumer
			{
				public static object Test(Current a, Current b, Resistance r) => a * (b * r);
			}
			""";
		CSharpCompilation compilation = Compile(source);
		ImmutableArray<ArithmeticOperation> result = Analyse(compilation);
		Dimension current = new(ElectricCurrent: 1);
		Dimension resistance = new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -2);
		Dimension currentTimesResistance = new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -1);
		Dimension power = new(Length: 2, Mass: 1, Time: -3);
		ArithmeticOperation[] expected =
		[
			new(ArithmeticOperationKind.Multiply, current, resistance, currentTimesResistance),
			new(ArithmeticOperationKind.Multiply, current, currentTimesResistance, power)
		];

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("a * missing")]
	[InlineData("a * 1m")]
	[InlineData("a * \"text\"")]
	[InlineData("a + b")]
	public void DiscoverySkipsUnsupportedExpressions(string expression)
	{
		string source = $$"""
			public static class Consumer
			{
				public static object Test(Length a, Time b) => {{expression}};
			}
			""";
		CSharpCompilation compilation = Compile(source);
		ImmutableArray<ArithmeticOperation> result = Analyse(compilation);

		Assert.Empty(result);
	}

	[Fact]
	public void ScalarDivisionCreatesInverseDimensionForNestedQuantityArithmetic()
	{
		const string source = """
			public static class Consumer
			{
				public static object Test(Length length, Time time) => (2d / length) * time;
			}
			""";
		CSharpCompilation compilation = Compile(source);
		ImmutableArray<ArithmeticOperation> result = Analyse(compilation);
		Dimension length = new(Length: 1);
		Dimension inverseLength = new(Length: -1);
		Dimension time = new(Time: 1);
		Dimension timePerLength = new(Length: -1, Time: 1);
		ArithmeticOperation[] expected =
		[
			new(ArithmeticOperationKind.ScalarDivide, default, length, inverseLength),
			new(ArithmeticOperationKind.Multiply, inverseLength, time, timePerLength)
		];

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("(2 + 3) * a * b")]
	[InlineData("(2 * 3) * a * b")]
	[InlineData("(a * b) / 2")]
	public void ScalarFactorsPreserveQuantityDimensionsInChains(string expression)
	{
		string source = $$"""
			public static class Consumer
			{
				public static object Test(Length a, Time b) => {{expression}};
			}
			""";
		CSharpCompilation compilation = Compile(source);
		ImmutableArray<ArithmeticOperation> result = Analyse(compilation);
		Dimension length = new(Length: 1);
		Dimension time = new(Time: 1);
		Dimension lengthTime = new(Length: 1, Time: 1);
		ArithmeticOperation expected = new(ArithmeticOperationKind.Multiply, length, time, lengthTime);

		Assert.Equal(expected, Assert.Single(result));
	}

	[Fact]
	public void DiscoverySkipsDimensionlessQuantityOperand()
	{
		const string source = """
			using DimensionalQuantities.TypeSystem;
			public static class Consumer
			{
				public static object Test(Length length,
					Quantity<Dimension<TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0>> scalar)
					=> length * scalar;
			}
			""";
		CSharpCompilation compilation = Compile(source);
		ImmutableArray<ArithmeticOperation> result = Analyse(compilation);

		Assert.Empty(result);
	}

	[Theory]
	[InlineData("var squared = current * current;", "squared * resistance")]
	[InlineData("var squared = current * current; var copy = squared;", "copy * resistance")]
	[InlineData("var squared = (2 * current) * current / 2;", "squared * resistance")]
	public void FollowsLocalInitialisersAndCopies(string declarations, string expression)
	{
		string source = $$"""
			public static class Consumer
			{
				public static object Test(Current current, Resistance resistance)
				{
					{{declarations}}
					return {{expression}};
				}
			}
			""";
		ImmutableArray<ArithmeticOperation> result = AnalyseReturn(source);
		Dimension current = new(ElectricCurrent: 1);
		Dimension squared = new(ElectricCurrent: 2);
		Dimension resistance = new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -2);
		Dimension power = new(Length: 2, Mass: 1, Time: -3);
		ArithmeticOperation[] expected =
		[
			new(ArithmeticOperationKind.Multiply, current, current, squared),
			new(ArithmeticOperationKind.Multiply, squared, resistance, power)
		];

		Assert.Equal(expected, result);
	}

	[Fact]
	public void FollowsSeveralDependentLocalInitialisers()
	{
		const string source = """
			public static class Consumer
			{
				public static object Test(Current current, Resistance resistance, Time time)
				{
					var squared = current * current;
					var heating = squared * resistance;
					return heating * time;
				}
			}
			""";
		ImmutableArray<ArithmeticOperation> result = AnalyseReturn(source);
		Dimension current = new(ElectricCurrent: 1);
		Dimension squared = new(ElectricCurrent: 2);
		Dimension resistance = new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -2);
		Dimension power = new(Length: 2, Mass: 1, Time: -3);
		Dimension time = new(Time: 1);
		Dimension energy = new(Length: 2, Mass: 1, Time: -2);
		ArithmeticOperation[] expected =
		[
			new(ArithmeticOperationKind.Multiply, current, current, squared),
			new(ArithmeticOperationKind.Multiply, squared, resistance, power),
			new(ArithmeticOperationKind.Multiply, power, time, energy)
		];

		Assert.Equal(expected, result);
	}

	[Fact]
	public void RepeatedLocalReferencesShareTheirInferredOperations()
	{
		const string source = """
			public static class Consumer
			{
				public static object Test(Current current)
				{
					var squared = current * current;
					return squared * squared;
				}
			}
			""";
		ImmutableArray<ArithmeticOperation> result = AnalyseReturn(source);
		Dimension current = new(ElectricCurrent: 1);
		Dimension squared = new(ElectricCurrent: 2);
		Dimension fourth = new(ElectricCurrent: 4);
		ArithmeticOperation[] expected =
		[
			new(ArithmeticOperationKind.Multiply, current, current, squared),
			new(ArithmeticOperationKind.Multiply, squared, squared, fourth)
		];

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("var value = ;")]
	[InlineData("var value;")]
	[InlineData("var value = missing;")]
	[InlineData("var value = value;")]
	[InlineData("var value = other; var other = value;")]
	[InlineData("var value = other * current; var other = value * current;")]
	[InlineData("object value = current * current;")]
	[InlineData("Missing value = current * current;")]
	[InlineData("LengthCurrent value = current * current;")]
	[InlineData("LengthCurrent value = value;")]
	[InlineData("LengthCurrent value = other; LengthCurrent other = value;")]
	public void DoesNotInferUnsupportedOrCyclicLocals(string declaration)
	{
		string source = $$"""
			public static class Consumer
			{
				public static object Test(Current current, Resistance resistance)
				{
					{{declaration}}
					return value * resistance;
				}
			}
			""";
		ImmutableArray<ArithmeticOperation> result = AnalyseReturn(source);

		Assert.Empty(result);
	}

	[Theory]
	[InlineData("using var = System.Object;")]
	[InlineData("public class var { }")]
	public void DoesNotTreatATypedDeclarationNamedVarAsImplicitTyping(string typeDeclaration)
	{
		string source = $$"""
			{{typeDeclaration}}
			public static class Consumer
			{
				public static object Test(Current current, Resistance resistance)
				{
					var value = current * current;
					return value * resistance;
				}
			}
			""";
		ImmutableArray<ArithmeticOperation> result = AnalyseReturn(source);

		Assert.Empty(result);
	}

	[Fact]
	public void UsesTheDeclarationTypeRatherThanLaterAssignments()
	{
		const string source = """
			public static class Consumer
			{
				public static object Test(Current current, Resistance resistance, Length length)
				{
					var value = current * current;
					value = length * length;
					return value * resistance;
				}
			}
			""";
		ImmutableArray<ArithmeticOperation> result = AnalyseReturn(source);
		Dimension current = new(ElectricCurrent: 1);
		Dimension squared = new(ElectricCurrent: 2);
		Dimension resistance = new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -2);
		Dimension power = new(Length: 2, Mass: 1, Time: -3);
		ArithmeticOperation[] expected =
		[
			new(ArithmeticOperationKind.Multiply, current, current, squared),
			new(ArithmeticOperationKind.Multiply, squared, resistance, power)
		];

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("var squared = current * current; var combined = squared + squared;", "combined * resistance")]
	[InlineData("var squared = current * current; var combined = squared - squared;", "combined * resistance")]
	[InlineData("var squared = current * current; var combined = -squared;", "combined * resistance")]
	[InlineData("var squared = current * current;", "(-(squared + squared)) * resistance")]
	public void SameDimensionArithmeticPropagatesInferredLocalDimensions(string declarations, string expression)
	{
		string source = $$"""
			public static class Consumer
			{
				public static object Test(Current current, Resistance resistance)
				{
					{{declarations}}
					return {{expression}};
				}
			}
			""";
		ImmutableArray<ArithmeticOperation> result = AnalyseReturn(source);
		Dimension current = new(ElectricCurrent: 1);
		Dimension squared = new(ElectricCurrent: 2);
		Dimension resistance = new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -2);
		Dimension power = new(Length: 2, Mass: 1, Time: -3);
		ArithmeticOperation product = new(ArithmeticOperationKind.Multiply, current, current, squared);
		ArithmeticOperation heating = new(ArithmeticOperationKind.Multiply, squared, resistance, power);
		ArithmeticOperation[] expected = [product, heating];

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("current + length")]
	[InlineData("current - length")]
	[InlineData("current + 2")]
	[InlineData("2 + current")]
	[InlineData("current - 2")]
	[InlineData("2 - current")]
	[InlineData("current + missing")]
	public void InvalidSumsDoNotSupplyDimensionsToEnclosingArithmetic(string sum)
	{
		string source = $$"""
			public static class Consumer
			{
				public static object Test(Current current, Length length, Time time) => ({{sum}}) * time;
			}
			""";
		ImmutableArray<ArithmeticOperation> result = Analyse(Compile(source));

		Assert.Empty(result);
	}

	[Theory]
	[InlineData("squared + area")]
	[InlineData("squared - area")]
	public void InvalidInferredSumsRetainChildOperationsWithoutInferringAResult(string expression)
	{
		string source = $$"""
			public static class Consumer
			{
				public static object Test(Current current, Length length, Time time)
				{
					var squared = current * current;
					var area = length * length;
					var invalid = {{expression}};
					return invalid * time;
				}
			}
			""";
		ImmutableArray<ArithmeticOperation> result = AnalyseReturn(source);
		Dimension current = new(ElectricCurrent: 1);
		Dimension length = new(Length: 1);
		ArithmeticOperation[] expected =
		[
			new(ArithmeticOperationKind.Multiply, current, current, new(ElectricCurrent: 2)),
			new(ArithmeticOperationKind.Multiply, length, length, new(Length: 2))
		];

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("(2 - 3) * current * time")]
	[InlineData("-(2 + 3) * current * time")]
	[InlineData("(-current) * time")]
	public void ScalarSumsAndNegationPreserveDimensions(string expression)
	{
		string source = $$"""
			public static class Consumer
			{
				public static object Test(Current current, Time time) => {{expression}};
			}
			""";
		ImmutableArray<ArithmeticOperation> result = Analyse(Compile(source));
		Dimension current = new(ElectricCurrent: 1);
		Dimension time = new(Time: 1);
		ArithmeticOperation expected = new(ArithmeticOperationKind.Multiply, current, time, new(Time: 1, ElectricCurrent: 1));

		Assert.Equal(expected, Assert.Single(result));
	}

	[Theory]
	[InlineData("(wrapper + wrapper) * time")]
	[InlineData("(wrapper - wrapper) * time")]
	[InlineData("(-wrapper) * time")]
	public void BoundOperatorReturnTypesRemainAuthoritative(string expression)
	{
		string source = $$"""
			public sealed class Wrapper
			{
				public static Current operator +(Wrapper left, Wrapper right) => Current.FromAmps(2);
				public static Current operator -(Wrapper left, Wrapper right) => Current.FromAmps(1);
				public static Current operator -(Wrapper value) => Current.FromAmps(-1);
			}
			public static class Consumer
			{
				public static object Test(Wrapper wrapper, Time time)
				{
					return {{expression}};
				}
			}
			""";
		ImmutableArray<ArithmeticOperation> result = AnalyseReturn(source);
		Dimension current = new(ElectricCurrent: 1);
		Dimension time = new(Time: 1);
		ArithmeticOperation expected = new(ArithmeticOperationKind.Multiply, current, time, new(Time: 1, ElectricCurrent: 1));

		Assert.Equal(expected, Assert.Single(result));
	}

	[Theory]
	[InlineData("(length / length) * current * time")]
	[InlineData("current * (length / length) * time")]
	[InlineData("current / (length / length) * time")]
	[InlineData("(-(length / length) + 2) * current * time")]
	public void RatiosActAsScalarsInLaterArithmetic(string expression)
	{
		string source = $$"""
			public static class Consumer
			{
				public static object Test(Length length, Current current, Time time) => {{expression}};
			}
			""";
		ImmutableArray<ArithmeticOperation> result = Analyse(Compile(source));
		Dimension current = new(ElectricCurrent: 1);
		Dimension time = new(Time: 1);
		ArithmeticOperation expected = new(ArithmeticOperationKind.Multiply, current, time, new(Time: 1, ElectricCurrent: 1));

		Assert.Equal(expected, Assert.Single(result));
	}

	[Theory]
	[InlineData("ratio * length * time")]
	[InlineData("length / ratio * time")]
	[InlineData("(ratio + 2) * length * time")]
	public void RatiosOfInferredLocalsPreserveChildOperationsAndSupplyScalars(string expression)
	{
		string source = $$"""
			public static class Consumer
			{
				public static object Test(Current current, Length length, Time time)
				{
					var squared = current * current;
					var ratio = squared / squared;
					return {{expression}};
				}
			}
			""";
		ImmutableArray<ArithmeticOperation> result = AnalyseReturn(source);
		Dimension current = new(ElectricCurrent: 1);
		Dimension length = new(Length: 1);
		Dimension time = new(Time: 1);
		ArithmeticOperation[] expected =
		[
			new(ArithmeticOperationKind.Multiply, current, current, new(ElectricCurrent: 2)),
			new(ArithmeticOperationKind.Multiply, length, time, new(Length: 1, Time: 1))
		];

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("var cancelled = current * inverse;", "cancelled * length * time")]
	[InlineData("var cancelled = current * inverse; var copy = cancelled;", "copy * length * time")]
	[InlineData("var cancelled = current * inverse;", "(cancelled - 2) * length * time")]
	public void InverseProductsProduceScalarsForLaterArithmetic(string declaration, string expression)
	{
		string source = $$"""
			public static class Consumer
			{
				public static object Test(Current current, Length length, Time time)
				{
					var inverse = 2 / current;
					{{declaration}}
					return {{expression}};
				}
			}
			""";
		ImmutableArray<ArithmeticOperation> result = AnalyseReturn(source);
		Dimension current = new(ElectricCurrent: 1);
		Dimension inverse = new(ElectricCurrent: -1);
		Dimension length = new(Length: 1);
		Dimension time = new(Time: 1);
		ArithmeticOperation[] expected =
		[
			new(ArithmeticOperationKind.ScalarDivide, default, current, inverse),
			new(ArithmeticOperationKind.Multiply, current, inverse, default),
			new(ArithmeticOperationKind.Multiply, length, time, new(Length: 1, Time: 1))
		];

		Assert.Equal(expected, result);
	}

	[Fact]
	public void TracksDimensionsThroughProductsPowersAndLocalVariables()
	{
		const string source = """
			using DimensionalQuantities.TypeSystem;
			public static class Consumer
			{
				public static object Test(Current current, Time time)
				{
					var squared = current * current;
					var cubed = Power<Fraction<TerminalHex3, TerminalHex2>>.Of(squared);
					return cubed * time;
				}
			}
			""";
		CSharpCompilation compilation = Compile(source);
		SyntaxTree tree = compilation.SyntaxTrees.Single(candidate => candidate.FilePath == "Consumer.cs");
		ReturnStatementSyntax statement = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<ReturnStatementSyntax>().Single();
		QuantityExpressionAnalyser analyser = new(compilation.GetSemanticModel(tree));
		QuantityExpressionAnalysis result = analyser.Analyse(statement.Expression!, TestContext.Current.CancellationToken);
		Dimension current = new(ElectricCurrent: 1);
		Dimension squared = new(ElectricCurrent: 2);
		Dimension cubed = new(ElectricCurrent: 3);
		Dimension time = new(Time: 1);
		ArithmeticOperation[] expectedArithmetic =
		[
			new(ArithmeticOperationKind.Multiply, current, current, squared),
			new(ArithmeticOperationKind.Multiply, cubed, time, new(Time: 1, ElectricCurrent: 3))
		];
		PowerOperation expectedPower = new(squared, new Rational(3, 2), cubed,
			"global::DimensionalQuantities.TypeSystem.Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex3, global::DimensionalQuantities.TypeSystem.TerminalHex2>");

		Assert.Multiple(
			() => Assert.Equal(expectedArithmetic, result.ArithmeticOperations),
			() => Assert.Equal(expectedPower, Assert.Single(result.PowerOperations)));
	}

	[Fact]
	public void TracksConveniencePowersThroughLocalsIntoArithmetic()
	{
		const string source = """
			class Consumer
			{
				object Value(Current current, Resistance resistance)
				{
					var squared = current.Squared();
					var root = squared.SquareRoot();
					return root * resistance;
				}
			}
			""";
		CSharpCompilation compilation = Compile(source);
		SyntaxTree tree = compilation.SyntaxTrees.Single(candidate => candidate.FilePath == "Consumer.cs");
		ReturnStatementSyntax statement = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<ReturnStatementSyntax>().Single();
		QuantityExpressionAnalyser analyser = new(compilation.GetSemanticModel(tree));
		Dimension current = new(ElectricCurrent: 1);
		Dimension squared = new(ElectricCurrent: 2);
		Dimension resistance = new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -2);
		Dimension voltage = new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -1);
		PowerMethodOperation[] expectedMethods =
		[
			new(current, 2, squared, "Squared"),
			new(squared, new Rational(1, 2), current, "SquareRoot")
		];
		ArithmeticOperation expectedArithmetic = new(ArithmeticOperationKind.Multiply, current, resistance, voltage);

		QuantityExpressionAnalysis result = analyser.Analyse(statement.Expression!, TestContext.Current.CancellationToken);

		Assert.Multiple(
			() => Assert.Equal(expectedMethods, result.PowerMethodOperations),
			() => Assert.Equal(expectedArithmetic, Assert.Single(result.ArithmeticOperations)));
	}

	[Theory]
	[InlineData("LengthCurrent value = length * current;")]
	[InlineData("LengthCurrent first = length * current; LengthCurrent value = first;")]
	public void FollowsMatchingExplicitLocalInitialisers(string declarations)
	{
		string source = $$"""
			class Consumer
			{
				object Test(Length length, Current current, Time time)
				{
					{{declarations}}
					return value * time;
				}
			}
			""";
		Dimension length = new(Length: 1);
		Dimension current = new(ElectricCurrent: 1);
		Dimension product = new(Length: 1, ElectricCurrent: 1);
		Dimension time = new(Time: 1);
		ArithmeticOperation[] expected =
		[
			new(ArithmeticOperationKind.Multiply, length, current, product),
			new(ArithmeticOperationKind.Multiply, product, time, new(Length: 1, Time: 1, ElectricCurrent: 1))
		];

		ImmutableArray<ArithmeticOperation> result = AnalyseReturn(source);

		Assert.Equal(expected, result);
	}

	private static ImmutableArray<ArithmeticOperation> Analyse(CSharpCompilation compilation)
	{
		SyntaxTree tree = compilation.SyntaxTrees.Single(candidate => candidate.FilePath == "Consumer.cs");
		ArrowExpressionClauseSyntax expressionBody = tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>().Single();
		SemanticModel semanticModel = compilation.GetSemanticModel(tree);
		QuantityExpressionAnalyser analyser = new(semanticModel);
		return analyser.Analyse(expressionBody.Expression, TestContext.Current.CancellationToken).ArithmeticOperations;
	}

	private static CSharpCompilation Compile(string source)
	{
		string[] assemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = assemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		QuantityGenerationPlan plan = QuantityDiscovery.Discover([]);
		SyntaxTree knownQuantities = CSharpSyntaxTree.ParseText(QuantitySourceEmitter.Emit(plan.KnownQuantities), _parseOptions,
			path: "KnownQuantities.g.cs");
		return CSharpCompilation.Create("DiscoveryConsumer",
			[CSharpSyntaxTree.ParseText(source, _parseOptions, path: "Consumer.cs"), knownQuantities], references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
	}

	private static ImmutableArray<ArithmeticOperation> AnalyseReturn(string source)
	{
		CSharpCompilation compilation = Compile(source);
		SyntaxTree tree = compilation.SyntaxTrees.Single(candidate => candidate.FilePath == "Consumer.cs");
		ReturnStatementSyntax statement = tree.GetRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single();
		QuantityExpressionAnalyser analyser = new(compilation.GetSemanticModel(tree));
		return analyser.Analyse(statement.Expression!, TestContext.Current.CancellationToken).ArithmeticOperations;
	}
}
