using System.Collections.Immutable;
using System.Text;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Roslyn.Discovery;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DimensionalQuantities.Roslyn.Generators;

[Generator]
public sealed class QuantityGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		QuantityGenerationPlan defaultPlan = DefaultQuantityDiscovery.Discover();
		context.RegisterPostInitializationOutput(output =>
		{
			string source = QuantitySourceEmitter.Emit([.. defaultPlan.KnownQuantities, .. defaultPlan.AdditionalQuantities]);
			output.AddSource("Quantities.g.cs", SourceText.From(source, Encoding.UTF8));
			string powerSource = PowerSourceEmitter.Emit(defaultPlan, "DefaultPowerExtensions");
			output.AddSource("DefaultPowers.g.cs", SourceText.From(powerSource, Encoding.UTF8));
			string methodSource = PowerMethodSourceEmitter.Emit(defaultPlan, "DefaultPowerMethodExtensions");
			output.AddSource("DefaultPowerMethods.g.cs", SourceText.From(methodSource, Encoding.UTF8));
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

		IncrementalValuesProvider<Dimension?> typeNames = context.SyntaxProvider.CreateSyntaxProvider(
			static (node, _) => node is IdentifierNameSyntax name && SyntaxFacts.IsInTypeOnlyContext(name),
			static (syntaxContext, cancellationToken) => QuantityNameDiscovery.TryRead(
				(IdentifierNameSyntax)syntaxContext.Node, syntaxContext.SemanticModel, out Dimension dimension, cancellationToken)
					? (Dimension?)dimension : null);
		IncrementalValuesProvider<Dimension> requestedDimensions = typeNames
			.Where(static dimension => dimension.HasValue)
			.Select(static (dimension, _) => dimension.GetValueOrDefault());

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

		IncrementalValueProvider<(IEnumerable<QuantityExpressionAnalysis> Expressions, ImmutableArray<Dimension> Dimensions)> requests = arithmeticExpressions.Collect()
			.Combine(powerExpressions.Collect()).Combine(requestedDimensions.Collect())
			.Select(static (collected, _) => ((IEnumerable<QuantityExpressionAnalysis>)collected.Left.Left.Concat(collected.Left.Right), collected.Right));

		context.RegisterSourceOutput(requests, (output, discovered) =>
		{
			ImmutableArray<ArithmeticOperation>.Builder discoveredOperations = ImmutableArray.CreateBuilder<ArithmeticOperation>();
			ImmutableArray<PowerOperation>.Builder discoveredPowers = ImmutableArray.CreateBuilder<PowerOperation>();
			ImmutableArray<PowerMethodOperation>.Builder discoveredPowerMethods = ImmutableArray.CreateBuilder<PowerMethodOperation>();
			foreach (QuantityExpressionAnalysis expression in discovered.Expressions)
			{
				output.CancellationToken.ThrowIfCancellationRequested();
				discoveredOperations.AddRange(expression.ArithmeticOperations);
				discoveredPowers.AddRange(expression.PowerOperations);
				discoveredPowerMethods.AddRange(expression.PowerMethodOperations);
			}

			if (discoveredOperations.Count == 0 && discoveredPowers.Count == 0 && discoveredPowerMethods.Count == 0 && discovered.Dimensions.IsEmpty)
			{
				return;
			}

			QuantityGenerationPlan plan = QuantityDiscovery.DiscoverWithPredefinedQuantities(defaultPlan,
				discoveredOperations, discoveredPowers, discoveredPowerMethods, discovered.Dimensions);
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
