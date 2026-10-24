using System.Collections.Immutable;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.Roslyn.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Discovery;

public sealed class ArithmeticDiscoveryTests
{
	private static readonly CSharpParseOptions _parseOptions = new(LanguageVersion.Preview);

	[Theory]
	[InlineData("length * time", true)]
	[InlineData("length / time", false)]
	public void ReadsBoundQuantityOperands(string expression, bool multiply)
	{
		ArithmeticOperation operation = Assert.Single(Discover(expression));
		Dimension left = new(Length: 1);
		Dimension right = new(Time: 1);
		Assert.Equal(new(multiply ? ArithmeticOperationKind.Multiply : ArithmeticOperationKind.Divide,
			left, right, multiply ? left * right : left / right), operation);
	}

	[Theory]
	[InlineData("length * missing")]
	[InlineData("length * 1m")]
	[InlineData("length * \"text\"")]
	[InlineData("length + time")]
	[InlineData("length * scalar")]
	public void RejectsUnsupportedOperands(string expression)
	{
		Assert.Empty(Discover(expression));
	}

	[Fact]
	public void ExistingBoundOperatorControlsTheResult()
	{
		const string source = """
			public static class Custom
			{
				extension(Length)
				{
					public static double operator *(Length length, Time time) => 7;
				}
			}
			public static class Consumer
			{
				public static object Value(Length length, Time time) => length * time;
			}
			""";
		Assert.Empty(DiscoverFromSource(source));
	}

	[Fact]
	public void ScalarDivisionRequestsTheInverseDimension()
	{
		const string source = """
			public static class Consumer
			{
				public static object Value(Length length) => 2d / length;
			}
			""";
		ArithmeticOperation operation = Assert.Single(DiscoverFromSource(source));
		Assert.Equal(new(ArithmeticOperationKind.ScalarDivide, default, new(Length: 1), new(Length: -1)), operation);
	}

	[Fact]
	public void DecimalDoesNotBecomeAnImplicitDoubleScalar()
	{
		const string source = """
			public static class Consumer
			{
				public static object Value(Length length) => 2m / length;
			}
			""";
		Assert.Empty(DiscoverFromSource(source));
	}

	private static ImmutableArray<ArithmeticOperation> Discover(string expression)
	{
		string source = $$"""
			using DimensionalQuantities.TypeSystem;
			public static class Consumer
			{
				public static object Value(Length length, Time time,
					Quantity<Dimension<TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0>> scalar)
					=> {{expression}};
			}
			""";
		return DiscoverFromSource(source);
	}

	private static ImmutableArray<ArithmeticOperation> DiscoverFromSource(string source)
	{
		SyntaxTree tree = CSharpSyntaxTree.ParseText(source, _parseOptions);
		QuantityGenerationPlan defaults = DefaultQuantityDiscovery.Discover();
		string defaultSource = QuantitySourceEmitter.Emit([.. defaults.KnownQuantities, .. defaults.AdditionalQuantities]);
		SyntaxTree defaultsTree = CSharpSyntaxTree.ParseText(defaultSource, _parseOptions);
		MetadataReference[] references = [MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(DimensionalQuantities.TypeSystem.Quantity<>).Assembly.Location)];
		CSharpCompilation compilation = CSharpCompilation.Create("ArithmeticDiscoveryTests", [tree, defaultsTree], references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		SemanticModel model = compilation.GetSemanticModel(tree);
		QuantityTypeReader reader = new(compilation);
		ImmutableArray<ArithmeticOperation>.Builder operations = ImmutableArray.CreateBuilder<ArithmeticOperation>();
		foreach (BinaryExpressionSyntax binary in tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>())
		{
			if (ArithmeticDiscovery.TryRead(binary, model, reader, out ArithmeticOperation operation, TestContext.Current.CancellationToken))
			{
				operations.Add(operation);
			}
		}

		return operations.ToImmutable();
	}
}
