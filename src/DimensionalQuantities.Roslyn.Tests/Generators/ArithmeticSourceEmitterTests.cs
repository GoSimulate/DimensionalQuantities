using System.Reflection;
using Assembly = System.Reflection.Assembly;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.Roslyn.Generators;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace DimensionalQuantities.Roslyn.Tests.Generators;

[NotInParallel(nameof(ArithmeticSourceEmitterTests))]
public sealed class ArithmeticSourceEmitterTests
{
	private static readonly CSharpParseOptions _parseOptions = new(LanguageVersion.Preview);

	[Test]
	public async Task ExplicitPlanEmitsUsableArithmetic()
	{
		Dimension current = new(ElectricCurrent: 1);
		Dimension currentSquared = new(ElectricCurrent: 2);
		Dimension resistance = new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -2);
		Dimension currentResistance = current * resistance;
		Dimension power = new(Length: 2, Mass: 1, Time: -3);
		Dimension voltage = new(Length: 2, Mass: 1, Time: -3, ElectricCurrent: -1);
		ArithmeticOperation[] operations =
		[
			new(ArithmeticOperationKind.Multiply, current, current, currentSquared),
			new(ArithmeticOperationKind.Multiply, currentSquared, resistance, power),
			new(ArithmeticOperationKind.Multiply, current, resistance, currentResistance),
			new(ArithmeticOperationKind.Multiply, current, currentResistance, power),
			new(ArithmeticOperationKind.Divide, voltage, current, resistance)
		];
		QuantityGenerationPlan plan = QuantityDiscovery.Discover(operations);
		const string source = """
			public static class Consumer
			{
				public static double Left() => (Current.FromAmps(2) * Current.FromAmps(3) * Resistance.FromSiBaseUnit(4)).AsSiBaseUnit();
				public static double Right() => (Current.FromAmps(2) * (Current.FromAmps(3) * Resistance.FromSiBaseUnit(4))).AsSiBaseUnit();
				public static double Divided() => (Voltage.FromSiBaseUnit(12) / Current.FromAmps(3)).AsSiBaseUnit();
			}
			""";
		Assembly assembly = await CompileAndEmit(source, plan);
		Type consumer = assembly.GetType("Consumer")!;
		double left = (double)consumer.GetMethod("Left")!.Invoke(null, null)!;
		double right = (double)consumer.GetMethod("Right")!.Invoke(null, null)!;
		double divided = (double)consumer.GetMethod("Divided")!.Invoke(null, null)!;

		using (Assert.Multiple())
		{
			await Assert.That(left).IsEqualTo(24);
			await Assert.That(right).IsEqualTo(24);
			await Assert.That(divided).IsEqualTo(4);
		}
	}

	[Test]
	[Arguments("Current.FromAmps(3) * InverseCurrent.FromSiBaseUnit(2)", false)]
	[Arguments("InverseCurrent.FromSiBaseUnit(2) * Current.FromAmps(3)", true)]
	public async Task CancellingProductEmitsAnOperatorReturningDouble(string expression, bool inverseFirst)
	{
		Dimension current = new(ElectricCurrent: 1);
		Dimension inverse = new(ElectricCurrent: -1);
		ArithmeticOperation operation = inverseFirst
			? new(ArithmeticOperationKind.Multiply, inverse, current, default)
			: new(ArithmeticOperationKind.Multiply, current, inverse, default);
		QuantityGenerationPlan plan = QuantityDiscovery.Discover([operation]);
		string source = $$"""
			public static class Consumer
			{
				public static double Value() => {{expression}};
			}
			""";
		Assembly assembly = await CompileAndEmit(source, plan);
		double result = (double)assembly.GetType("Consumer")!.GetMethod("Value")!.Invoke(null, null)!;
		double expected = 6;

		await Assert.That(result).IsEqualTo(expected);
	}

	private static async Task<Assembly> CompileAndEmit(string source, QuantityGenerationPlan plan)
	{
		string[] assemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = assemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		CSharpCompilation input = CSharpCompilation.Create(
			"ExplicitArithmeticConsumer",
			[CSharpSyntaxTree.ParseText(source, _parseOptions, path: "Consumer.cs")],
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		SyntaxTree knownQuantities = CSharpSyntaxTree.ParseText(QuantitySourceEmitter.Emit(plan.KnownQuantities), _parseOptions, path: "KnownQuantities.g.cs");
		SyntaxTree additionalQuantities = CSharpSyntaxTree.ParseText(QuantitySourceEmitter.Emit(plan.AdditionalQuantities), _parseOptions, path: "AdditionalQuantities.g.cs");
		SyntaxTree arithmetic = CSharpSyntaxTree.ParseText(ArithmeticSourceEmitter.Emit(plan), _parseOptions, path: "Arithmetic.g.cs");
		CSharpCompilation compilation = input.AddSyntaxTrees(knownQuantities, additionalQuantities, arithmetic);
		Diagnostic[] errors = compilation.GetDiagnostics(TestContext.Current!.Execution.CancellationToken)
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
		await Assert.That(errors).IsEmpty();

		using MemoryStream assemblyBytes = new();
		EmitResult emit = compilation.Emit(assemblyBytes, cancellationToken: TestContext.Current!.Execution.CancellationToken);
		await Assert.That(emit.Success).IsTrue().Because(string.Join(Environment.NewLine, emit.Diagnostics));
		return Assembly.Load(assemblyBytes.ToArray());
	}
}
