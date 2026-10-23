using DimensionalQuantities.Numerics;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;

namespace DimensionalQuantities.Roslyn.Discovery;

internal sealed class TypeSystemNumberReader
{
	private readonly INamedTypeSymbol _hex;
	private readonly INamedTypeSymbol _negative;
	private readonly INamedTypeSymbol _fraction;
	private readonly Dictionary<ITypeSymbol, Type> _terminalTypes = new(SymbolEqualityComparer.Default);

	public TypeSystemNumberReader(Compilation compilation)
	{
		_hex = compilation.GetRequiredType(typeof(Hex<,>));
		_negative = compilation.GetRequiredType(typeof(Negative<>));
		_fraction = compilation.GetRequiredType(typeof(Fraction<,>));

		for (int digit = 0; digit < 16; digit++)
		{
			Type terminalType = TypeSystemNumberCodec.CreateType(new(digit));
			_terminalTypes.Add(compilation.GetRequiredType(terminalType), terminalType);
		}
	}

	public bool TryRead(ITypeSymbol? symbol, out Rational value, CancellationToken cancellationToken = default)
	{
		value = default;
		if (!TryGetRuntimeType(symbol, out Type? type, cancellationToken))
		{
			return false;
		}

		value = TypeSystemNumberCodec.Parse(type!);
		return true;
	}

	private bool TryGetRuntimeType(ITypeSymbol? symbol, out Type? type, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		type = null;
		if (symbol is not INamedTypeSymbol named)
		{
			return false;
		}

		if (_terminalTypes.TryGetValue(named, out Type? terminalType))
		{
			type = terminalType;
			return true;
		}

		Type? definition = IsDefinition(named, _hex) ? typeof(Hex<,>)
			: IsDefinition(named, _negative) ? typeof(Negative<>)
			: IsDefinition(named, _fraction) ? typeof(Fraction<,>) : null;
		if (definition is null)
		{
			return false;
		}

		Type[] arguments = new Type[named.TypeArguments.Length];
		for (int index = 0; index < arguments.Length; index++)
		{
			if (!TryGetRuntimeType(named.TypeArguments[index], out Type? argument, cancellationToken))
			{
				return false;
			}

			arguments[index] = argument!;
		}

		try
		{
			type = definition.MakeGenericType(arguments);
			return true;
		}
		catch (ArgumentException)
		{
			return false;
		}
	}

	private static bool IsDefinition(INamedTypeSymbol symbol, INamedTypeSymbol definition) =>
		SymbolEqualityComparer.Default.Equals(symbol.OriginalDefinition, definition);
}
