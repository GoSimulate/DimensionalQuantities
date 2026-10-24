using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Roslyn.Discovery;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Discovery;

public sealed class QuantityNameDiscoveryTests
{
	[Theory]
	[InlineData("class Consumer { void Read(LengthCurrent value) { } }")]
	[InlineData("class Consumer { LengthCurrent Value; }")]
	[InlineData("class Consumer { LengthCurrent Value { get; set; } }")]
	[InlineData("class Consumer { LengthCurrent Value() => default; }")]
	[InlineData("class Consumer { System.Collections.Generic.List<LengthCurrent> Values; }")]
	[InlineData("class Consumer { LengthCurrent[] Values; }")]
	[InlineData("class Consumer { System.Type Value() => typeof(LengthCurrent); }")]
	public void ReadsUnresolvedNamesInTypePositions(string source)
	{
		CSharpCompilation compilation = Compile(source);
		SyntaxTree tree = compilation.SyntaxTrees.Single();
		IdentifierNameSyntax name = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<IdentifierNameSyntax>()
			.Single(identifier => identifier.Identifier.ValueText == "LengthCurrent");

		bool success = QuantityNameDiscovery.TryRead(name, compilation.GetSemanticModel(tree), out Dimension result, TestContext.Current.CancellationToken);
		Dimension expected = new(Length: 1, ElectricCurrent: 1);

		Assert.Multiple(
			() => Assert.True(success),
			() => Assert.Equal(expected, result));
	}

	[Theory]
	[InlineData("class LengthCurrent { } class Consumer { LengthCurrent Value; }")]
	[InlineData("using LengthCurrent = System.Int32; class Consumer { LengthCurrent Value; }")]
	[InlineData("class Consumer { object Value() => LengthCurrent; }")]
	[InlineData("class Consumer { Missing.LengthCurrent Value; }")]
	public void IgnoresBoundSymbolsAndNamesOutsideSimpleTypePositions(string source)
	{
		CSharpCompilation compilation = Compile(source);
		SyntaxTree tree = compilation.SyntaxTrees.Single();
		IdentifierNameSyntax name = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<IdentifierNameSyntax>()
			.Last(identifier => identifier.Identifier.ValueText == "LengthCurrent");

		bool result = QuantityNameDiscovery.TryRead(name, compilation.GetSemanticModel(tree), out _, TestContext.Current.CancellationToken);

		Assert.False(result);
	}

	private static CSharpCompilation Compile(string source)
	{
		string[] assemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = assemblies.Select(path => MetadataReference.CreateFromFile(path));
		return CSharpCompilation.Create("TypeNameConsumer",
			[CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp14), cancellationToken: TestContext.Current.CancellationToken)],
			references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
	}
}
