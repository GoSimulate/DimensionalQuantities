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
		CSharpCompilation compilation = ReaderCompilation.Create(source);
		IFieldSymbol field = GetField(compilation, "Value");
		QuantityTypeReader reader = new(compilation);
		bool read = reader.TryRead(field.Type, out Dimension result, TestContext.Current.CancellationToken);
		Dimension expected = new(19, -2, new Rational(1, 2), 4, 5, 6, 7);

		Assert.Multiple(
			() => Assert.True(read),
			() => Assert.Equal(expected, result));
	}

	[Theory]
	[InlineData("Other.Quantity<int>")]
	[InlineData("string")]
	[InlineData("Missing")]
	public void ReaderRejectsUnrelatedOrUnresolvedType(string typeName)
	{
		string source = $$"""
			namespace Other
			{
				public class Quantity<T>
				{
				}
			}
			public static class Consumer
			{
				public static {{typeName}} Value;
			}
			""";
		CSharpCompilation compilation = ReaderCompilation.Create(source, allowErrors: typeName == "Missing");
		QuantityTypeReader reader = new(compilation);

		bool result = reader.TryRead(GetField(compilation, "Value").Type, out _, TestContext.Current.CancellationToken);

		Assert.False(result);
	}

	private static IFieldSymbol GetField(CSharpCompilation compilation, string name) =>
		Assert.IsAssignableFrom<IFieldSymbol>(Assert.Single(compilation.GetTypeByMetadataName("Consumer")!.GetMembers(name)));

}
