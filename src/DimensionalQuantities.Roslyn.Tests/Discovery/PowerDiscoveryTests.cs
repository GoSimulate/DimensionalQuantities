using DimensionalQuantities.Numerics;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DimensionalQuantities.Roslyn.Tests.Discovery;

[NotInParallel(nameof(PowerDiscoveryTests))]
public sealed class PowerDiscoveryTests
{
	[Test]
	public async Task MissingRuntimeReferenceThrowsWithTheRequiredTypeName()
	{
		SyntaxTree tree = CSharpSyntaxTree.ParseText("class Consumer { }", cancellationToken: TestContext.Current!.Execution.CancellationToken);
		CSharpCompilation compilation = CSharpCompilation.Create("MissingRuntimeConsumer", [tree]);
		SemanticModel model = compilation.GetSemanticModel(tree);

		InvalidOperationException result = (await Assert.That(() => new PowerDiscovery(model)).ThrowsExactly<InvalidOperationException>())!;

		await Assert.That(result.Message).Contains(typeof(Power<>).FullName!);
	}

	[Test]
	[Arguments("Power<Fraction<TerminalHex2, TerminalHex4>>", "", "global::DimensionalQuantities.TypeSystem.Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex2, global::DimensionalQuantities.TypeSystem.TerminalHex4>")]
	[Arguments("Power<Half>", "using Half = DimensionalQuantities.TypeSystem.Fraction<DimensionalQuantities.TypeSystem.TerminalHex2, DimensionalQuantities.TypeSystem.TerminalHex4>;", "global::DimensionalQuantities.TypeSystem.Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex2, global::DimensionalQuantities.TypeSystem.TerminalHex4>")]
	[Arguments("Root", "using Root = DimensionalQuantities.TypeSystem.Power<DimensionalQuantities.TypeSystem.Fraction<DimensionalQuantities.TypeSystem.TerminalHex2, DimensionalQuantities.TypeSystem.TerminalHex4>>;", "global::DimensionalQuantities.TypeSystem.Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex2, global::DimensionalQuantities.TypeSystem.TerminalHex4>")]
	public async Task ReadsTheExponentWithoutChangingItsDeclaredType(string powerTypeName, string alias, string expectedExponentTypeName)
	{
		string source = $$"""
			using DimensionalQuantities.TypeSystem;
			{{alias}}
			public class Consumer { public object Value(object quantity) => {{powerTypeName}}.Of(quantity); }
			""";
		CSharpCompilation compilation = Compile(source);
		SyntaxTree tree = compilation.SyntaxTrees.Single();
		InvocationExpressionSyntax invocation = tree.GetRoot(TestContext.Current!.Execution.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
		PowerDiscovery discovery = new(compilation.GetSemanticModel(tree));
		bool read = discovery.TryRead(invocation, out Rational result, out string exponentTypeName, TestContext.Current!.Execution.CancellationToken);
		Rational expected = new(1, 2);

		using (Assert.Multiple())
		{
			await Assert.That(read).IsTrue();
			await Assert.That(result).IsEqualTo(expected);
			await Assert.That(exponentTypeName).IsEqualTo(expectedExponentTypeName);
		}
	}

	[Test]
	[Arguments("NumberTwo", 2, 1, "global::DimensionalQuantities.TypeSystem.TerminalHex2")]
	[Arguments("NumberHalf", 1, 2, "global::DimensionalQuantities.TypeSystem.Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex2>")]
	[Arguments("NumberTwoQuarters", 1, 2, "global::DimensionalQuantities.TypeSystem.Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex2>")]
	[Arguments("NumberMinusTwo", -2, 1, "global::DimensionalQuantities.TypeSystem.Negative<global::DimensionalQuantities.TypeSystem.TerminalHex2>")]
	public async Task ReadsUnresolvedEnglishExponentNames(string name, int numerator, int denominator, string expectedTypeName)
	{
		string source = $$"""
			using DimensionalQuantities.TypeSystem;
			public class Consumer { public object Value(object quantity) => Power<{{name}}>.Of(quantity); }
			""";
		CSharpCompilation compilation = Compile(source);
		SyntaxTree tree = compilation.SyntaxTrees.Single();
		InvocationExpressionSyntax invocation = tree.GetRoot(TestContext.Current!.Execution.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
		PowerDiscovery discovery = new(compilation.GetSemanticModel(tree));

		bool read = discovery.TryRead(invocation, out Rational result, out string typeName, TestContext.Current!.Execution.CancellationToken);
		Rational expected = new(numerator, denominator);

		using (Assert.Multiple())
		{
			await Assert.That(read).IsTrue();
			await Assert.That(result).IsEqualTo(expected);
			await Assert.That(typeName).IsEqualTo(expectedTypeName);
		}
	}

	[Test]
	[Arguments("Other.Power<TerminalHex2>.Of(quantity)")]
	[Arguments("Power<NumberNotANumber>.Of(quantity)")]
	[Arguments("Power<NumberOneOverZero>.Of(quantity)")]
	[Arguments("Power<NumberThree>.Of(quantity)")]
	[Arguments("Power<Other.NumberTwo>.Of(quantity)")]
	[Arguments("Power<TerminalHex2>.Other(quantity)")]
	[Arguments("Power<TerminalHex2>.Of(quantity, quantity)")]
	[Arguments("Power<TerminalHex2>.Of()")]
	public async Task RejectsUnrelatedAndUnsupportedCalls(string expression)
	{
		string source = $$"""
			using DimensionalQuantities.TypeSystem;
			public struct NumberThree { }
			namespace Other { public struct Power<T> { public static object Of(object value) => value; } }
			public class Consumer { public object Value(object quantity) => {{expression}}; }
			""";
		CSharpCompilation compilation = Compile(source);
		SyntaxTree tree = compilation.SyntaxTrees.Single();
		InvocationExpressionSyntax invocation = tree.GetRoot(TestContext.Current!.Execution.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
		PowerDiscovery discovery = new(compilation.GetSemanticModel(tree));
		bool result = discovery.TryRead(invocation, out _, out _, TestContext.Current!.Execution.CancellationToken);

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
