using System.Reflection;
using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Providers;

public sealed class ProviderMetadataTests
{
	[Theory]
	[InlineData("Metric", "Voltage", "FromVolts")]
	[InlineData("Imperial", "Length", "FromFeet")]
	[InlineData("Nautical", "Speed", "FromKnots")]
	[InlineData("CgsEmag", "MagneticFluxDensity", "FromGauss")]
	public async Task OptionalConversionsBindPublicMetadataMethods(string package, string quantity, string factory)
	{
		string assemblyName = $"DimensionalQuantities.Extensions.{package}";
		QuantityGeneratorTest test = new() { TestCode = $"using {assemblyName}; class Consumer {{ {quantity} M() => {quantity}.{factory}(1); }}" };
		test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(Assembly.Load(assemblyName).Location));
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = test.Compilation.SyntaxTrees.First();
		InvocationExpressionSyntax call = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
		IMethodSymbol selected = Assert.IsAssignableFrom<IMethodSymbol>(test.Compilation.GetSemanticModel(tree).GetSymbolInfo(call, TestContext.Current.CancellationToken).Symbol);
		IMethodSymbol method = test.Compilation.GetTypeByMetadataName("Consumer")!.GetMembers("M").OfType<IMethodSymbol>().Single();
		Assert.Multiple(
			() => Assert.Equal(assemblyName, selected.ContainingAssembly.Name),
			() => Assert.Equal(Accessibility.Public, selected.DeclaredAccessibility),
			() => Assert.True(SymbolEqualityComparer.Default.Equals(method.ReturnType, selected.ReturnType)),
			() => Assert.DoesNotContain($"{factory}(double value)", string.Join("\n", test.GeneratedSources)));
	}

	[Theory]
	[InlineData("Voltage", "FromVolts")]
	[InlineData("Temperature", "FromCelsius")]
	[InlineData("Length", "FromFeet")]
	[InlineData("Speed", "FromKnots")]
	[InlineData("MagneticFluxDensity", "FromGauss")]
	public async Task CoreDoesNotSupplyOptionalFactories(string quantity, string factory)
	{
		QuantityGeneratorTest test = new() { TestCode = $"class Consumer {{ {quantity} M() => {quantity}.{{|#0:{factory}|}}(1); }}" };
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS0117").WithLocation(0));
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.DoesNotContain($"{factory}(double value)", string.Join("\n", test.GeneratedSources));
	}

	[Theory]
	[InlineData("Imperial", "Length", "FromFeet")]
	[InlineData("Nautical", "Speed", "FromKnots")]
	[InlineData("CgsEmag", "MagneticFluxDensity", "FromGauss")]
	[InlineData("Metric", "Voltage", "FromVolts")]
	public async Task OptionalNamespaceMustBeImported(string package, string quantity, string factory)
	{
		QuantityGeneratorTest test = new() { TestCode = $"class Consumer {{ {quantity} M() => {quantity}.{{|#0:{factory}|}}(1); }}" };
		test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(Assembly.Load($"DimensionalQuantities.Extensions.{package}").Location));
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS0117").WithLocation(0));
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.DoesNotContain($"{factory}(double value)", string.Join("\n", test.GeneratedSources));
	}

	[Theory]
	[InlineData("Imperial", "Length", "FromKiloFeet")]
	[InlineData("Nautical", "Speed", "FromKiloKnots")]
	[InlineData("CgsEmag", "MagneticFluxDensity", "FromKiloGauss")]
	[InlineData("Metric", "Temperature", "FromKiloCelsius")]
	public async Task UnsupportedOptionalPrefixFamiliesStayAbsent(string package, string quantity, string factory)
	{
		QuantityGeneratorTest test = new() { TestCode = $"using DimensionalQuantities.Extensions.{package}; class Consumer {{ {quantity} M() => {quantity}.{{|#0:{factory}|}}(1); }}" };
		test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(Assembly.Load($"DimensionalQuantities.Extensions.{package}").Location));
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS0117").WithLocation(0));
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.DoesNotContain($"{factory}(double value)", string.Join("\n", test.GeneratedSources));
	}

	[Fact]
	public async Task CompiledProviderPublishesMethodsWithoutRegeneration()
	{
		QuantityGeneratorTest producer = new() { TestCode = """
            using DimensionalQuantities.Units.Declarations;
            namespace Custom;
            [UnitSymbol("Widget", "Widgets", "w", Scale = 4)] class Widget {}
            [UnitProvider] static class Provider { [QuantityUnit(typeof(Length))] [UnitFactor(typeof(Widget))] class Distance {} }
            """ };
		await producer.RunAsync(TestContext.Current.CancellationToken);
		using MemoryStream assembly = new();
		EmitResult emitted = producer.Compilation.WithAssemblyName("CompiledProvider").Emit(assembly, cancellationToken: TestContext.Current.CancellationToken);
		if (!emitted.Success)
		{
			throw new InvalidOperationException(string.Join("\n", emitted.Diagnostics));
		}

		QuantityGeneratorTest consumer = new() { TestCode = "using Custom; class Consumer { Area M() { var length = Length.FromWidgets(1); return length*length; } }" };
		consumer.TestState.AdditionalReferences.Add(MetadataReference.CreateFromImage(assembly.ToArray()));
		await consumer.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = consumer.Compilation.SyntaxTrees.First();
		InvocationExpressionSyntax call = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
		IMethodSymbol selected = Assert.IsAssignableFrom<IMethodSymbol>(consumer.Compilation.GetSemanticModel(tree).GetSymbolInfo(call, TestContext.Current.CancellationToken).Symbol);
		Assert.Multiple(
			() => Assert.Equal("CompiledProvider", selected.ContainingAssembly.Name),
			() => Assert.Equal(Accessibility.Public, selected.DeclaredAccessibility),
			() => Assert.DoesNotContain("FromWidgets", string.Join("\n", consumer.GeneratedSources)));
	}
}
