using System.Collections.Immutable;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.Roslyn.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generators;

public sealed partial class QuantityGeneratorTests
{
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
	public void GeneratorIgnoresUnrelatedIncompleteAndDimensionlessArithmetic()
	{
		const string source = """
			public static class Consumer
			{
				public static int Number(int left, int right) => left * right;
				public static object Incomplete(Length length) => length * Missing;
				public static object Dimensionless(Length length) => length / length;
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
