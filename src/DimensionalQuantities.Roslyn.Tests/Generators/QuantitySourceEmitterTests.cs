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

[NotInParallel(nameof(QuantitySourceEmitterTests))]
public sealed class QuantitySourceEmitterTests
{
	private static readonly CSharpParseOptions _parseOptions = new(LanguageVersion.Preview);

	[Test]
	public async Task StandaloneTemperatureUsesTheDimensionAlias()
	{
		string source = QuantitySourceEmitter.Emit(QuantityDiscovery.Discover([]).KnownQuantities);
		await Assert.That(source).Contains("global using Temperature =");
	}

	[Test]
	public async Task DiscoveredMechanicalQuantityUsesItsGenericPrefixConversion()
	{
		Dimension mass = new(Mass: 1);
		Dimension massSquared = new(Mass: 2);
		ArithmeticOperation operation = new(ArithmeticOperationKind.Multiply, mass, mass, massSquared);
		QuantityGenerationPlan plan = QuantityDiscovery.Discover([operation]);
		const string source = "public static class Consumer { public static double Value() => MassSquared.FromPrefixedUnits(2, gramPrefixExponent: 0).AsSiBaseUnit(); }";
		Assembly assembly = CompileAndEmit(source, plan.KnownQuantities, plan.AdditionalQuantities);
		Type consumer = assembly.GetType("Consumer")!;
		double result = (double)consumer.GetMethod("Value")!.Invoke(null, null)!;

		await Assert.That(Math.Round(result, 12)).IsEqualTo(Math.Round(0.000002, 12));
	}

	[Test]
	public async Task DiscoveredQuantityOnlyHasGenericPrefixConversions()
	{
		Dimension current = new(ElectricCurrent: 1);
		Dimension currentSquared = new(ElectricCurrent: 2);
		ArithmeticOperation operation = new(ArithmeticOperationKind.Multiply, current, current, currentSquared);
		QuantityGenerationPlan plan = QuantityDiscovery.Discover([operation]);
		CSharpCompilation compilation = Compile(QuantitySourceEmitter.Emit(plan.AdditionalQuantities));
		INamedTypeSymbol extensions = compilation.GetTypeByMetadataName("DimensionalQuantities.Generated.CurrentSquaredExtensions")!;
		string[] methods = extensions.GetMembers().OfType<IMethodSymbol>().Select(method => method.Name).ToArray();

		using (Assert.Multiple())
		{
			await Assert.That(methods).Contains("FromPrefixedUnits");
			await Assert.That(methods).Contains("AsPrefixedUnits");
			await Assert.That(methods.Any(method => method.StartsWith("FromSquare", StringComparison.Ordinal))).IsFalse();
			await Assert.That(methods.Any(method => method.StartsWith("AsSquare", StringComparison.Ordinal))).IsFalse();
		}
	}

	[Test]
	public async Task VoltageAndMechanicalAliasAreTheSameType()
	{
		const string expression = "typeof(Voltage) == typeof(LengthSquaredMassPerTimeCubedCurrent)";
		bool result = Evaluate<bool>(expression);
		await Assert.That(result).IsTrue();
	}

	[Test]
	[Arguments("Area", "LengthSquared")]
	[Arguments("CurrentDensity", "CurrentPerLengthSquared")]
	[Arguments("Pressure", "Stress")]
	[Arguments("Pressure", "YoungsModulus")]
	[Arguments("Energy", "Torque")]
	public async Task CatalogueAliasesShareTheCanonicalDimensionType(string alias, string equivalent)
	{
		bool result = Evaluate<bool>($"typeof({alias}) == typeof({equivalent})");
		await Assert.That(result).IsTrue();
	}

	private static T Evaluate<T>(string expression)
	{
		string source = $$"""
			public static class Consumer
			{
				public static global::{{typeof(T).FullName}} Value() => {{expression}};
			}
			""";
		Assembly assembly = CompileAndEmit(source, QuantityDiscovery.Discover([]).KnownQuantities, []);
		MethodInfo method = assembly.GetType("Consumer")!.GetMethod("Value")!;
		return (T)method.Invoke(null, null)!;
	}

	private static CSharpCompilation Compile(string source)
	{
		SyntaxTree generated = CSharpSyntaxTree.ParseText(
			QuantitySourceEmitter.Emit(QuantityDiscovery.Discover([]).KnownQuantities),
			_parseOptions,
			path: "KnownQuantities.g.cs");
		CSharpCompilation compilation = CreateInputCompilation(source).AddSyntaxTrees(generated);
		Diagnostic[] errors = compilation.GetDiagnostics(TestContext.Current!.Execution.CancellationToken)
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
		if (errors.Length != 0)
		{
			throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
		}
		return compilation;
	}

	private static Assembly CompileAndEmit(string source, IReadOnlyList<PlannedQuantity> known, IReadOnlyList<PlannedQuantity> additional)
	{
		SyntaxTree knownQuantities = CSharpSyntaxTree.ParseText(QuantitySourceEmitter.Emit(known), _parseOptions, path: "KnownQuantities.g.cs");
		SyntaxTree additionalQuantities = CSharpSyntaxTree.ParseText(QuantitySourceEmitter.Emit(additional), _parseOptions, path: "AdditionalQuantities.g.cs");
		CSharpCompilation compilation = CreateInputCompilation(source).AddSyntaxTrees(knownQuantities, additionalQuantities);
		Diagnostic[] errors = compilation.GetDiagnostics(TestContext.Current!.Execution.CancellationToken)
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
		if (errors.Length != 0)
		{
			throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
		}

		using MemoryStream assemblyBytes = new();
		EmitResult emit = compilation.Emit(assemblyBytes, cancellationToken: TestContext.Current!.Execution.CancellationToken);
		if (!emit.Success)
		{
			throw new InvalidOperationException(string.Join(Environment.NewLine, emit.Diagnostics.Select(diagnostic => diagnostic.ToString())));
		}
		return Assembly.Load(assemblyBytes.ToArray());
	}

	private static CSharpCompilation CreateInputCompilation(string source)
	{
		string[] platformAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = platformAssemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		return CSharpCompilation.Create(
			"QuantitySourceEmitterConsumer",
			[CSharpSyntaxTree.ParseText(source, _parseOptions, path: "Consumer.cs")],
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
	}
}
