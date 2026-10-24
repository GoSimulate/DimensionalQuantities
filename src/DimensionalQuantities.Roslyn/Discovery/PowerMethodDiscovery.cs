using DimensionalQuantities.Numerics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DimensionalQuantities.Roslyn.Discovery;

internal sealed class PowerMethodDiscovery
{
	private readonly SemanticModel _semanticModel;

	public PowerMethodDiscovery(SemanticModel semanticModel)
	{
		_semanticModel = semanticModel;
	}

	public static bool IsSupportedName(string name) => TryGetExponent(name, out _);

	public bool TryRead(InvocationExpressionSyntax invocation, out Rational exponent, out string methodName, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		exponent = default;
		methodName = string.Empty;
		if (invocation.Expression is not MemberAccessExpressionSyntax { Name: IdentifierNameSyntax name }
			|| invocation.ArgumentList.Arguments.Count != 0)
		{
			return false;
		}

		string candidateName = name.Identifier.ValueText;
		if (!TryGetExponent(candidateName, out exponent))
		{
			return false;
		}

		SymbolInfo binding = _semanticModel.GetSymbolInfo(invocation, cancellationToken);
		if (binding.Symbol is IMethodSymbol)
		{
			return false;
		}

		methodName = candidateName;
		return true;
	}

	private static bool TryGetExponent(string name, out Rational exponent)
	{
		exponent = name switch
		{
			"Squared" => new(2),
			"Cubed" => new(3),
			"Pow4" => new(4),
			"Pow5" => new(5),
			"Pow6" => new(6),
			"SquareRoot" => new(1, 2),
			"CubeRoot" => new(1, 3),
			"Root4" => new(1, 4),
			"Root5" => new(1, 5),
			"Root6" => new(1, 6),
			_ => default
		};
		return exponent != default;
	}
}
