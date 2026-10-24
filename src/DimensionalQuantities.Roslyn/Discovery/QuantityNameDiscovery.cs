using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Naming;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DimensionalQuantities.Roslyn.Discovery;

internal static class QuantityNameDiscovery
{
	public static bool TryRead(IdentifierNameSyntax name, SemanticModel semanticModel, out Dimension dimension,
		CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		dimension = default;
		if (name.Parent is QualifiedNameSyntax or AliasQualifiedNameSyntax
			|| !SyntaxFacts.IsInTypeOnlyContext(name))
		{
			return false;
		}

		SymbolInfo binding = semanticModel.GetSymbolInfo(name, cancellationToken);
		if (binding.Symbol is not null || !binding.CandidateSymbols.IsEmpty
			|| semanticModel.GetAliasInfo(name, cancellationToken) is not null
			|| semanticModel.GetTypeInfo(name, cancellationToken).Type is not IErrorTypeSymbol
			|| ExplicitAliasReservation.IsReserved(semanticModel.Compilation, name.Identifier.ValueText, cancellationToken))
		{
			return false;
		}

		return DimensionNameParser.TryParse(name.Identifier.ValueText, out dimension);
	}
}
