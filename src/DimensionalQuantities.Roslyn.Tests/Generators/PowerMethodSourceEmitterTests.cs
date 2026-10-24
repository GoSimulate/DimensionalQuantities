using System.Reflection;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Numerics;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.Roslyn.Generators;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generators;

public sealed class PowerMethodSourceEmitterTests
{
	[Theory]
	[InlineData("Squared", 2, 1, -3, 9)]
	[InlineData("CubeRoot", 1, 3, -8, -2)]
	[InlineData("Root5", 1, 5, -32, -2)]
	[InlineData("SquareRoot", 1, 2, -9, double.NaN)]
	[InlineData("Root4", 1, 4, -16, double.NaN)]
	public void EmittedMethodsUseTheRealOddRootAndPreserveEvenRootBehaviour(string method, int numerator, int denominator, double value, double expected)
	{
		Dimension operand = new(Length: 1);
		Rational exponent = new(numerator, denominator);
		Dimension resultDimension = operand.Pow(exponent);
		PowerMethodOperation operation = new(operand, exponent, resultDimension, method);
		QuantityGenerationPlan plan = QuantityDiscovery.Discover([], [], [operation]);
		string source = $$"""
			public static class Consumer
			{
				public static double Value() => Length.FromMetres({{value.ToString(System.Globalization.CultureInfo.InvariantCulture)}}).{{method}}().AsSiBaseUnit();
			}
			""";
		CSharpParseOptions options = new(LanguageVersion.CSharp14);
		string[] assemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = assemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		SyntaxTree[] trees =
		[
			CSharpSyntaxTree.ParseText(source, options, cancellationToken: TestContext.Current.CancellationToken),
			CSharpSyntaxTree.ParseText(QuantitySourceEmitter.Emit(plan.KnownQuantities), options, cancellationToken: TestContext.Current.CancellationToken),
			CSharpSyntaxTree.ParseText(QuantitySourceEmitter.Emit(plan.AdditionalQuantities), options, cancellationToken: TestContext.Current.CancellationToken),
			CSharpSyntaxTree.ParseText(PowerMethodSourceEmitter.Emit(plan), options, cancellationToken: TestContext.Current.CancellationToken)
		];
		CSharpCompilation compilation = CSharpCompilation.Create("PowerMethodConsumer", trees, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		using MemoryStream assemblyBytes = new();
		EmitResult emit = compilation.Emit(assemblyBytes, cancellationToken: TestContext.Current.CancellationToken);
		Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
		Assembly assembly = Assembly.Load(assemblyBytes.ToArray());

		double result = (double)assembly.GetType("Consumer")!.GetMethod("Value")!.Invoke(null, null)!;

		Assert.Equal(expected, result, 12);
	}
}
