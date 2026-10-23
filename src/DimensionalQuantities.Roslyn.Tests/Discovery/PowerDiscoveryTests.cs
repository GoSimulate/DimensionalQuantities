using DimensionalQuantities.Numerics;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Discovery;

public sealed class PowerDiscoveryTests
{
	[Fact]
	public void MissingRuntimeReferenceThrowsWithTheRequiredTypeName()
	{
		SyntaxTree tree = CSharpSyntaxTree.ParseText("class Consumer { }", cancellationToken: TestContext.Current.CancellationToken);
		CSharpCompilation compilation = CSharpCompilation.Create("MissingRuntimeConsumer", [tree]);
		SemanticModel model = compilation.GetSemanticModel(tree);

		InvalidOperationException result = Assert.Throws<InvalidOperationException>(() => new PowerDiscovery(model));

		Assert.Contains(typeof(Power<>).FullName!, result.Message);
	}

	[Theory]
	[InlineData("Power<Fraction<TerminalHex2, TerminalHex4>>", "", "global::DimensionalQuantities.TypeSystem.Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex2, global::DimensionalQuantities.TypeSystem.TerminalHex4>")]
	[InlineData("Power<Half>", "using Half = DimensionalQuantities.TypeSystem.Fraction<DimensionalQuantities.TypeSystem.TerminalHex2, DimensionalQuantities.TypeSystem.TerminalHex4>;", "global::DimensionalQuantities.TypeSystem.Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex2, global::DimensionalQuantities.TypeSystem.TerminalHex4>")]
	[InlineData("Root", "using Root = DimensionalQuantities.TypeSystem.Power<DimensionalQuantities.TypeSystem.Fraction<DimensionalQuantities.TypeSystem.TerminalHex2, DimensionalQuantities.TypeSystem.TerminalHex4>>;", "global::DimensionalQuantities.TypeSystem.Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex2, global::DimensionalQuantities.TypeSystem.TerminalHex4>")]
	public void ReadsTheExponentWithoutChangingItsDeclaredType(string powerTypeName, string alias, string expectedExponentTypeName)
	{
		string source = $$"""
			using DimensionalQuantities.TypeSystem;
			{{alias}}
			public class Consumer { public object Value(object quantity) => {{powerTypeName}}.Of(quantity); }
			""";
		CSharpCompilation compilation = Compile(source);
		SyntaxTree tree = compilation.SyntaxTrees.Single();
		InvocationExpressionSyntax invocation = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
		PowerDiscovery discovery = new(compilation.GetSemanticModel(tree));
		bool read = discovery.TryRead(invocation, out Rational result, out string exponentTypeName, TestContext.Current.CancellationToken);
		Rational expected = new(1, 2);

		Assert.Multiple(
			() => Assert.True(read),
			() => Assert.Equal(expected, result),
			() => Assert.Equal(expectedExponentTypeName, exponentTypeName));
	}

	[Theory]
	[InlineData("Other.Power<TerminalHex2>.Of(quantity)")]
	[InlineData("Power<Seventeen>.Of(quantity)")]
	[InlineData("Power<TerminalHex2>.Other(quantity)")]
	[InlineData("Power<TerminalHex2>.Of(quantity, quantity)")]
	[InlineData("Power<TerminalHex2>.Of()")]
	public void RejectsUnrelatedAndUnsupportedCalls(string expression)
	{
		string source = $$"""
			using DimensionalQuantities.TypeSystem;
			namespace Other { public struct Power<T> { public static object Of(object value) => value; } }
			public class Consumer { public object Value(object quantity) => {{expression}}; }
			""";
		CSharpCompilation compilation = Compile(source);
		SyntaxTree tree = compilation.SyntaxTrees.Single();
		InvocationExpressionSyntax invocation = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
		PowerDiscovery discovery = new(compilation.GetSemanticModel(tree));
		bool result = discovery.TryRead(invocation, out _, out _, TestContext.Current.CancellationToken);

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
