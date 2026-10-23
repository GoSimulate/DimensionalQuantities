using Microsoft.CodeAnalysis;

namespace DimensionalQuantities.Roslyn.Discovery;

internal static class CompilationExtensions
{
	public static INamedTypeSymbol GetRequiredType(this Compilation compilation, Type type) =>
		compilation.GetTypeByMetadataName(type.FullName!)
		?? throw new InvalidOperationException($"Required type '{type.FullName}' is missing or ambiguous in the consuming project. Check its DimensionalQuantities reference.");
}
