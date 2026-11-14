using DimensionalQuantities.Numerics;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DimensionalQuantities.Roslyn.Tests.Discovery;

[NotInParallel(nameof(PowerMethodDiscoveryTests))]
public sealed class PowerMethodDiscoveryTests
{
	[Test]
	[Arguments("Squared", 2, 1)]
	[Arguments("Cubed", 3, 1)]
	[Arguments("Pow4", 4, 1)]
	[Arguments("Pow5", 5, 1)]
	[Arguments("Pow6", 6, 1)]
	[Arguments("SquareRoot", 1, 2)]
	[Arguments("CubeRoot", 1, 3)]
	[Arguments("Root4", 1, 4)]
	[Arguments("Root5", 1, 5)]
	[Arguments("Root6", 1, 6)]
	public async Task ReadsTheExponentForEachConvenienceMethod(string name, int numerator, int denominator)
	{
		string source = $$"""
			class Consumer { object Value(object quantity) => quantity.{{name}}(); }
			""";
		CSharpCompilation compilation = Compile(source);
		SyntaxTree tree = compilation.SyntaxTrees.Single();
		InvocationExpressionSyntax invocation = tree.GetRoot(TestContext.Current!.Execution.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
		PowerMethodDiscovery discovery = new(compilation.GetSemanticModel(tree));

		bool success = discovery.TryRead(invocation, out Rational result, out string methodName, TestContext.Current!.Execution.CancellationToken);
		Rational expected = new(numerator, denominator);

		using (Assert.Multiple())
		{
			await Assert.That(success).IsTrue();
			await Assert.That(result).IsEqualTo(expected);
			await Assert.That(methodName).IsEqualTo(name);
		}
	}

	[Test]
	[Arguments("quantity.Pow7()")]
	[Arguments("quantity.Root7()")]
	[Arguments("quantity.Squared(2)")]
	[Arguments("quantity.Squared<int>()")]
	[Arguments("quantity.squared()")]
	public async Task RejectsUnsupportedMethodCalls(string expression)
	{
		string source = $$"""class Consumer { object Value(object quantity) => {{expression}}; }""";
		CSharpCompilation compilation = Compile(source);
		SyntaxTree tree = compilation.SyntaxTrees.Single();
		InvocationExpressionSyntax invocation = tree.GetRoot(TestContext.Current!.Execution.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
		PowerMethodDiscovery discovery = new(compilation.GetSemanticModel(tree));

		bool result = discovery.TryRead(invocation, out _, out _, TestContext.Current!.Execution.CancellationToken);

		await Assert.That(result).IsFalse();
	}

	[Test]
	[Arguments("class Other { public int Squared() => 7; }", "quantity.Squared()")]
	[Arguments("class Other { public int Squared() => 7; }", "quantity?.Squared()")]
	[Arguments("class Other { } static class Extensions { public static int Squared(this Other value) => 7; }", "quantity.Squared()")]
	[Arguments("class Other { } static class Extensions { public static int Squared(this Other value) => 7; }", "quantity?.Squared()")]
	public async Task PreservesAlreadyBoundMethods(string declaration, string expression)
	{
		string source = $$"""{{declaration}} class Consumer { object Value(Other quantity) => {{expression}}; }""";
		CSharpCompilation compilation = Compile(source);
		SyntaxTree tree = compilation.SyntaxTrees.Single();
		InvocationExpressionSyntax invocation = tree.GetRoot(TestContext.Current!.Execution.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
		PowerMethodDiscovery discovery = new(compilation.GetSemanticModel(tree));

		bool result = discovery.TryRead(invocation, out _, out _, TestContext.Current!.Execution.CancellationToken);

		await Assert.That(result).IsFalse();
	}

	[Test]
	public async Task ReadsConditionalMemberBindingWithinAChain()
	{
		const string source = "class Consumer { object Value(object quantity) => quantity?.Squared().ToString(); }";
		CSharpCompilation compilation = Compile(source);
		SyntaxTree tree = compilation.SyntaxTrees.Single();
		InvocationExpressionSyntax invocation = tree.GetRoot(TestContext.Current!.Execution.CancellationToken).DescendantNodes()
			.OfType<InvocationExpressionSyntax>().Single(call => call.Expression is MemberBindingExpressionSyntax);
		PowerMethodDiscovery discovery = new(compilation.GetSemanticModel(tree));

		bool success = discovery.TryRead(invocation, out Rational exponent, out string methodName, TestContext.Current!.Execution.CancellationToken);

		using (Assert.Multiple())
		{
			await Assert.That(success).IsTrue();
			await Assert.That(exponent).IsEqualTo(new Rational(2));
			await Assert.That(methodName).IsEqualTo("Squared");
		}
	}

	[Test]
	public async Task AllowsAConvenienceMethodAlongsideAnOverloadWithRequiredArguments()
	{
		const string source = """
			class Other { }
			static class Extensions { public static int Squared(this Other value, int factor) => factor; }
			class Consumer { object Value(Other quantity) => quantity.Squared(); }
			""";
		CSharpCompilation compilation = Compile(source);
		SyntaxTree tree = compilation.SyntaxTrees.Single();
		InvocationExpressionSyntax invocation = tree.GetRoot(TestContext.Current!.Execution.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
		PowerMethodDiscovery discovery = new(compilation.GetSemanticModel(tree));

		bool success = discovery.TryRead(invocation, out Rational result, out string methodName, TestContext.Current!.Execution.CancellationToken);
		Rational expected = new(2);

		using (Assert.Multiple())
		{
			await Assert.That(success).IsTrue();
			await Assert.That(result).IsEqualTo(expected);
			await Assert.That(methodName).IsEqualTo("Squared");
		}
	}

	private static CSharpCompilation Compile(string source)
	{
		string[] assemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = assemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		return CSharpCompilation.Create("PowerMethodConsumer",
			[CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview), cancellationToken: TestContext.Current!.Execution.CancellationToken)],
			references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
	}
}
