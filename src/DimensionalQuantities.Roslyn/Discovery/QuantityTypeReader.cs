using DimensionalQuantities.Numerics;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Dimension = DimensionalQuantities.Dimensions.Dimension;

namespace DimensionalQuantities.Roslyn.Discovery;

internal sealed class QuantityTypeReader
{
	private readonly INamedTypeSymbol _quantity;
	private readonly INamedTypeSymbol _dimension;
	private readonly TypeSystemNumberReader _numberReader;

	public QuantityTypeReader(Compilation compilation)
	{
		_quantity = compilation.GetRequiredType(typeof(Quantity<>));
		_dimension = compilation.GetRequiredType(typeof(TypeSystem.Dimension<,,,,,,>));
		_numberReader = new(compilation);
	}

	public bool TryRead(ITypeSymbol? type, out Dimension dimension, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		dimension = default;
		if (type is not INamedTypeSymbol quantity || !IsDefinition(quantity, _quantity)
			|| quantity.TypeArguments[0] is not INamedTypeSymbol encodedDimension || !IsDefinition(encodedDimension, _dimension))
		{
			return false;
		}

		if (!_numberReader.TryRead(encodedDimension.TypeArguments[0], out Rational length, cancellationToken)
			|| !_numberReader.TryRead(encodedDimension.TypeArguments[1], out Rational mass, cancellationToken)
			|| !_numberReader.TryRead(encodedDimension.TypeArguments[2], out Rational time, cancellationToken)
			|| !_numberReader.TryRead(encodedDimension.TypeArguments[3], out Rational electricCurrent, cancellationToken)
			|| !_numberReader.TryRead(encodedDimension.TypeArguments[4], out Rational temperature, cancellationToken)
			|| !_numberReader.TryRead(encodedDimension.TypeArguments[5], out Rational amountOfSubstance, cancellationToken)
			|| !_numberReader.TryRead(encodedDimension.TypeArguments[6], out Rational luminousIntensity, cancellationToken))
		{
			return false;
		}

		dimension = new(length, mass, time, electricCurrent, temperature, amountOfSubstance, luminousIntensity);
		return true;
	}

	public bool TryReadIncludingNullable(ITypeSymbol? type, out Dimension dimension, CancellationToken cancellationToken = default)
	{
		if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
		{
			type = nullable.TypeArguments[0];
		}

		return TryRead(type, out dimension, cancellationToken);
	}

	private static bool IsDefinition(INamedTypeSymbol symbol, INamedTypeSymbol definition) =>
		SymbolEqualityComparer.Default.Equals(symbol.OriginalDefinition, definition);
}
