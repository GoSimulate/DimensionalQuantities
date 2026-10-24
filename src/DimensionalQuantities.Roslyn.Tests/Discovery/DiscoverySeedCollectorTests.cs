using System.Collections.Immutable;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Discovery;

public sealed class DiscoverySeedCollectorTests
{
	[Fact]
	public void RawQuantityDeclarationRequestsItsDimension()
	{
		const string source = """
			using DimensionalQuantities.TypeSystem;
			public class Consumer
			{
				public Quantity<Dimension<TerminalHex1, TerminalHex0, TerminalHex0, TerminalHex1, TerminalHex0, TerminalHex0, TerminalHex0>> Value { get; }
			}
			""";
		SyntaxTree tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview), cancellationToken: TestContext.Current.CancellationToken);
		string[] assemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = assemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		CSharpCompilation compilation = CSharpCompilation.Create("Consumer", [tree], references, new(OutputKind.DynamicallyLinkedLibrary));
		ImmutableArray<Dimension> result = DiscoverySeedCollector.FindRequestedDimensions(compilation, [tree], TestContext.Current.CancellationToken);
		Dimension expected = new(Length: 1, ElectricCurrent: 1);

		Assert.Equal(expected, Assert.Single(result));
	}
}
