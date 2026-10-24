using DimensionalQuantities.Numerics;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;

namespace DimensionalQuantities.Roslyn.Analyzers;

internal static class CanonicalTypeEncoding
{
	public static bool TryInspect(ITypeSymbol type, TypeSystemNumberReader reader, Compilation compilation,
		out bool canonical, CancellationToken cancellationToken)
	{
		canonical = false;
		if (!reader.TryRead(type, out Rational value, cancellationToken))
		{
			return false;
		}

		Type expectedType = TypeSystemNumberCodec.CreateType(value);
		ITypeSymbol? expected = Resolve(expectedType, compilation);
		canonical = expected is not null && SymbolEqualityComparer.Default.Equals(type, expected);
		return true;
	}

	private static ITypeSymbol? Resolve(Type runtimeType, Compilation compilation)
	{
		if (!runtimeType.IsConstructedGenericType)
		{
			return compilation.GetTypeByMetadataName(runtimeType.FullName!);
		}

		INamedTypeSymbol? definition = compilation.GetTypeByMetadataName(runtimeType.GetGenericTypeDefinition().FullName!);
		if (definition is null)
		{
			return null;
		}

		ITypeSymbol?[] arguments = runtimeType.GetGenericArguments().Select(argument => Resolve(argument, compilation)).ToArray();
		return arguments.Any(argument => argument is null) ? null : definition.Construct(arguments!);
	}
}
