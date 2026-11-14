using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.Roslyn.Generators;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Discovery;

public sealed class QuantityDiscoveryCoordinatorTests
{
	[Fact]
	public void EmptyInputNeedsNoDynamicSource()
	{
		CSharpCompilation compilation = CSharpCompilation.Create("EmptyConsumer");
		QuantityGenerationPlan defaults = DefaultQuantityDiscovery.Discover();

		QuantityDiscoveryResult result = QuantityDiscoveryCoordinator.Discover(compilation, [], defaults, TestContext.Current.CancellationToken);

		Assert.Empty(result.Sources);
	}

	[Fact]
	public void CancellationStopsDiscovery()
	{
		using CancellationTokenSource cancellation = new();
		cancellation.Cancel();
		QuantityGenerationPlan defaults = DefaultQuantityDiscovery.Discover();
		CSharpCompilation compilation = CSharpCompilation.Create("CancelledConsumer");

		Assert.Throws<OperationCanceledException>(() => QuantityDiscoveryCoordinator.Discover(compilation, [], defaults, cancellation.Token));
	}
}
