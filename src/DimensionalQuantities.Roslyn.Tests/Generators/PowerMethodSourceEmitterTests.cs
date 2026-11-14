using System.Reflection;
using Assembly = System.Reflection.Assembly;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Numerics;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.Roslyn.Generators;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace DimensionalQuantities.Roslyn.Tests.Generators;

[NotInParallel(nameof(PowerMethodSourceEmitterTests))]
public sealed class PowerMethodSourceEmitterTests
{
	[Test]
	[Arguments("Squared", 2, 1)]
	[Arguments("SquareRoot", 1, 2)]
	public async Task ConvenienceMethodDelegatesToTheEncodedPowerOverload(string method, int numerator, int denominator)
	{
		Dimension operand = new(Length: 1);
		Rational exponent = new(numerator, denominator);
		PowerMethodOperation operation = new(operand, exponent, operand.Pow(exponent), method);
		QuantityGenerationPlan plan = QuantityDiscovery.Discover([], [], [operation]);
		string exponentType = TypeSystemTypeNameFormatter.Format(TypeSystemNumberCodec.CreateType(exponent));
		string resultType = plan.GetQuantityName(operation.Result);
		string source = $$"""
			namespace DimensionalQuantities.Generated;
			public static class StubPowerExtensions
			{
				extension(global::DimensionalQuantities.TypeSystem.Power<{{exponentType}}>)
				{
					public static {{resultType}} Of(Length value) => {{resultType}}.FromSiBaseUnit(value.AsSiBaseUnit() + 100);
				}
			}
			public static class Consumer
			{
				public static double Value() => Length.FromMetres(9).{{method}}().AsSiBaseUnit();
			}
			""";
		CSharpParseOptions options = new(LanguageVersion.Preview);
		string[] assemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = assemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		SyntaxTree[] trees =
		[
			CSharpSyntaxTree.ParseText(source, options, cancellationToken: TestContext.Current!.Execution.CancellationToken),
			CSharpSyntaxTree.ParseText(QuantitySourceEmitter.Emit(plan.KnownQuantities), options, cancellationToken: TestContext.Current!.Execution.CancellationToken),
			CSharpSyntaxTree.ParseText(QuantitySourceEmitter.Emit(plan.AdditionalQuantities), options, cancellationToken: TestContext.Current!.Execution.CancellationToken),
			CSharpSyntaxTree.ParseText(PowerMethodSourceEmitter.Emit(plan), options, cancellationToken: TestContext.Current!.Execution.CancellationToken)
		];
		CSharpCompilation compilation = CSharpCompilation.Create("DelegatingPowerConsumer", trees, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		using MemoryStream assemblyBytes = new();
		EmitResult emit = compilation.Emit(assemblyBytes, cancellationToken: TestContext.Current!.Execution.CancellationToken);
		await Assert.That(emit.Success).IsTrue().Because(string.Join(Environment.NewLine, emit.Diagnostics));
		Assembly assembly = Assembly.Load(assemblyBytes.ToArray());

		double result = (double)assembly.GetType("DimensionalQuantities.Generated.Consumer")!.GetMethod("Value")!.Invoke(null, null)!;

		await Assert.That(result).IsEqualTo(109);
	}

	[Test]
	[Arguments("Squared", 2, 1, -3, 9)]
	[Arguments("CubeRoot", 1, 3, -8, -2)]
	[Arguments("Root5", 1, 5, -32, -2)]
	[Arguments("SquareRoot", 1, 2, -9, double.NaN)]
	[Arguments("Root4", 1, 4, -16, double.NaN)]
	public async Task EmittedMethodsUseTheRealOddRootAndPreserveEvenRootBehaviour(string method, int numerator, int denominator, double value, double expected)
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
		CSharpParseOptions options = new(LanguageVersion.Preview);
		string[] assemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = assemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		SyntaxTree[] trees =
		[
			CSharpSyntaxTree.ParseText(source, options, cancellationToken: TestContext.Current!.Execution.CancellationToken),
			CSharpSyntaxTree.ParseText(QuantitySourceEmitter.Emit(plan.KnownQuantities), options, cancellationToken: TestContext.Current!.Execution.CancellationToken),
			CSharpSyntaxTree.ParseText(QuantitySourceEmitter.Emit(plan.AdditionalQuantities), options, cancellationToken: TestContext.Current!.Execution.CancellationToken),
			CSharpSyntaxTree.ParseText(PowerMethodSourceEmitter.Emit(plan), options, cancellationToken: TestContext.Current!.Execution.CancellationToken),
			CSharpSyntaxTree.ParseText(PowerSourceEmitter.Emit(plan), options, cancellationToken: TestContext.Current!.Execution.CancellationToken)
		];
		CSharpCompilation compilation = CSharpCompilation.Create("PowerMethodConsumer", trees, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		using MemoryStream assemblyBytes = new();
		EmitResult emit = compilation.Emit(assemblyBytes, cancellationToken: TestContext.Current!.Execution.CancellationToken);
		await Assert.That(emit.Success).IsTrue().Because(string.Join(Environment.NewLine, emit.Diagnostics));
		Assembly assembly = Assembly.Load(assemblyBytes.ToArray());

		double result = (double)assembly.GetType("Consumer")!.GetMethod("Value")!.Invoke(null, null)!;

		await Assert.That(Math.Round(result, 12)).IsEqualTo(Math.Round(expected, 12));
	}
}
