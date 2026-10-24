using System.Diagnostics.CodeAnalysis;
using DimensionalQuantities.Numerics;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DimensionalQuantities.Roslyn.Discovery;

internal sealed class EnglishExponentDiscovery
{
	private readonly SemanticModel _semanticModel;
	private readonly INamedTypeSymbol _power;

	public EnglishExponentDiscovery(SemanticModel semanticModel)
	{
		_semanticModel = semanticModel;
		_power = semanticModel.Compilation.GetRequiredType(typeof(Power<>));
	}

	public bool TryRead(InvocationExpressionSyntax invocation, out EnglishExponentAlias alias, CancellationToken cancellationToken = default)
	{
		alias = default;
		if (!TryGetUnresolvedRequest(invocation, out IdentifierNameSyntax? exponentName, cancellationToken)
			|| ExplicitAliasReservation.IsReserved(_semanticModel.Compilation, exponentName.Identifier.ValueText, cancellationToken)
			|| !TryParseExponent(exponentName, out Rational value))
		{
			return false;
		}

		Type encoded = TypeSystemNumberCodec.CreateType(value);
		alias = new(exponentName.Identifier.ValueText, value, TypeSystemTypeNameFormatter.Format(encoded));
		return true;
	}

	public bool TryGetUnresolvedRequest(InvocationExpressionSyntax invocation, [NotNullWhen(true)] out IdentifierNameSyntax? exponentName,
		CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		exponentName = null;
		if (invocation.Expression is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Of" } member
			|| invocation.ArgumentList.Arguments.Count != 1
			|| !TryGetExponentName(member.Expression, out exponentName))
		{
			return false;
		}

		ISymbol? receiver = _semanticModel.GetSymbolInfo(member.Expression, cancellationToken).Symbol;
		if (receiver is not INamedTypeSymbol powerType
			|| !SymbolEqualityComparer.Default.Equals(powerType.OriginalDefinition, _power))
		{
			return false;
		}

		SymbolInfo exponentSymbol = _semanticModel.GetSymbolInfo(exponentName, cancellationToken);
		string name = exponentName.Identifier.ValueText;
		if (exponentSymbol.Symbol is not null || !exponentSymbol.CandidateSymbols.IsEmpty
			|| _semanticModel.GetAliasInfo(exponentName, cancellationToken) is not null
			|| !name.StartsWith("Number", StringComparison.Ordinal))
		{
			return false;
		}

		return true;
	}

	public static bool TryParseExponent(IdentifierNameSyntax name, out Rational value) =>
		EnglishRationalParser.TryParse(name.Identifier.ValueText.Substring("Number".Length), out value);

	private static bool TryGetExponentName(ExpressionSyntax receiver, [NotNullWhen(true)] out IdentifierNameSyntax? exponentName)
	{
		GenericNameSyntax? powerName = receiver switch
		{
			GenericNameSyntax generic => generic,
			MemberAccessExpressionSyntax { Name: GenericNameSyntax generic } => generic,
			_ => null
		};
		exponentName = powerName is { TypeArgumentList.Arguments.Count: 1 }
			? powerName.TypeArgumentList.Arguments[0] as IdentifierNameSyntax : null;
		return exponentName is not null;
	}
}
