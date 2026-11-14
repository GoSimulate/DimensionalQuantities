using DimensionalQuantities.Numerics;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DimensionalQuantities.Roslyn.Tests.Discovery;

[NotInParallel(nameof(EnglishExponentDiscoveryTests))]
public sealed class EnglishExponentDiscoveryTests
{
	[Test]
	[Arguments("NumberTwo", 2, 1)]
	[Arguments("NumberHalf", 1, 2)]
	[Arguments("NumberTwoQuarters", 1, 2)]
	[Arguments("NumberMinusThreeOverTwo", -3, 2)]
	[Arguments("NumberMillionth", 1, 1000000)]
	public async Task DiscoversOnlyTheRequestedEnglishName(string name, int numerator, int denominator)
	{
		string source = $$"""
			using DimensionalQuantities.TypeSystem;
			class Consumer { object Value(object quantity) => Power<{{name}}>.Of(quantity); }
			""";
		CSharpCompilation compilation = Compile(source);
		SyntaxTree tree = compilation.SyntaxTrees.Single();
		InvocationExpressionSyntax invocation = tree.GetRoot(TestContext.Current!.Execution.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
		EnglishExponentDiscovery discovery = new(compilation.GetSemanticModel(tree));

		bool success = discovery.TryRead(invocation, out EnglishExponentAlias result, TestContext.Current!.Execution.CancellationToken);
		Rational expected = new(numerator, denominator);

		using (Assert.Multiple())
		{
			await Assert.That(success).IsTrue();
			await Assert.That(result.Name).IsEqualTo(name);
			await Assert.That(result.Value).IsEqualTo(expected);
		}
	}

	[Test]
	[Arguments("Power<Two>.Of(quantity)", "")]
	[Arguments("Power<Number>.Of(quantity)", "")]
	[Arguments("Power<NumbersTwo>.Of(quantity)", "")]
	[Arguments("Power<NumberNotANumber>.Of(quantity)", "")]
	[Arguments("Power<NumberOneOverZero>.Of(quantity)", "")]
	[Arguments("Power<Other.NumberTwo>.Of(quantity)", "")]
	[Arguments("Power<NumberTwo<int>>.Of(quantity)", "")]
	[Arguments("Power<NumberTwo>.Of(quantity)", "struct NumberTwo { }")]
	[Arguments("Power<NumberTwo>.Of(quantity)", "using NumberTwo = DimensionalQuantities.TypeSystem.TerminalHex3;")]
	[Arguments("Power<TerminalHex2>.Of(quantity)", "")]
	[Arguments("Other.Power<NumberTwo>.Of(quantity)", "namespace Other { struct Power<T> { } }")]
	[Arguments("Power<NumberTwo>.Other(quantity)", "")]
	[Arguments("Power<NumberTwo>.Of(quantity, quantity)", "")]
	public async Task IgnoresNamesThatShouldNotBecomeGeneratedAliases(string expression, string declarations)
	{
		string source = $$"""
			using DimensionalQuantities.TypeSystem;
			{{declarations}}
			class Consumer { object Value(object quantity) => {{expression}}; }
			""";
		CSharpCompilation compilation = Compile(source);
		SyntaxTree tree = compilation.SyntaxTrees.Single();
		InvocationExpressionSyntax invocation = tree.GetRoot(TestContext.Current!.Execution.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
		EnglishExponentDiscovery discovery = new(compilation.GetSemanticModel(tree));

		bool result = discovery.TryRead(invocation, out _, TestContext.Current!.Execution.CancellationToken);

		await Assert.That(result).IsFalse();
	}

	[Test]
	public async Task DoesNotReplaceAnAmbiguousExistingType()
	{
		const string source = """
			using DimensionalQuantities.TypeSystem;
			using First;
			using Second;
			namespace First { public struct NumberTwo { } }
			namespace Second { public struct NumberTwo { } }
			class Consumer { object Value(object quantity) => Power<NumberTwo>.Of(quantity); }
			""";
		CSharpCompilation compilation = Compile(source);
		SyntaxTree tree = compilation.SyntaxTrees.Single();
		InvocationExpressionSyntax invocation = tree.GetRoot(TestContext.Current!.Execution.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
		EnglishExponentDiscovery discovery = new(compilation.GetSemanticModel(tree));

		bool result = discovery.TryRead(invocation, out _, TestContext.Current!.Execution.CancellationToken);

		await Assert.That(result).IsFalse();
	}

	private static CSharpCompilation Compile(string source)
	{
		string[] assemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = assemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		return CSharpCompilation.Create("EnglishExponentConsumer", [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))],
			references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
	}
}
