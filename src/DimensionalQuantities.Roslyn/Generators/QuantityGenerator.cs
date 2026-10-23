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

		IncrementalValuesProvider<QuantityExpressionAnalysis> arithmeticExpressions = context.SyntaxProvider.CreateSyntaxProvider(
			static (node, _) => node is BinaryExpressionSyntax binary && ArithmeticDiscovery.IsRoot(binary),
			static (syntaxContext, cancellationToken) => ArithmeticDiscovery.Analyse(
				(BinaryExpressionSyntax)syntaxContext.Node, syntaxContext.SemanticModel, cancellationToken));

		IncrementalValuesProvider<QuantityExpressionAnalysis> powerExpressions = context.SyntaxProvider.CreateSyntaxProvider(
			static (node, _) => node is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Of" } },
			static (syntaxContext, cancellationToken) => new QuantityExpressionAnalyser(syntaxContext.SemanticModel)
				.Analyse((InvocationExpressionSyntax)syntaxContext.Node, cancellationToken));

		context.RegisterSourceOutput(arithmeticExpressions.Collect().Combine(powerExpressions.Collect()), static (output, discovered) =>
		{
			ImmutableArray<ArithmeticOperation>.Builder discoveredOperations = ImmutableArray.CreateBuilder<ArithmeticOperation>();
			ImmutableArray<PowerOperation>.Builder discoveredPowers = ImmutableArray.CreateBuilder<PowerOperation>();
			foreach (QuantityExpressionAnalysis expression in discovered.Left.Concat(discovered.Right))
			{
				output.CancellationToken.ThrowIfCancellationRequested();
				discoveredOperations.AddRange(expression.ArithmeticOperations);
				discoveredPowers.AddRange(expression.PowerOperations);
			}

			if (discoveredOperations.Count == 0 && discoveredPowers.Count == 0)
			{
				return;
			}

			QuantityGenerationPlan plan = QuantityDiscovery.Discover(discoveredOperations, discoveredPowers);
			if (!plan.AdditionalQuantities.IsEmpty)
			{
				string quantitySource = QuantitySourceEmitter.Emit(plan.AdditionalQuantities);
				output.AddSource("DiscoveredQuantities.g.cs", SourceText.From(quantitySource, Encoding.UTF8));
			}

			if (!plan.Operations.IsEmpty)
			{
				string arithmeticSource = ArithmeticSourceEmitter.Emit(plan);
				output.AddSource("Arithmetic.g.cs", SourceText.From(arithmeticSource, Encoding.UTF8));
			}

			if (!plan.PowerOperations.IsEmpty)
			{
				string powerSource = PowerSourceEmitter.Emit(plan);
				output.AddSource("Powers.g.cs", SourceText.From(powerSource, Encoding.UTF8));
			}
		});
	}
}
