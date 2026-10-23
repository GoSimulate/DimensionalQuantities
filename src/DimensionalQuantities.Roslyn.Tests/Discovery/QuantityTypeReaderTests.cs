using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Numerics;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Discovery;

public sealed class QuantityTypeReaderTests
{
	private static readonly CSharpParseOptions _parseOptions = new(LanguageVersion.CSharp14);

	[Fact]
	public void MissingRuntimeReferenceThrowsWithTheRequiredTypeName()
	{
		CSharpCompilation compilation = CSharpCompilation.Create("MissingRuntimeConsumer");

		InvalidOperationException result = Assert.Throws<InvalidOperationException>(() => new QuantityTypeReader(compilation));

		Assert.Contains(typeof(Quantity<>).FullName!, result.Message);
	}

	[Fact]
	public void ReaderDecodesEveryAxisAndRationalMarker()
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
		IFieldSymbol field = GetField(compilation, "Value");
		QuantityTypeReader reader = new(compilation);
		bool read = reader.TryRead(field.Type, out Dimension result, TestContext.Current.CancellationToken);
		Dimension expected = new(19, -2, new Rational(1, 2), 4, 5, 6, 7);

		Assert.Multiple(
			() => Assert.True(read),
			() => Assert.Equal(expected, result));
	}

	[Fact]
	public void ReaderRejectsUnrelatedAndUnresolvedTypes()
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
		bool unrelated = reader.TryRead(GetField(compilation, "Unrelated").Type, out _, TestContext.Current.CancellationToken);
		bool plain = reader.TryRead(GetField(compilation, "Plain").Type, out _, TestContext.Current.CancellationToken);
		bool unresolved = reader.TryRead(GetField(compilation, "Unresolved").Type, out _, TestContext.Current.CancellationToken);

		Assert.Multiple(
			() => Assert.False(unrelated),
			() => Assert.False(plain),
			() => Assert.False(unresolved));
	}

	private static IFieldSymbol GetField(CSharpCompilation compilation, string name) =>
		Assert.IsAssignableFrom<IFieldSymbol>(Assert.Single(compilation.GetTypeByMetadataName("Consumer")!.GetMembers(name)));

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
