using DimensionalQuantities.Numerics;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.Roslyn.Generators;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generators;

public sealed class EnglishExponentAliasEmitterTests
{
	[Fact]
	public void EmitsOnlyRequestedAliasesWithoutDuplicates()
	{
		EnglishExponentAlias[] aliases =
		[
			new("NumberTwo", new Rational(2), "global::DimensionalQuantities.TypeSystem.TerminalHex2"),
			new("NumberHalf", new Rational(1, 2), "global::DimensionalQuantities.TypeSystem.Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex2>"),
			new("NumberTwo", new Rational(2), "global::DimensionalQuantities.TypeSystem.TerminalHex2")
		];

		string result = EnglishExponentAliasEmitter.Emit(aliases);
		SyntaxTree tree = CSharpSyntaxTree.ParseText(result, new CSharpParseOptions(LanguageVersion.CSharp14), cancellationToken: TestContext.Current.CancellationToken);
		string[] names = tree.GetCompilationUnitRoot(TestContext.Current.CancellationToken).Usings.Select(directive => directive.Alias!.Name.Identifier.ValueText).OrderBy(name => name).ToArray();
		string[] expected = ["NumberHalf", "NumberTwo"];

		Assert.Equal(expected, names);
	}

	[Fact]
	public void AliasesResolveToTheirEncodedTypesInAnotherFile()
	{
		EnglishExponentAlias[] aliases =
		[
			new("NumberTwo", new Rational(2), "global::DimensionalQuantities.TypeSystem.TerminalHex2"),
			new("NumberHalf", new Rational(1, 2), "global::DimensionalQuantities.TypeSystem.Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex2>")
		];
		const string source = "class Consumer { public NumberTwo Integer; public NumberHalf Fraction; }";
		CSharpParseOptions options = new(LanguageVersion.CSharp14);
		string[] assemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = assemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		CSharpCompilation compilation = CSharpCompilation.Create("AliasConsumer",
			[CSharpSyntaxTree.ParseText(EnglishExponentAliasEmitter.Emit(aliases), options, cancellationToken: TestContext.Current.CancellationToken), CSharpSyntaxTree.ParseText(source, options, cancellationToken: TestContext.Current.CancellationToken)],
			references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		INamedTypeSymbol consumer = compilation.GetTypeByMetadataName("Consumer")!;
		IFieldSymbol integer = Assert.IsAssignableFrom<IFieldSymbol>(Assert.Single(consumer.GetMembers("Integer")));
		IFieldSymbol fraction = Assert.IsAssignableFrom<IFieldSymbol>(Assert.Single(consumer.GetMembers("Fraction")));
		TypeSystemNumberReader reader = new(compilation);
		bool integerRead = reader.TryRead(integer.Type, out Rational integerResult, TestContext.Current.CancellationToken);
		bool fractionRead = reader.TryRead(fraction.Type, out Rational fractionResult, TestContext.Current.CancellationToken);

		Assert.Multiple(
			() => Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)),
			() => Assert.True(integerRead),
			() => Assert.True(fractionRead),
			() => Assert.Equal(new Rational(2), integerResult),
			() => Assert.Equal(new Rational(1, 2), fractionResult));
	}

	[Fact]
	public void NoRequestedNamesProducesNoAliases()
	{
		string result = EnglishExponentAliasEmitter.Emit([]);
		SyntaxTree tree = CSharpSyntaxTree.ParseText(result, new CSharpParseOptions(LanguageVersion.CSharp14), cancellationToken: TestContext.Current.CancellationToken);

		Assert.Empty(tree.GetCompilationUnitRoot(TestContext.Current.CancellationToken).Usings);
	}
}
