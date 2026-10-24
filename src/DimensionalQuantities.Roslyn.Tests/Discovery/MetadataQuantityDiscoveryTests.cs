using System.Collections.Immutable;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Discovery;

public sealed class MetadataQuantityDiscoveryTests
{
	[Theory]
	[InlineData("Library.Create()")]
	[InlineData("Library.Field")]
	[InlineData("Library.Property")]
	[InlineData("Library.Values()")]
	[InlineData("Library.Values()[0]")]
	[InlineData("Library.Pair()")]
	public void DiscoversQuantityTypesInReferencedResultsAndCollections(string expression)
	{
		CSharpCompilation compilation = CompileConsumer(expression);
		SyntaxTree tree = compilation.SyntaxTrees.Single();
		ExpressionSyntax value = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<ArrowExpressionClauseSyntax>().Single().Expression;
		MetadataQuantityDiscovery discovery = new(compilation.GetSemanticModel(tree));

		ImmutableArray<Dimension> result = discovery.Discover(value, TestContext.Current.CancellationToken);
		Dimension expected = new(Length: 1, ElectricCurrent: 1);

		Assert.Equal(expected, Assert.Single(result));
	}

	[Theory]
	[InlineData("Library.Other()")]
	[InlineData("Library.Missing()")]
	[InlineData("Library.Zero()")]
	public void IgnoresUnrelatedUnresolvedAndDimensionlessTypes(string expression)
	{
		CSharpCompilation compilation = CompileConsumer(expression);
		SyntaxTree tree = compilation.SyntaxTrees.Single();
		ExpressionSyntax value = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<ArrowExpressionClauseSyntax>().Single().Expression;
		MetadataQuantityDiscovery discovery = new(compilation.GetSemanticModel(tree));

		ImmutableArray<Dimension> result = discovery.Discover(value, TestContext.Current.CancellationToken);

		Assert.Empty(result);
	}

	private static CSharpCompilation CompileConsumer(string expression)
	{
		const string librarySource = """
			using DimensionalQuantities.TypeSystem;
			using Q = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<
				DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.TerminalHex0,
				DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex1,
				DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0,
				DimensionalQuantities.TypeSystem.TerminalHex0>>;
			public static class Library
			{
				public static Q Create() => default;
				public static Q Field;
				public static Q Property => default;
				public static System.Collections.Generic.List<Q[]> Values() => new();
				public static (Q, Q) Pair() => default;
				public static string Other() => "text";
				public static Quantity<Dimension<TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0>> Zero() => default;
			}
			""";
		string[] platformAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		MetadataReference[] references = platformAssemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location)).ToArray();
		CSharpParseOptions options = new(LanguageVersion.CSharp14);
		CSharpCompilation library = CSharpCompilation.Create("MetadataLibrary", [CSharpSyntaxTree.ParseText(librarySource, options)], references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		using MemoryStream stream = new();
		EmitResult emitted = library.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);
		if (!emitted.Success)
		{
			throw new InvalidOperationException(string.Join(Environment.NewLine, emitted.Diagnostics));
		}

		return CSharpCompilation.Create("MetadataConsumer",
			[CSharpSyntaxTree.ParseText($"public class Consumer {{ public object Value() => {expression}; }}", options)],
			[.. references, MetadataReference.CreateFromImage(stream.ToArray())], new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
	}
}
