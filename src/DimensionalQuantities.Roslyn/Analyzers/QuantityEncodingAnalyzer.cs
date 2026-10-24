using System.Collections.Immutable;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DimensionalQuantities.Roslyn.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class QuantityEncodingAnalyzer : DiagnosticAnalyzer
{
	internal const string AliasNameProperty = "AliasName";

	internal static readonly DiagnosticDescriptor ReadableAlias = new(
		"DQ0003", "Use a readable quantity alias", "Use the readable alias '{0}' for this quantity",
		"DimensionalQuantities", DiagnosticSeverity.Info, isEnabledByDefault: true);

	internal static readonly DiagnosticDescriptor NonCanonical = new(
		"DQ0004", "Noncanonical type-system number", "This encoding is noncanonical; equivalent values have distinct type identity",
		"DimensionalQuantities", DiagnosticSeverity.Warning, isEnabledByDefault: true);

	internal static readonly DiagnosticDescriptor Invalid = new(
		"DQ0005", "Invalid type-system encoding", "This closed type-system encoding is invalid",
		"DimensionalQuantities", DiagnosticSeverity.Error, isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [ReadableAlias, NonCanonical, Invalid];

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(start =>
		{
			INamedTypeSymbol? quantity = start.Compilation.GetTypeByMetadataName(typeof(Quantity<>).FullName!);
			INamedTypeSymbol? dimension = start.Compilation.GetTypeByMetadataName(typeof(TypeSystem.Dimension<,,,,,,>).FullName!);
			INamedTypeSymbol? power = start.Compilation.GetTypeByMetadataName(typeof(Power<>).FullName!);
			if (quantity is null || dimension is null || power is null)
			{
				return;
			}

			TypeSystemNumberReader reader = new(start.Compilation);
			start.RegisterSyntaxNodeAction(node => AnalyzeGeneric(node, reader, quantity, dimension, power), SyntaxKind.GenericName);
		});
	}

	private static void AnalyzeGeneric(SyntaxNodeAnalysisContext context, TypeSystemNumberReader reader,
		INamedTypeSymbol quantity, INamedTypeSymbol dimension, INamedTypeSymbol power)
	{
		GenericNameSyntax syntax = (GenericNameSyntax)context.Node;
		if (context.SemanticModel.GetTypeInfo(syntax, context.CancellationToken).Type is not INamedTypeSymbol type
			|| IsIncomplete(type))
		{
			return;
		}

		if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, quantity))
		{
			AnalyzeQuantity(context, reader, syntax, type, dimension);
		}
		else if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, power))
		{
			AnalyzePower(context, reader, syntax, type);
		}
	}

	private static void AnalyzeQuantity(SyntaxNodeAnalysisContext context, TypeSystemNumberReader reader,
		GenericNameSyntax syntax, INamedTypeSymbol quantity, INamedTypeSymbol dimensionType)
	{
		if (quantity.TypeArguments[0] is not INamedTypeSymbol encodedDimension
			|| !SymbolEqualityComparer.Default.Equals(encodedDimension.OriginalDefinition, dimensionType))
		{
			Report(context, Invalid, syntax);
			return;
		}

		bool canonical = true;
		foreach (ITypeSymbol exponent in encodedDimension.TypeArguments)
		{
			if (!CanonicalTypeEncoding.TryInspect(exponent, reader, context.SemanticModel.Compilation,
				out bool exponentCanonical, context.CancellationToken))
			{
				Report(context, Invalid, syntax);
				return;
			}

			canonical &= exponentCanonical;
		}

		if (!canonical)
		{
			Report(context, NonCanonical, syntax);
			return;
		}

		if (syntax.Ancestors().OfType<UsingDirectiveSyntax>().Any())
		{
			return;
		}

		QuantityTypeReader quantityReader = new(context.SemanticModel.Compilation);
		if (!quantityReader.TryRead(quantity, out Dimension dimension, context.CancellationToken) || dimension == default)
		{
			return;
		}

		string alias = DimensionNames.Get(dimension);
		ImmutableDictionary<string, string?> properties = ImmutableDictionary<string, string?>.Empty.Add(AliasNameProperty, alias);
		context.ReportDiagnostic(Diagnostic.Create(ReadableAlias, syntax.GetLocation(), properties, alias));
	}

	private static void AnalyzePower(SyntaxNodeAnalysisContext context, TypeSystemNumberReader reader,
		GenericNameSyntax syntax, INamedTypeSymbol power)
	{
		if (!CanonicalTypeEncoding.TryInspect(power.TypeArguments[0], reader, context.SemanticModel.Compilation,
			out bool canonical, context.CancellationToken))
		{
			Report(context, Invalid, syntax);
		}
		else if (!canonical)
		{
			Report(context, NonCanonical, syntax);
		}
	}

	private static bool IsIncomplete(ITypeSymbol type) => type is ITypeParameterSymbol or IErrorTypeSymbol
		|| type is INamedTypeSymbol named && (named.IsUnboundGenericType || named.TypeArguments.Any(IsIncomplete));

	private static void Report(SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, GenericNameSyntax syntax) =>
		context.ReportDiagnostic(Diagnostic.Create(descriptor, syntax.GetLocation()));
}
