using Microsoft.CodeAnalysis;

namespace DimensionalQuantities.Roslyn.Generators;

[Generator]
public sealed class SmokeTestGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(static output => output.AddSource("GeneratedSmokeTest.g.cs", "namespace DimensionalQuantities.Generated; public sealed class GeneratedSmokeTest { }"));
	}
}
