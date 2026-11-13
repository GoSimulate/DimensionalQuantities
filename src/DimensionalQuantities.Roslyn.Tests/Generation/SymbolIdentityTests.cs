using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generation;

public sealed class SymbolIdentityTests
{
	[Theory]
	[InlineData("Custom.Dimension")]
	[InlineData("DimensionalQuantities.TypeSystem.Dimension<Custom.Number, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>")]
	public async Task UnreadableDimensionDoesNotAcquireGeneratedArithmetic(string dimension)
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
                using DimensionalQuantities.TypeSystem;
                namespace Custom { struct Dimension {} struct Number : ITypeSystemNumber {} }
                class Consumer { object M(Quantity<{{dimension}}> a, Quantity<{{dimension}}> b) => {|#0:a*b|}; }
                """
		};
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS0019").WithLocation(0));
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.DoesNotContain("Custom.", string.Join("\n", test.GeneratedSources));
	}

	[Fact]
	public async Task UnrelatedReceiverDoesNotAcquireQuantityHelper()
	{
		QuantityGeneratorTest test = new() { TestCode = "class Consumer { object M(string text) => {|#0:text|}.Squared(); }" };
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS1929").WithLocation(0));
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.DoesNotContain("extension(string", string.Join("\n", test.GeneratedSources));
	}
}
