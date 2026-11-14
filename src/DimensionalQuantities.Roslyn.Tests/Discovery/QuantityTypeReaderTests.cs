using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Numerics;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DimensionalQuantities.Roslyn.Tests.Discovery;

[NotInParallel(nameof(QuantityTypeReaderTests))]
public sealed class QuantityTypeReaderTests
{
	private static readonly CSharpParseOptions _parseOptions = new(LanguageVersion.Preview);

	[Test]
	public async Task MissingRuntimeReferenceThrowsWithTheRequiredTypeName()
	{
		CSharpCompilation compilation = CSharpCompilation.Create("MissingRuntimeConsumer");

		InvalidOperationException result = (await Assert.That(() => new QuantityTypeReader(compilation)).ThrowsExactly<InvalidOperationException>())!;

		await Assert.That(result.Message).Contains(typeof(Quantity<>).FullName!);
	}

	[Test]
	public async Task ReaderDecodesEveryAxisAndRationalMarker()
	{
		const string source = """
			using DimensionalQuantities.TypeSystem;
			public static class Consumer
			{
				public static Quantity<Dimension<Hex<TerminalHex3, TerminalHex1>, Negative<TerminalHex2>,
					Fraction<TerminalHex1, TerminalHex2>, TerminalHex4, TerminalHex5, TerminalHex6, TerminalHex7>> Value;
			}
			""";
		CSharpCompilation compilation = Compile(source);
		IFieldSymbol field = await GetField(compilation, "Value");
		QuantityTypeReader reader = new(compilation);
		bool read = reader.TryRead(field.Type, out Dimension result, TestContext.Current!.Execution.CancellationToken);
		Dimension expected = new(19, -2, new Rational(1, 2), 4, 5, 6, 7);

		using (Assert.Multiple())
		{
			await Assert.That(read).IsTrue();
			await Assert.That(result).IsEqualTo(expected);
		}
	}

	[Test]
	public async Task ReaderRejectsUnrelatedAndUnresolvedTypes()
	{
		const string source = """
			namespace Other { public class Quantity<T> { } }
			public static class Consumer
			{
				public static Other.Quantity<int> Unrelated;
				public static string Plain;
				public static Missing Unresolved;
			}
			""";
		CSharpCompilation compilation = Compile(source);
		QuantityTypeReader reader = new(compilation);
		bool unrelated = reader.TryRead((await GetField(compilation, "Unrelated")).Type, out _, TestContext.Current!.Execution.CancellationToken);
		bool plain = reader.TryRead((await GetField(compilation, "Plain")).Type, out _, TestContext.Current!.Execution.CancellationToken);
		bool unresolved = reader.TryRead((await GetField(compilation, "Unresolved")).Type, out _, TestContext.Current!.Execution.CancellationToken);

		using (Assert.Multiple())
		{
			await Assert.That(unrelated).IsFalse();
			await Assert.That(plain).IsFalse();
			await Assert.That(unresolved).IsFalse();
		}
	}

	private static async Task<IFieldSymbol> GetField(CSharpCompilation compilation, string name) =>
		(await Assert.That(await Assert.That(compilation.GetTypeByMetadataName("Consumer")!.GetMembers(name)).HasSingleItem()).IsAssignableTo<IFieldSymbol>())!;

	private static CSharpCompilation Compile(string source)
	{
		string[] assemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = assemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		CSharpCompilation compilation = CSharpCompilation.Create("DiscoveryConsumer",
			[CSharpSyntaxTree.ParseText(source, _parseOptions, path: "Consumer.cs")], references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		return compilation;
	}
}
