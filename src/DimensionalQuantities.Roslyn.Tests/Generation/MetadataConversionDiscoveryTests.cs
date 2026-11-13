using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generation;

public sealed class MetadataConversionDiscoveryTests
{
	[Theory]
	[InlineData("public static LengthCurrent Get() => default;", "Library.Get()")]
	[InlineData("public static LengthCurrent Field;", "Library.Field")]
	[InlineData("public static LengthCurrent Property => default;", "Library.Property")]
	[InlineData("public static LengthCurrent[] Values() => null;", "Library.Values()[0]")]
	[InlineData("public sealed class Holder { public LengthCurrent Value; } public static Holder Get() => null;", "Library.Get().Value")]
	[InlineData("public static System.Collections.Generic.IEnumerable<LengthCurrent> Values() => null;", "Library.Values().First()")]
	public async Task ReferencedValueGetsLocalConversionsWithoutArithmeticOrTypeNameDemand(string members, string expression)
	{
		QuantityGeneratorTest producer = new() { TestCode = $"public static class Library {{ {members} }}" };
		await producer.RunAsync(TestContext.Current.CancellationToken);
		using MemoryStream assembly = new();
		EmitResult emitted = producer.Compilation.WithAssemblyName("MetadataConversionProducer").Emit(assembly, cancellationToken: TestContext.Current.CancellationToken);
		if (!emitted.Success)
		{
			throw new InvalidOperationException(string.Join("\n", emitted.Diagnostics));
		}

		QuantityGeneratorTest consumer = new() { TestCode = $"using System.Linq; class Consumer {{ double M() => {expression}.AsPrefixedUnits(3, 0); }}" };
		consumer.TestState.AdditionalReferences.Add(MetadataReference.CreateFromImage(assembly.ToArray()));
		await consumer.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = consumer.Compilation.SyntaxTrees.First();
		SemanticModel model = consumer.Compilation.GetSemanticModel(tree);
		InvocationExpressionSyntax call = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>()
			.Single(invocation => invocation.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "AsPrefixedUnits" });
		IMethodSymbol selected = Assert.IsAssignableFrom<IMethodSymbol>(model.GetSymbolInfo(call, TestContext.Current.CancellationToken).Symbol);
		MemberAccessExpressionSyntax access = (MemberAccessExpressionSyntax)call.Expression;
		INamedTypeSymbol receiver = Assert.IsAssignableFrom<INamedTypeSymbol>(model.GetTypeInfo(access.Expression, TestContext.Current.CancellationToken).Type);
		INamedTypeSymbol dimension = Assert.IsAssignableFrom<INamedTypeSymbol>(receiver.TypeArguments.Single());
		Assert.Multiple(
			() => Assert.True(SymbolEqualityComparer.Default.Equals(consumer.Compilation.Assembly, selected.ContainingAssembly)),
			() => Assert.Equal(SpecialType.System_Double, selected.ReturnType.SpecialType),
			() => Assert.Equal("TerminalHex1,TerminalHex0,TerminalHex0,TerminalHex1,TerminalHex0,TerminalHex0,TerminalHex0",
				string.Join(",", dimension.TypeArguments.Select(argument => argument.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)))),
			() => Assert.Equal(new[] { "metrePrefixExponent", "ampPrefixExponent" }, selected.Parameters.Select(parameter => parameter.Name)),
			() => Assert.Contains(consumer.GeneratedSources.SelectMany(source => source.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>()),
				alias => alias.Alias?.Name.Identifier.ValueText == "LengthCurrent"));
	}

	[Fact]
	public async Task BareNestedCollectionResultSeedsLocalConversions()
	{
		QuantityGeneratorTest producer = new() { TestCode = "public static class Library { public static System.Collections.Generic.List<System.Collections.Generic.List<LengthCurrent>> Values() => null; }" };
		await producer.RunAsync(TestContext.Current.CancellationToken);
		using MemoryStream assembly = new();
		EmitResult emitted = producer.Compilation.WithAssemblyName("NestedCollectionProducer").Emit(assembly, cancellationToken: TestContext.Current.CancellationToken);
		if (!emitted.Success)
		{
			throw new InvalidOperationException(string.Join("\n", emitted.Diagnostics));
		}

		QuantityGeneratorTest consumer = new() { TestCode = "class Consumer { void M() { var values = Library.Values(); } }" };
		consumer.TestState.AdditionalReferences.Add(MetadataReference.CreateFromImage(assembly.ToArray()));
		await consumer.RunAsync(TestContext.Current.CancellationToken);
		MethodDeclarationSyntax accessor = consumer.GeneratedSources.SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
			.Single(method => method.Identifier.ValueText == "AsPrefixedUnits" && method.Parent is ExtensionBlockDeclarationSyntax extension
				&& extension.ParameterList?.Parameters.Single().Type?.ToString() == "LengthCurrent");
		IMethodSymbol selected = (IMethodSymbol)consumer.Compilation.GetSemanticModel(accessor.SyntaxTree).GetDeclaredSymbol(accessor, TestContext.Current.CancellationToken)!;
		Assert.Multiple(
			() => Assert.Equal(SpecialType.System_Double, selected.ReturnType.SpecialType),
			() => Assert.Contains(consumer.GeneratedSources.SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>()), alias => alias.Alias?.Name.Identifier.ValueText == "LengthCurrent"));
	}
}
