using System.Collections.Immutable;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.Roslyn.Generators;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Discovery;

public sealed class ArithmeticDiscoveryTests
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
	[InlineData("a / a")]
	public void DiscoverySkipsUnsupportedOrDimensionlessExpressions(string expression)
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

	private static ImmutableArray<ArithmeticOperation> Analyse(CSharpCompilation compilation)
	{
		SyntaxTree tree = compilation.SyntaxTrees.Single(candidate => candidate.FilePath == "Consumer.cs");
		ArrowExpressionClauseSyntax expressionBody = tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>().Single();
		SemanticModel semanticModel = compilation.GetSemanticModel(tree);
		return ArithmeticDiscovery.Analyse(expressionBody.Expression, semanticModel, TestContext.Current.CancellationToken);
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
}
