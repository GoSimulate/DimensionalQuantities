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
			static (node, _) => node is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member }
				&& (member.Name.Identifier.ValueText == "Of" || PowerMethodDiscovery.IsSupportedName(member.Name.Identifier.ValueText)),
			static (syntaxContext, cancellationToken) => new QuantityExpressionAnalyser(syntaxContext.SemanticModel)
				.Analyse((InvocationExpressionSyntax)syntaxContext.Node, cancellationToken));

		IncrementalValuesProvider<EnglishExponentAlias?> englishAliases = context.SyntaxProvider.CreateSyntaxProvider(
			static (node, _) => node is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Of" } },
			static (syntaxContext, cancellationToken) => new EnglishExponentDiscovery(syntaxContext.SemanticModel)
				.TryRead((InvocationExpressionSyntax)syntaxContext.Node, out EnglishExponentAlias alias, cancellationToken)
					? (EnglishExponentAlias?)alias : null);
		IncrementalValuesProvider<EnglishExponentAlias> requestedAliases = englishAliases
			.Where(static alias => alias.HasValue)
			.Select(static (alias, _) => alias.GetValueOrDefault());

		context.RegisterSourceOutput(requestedAliases.Collect(), static (output, aliases) =>
		{
			if (!aliases.IsEmpty)
			{
				string aliasSource = EnglishExponentAliasEmitter.Emit(aliases);
				output.AddSource("EnglishExponents.g.cs", SourceText.From(aliasSource, Encoding.UTF8));
			}
		});

		context.RegisterSourceOutput(arithmeticExpressions.Collect().Combine(powerExpressions.Collect()), static (output, discovered) =>
		{
			ImmutableArray<ArithmeticOperation>.Builder discoveredOperations = ImmutableArray.CreateBuilder<ArithmeticOperation>();
			ImmutableArray<PowerOperation>.Builder discoveredPowers = ImmutableArray.CreateBuilder<PowerOperation>();
			ImmutableArray<PowerMethodOperation>.Builder discoveredPowerMethods = ImmutableArray.CreateBuilder<PowerMethodOperation>();
			foreach (QuantityExpressionAnalysis expression in discovered.Left.Concat(discovered.Right))
			{
				output.CancellationToken.ThrowIfCancellationRequested();
				discoveredOperations.AddRange(expression.ArithmeticOperations);
				discoveredPowers.AddRange(expression.PowerOperations);
				discoveredPowerMethods.AddRange(expression.PowerMethodOperations);
			}

			if (discoveredOperations.Count == 0 && discoveredPowers.Count == 0 && discoveredPowerMethods.Count == 0)
			{
				return;
			}

			QuantityGenerationPlan plan = QuantityDiscovery.Discover(discoveredOperations, discoveredPowers, discoveredPowerMethods);
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

			if (!plan.PowerMethodOperations.IsEmpty)
			{
				string methodSource = PowerMethodSourceEmitter.Emit(plan);
				output.AddSource("PowerMethods.g.cs", SourceText.From(methodSource, Encoding.UTF8));
			}
		});
	}
}
