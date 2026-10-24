using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DimensionalQuantities.Roslyn.Discovery;

internal static class ExplicitAliasReservation
{
	public static bool IsReserved(Compilation compilation, string name, CancellationToken cancellationToken)
	{
		foreach (SyntaxTree tree in compilation.SyntaxTrees)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (tree.GetRoot(cancellationToken) is CompilationUnitSyntax root
				&& root.Usings.Any(directive => directive.Alias?.Name.Identifier.ValueText == name))
			{
				return true;
			}
		}

		return false;
	}
}
