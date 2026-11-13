using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generation;

public sealed class UnsupportedRequestTests
{
	[Fact]
	public async Task UnknownPowerMarkerDoesNotReceiveAnOverload()
	{
		QuantityGeneratorTest test = new() { TestCode = "using DimensionalQuantities.TypeSystem; struct Marker : ITypeSystemNumber {} class Consumer { object M(Length l) => {|#0:Power<Marker>|}.Of(l); }" };
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS1929").WithLocation(0));
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.DoesNotContain("extension(global::DimensionalQuantities.TypeSystem.Power<global::Marker>)", string.Join("\n", test.GeneratedSources));
	}

	[Theory]
	[InlineData("l.{|#0:Squarred|}()", "CS1061")]
	[InlineData("l.{|#0:Squared|}(1)", "CS1501")]
	public async Task InvalidHelperRequestsDoNotInventOverloads(string call, string diagnostic)
	{
		QuantityGeneratorTest test = new() { TestCode = $"class Consumer {{ object M(Length l) => {call}; }}" };
		test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError(diagnostic).WithLocation(0));
		await test.RunAsync(TestContext.Current.CancellationToken);
		string generated = string.Join("\n", test.GeneratedSources);
		Assert.Multiple(() => Assert.DoesNotContain("Squarred()", generated), () => Assert.DoesNotContain("Squared(int", generated));
	}

	[Fact]
	public async Task QualifiedConsumerNameRetainsItsBinding()
	{
		QuantityGeneratorTest test = new() { TestCode = "namespace Custom { class LengthCurrent {} } class Consumer { Custom.LengthCurrent field; }" };
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.DoesNotContain("global using LengthCurrent =", string.Join("\n", test.GeneratedSources));
	}
}
