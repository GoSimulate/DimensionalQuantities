using DimensionalQuantities.Numerics;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Discovery;

public sealed class EnglishExponentDiscoveryTests
{
	[Theory]
	[InlineData("NumberTwo", 2, 1)]
	[InlineData("NumberHalf", 1, 2)]
	[InlineData("NumberTwoQuarters", 1, 2)]
	[InlineData("NumberMinusThreeOverTwo", -3, 2)]
	[InlineData("NumberMillionth", 1, 1000000)]
	public void DiscoversOnlyTheRequestedEnglishName(string name, int numerator, int denominator)
	{
		string source = $$"""
			using DimensionalQuantities.TypeSystem;
			class Consumer { object Value(object quantity) => Power<{{name}}>.Of(quantity); }
			""";
		CSharpCompilation compilation = Compile(source);
		SyntaxTree tree = compilation.SyntaxTrees.Single();
		InvocationExpressionSyntax invocation = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
		EnglishExponentDiscovery discovery = new(compilation.GetSemanticModel(tree));

		bool success = discovery.TryRead(invocation, out EnglishExponentAlias result, TestContext.Current.CancellationToken);
		Rational expected = new(numerator, denominator);

		Assert.Multiple(
			() => Assert.True(success),
			() => Assert.Equal(name, result.Name),
			() => Assert.Equal(expected, result.Value));
	}

	[Theory]
	[InlineData("Power<Two>.Of(quantity)", "")]
	[InlineData("Power<Number>.Of(quantity)", "")]
	[InlineData("Power<NumbersTwo>.Of(quantity)", "")]
	[InlineData("Power<NumberNotANumber>.Of(quantity)", "")]
	[InlineData("Power<NumberOneOverZero>.Of(quantity)", "")]
	[InlineData("Power<Other.NumberTwo>.Of(quantity)", "")]
	[InlineData("Power<NumberTwo<int>>.Of(quantity)", "")]
	[InlineData("Power<NumberTwo>.Of(quantity)", "struct NumberTwo { }")]
	[InlineData("Power<NumberTwo>.Of(quantity)", "using NumberTwo = DimensionalQuantities.TypeSystem.TerminalHex3;")]
	[InlineData("Power<TerminalHex2>.Of(quantity)", "")]
	[InlineData("Other.Power<NumberTwo>.Of(quantity)", "namespace Other { struct Power<T> { } }")]
	[InlineData("Power<NumberTwo>.Other(quantity)", "")]
	[InlineData("Power<NumberTwo>.Of(quantity, quantity)", "")]
	public void IgnoresNamesThatShouldNotBecomeGeneratedAliases(string expression, string declarations)
	{
		string source = $$"""
			using DimensionalQuantities.TypeSystem;
			{{declarations}}
			class Consumer { object Value(object quantity) => {{expression}}; }
			""";
		CSharpCompilation compilation = Compile(source);
		SyntaxTree tree = compilation.SyntaxTrees.Single();
		InvocationExpressionSyntax invocation = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
		EnglishExponentDiscovery discovery = new(compilation.GetSemanticModel(tree));

		bool result = discovery.TryRead(invocation, out _, TestContext.Current.CancellationToken);

		Assert.False(result);
	}

	[Fact]
	public void DoesNotReplaceAnAmbiguousExistingType()
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
		InvocationExpressionSyntax invocation = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
		EnglishExponentDiscovery discovery = new(compilation.GetSemanticModel(tree));

		bool result = discovery.TryRead(invocation, out _, TestContext.Current.CancellationToken);

		Assert.False(result);
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
