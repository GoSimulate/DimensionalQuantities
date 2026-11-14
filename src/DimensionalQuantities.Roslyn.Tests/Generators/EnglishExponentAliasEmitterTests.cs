using DimensionalQuantities.Numerics;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.Roslyn.Generators;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DimensionalQuantities.Roslyn.Tests.Generators;

[NotInParallel(nameof(EnglishExponentAliasEmitterTests))]
public sealed class EnglishExponentAliasEmitterTests
{
	[Test]
	public async Task EmitsOnlyRequestedAliasesWithoutDuplicates()
	{
		EnglishExponentAlias[] aliases =
		[
			new("NumberTwo", new Rational(2), "global::DimensionalQuantities.TypeSystem.TerminalHex2"),
			new("NumberHalf", new Rational(1, 2), "global::DimensionalQuantities.TypeSystem.Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex2>"),
			new("NumberTwo", new Rational(2), "global::DimensionalQuantities.TypeSystem.TerminalHex2")
		];

		string result = EnglishExponentAliasEmitter.Emit(aliases);
		SyntaxTree tree = CSharpSyntaxTree.ParseText(result, new CSharpParseOptions(LanguageVersion.Preview), cancellationToken: TestContext.Current!.Execution.CancellationToken);
		string[] names = tree.GetCompilationUnitRoot(TestContext.Current!.Execution.CancellationToken).Usings.Select(directive => directive.Alias!.Name.Identifier.ValueText).OrderBy(name => name).ToArray();
		string[] expected = ["NumberHalf", "NumberTwo"];

		await Assert.That(names).IsEquivalentTo(expected, TUnit.Assertions.Enums.CollectionOrdering.Matching);
	}

	[Test]
	public async Task AliasesResolveToTheirEncodedTypesInAnotherFile()
	{
		EnglishExponentAlias[] aliases =
		[
			new("NumberTwo", new Rational(2), "global::DimensionalQuantities.TypeSystem.TerminalHex2"),
			new("NumberHalf", new Rational(1, 2), "global::DimensionalQuantities.TypeSystem.Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex2>")
		];
		const string source = "class Consumer { public NumberTwo Integer; public NumberHalf Fraction; }";
		CSharpParseOptions options = new(LanguageVersion.Preview);
		string[] assemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = assemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		CSharpCompilation compilation = CSharpCompilation.Create("AliasConsumer",
			[CSharpSyntaxTree.ParseText(EnglishExponentAliasEmitter.Emit(aliases), options, cancellationToken: TestContext.Current!.Execution.CancellationToken), CSharpSyntaxTree.ParseText(source, options, cancellationToken: TestContext.Current!.Execution.CancellationToken)],
			references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		INamedTypeSymbol consumer = compilation.GetTypeByMetadataName("Consumer")!;
		IFieldSymbol integer = (await Assert.That(await Assert.That(consumer.GetMembers("Integer")).HasSingleItem()).IsAssignableTo<IFieldSymbol>())!;
		IFieldSymbol fraction = (await Assert.That(await Assert.That(consumer.GetMembers("Fraction")).HasSingleItem()).IsAssignableTo<IFieldSymbol>())!;
		TypeSystemNumberReader reader = new(compilation);
		bool integerRead = reader.TryRead(integer.Type, out Rational integerResult, TestContext.Current!.Execution.CancellationToken);
		bool fractionRead = reader.TryRead(fraction.Type, out Rational fractionResult, TestContext.Current!.Execution.CancellationToken);

		using (Assert.Multiple())
		{
			await Assert.That(compilation.GetDiagnostics(TestContext.Current!.Execution.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)).IsEmpty();
			await Assert.That(integerRead).IsTrue();
			await Assert.That(fractionRead).IsTrue();
			await Assert.That(integerResult).IsEqualTo(new Rational(2));
			await Assert.That(fractionResult).IsEqualTo(new Rational(1, 2));
		}
	}

	[Test]
	public async Task NoRequestedNamesProducesNoAliases()
	{
		string result = EnglishExponentAliasEmitter.Emit([]);
		SyntaxTree tree = CSharpSyntaxTree.ParseText(result, new CSharpParseOptions(LanguageVersion.Preview), cancellationToken: TestContext.Current!.Execution.CancellationToken);

		await Assert.That(tree.GetCompilationUnitRoot(TestContext.Current!.Execution.CancellationToken).Usings).IsEmpty();
	}
}
