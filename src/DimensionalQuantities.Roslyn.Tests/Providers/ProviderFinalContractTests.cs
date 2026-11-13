using System.Reflection;
using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Providers;

public sealed class ProviderFinalContractTests
{
	[Fact]
	public async Task OmittedProviderOptionsDoNotPublishPrefixFamilies()
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = """
                using DimensionalQuantities.Units.Declarations;
                using DimensionalQuantities.Units.Symbols;
                namespace Custom;
                [UnitProvider] class Provider { [QuantityUnit(typeof(Length))] [UnitFactor(typeof(Metre))] class Distance {} }
                """
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		string source = string.Join("\n", test.GeneratedSources.Where(tree => tree.ToString().Contains("namespace Custom;")));
		Assert.Multiple(() => Assert.Contains("FromMetres(double value)", source),
			() => Assert.DoesNotContain("FromKiloMetres", source),
			() => Assert.DoesNotContain("FromPrefixedUnits", source));
	}

	[Fact]
	public async Task WrongDimensionDeltaCannotUseTemperatureAccessor()
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = "using DimensionalQuantities.Extensions.Metric; class Consumer { double M() => {|#0:Length.FromSiBaseUnit(1).AsDelta()|}.AsCelsius(); }"
		};
		test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("DimensionalQuantities.Extensions.Metric").Location));
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS1929").WithLocation(0));
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.DoesNotContain("AsCelsius", string.Join("\n", test.GeneratedSources));
	}

	[Fact]
	public async Task MetricVoltageAndCurrentProductBindsToPower()
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = "using DimensionalQuantities.Extensions.Metric; class Consumer { Power M() => Voltage.FromVolts(3)*Current.FromAmps(2); }"
		};
		test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("DimensionalQuantities.Extensions.Metric").Location));
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = test.Compilation.SyntaxTrees.First();
		BinaryExpressionSyntax binary = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<BinaryExpressionSyntax>().Single();
		IMethodSymbol selected = Assert.IsAssignableFrom<IMethodSymbol>(test.Compilation.GetSemanticModel(tree).GetSymbolInfo(binary, TestContext.Current.CancellationToken).Symbol);
		ITypeSymbol expected = test.Compilation.GetTypeByMetadataName("Consumer")!.GetMembers("M").OfType<IMethodSymbol>().Single().ReturnType;
		Assert.True(SymbolEqualityComparer.Default.Equals(expected, selected.ReturnType));
	}
}
