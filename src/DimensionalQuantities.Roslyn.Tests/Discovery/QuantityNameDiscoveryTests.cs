using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Roslyn.Discovery;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DimensionalQuantities.Roslyn.Tests.Discovery;

[NotInParallel(nameof(QuantityNameDiscoveryTests))]
public sealed class QuantityNameDiscoveryTests
{
	[Test]
	[Arguments("class Consumer { void Read(LengthCurrent value) { } }")]
	[Arguments("class Consumer { LengthCurrent Value; }")]
	[Arguments("class Consumer { LengthCurrent Value { get; set; } }")]
	[Arguments("class Consumer { LengthCurrent Value() => default; }")]
	[Arguments("class Consumer { System.Collections.Generic.List<LengthCurrent> Values; }")]
	[Arguments("class Consumer { LengthCurrent[] Values; }")]
	[Arguments("class Consumer { System.Type Value() => typeof(LengthCurrent); }")]
	public async Task ReadsUnresolvedNamesInTypePositions(string source)
	{
		CSharpCompilation compilation = Compile(source);
		SyntaxTree tree = compilation.SyntaxTrees.Single();
		IdentifierNameSyntax name = tree.GetRoot(TestContext.Current!.Execution.CancellationToken).DescendantNodes().OfType<IdentifierNameSyntax>()
			.Single(identifier => identifier.Identifier.ValueText == "LengthCurrent");

		bool success = QuantityNameDiscovery.TryRead(name, compilation.GetSemanticModel(tree), out Dimension result, TestContext.Current!.Execution.CancellationToken);
		Dimension expected = new(Length: 1, ElectricCurrent: 1);

		using (Assert.Multiple())
		{
			await Assert.That(success).IsTrue();
			await Assert.That(result).IsEqualTo(expected);
		}
	}

	[Test]
	[Arguments("class LengthCurrent { } class Consumer { LengthCurrent Value; }")]
	[Arguments("using LengthCurrent = System.Int32; class Consumer { LengthCurrent Value; }")]
	[Arguments("class Consumer { object Value() => LengthCurrent; }")]
	[Arguments("class Consumer { Missing.LengthCurrent Value; }")]
	public async Task IgnoresBoundSymbolsAndNamesOutsideSimpleTypePositions(string source)
	{
		CSharpCompilation compilation = Compile(source);
		SyntaxTree tree = compilation.SyntaxTrees.Single();
		IdentifierNameSyntax name = tree.GetRoot(TestContext.Current!.Execution.CancellationToken).DescendantNodes().OfType<IdentifierNameSyntax>()
			.Last(identifier => identifier.Identifier.ValueText == "LengthCurrent");

		bool result = QuantityNameDiscovery.TryRead(name, compilation.GetSemanticModel(tree), out _, TestContext.Current!.Execution.CancellationToken);

		await Assert.That(result).IsFalse();
	}

	private static CSharpCompilation Compile(string source)
	{
		string[] assemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = assemblies.Select(path => MetadataReference.CreateFromFile(path));
		return CSharpCompilation.Create("TypeNameConsumer",
			[CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview), cancellationToken: TestContext.Current!.Execution.CancellationToken)],
			references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
	}
}
