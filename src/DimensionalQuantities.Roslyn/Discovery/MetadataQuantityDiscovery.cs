using System.Collections.Immutable;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Naming;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DimensionalQuantities.Roslyn.Discovery;

internal sealed class MetadataQuantityDiscovery
{
	private readonly SemanticModel _semanticModel;
	private QuantityTypeReader? _typeReader;

	public MetadataQuantityDiscovery(SemanticModel semanticModel)
	{
		_semanticModel = semanticModel;
	}

	public ImmutableArray<Dimension> Discover(ExpressionSyntax expression, CancellationToken cancellationToken = default)
	{
		HashSet<Dimension> dimensions = [];
		Visit(_semanticModel.GetTypeInfo(expression, cancellationToken).Type);
		return [.. dimensions];

		void Visit(ITypeSymbol? type)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (type is null || type is IErrorTypeSymbol)
			{
				return;
			}

			if (type is INamedTypeSymbol namedQuantity
				&& namedQuantity.Name == nameof(TypeSystem.Quantity<>) && namedQuantity.Arity == 1
				&& (_typeReader ??= new(_semanticModel.Compilation)).TryRead(namedQuantity, out Dimension dimension, cancellationToken))
			{
				if (dimension == default)
				{
					return;
				}

				string name = DimensionNameGenerator.GetQuantityName(dimension);
				if (!ExplicitAliasReservation.IsReserved(_semanticModel.Compilation, name, cancellationToken)
					&& !_semanticModel.Compilation.GlobalNamespace.GetMembers(name)
						.Any(candidate => candidate is INamespaceSymbol or INamedTypeSymbol { Arity: 0 }))
				{
					dimensions.Add(dimension);
				}
				return;
			}

			switch (type)
			{
				case IArrayTypeSymbol array:
					Visit(array.ElementType);
					break;
				case INamedTypeSymbol named:
					foreach (ITypeSymbol argument in named.TypeArguments)
					{
						Visit(argument);
					}
					break;
			}
		}
	}
}
