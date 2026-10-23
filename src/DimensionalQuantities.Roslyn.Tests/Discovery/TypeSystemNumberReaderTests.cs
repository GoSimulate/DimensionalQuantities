using DimensionalQuantities.Numerics;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Discovery;

public sealed class TypeSystemNumberReaderTests
{
	[Fact]
	public void MissingRuntimeReferenceThrowsWithTheRequiredTypeName()
	{
		CSharpCompilation compilation = CSharpCompilation.Create("MissingRuntimeConsumer");

		InvalidOperationException result = Assert.Throws<InvalidOperationException>(() => new TypeSystemNumberReader(compilation));

		Assert.Contains(typeof(Hex<,>).FullName!, result.Message);
	}

	[Theory]
	[InlineData("TerminalHex0", 0, 1)]
	[InlineData("TerminalHexF", 15, 1)]
	[InlineData("Hex<TerminalHex0, TerminalHex1>", 16, 1)]
	[InlineData("Negative<TerminalHex3>", -3, 1)]
	[InlineData("Fraction<TerminalHex2, TerminalHex4>", 1, 2)]
	[InlineData("Fraction<Negative<TerminalHex3>, TerminalHex2>", -3, 2)]
	public void ReadsNumbersFromTheirTypes(string encoding, int numerator, int denominator)
	{
		CSharpCompilation compilation = Compile($"using DimensionalQuantities.TypeSystem; public class Consumer {{ public {encoding} Value; }}");
		IFieldSymbol field = (IFieldSymbol)compilation.GetTypeByMetadataName("Consumer")!.GetMembers("Value").Single();
		TypeSystemNumberReader reader = new(compilation);
		bool read = reader.TryRead(field.Type, out Rational result, TestContext.Current.CancellationToken);
		Rational expected = new(numerator, denominator);

		Assert.Multiple(
			() => Assert.True(read),
			() => Assert.Equal(expected, result));
	}

	[Theory]
	[InlineData("int")]
	[InlineData("Missing")]
	[InlineData("Fraction<TerminalHex1, TerminalHex0>")]
	[InlineData("Custom")]
	public void RejectsUnrecognisedNumbersAndZeroDenominators(string encoding)
	{
		CSharpCompilation compilation = Compile($"using DimensionalQuantities.TypeSystem; public struct Custom : ITypeSystemNumber {{ }} public class Consumer {{ public {encoding} Value; }}");
		IFieldSymbol field = (IFieldSymbol)compilation.GetTypeByMetadataName("Consumer")!.GetMembers("Value").Single();
		TypeSystemNumberReader reader = new(compilation);
		bool result = reader.TryRead(field.Type, out _, TestContext.Current.CancellationToken);

		Assert.False(result);
	}

	private static CSharpCompilation Compile(string source)
	{
		string[] assemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = assemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		return CSharpCompilation.Create("PowerDiscoveryConsumer", [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp14))],
			references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
	}
}
