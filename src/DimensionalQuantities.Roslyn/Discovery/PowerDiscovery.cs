using DimensionalQuantities.Numerics;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DimensionalQuantities.Roslyn.Discovery;

internal sealed class PowerDiscovery
{
	private readonly SemanticModel _semanticModel;
	private readonly INamedTypeSymbol _power;
	private readonly TypeSystemNumberReader _numberReader;

	public PowerDiscovery(SemanticModel semanticModel)
	{
		_semanticModel = semanticModel;
		_power = semanticModel.Compilation.GetRequiredType(typeof(Power<>));
		_numberReader = new(semanticModel.Compilation);
	}

	public bool TryRead(InvocationExpressionSyntax invocation, out Rational exponent, out string exponentTypeName, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		exponent = default;
		exponentTypeName = string.Empty;
		if (invocation.Expression is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Of" } member
			|| invocation.ArgumentList.Arguments.Count != 1)
		{
			return false;
		}

		ISymbol? powerTypeSymbol = _semanticModel.GetSymbolInfo(member.Expression, cancellationToken).Symbol;
		if (powerTypeSymbol is IAliasSymbol alias)
		{
			powerTypeSymbol = alias.Target;
		}

		if (powerTypeSymbol is not INamedTypeSymbol type
			|| !SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, _power)
			|| !_numberReader.TryRead(type.TypeArguments[0], out exponent, cancellationToken))
		{
			return false;
		}

		exponentTypeName = type.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		return true;
	}
}
