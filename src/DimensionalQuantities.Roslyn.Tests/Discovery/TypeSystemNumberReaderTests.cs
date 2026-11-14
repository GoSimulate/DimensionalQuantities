using DimensionalQuantities.Numerics;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DimensionalQuantities.Roslyn.Tests.Discovery;

[NotInParallel(nameof(TypeSystemNumberReaderTests))]
public sealed class TypeSystemNumberReaderTests
{
	[Test]
	public async Task MissingRuntimeReferenceThrowsWithTheRequiredTypeName()
	{
		CSharpCompilation compilation = CSharpCompilation.Create("MissingRuntimeConsumer");

		InvalidOperationException result = (await Assert.That(() => new TypeSystemNumberReader(compilation)).ThrowsExactly<InvalidOperationException>())!;

		await Assert.That(result.Message).Contains(typeof(Hex<,>).FullName!);
	}

	[Test]
	[Arguments("TerminalHex0", 0, 1)]
	[Arguments("TerminalHexF", 15, 1)]
	[Arguments("Hex<TerminalHex0, TerminalHex1>", 16, 1)]
	[Arguments("Negative<TerminalHex3>", -3, 1)]
	[Arguments("Fraction<TerminalHex2, TerminalHex4>", 1, 2)]
	[Arguments("Fraction<Negative<TerminalHex3>, TerminalHex2>", -3, 2)]
	public async Task ReadsNumbersFromTheirTypes(string encoding, int numerator, int denominator)
	{
		CSharpCompilation compilation = Compile($"using DimensionalQuantities.TypeSystem; public class Consumer {{ public {encoding} Value; }}");
		IFieldSymbol field = (IFieldSymbol)compilation.GetTypeByMetadataName("Consumer")!.GetMembers("Value").Single();
		TypeSystemNumberReader reader = new(compilation);
		bool read = reader.TryRead(field.Type, out Rational result, TestContext.Current!.Execution.CancellationToken);
		Rational expected = new(numerator, denominator);

		using (Assert.Multiple())
		{
			await Assert.That(read).IsTrue();
			await Assert.That(result).IsEqualTo(expected);
		}
	}

	[Test]
	[Arguments("int")]
	[Arguments("Missing")]
	[Arguments("Fraction<TerminalHex1, TerminalHex0>")]
	[Arguments("Custom")]
	public async Task RejectsUnrecognisedNumbersAndZeroDenominators(string encoding)
	{
		CSharpCompilation compilation = Compile($"using DimensionalQuantities.TypeSystem; public struct Custom : ITypeSystemNumber {{ }} public class Consumer {{ public {encoding} Value; }}");
		IFieldSymbol field = (IFieldSymbol)compilation.GetTypeByMetadataName("Consumer")!.GetMembers("Value").Single();
		TypeSystemNumberReader reader = new(compilation);
		bool result = reader.TryRead(field.Type, out _, TestContext.Current!.Execution.CancellationToken);

		await Assert.That(result).IsFalse();
	}

	private static CSharpCompilation Compile(string source)
	{
		string[] assemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = assemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		return CSharpCompilation.Create("PowerDiscoveryConsumer", [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))],
			references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
	}
}
