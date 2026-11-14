using DimensionalQuantities.Numerics;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.Roslyn.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Emission;

public sealed class EnglishExponentAliasEmitterTests
{
	[Fact]
	public void EmitsOnlyRequestedAliasesWithoutDuplicates()
	{
		EnglishExponentAlias[] aliases =
		[
			new("NumberTwo", 2, "global::DimensionalQuantities.TypeSystem.TerminalHex2"),
			new("NumberHalf", new Rational(1, 2), "global::DimensionalQuantities.TypeSystem.Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex2>"),
			new("NumberTwo", 2, "global::DimensionalQuantities.TypeSystem.TerminalHex2")
		];
		SyntaxTree tree = CSharpSyntaxTree.ParseText(EnglishExponentAliasEmitter.Emit(aliases), new CSharpParseOptions(LanguageVersion.Preview), cancellationToken: TestContext.Current.CancellationToken);
		Assert.Equal(new[] { "NumberHalf", "NumberTwo" }, tree.GetCompilationUnitRoot(TestContext.Current.CancellationToken).Usings
			.Select(directive => directive.Alias!.Name.Identifier.ValueText).OrderBy(name => name));
	}

	[Theory]
	[InlineData("NumberTwo", 2, 1, "global::DimensionalQuantities.TypeSystem.TerminalHex2")]
	[InlineData("NumberHalf", 1, 2, "global::DimensionalQuantities.TypeSystem.Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex2>")]
	public void AliasResolvesToItsEncodedTypeInAnotherFile(string name, int numerator, int denominator, string encodedType)
	{
		Rational expected = new(numerator, denominator);
		CSharpCompilation compilation = QuantitySourceEmitterTests.Compile(
			$$"""
			class Consumer
			{
				{{name}} Aliased;
				{{encodedType}} Encoded;
			}
			""", EnglishExponentAliasEmitter.Emit([new(name, expected, encodedType)]));
		INamedTypeSymbol consumer = compilation.GetTypeByMetadataName("Consumer")!;
		IFieldSymbol aliased = consumer.GetMembers("Aliased").OfType<IFieldSymbol>().Single();
		IFieldSymbol encoded = consumer.GetMembers("Encoded").OfType<IFieldSymbol>().Single();
		TypeSystemNumberReader reader = new(compilation);
		bool read = reader.TryRead(aliased.Type, out Rational actual, TestContext.Current.CancellationToken);
		Assert.Multiple(
			() => Assert.True(SymbolEqualityComparer.Default.Equals(aliased.Type, encoded.Type)),
			() => Assert.True(read),
			() => Assert.Equal(expected, actual));
	}

	[Fact]
	public void NoRequestedNamesProducesNoAliases()
	{
		SyntaxTree tree = CSharpSyntaxTree.ParseText(EnglishExponentAliasEmitter.Emit([]), new CSharpParseOptions(LanguageVersion.Preview), cancellationToken: TestContext.Current.CancellationToken);
		Assert.Empty(tree.GetCompilationUnitRoot(TestContext.Current.CancellationToken).Usings);
	}
}
