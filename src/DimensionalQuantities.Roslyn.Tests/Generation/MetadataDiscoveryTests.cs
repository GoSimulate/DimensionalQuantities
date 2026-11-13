using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generation;

public sealed class MetadataDiscoveryTests
{
	[Theory]
	[InlineData("public static LengthCurrent Value;", "Producer.Value")]
	[InlineData("public static LengthCurrent Value => default;", "Producer.Value")]
	[InlineData("public static LengthCurrent[] Values() => null;", "Producer.Values()[0]")]
	[InlineData("public sealed class Holder { public LengthCurrent Value; } public static Holder Get() => null;", "Producer.Get().Value")]
	public async Task ReachableMetadataValuesSeedLocalArithmetic(string members, string expression)
	{
		QuantityGeneratorTest producer = new() { TestCode = $"public static class Producer {{ {members} }} public class Unused {{ public LengthMassCurrent Value; }}" };
		await producer.RunAsync(TestContext.Current.CancellationToken);
		using MemoryStream assembly = new();
		EmitResult emitted = producer.Compilation.WithAssemblyName("MetadataValues").Emit(assembly, cancellationToken: TestContext.Current.CancellationToken);
		if (!emitted.Success)
		{
			throw new InvalidOperationException(string.Join("\n", emitted.Diagnostics));
		}

		QuantityGeneratorTest consumer = new() { TestCode = $"class Consumer {{ LengthTimeCurrent M() => {expression}*Time.FromSiBaseUnit(1); }}" };
		consumer.TestState.AdditionalReferences.Add(MetadataReference.CreateFromImage(assembly.ToArray()));
		await consumer.RunAsync(TestContext.Current.CancellationToken);
		string source = string.Join("\n", consumer.GeneratedSources);
		SyntaxTree tree = consumer.Compilation.SyntaxTrees.First();
		BinaryExpressionSyntax binary = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<BinaryExpressionSyntax>().Single();
		IMethodSymbol selected = Assert.IsAssignableFrom<IMethodSymbol>(consumer.Compilation.GetSemanticModel(tree).GetSymbolInfo(binary, TestContext.Current.CancellationToken).Symbol);
		ITypeSymbol expected = consumer.Compilation.GetTypeByMetadataName("Consumer")!.GetMembers("M").OfType<IMethodSymbol>().Single().ReturnType;
		Assert.Multiple(() => Assert.True(SymbolEqualityComparer.Default.Equals(expected, selected.ReturnType)),
			() => Assert.Contains("global using LengthCurrent =", source),
			() => Assert.DoesNotContain("global using LengthMassCurrent =", source));
	}

	[Fact]
	public async Task IndependentProducersShareCanonicalRuntimeIdentity()
	{
		QuantityGeneratorTest first = new() { TestCode = "public static class First { public static LengthCurrent Get() => default; }" };
		await first.RunAsync(TestContext.Current.CancellationToken);
		using MemoryStream firstAssembly = new();
		EmitResult firstEmit = first.Compilation.WithAssemblyName("FirstProducer").Emit(firstAssembly, cancellationToken: TestContext.Current.CancellationToken);
		if (!firstEmit.Success)
		{
			throw new InvalidOperationException(string.Join("\n", firstEmit.Diagnostics));
		}

		QuantityGeneratorTest second = new() { TestCode = "public static class Second { public static LengthCurrent Echo(LengthCurrent value) => value; }" };
		await second.RunAsync(TestContext.Current.CancellationToken);
		using MemoryStream secondAssembly = new();
		EmitResult secondEmit = second.Compilation.WithAssemblyName("SecondProducer").Emit(secondAssembly, cancellationToken: TestContext.Current.CancellationToken);
		if (!secondEmit.Success)
		{
			throw new InvalidOperationException(string.Join("\n", secondEmit.Diagnostics));
		}

		QuantityGeneratorTest consumer = new() { TestCode = "class Consumer { LengthCurrent M() => Second.Echo(First.Get()); }" };
		consumer.TestState.AdditionalReferences.Add(MetadataReference.CreateFromImage(firstAssembly.ToArray()));
		consumer.TestState.AdditionalReferences.Add(MetadataReference.CreateFromImage(secondAssembly.ToArray()));
		await consumer.RunAsync(TestContext.Current.CancellationToken);
		ITypeSymbol firstReturn = consumer.Compilation.GetTypeByMetadataName("First")!.GetMembers("Get").OfType<IMethodSymbol>().Single().ReturnType;
		ITypeSymbol secondParameter = consumer.Compilation.GetTypeByMetadataName("Second")!.GetMembers("Echo").OfType<IMethodSymbol>().Single().Parameters[0].Type;
		Assert.True(SymbolEqualityComparer.Default.Equals(firstReturn, secondParameter));
	}
}
