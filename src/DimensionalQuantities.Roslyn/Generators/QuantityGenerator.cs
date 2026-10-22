using System.Text;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Units;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace DimensionalQuantities.Roslyn.Generators;

[Generator]
public sealed class QuantityGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(static output =>
		{
			IReadOnlyDictionary<Dimension, UnitDefinition[]> quantities = QuantityDiscovery.Discover();
			string source = QuantitySourceEmitter.Emit(quantities);
			output.AddSource("Quantities.g.cs", SourceText.From(source, Encoding.UTF8));
		});
	}
}
