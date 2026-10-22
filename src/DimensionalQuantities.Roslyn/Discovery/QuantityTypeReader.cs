using System.Numerics;
using DimensionalQuantities.Numerics;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Dimension = DimensionalQuantities.Dimensions.Dimension;

namespace DimensionalQuantities.Roslyn.Discovery;

internal sealed class QuantityTypeReader
{
	private readonly INamedTypeSymbol? _quantity;
	private readonly INamedTypeSymbol? _dimension;
	private readonly INamedTypeSymbol? _hex;
	private readonly INamedTypeSymbol? _negative;
	private readonly INamedTypeSymbol? _fraction;
	private readonly Dictionary<ITypeSymbol, int> _terminalDigits = new(SymbolEqualityComparer.Default);

	public QuantityTypeReader(Compilation compilation)
	{
		_quantity = compilation.GetTypeByMetadataName(typeof(Quantity<>).FullName!);
		_dimension = compilation.GetTypeByMetadataName(typeof(TypeSystem.Dimension<,,,,,,>).FullName!);
		_hex = compilation.GetTypeByMetadataName(typeof(Hex<,>).FullName!);
		_negative = compilation.GetTypeByMetadataName(typeof(Negative<>).FullName!);
		_fraction = compilation.GetTypeByMetadataName(typeof(Fraction<,>).FullName!);

		for (int digit = 0; digit < 16; digit++)
		{
			string name = TypeSystemNumberCodec.CreateType(new(digit)).FullName!;
			INamedTypeSymbol? symbol = compilation.GetTypeByMetadataName(name);
			if (symbol is not null)
			{
				_terminalDigits.Add(symbol, digit);
			}
		}
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

		if (!TryReadNumber(encodedDimension.TypeArguments[0], out Rational length, cancellationToken)
			|| !TryReadNumber(encodedDimension.TypeArguments[1], out Rational mass, cancellationToken)
			|| !TryReadNumber(encodedDimension.TypeArguments[2], out Rational time, cancellationToken)
			|| !TryReadNumber(encodedDimension.TypeArguments[3], out Rational electricCurrent, cancellationToken)
			|| !TryReadNumber(encodedDimension.TypeArguments[4], out Rational temperature, cancellationToken)
			|| !TryReadNumber(encodedDimension.TypeArguments[5], out Rational amountOfSubstance, cancellationToken)
			|| !TryReadNumber(encodedDimension.TypeArguments[6], out Rational luminousIntensity, cancellationToken))
		{
			return false;
		}

		dimension = new(length, mass, time, electricCurrent, temperature, amountOfSubstance, luminousIntensity);
		return true;
	}

	private bool TryReadNumber(ITypeSymbol type, out Rational value, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		value = default;
		if (type is not INamedTypeSymbol named)
		{
			return false;
		}

		if (_terminalDigits.TryGetValue(named, out int digit))
		{
			value = new(digit);
			return true;
		}

		if (IsDefinition(named, _hex) && TryReadNumber(named.TypeArguments[0], out Rational low, cancellationToken)
			&& TryReadNumber(named.TypeArguments[1], out Rational rest, cancellationToken) && low.Denominator.IsOne && rest.Denominator.IsOne)
		{
			value = 16 * rest + low;
			return true;
		}

		if (IsDefinition(named, _negative) && TryReadNumber(named.TypeArguments[0], out Rational magnitude, cancellationToken) && magnitude.Denominator.IsOne)
		{
			value = -magnitude;
			return true;
		}

		if (IsDefinition(named, _fraction) && TryReadNumber(named.TypeArguments[0], out Rational numerator, cancellationToken)
			&& TryReadNumber(named.TypeArguments[1], out Rational denominator, cancellationToken) && numerator.Denominator.IsOne
			&& denominator.Denominator.IsOne && denominator.Numerator > BigInteger.Zero)
		{
			value = numerator / denominator;
			return true;
		}

		return false;
	}

	private static bool IsDefinition(INamedTypeSymbol symbol, INamedTypeSymbol? definition) =>
		definition is not null && SymbolEqualityComparer.Default.Equals(symbol.OriginalDefinition, definition);
}
