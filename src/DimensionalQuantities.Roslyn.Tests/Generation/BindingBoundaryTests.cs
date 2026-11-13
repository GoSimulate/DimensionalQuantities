using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generation;

public sealed class BindingBoundaryTests
{
	[Theory]
	[InlineData("void M(LengthCurrent input) { LengthCurrent explicitLocal = input; var result = explicitLocal * Time.FromSiBaseUnit(1); }")]
	[InlineData("void M(LengthCurrent input) { var inferred = input; var result = inferred * Time.FromSiBaseUnit(1); }")]
	[InlineData("void M(LengthCurrent a, LengthCurrent b, bool choose) { var value = choose ? a : b; var result = value * Time.FromSiBaseUnit(1); }")]
	[InlineData("void M(LengthCurrent a, LengthCurrent b, int choose) { var value = choose switch { 0 => a, _ => b }; var result = value * Time.FromSiBaseUnit(1); }")]
	[InlineData("LengthCurrent field; void M() { var result = field * Time.FromSiBaseUnit(1); }")]
	[InlineData("LengthCurrent Property { get; set; } void M() { var result = Property * Time.FromSiBaseUnit(1); }")]
	[InlineData("LengthCurrent Get() => default; void M() { var result = Get() * Time.FromSiBaseUnit(1); }")]
	[InlineData("void M(LengthCurrent parameter) { var result = parameter * Time.FromSiBaseUnit(1); }")]
	[InlineData("void M(LengthCurrent[] values) { var result = values[0] * Time.FromSiBaseUnit(1); }")]
	[InlineData("void M(System.Collections.Generic.List<LengthCurrent> values) { var result = values[0] * Time.FromSiBaseUnit(1); }")]
	[InlineData("void M(System.Span<LengthCurrent> values) { var result = values[0] * Time.FromSiBaseUnit(1); }")]
	[InlineData("LengthCurrent this[int index] => default; void M() { var result = this[0] * Time.FromSiBaseUnit(1); }")]
	[InlineData("T Identity<T>(T value) => value; void M(LengthCurrent input) { var result = Identity(input) * Time.FromSiBaseUnit(1); }")]
	[InlineData("interface IValue<T> { T Get(); } void M(IValue<LengthCurrent> value) { var result = value.Get() * Time.FromSiBaseUnit(1); }")]
	[InlineData("interface IValue { LengthCurrent Get(); } void M<T>(T value) where T : IValue { var result = value.Get() * Time.FromSiBaseUnit(1); }")]
	[InlineData("void Get(out LengthCurrent value) { value = default; } void M() { Get(out var value); var result = value * Time.FromSiBaseUnit(1); }")]
	[InlineData("ref LengthCurrent Get(ref LengthCurrent value) => ref value; void M(LengthCurrent value) { var result = Get(ref value) * Time.FromSiBaseUnit(1); }")]
	[InlineData("void M(object input) { if (input is LengthCurrent value) { var result = value * Time.FromSiBaseUnit(1); } }")]
	[InlineData("void M((LengthCurrent, int) input) { var (value, other) = input; var result = value * Time.FromSiBaseUnit(1); }")]
	[InlineData("async System.Threading.Tasks.Task M(System.Threading.Tasks.Task<LengthCurrent> task) { var value = await task; var result = value * Time.FromSiBaseUnit(1); }")]
	[InlineData("System.Collections.Generic.IEnumerable<LengthCurrent> Values() { yield return default; } void M() { foreach (var value in Values()) { var result = value * Time.FromSiBaseUnit(1); } }")]
	[InlineData("async System.Threading.Tasks.Task M(System.Collections.Generic.IAsyncEnumerable<LengthCurrent> values) { await foreach (var value in values) { var result = value * Time.FromSiBaseUnit(1); } }")]
	public async Task QuantitiesFlowThroughCSharpBindingBoundaries(string members)
	{
		QuantityGeneratorTest test = new() { TestCode = $"class Consumer {{ {members} }}" };
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = test.Compilation.SyntaxTrees.First();
		VariableDeclaratorSyntax result = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<VariableDeclaratorSyntax>().Single(variable => variable.Identifier.ValueText == "result");
		ILocalSymbol symbol = Assert.IsAssignableFrom<ILocalSymbol>(test.Compilation.GetSemanticModel(tree).GetDeclaredSymbol(result, TestContext.Current.CancellationToken));
		Assert.Equal("DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>>", symbol.Type.ToDisplayString());
	}

	[Fact]
	public async Task HelperReturnsAcrossFilesSeedFurtherArithmetic()
	{
		QuantityGeneratorTest test = new();
		test.TestState.Sources.Add(("Producer.cs", "static class Producer { public static LengthCurrent Get() => default; }"));
		test.TestState.Sources.Add(("Consumer.cs", "class Consumer { LengthTimeCurrent M() => Producer.Get()*Time.FromSiBaseUnit(1); }"));
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = test.Compilation.SyntaxTrees.Single(tree => tree.FilePath.EndsWith("Consumer.cs"));
		BinaryExpressionSyntax binary = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<BinaryExpressionSyntax>().Single();
		Assert.Multiple(
			() => Assert.Contains("global using LengthTimeCurrent =", string.Join("\n", test.GeneratedSources)),
			() => Assert.NotNull(test.Compilation.GetSemanticModel(tree).GetSymbolInfo(binary, TestContext.Current.CancellationToken).Symbol));
	}

	[Fact]
	public async Task MetadataReturnSeedsLocalHelpersAndArithmetic()
	{
		QuantityGeneratorTest producer = new() { TestCode = "public static class Producer { public static LengthCurrent Get() => default; }" };
		await producer.RunAsync(TestContext.Current.CancellationToken);
		using MemoryStream assembly = new();
		EmitResult emit = producer.Compilation.WithAssemblyName("QuantityProducer").Emit(assembly, cancellationToken: TestContext.Current.CancellationToken);
		if (!emit.Success)
		{
			throw new InvalidOperationException(string.Join("\n", emit.Diagnostics));
		}

		QuantityGeneratorTest consumer = new() { TestCode = "class Consumer { LengthSquaredCurrentSquared M() => Producer.Get().Squared(); }" };
		consumer.TestState.AdditionalReferences.Add(MetadataReference.CreateFromImage(assembly.ToArray()));
		await consumer.RunAsync(TestContext.Current.CancellationToken);
		IMethodSymbol producerMethod = consumer.Compilation.GetTypeByMetadataName("Producer")!.GetMembers("Get").OfType<IMethodSymbol>().Single();
		Assert.Multiple(
			() => Assert.Equal("QuantityProducer", producerMethod.ContainingAssembly.Name),
			() => Assert.Contains("global using LengthCurrent =", string.Join("\n", consumer.GeneratedSources)),
			() => Assert.Contains("public LengthSquaredCurrentSquared Squared()", string.Join("\n", consumer.GeneratedSources)));
	}
}
