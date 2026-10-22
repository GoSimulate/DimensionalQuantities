using System.Collections.Immutable;
using System.Text;
using DimensionalQuantities.Roslyn.Discovery;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DimensionalQuantities.Roslyn.Generators;

[Generator]
public sealed class QuantityGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(static output =>
		{
			QuantityGenerationPlan plan = QuantityDiscovery.Discover([]);
			string source = QuantitySourceEmitter.Emit(plan.KnownQuantities);
			output.AddSource("Quantities.g.cs", SourceText.From(source, Encoding.UTF8));
		});

		IncrementalValuesProvider<ImmutableArray<ArithmeticOperation>> operations = context.SyntaxProvider.CreateSyntaxProvider(
			static (node, _) => node is BinaryExpressionSyntax binary && ArithmeticDiscovery.IsRoot(binary),
			static (syntaxContext, cancellationToken) => ArithmeticDiscovery.Analyse(
				(BinaryExpressionSyntax)syntaxContext.Node, syntaxContext.SemanticModel, cancellationToken));

		context.RegisterSourceOutput(operations.Collect(), static (output, discoveredExpressions) =>
		{
			ImmutableArray<ArithmeticOperation>.Builder discoveredOperations = ImmutableArray.CreateBuilder<ArithmeticOperation>();
			foreach (ImmutableArray<ArithmeticOperation> expressionOperations in discoveredExpressions)
			{
				output.CancellationToken.ThrowIfCancellationRequested();
				discoveredOperations.AddRange(expressionOperations);
			}

			if (discoveredOperations.Count == 0)
			{
				return;
			}

			QuantityGenerationPlan plan = QuantityDiscovery.Discover(discoveredOperations);
			if (!plan.AdditionalQuantities.IsEmpty)
			{
				string quantitySource = QuantitySourceEmitter.Emit(plan.AdditionalQuantities);
				output.AddSource("DiscoveredQuantities.g.cs", SourceText.From(quantitySource, Encoding.UTF8));
			}

			string arithmeticSource = ArithmeticSourceEmitter.Emit(plan);
			output.AddSource("Arithmetic.g.cs", SourceText.From(arithmeticSource, Encoding.UTF8));
		});
	}
}
