using System.Collections.Immutable;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.Roslyn.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DimensionalQuantities.Roslyn.Tests.Discovery;

[NotInParallel(nameof(ArithmeticDiscoveryTests))]
public sealed class ArithmeticDiscoveryTests
{
	private static readonly CSharpParseOptions _parseOptions = new(LanguageVersion.Preview);

	[Test]
	[Arguments("length * time", true)]
	[Arguments("length / time", false)]
	public async Task ReadsBoundQuantityOperands(string expression, bool multiply)
	{
		ArithmeticOperation operation = (await Assert.That(Discover(expression)).HasSingleItem());
		Dimension left = new(Length: 1);
		Dimension right = new(Time: 1);
		await Assert.That(operation).IsEqualTo(new(multiply ? ArithmeticOperationKind.Multiply : ArithmeticOperationKind.Divide,
			left, right, multiply ? left * right : left / right));
	}

	[Test]
	[Arguments("length * time", true)]
	[Arguments("length / time", false)]
	public async Task ReadsNullableQuantityOperands(string expression, bool multiply)
	{
		string source = $$"""
			public static class Consumer
			{
				public static object Value(Length? length, Time? time) => {{expression}};
			}
			""";
		ArithmeticOperation operation = (await Assert.That(DiscoverFromSource(source)).HasSingleItem());
		Dimension left = new(Length: 1);
		Dimension right = new(Time: 1);
		await Assert.That(operation).IsEqualTo(new(multiply ? ArithmeticOperationKind.Multiply : ArithmeticOperationKind.Divide,
			left, right, multiply ? left * right : left / right));
	}

	[Test]
	public async Task NullableQuantityDivisorRequestsTheInverseDimension()
	{
		const string source = """
			public static class Consumer
			{
				public static object Value(Length? length) => 2d / length;
			}
			""";
		ArithmeticOperation operation = (await Assert.That(DiscoverFromSource(source)).HasSingleItem());
		await Assert.That(operation).IsEqualTo(new(ArithmeticOperationKind.ScalarDivide, default, new(Length: 1), new(Length: -1)));
	}

	[Test]
	[Arguments("double?", "Time")]
	[Arguments("double?", "Time?")]
	[Arguments("float?", "Time")]
	[Arguments("float?", "Time?")]
	[Arguments("int?", "Time")]
	[Arguments("int?", "Time?")]
	public async Task NullableNumericScalarDivisionRequestsTheInverseDimension(string scalarType, string quantityType)
	{
		string source = $$"""
			public static class Consumer
			{
				public static object Value({{scalarType}} scalar, {{quantityType}} time) => scalar / time;
			}
			""";
		ArithmeticOperation operation = (await Assert.That(DiscoverFromSource(source)).HasSingleItem());
		await Assert.That(operation).IsEqualTo(new(ArithmeticOperationKind.ScalarDivide, default, new(Time: 1), new(Time: -1)));
	}

	[Test]
	[Arguments("length * missing")]
	[Arguments("length * 1m")]
	[Arguments("length * \"text\"")]
	[Arguments("length + time")]
	[Arguments("length * scalar")]
	public async Task RejectsUnsupportedOperands(string expression)
	{
		await Assert.That(Discover(expression)).IsEmpty();
	}

	[Test]
	public async Task ExistingBoundOperatorControlsTheResult()
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
		await Assert.That(DiscoverFromSource(source)).IsEmpty();
	}

	[Test]
	public async Task ScalarDivisionRequestsTheInverseDimension()
	{
		const string source = """
			public static class Consumer
			{
				public static object Value(Length length) => 2d / length;
			}
			""";
		ArithmeticOperation operation = (await Assert.That(DiscoverFromSource(source)).HasSingleItem());
		await Assert.That(operation).IsEqualTo(new(ArithmeticOperationKind.ScalarDivide, default, new(Length: 1), new(Length: -1)));
	}

	[Test]
	public async Task DecimalDoesNotBecomeAnImplicitDoubleScalar()
	{
		const string source = """
			public static class Consumer
			{
				public static object Value(Length length) => 2m / length;
			}
			""";
		await Assert.That(DiscoverFromSource(source)).IsEmpty();
	}

	[Test]
	public async Task NullableDecimalDoesNotBecomeAnImplicitDoubleScalar()
	{
		const string source = """
			public static class Consumer
			{
				public static object Value(decimal? scalar, Time? time) => scalar / time;
			}
			""";
		await Assert.That(DiscoverFromSource(source)).IsEmpty();
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
			if (ArithmeticDiscovery.TryRead(binary, model, reader, out ArithmeticOperation operation, TestContext.Current!.Execution.CancellationToken))
			{
				operations.Add(operation);
			}
		}

		return operations.ToImmutable();
	}
}
