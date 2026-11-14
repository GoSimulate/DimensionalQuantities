using System.Collections.Immutable;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Roslyn.Discovery;
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
				public Quantity<Dimension<TerminalHex1, TerminalHex0, TerminalHex0, TerminalHex1, TerminalHex0, TerminalHex0, TerminalHex0>> Value
				{
					get;
				}
			}
			""";
		CSharpCompilation compilation = ReaderCompilation.Create(source);
		SyntaxTree tree = compilation.SyntaxTrees.Single();
		ImmutableArray<Dimension> result = DiscoverySeedCollector.FindRequestedDimensions(compilation, [tree], TestContext.Current.CancellationToken);
		Dimension expected = new(Length: 1, ElectricCurrent: 1);

		Assert.Equal(expected, Assert.Single(result));
	}
}
