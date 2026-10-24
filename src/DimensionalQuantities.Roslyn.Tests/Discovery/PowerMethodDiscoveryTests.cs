using DimensionalQuantities.Numerics;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Discovery;

public sealed class PowerMethodDiscoveryTests
{
	[Theory]
	[InlineData("Squared", 2, 1)]
	[InlineData("Cubed", 3, 1)]
	[InlineData("Pow4", 4, 1)]
	[InlineData("Pow5", 5, 1)]
	[InlineData("Pow6", 6, 1)]
	[InlineData("SquareRoot", 1, 2)]
	[InlineData("CubeRoot", 1, 3)]
	[InlineData("Root4", 1, 4)]
	[InlineData("Root5", 1, 5)]
	[InlineData("Root6", 1, 6)]
	public void ReadsTheExponentForEachConvenienceMethod(string name, int numerator, int denominator)
	{
		string source = $$"""
			class Consumer { object Value(object quantity) => quantity.{{name}}(); }
			""";
		CSharpCompilation compilation = Compile(source);
		SyntaxTree tree = compilation.SyntaxTrees.Single();
		InvocationExpressionSyntax invocation = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
		PowerMethodDiscovery discovery = new(compilation.GetSemanticModel(tree));

		bool success = discovery.TryRead(invocation, out Rational result, out string methodName, TestContext.Current.CancellationToken);
		Rational expected = new(numerator, denominator);

		Assert.Multiple(
			() => Assert.True(success),
			() => Assert.Equal(expected, result),
			() => Assert.Equal(name, methodName));
	}

	[Theory]
	[InlineData("quantity.Pow7()")]
	[InlineData("quantity.Root7()")]
	[InlineData("quantity.Squared(2)")]
	[InlineData("quantity.Squared<int>()")]
	[InlineData("quantity.squared()")]
	public void RejectsUnsupportedMethodCalls(string expression)
	{
		string source = $$"""class Consumer { object Value(object quantity) => {{expression}}; }""";
		CSharpCompilation compilation = Compile(source);
		SyntaxTree tree = compilation.SyntaxTrees.Single();
		InvocationExpressionSyntax invocation = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
		PowerMethodDiscovery discovery = new(compilation.GetSemanticModel(tree));

		bool result = discovery.TryRead(invocation, out _, out _, TestContext.Current.CancellationToken);

		Assert.False(result);
	}

	[Theory]
	[InlineData("class Other { public int Squared() => 7; }", "quantity.Squared()")]
	[InlineData("class Other { public int Squared() => 7; }", "quantity?.Squared()")]
	[InlineData("class Other { } static class Extensions { public static int Squared(this Other value) => 7; }", "quantity.Squared()")]
	[InlineData("class Other { } static class Extensions { public static int Squared(this Other value) => 7; }", "quantity?.Squared()")]
	public void PreservesAlreadyBoundMethods(string declaration, string expression)
	{
		string source = $$"""{{declaration}} class Consumer { object Value(Other quantity) => {{expression}}; }""";
		CSharpCompilation compilation = Compile(source);
		SyntaxTree tree = compilation.SyntaxTrees.Single();
		InvocationExpressionSyntax invocation = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
		PowerMethodDiscovery discovery = new(compilation.GetSemanticModel(tree));

		bool result = discovery.TryRead(invocation, out _, out _, TestContext.Current.CancellationToken);

		Assert.False(result);
	}

	[Fact]
	public void ReadsConditionalMemberBindingWithinAChain()
	{
		const string source = "class Consumer { object Value(object quantity) => quantity?.Squared().ToString(); }";
		CSharpCompilation compilation = Compile(source);
		SyntaxTree tree = compilation.SyntaxTrees.Single();
		InvocationExpressionSyntax invocation = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes()
			.OfType<InvocationExpressionSyntax>().Single(call => call.Expression is MemberBindingExpressionSyntax);
		PowerMethodDiscovery discovery = new(compilation.GetSemanticModel(tree));

		bool success = discovery.TryRead(invocation, out Rational exponent, out string methodName, TestContext.Current.CancellationToken);

		Assert.Multiple(
			() => Assert.True(success),
			() => Assert.Equal(new Rational(2), exponent),
			() => Assert.Equal("Squared", methodName));
	}

	[Fact]
	public void AllowsAConvenienceMethodAlongsideAnOverloadWithRequiredArguments()
	{
		const string source = """
			class Other { }
			static class Extensions { public static int Squared(this Other value, int factor) => factor; }
			class Consumer { object Value(Other quantity) => quantity.Squared(); }
			""";
		CSharpCompilation compilation = Compile(source);
		SyntaxTree tree = compilation.SyntaxTrees.Single();
		InvocationExpressionSyntax invocation = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
		PowerMethodDiscovery discovery = new(compilation.GetSemanticModel(tree));

		bool success = discovery.TryRead(invocation, out Rational result, out string methodName, TestContext.Current.CancellationToken);
		Rational expected = new(2);

		Assert.Multiple(
			() => Assert.True(success),
			() => Assert.Equal(expected, result),
			() => Assert.Equal("Squared", methodName));
	}

	private static CSharpCompilation Compile(string source)
	{
		string[] assemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = assemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		return CSharpCompilation.Create("PowerMethodConsumer",
			[CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview), cancellationToken: TestContext.Current.CancellationToken)],
			references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
	}
}
