using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Framework;

internal static class GeneratedCodeExecution
{
	public static double Execute(Compilation compilation, string methodName = "Execute")
	{
		using MemoryStream assemblyStream = new();
		EmitResult result = compilation.Emit(assemblyStream, cancellationToken: TestContext.Current.CancellationToken);
		if (!result.Success)
		{
			throw new InvalidOperationException(string.Join(Environment.NewLine, result.Diagnostics));
		}
		assemblyStream.Position = 0;

		// Dependencies fall back to the default context, preserving shared runtime type identity.
		AssemblyLoadContext loadContext = new(name: null, isCollectible: true);
		try
		{
			Assembly assembly = loadContext.LoadFromStream(assemblyStream);
			MethodInfo method = assembly.GetType("Consumer", throwOnError: true)!.GetMethod(methodName)!;
			return (double)method.Invoke(null, null)!;
		}
		finally
		{
			loadContext.Unload();
		}
	}
}
