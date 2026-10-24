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

public sealed class PowerSourceEmitterTests
{
	private static readonly CSharpParseOptions _parseOptions = new(LanguageVersion.Preview);

	[Fact]
	public void DifferentPowersReturnTheExpectedQuantityTypes()
	{
		Dimension length = new(Length: 1);
		PowerOperation[] operations =
		[
			new(length, 2, new(Length: 2), "global::DimensionalQuantities.TypeSystem.TerminalHex2"),
			new(length, 3, new(Length: 3), "global::DimensionalQuantities.TypeSystem.TerminalHex3"),
			new(new(Length: 2), new Rational(1, 2), length, "global::DimensionalQuantities.TypeSystem.Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex2>"),
			new(length, 0, default, "global::DimensionalQuantities.TypeSystem.TerminalHex0")
		];
		QuantityGenerationPlan plan = QuantityDiscovery.Discover([], operations);
		const string source = """
			using DimensionalQuantities.TypeSystem;
			public static class Consumer
			{
				public static double Value()
				{
					Length length = Length.FromMetres(3);
					Area area = Power<TerminalHex2>.Of(length);
					Volume volume = Power<TerminalHex3>.Of(length);
					Length root = Power<Fraction<TerminalHex1, TerminalHex2>>.Of(area);
					double identity = Power<TerminalHex0>.Of(length);
					return area.AsSiBaseUnit() + volume.AsSiBaseUnit() + root.AsSiBaseUnit() + identity;
				}
			}
			""";
		Assembly assembly = CompileAndEmit(source, plan);
		double result = (double)assembly.GetType("Consumer")!.GetMethod("Value")!.Invoke(null, null)!;
		double expected = 40;

		Assert.Equal(expected, result);
	}

	private static Assembly CompileAndEmit(string source, QuantityGenerationPlan plan)
	{
		string[] assemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = assemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		CSharpCompilation input = CSharpCompilation.Create(
			"ExplicitPowerConsumer",
			[CSharpSyntaxTree.ParseText(source, _parseOptions, path: "Consumer.cs")],
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		SyntaxTree knownQuantities = CSharpSyntaxTree.ParseText(QuantitySourceEmitter.Emit(plan.KnownQuantities), _parseOptions, path: "KnownQuantities.g.cs");
		SyntaxTree additionalQuantities = CSharpSyntaxTree.ParseText(QuantitySourceEmitter.Emit(plan.AdditionalQuantities), _parseOptions, path: "AdditionalQuantities.g.cs");
		SyntaxTree powers = CSharpSyntaxTree.ParseText(PowerSourceEmitter.Emit(plan), _parseOptions, path: "Powers.g.cs");
		CSharpCompilation compilation = input.AddSyntaxTrees(knownQuantities, additionalQuantities, powers);
		Diagnostic[] errors = compilation.GetDiagnostics(TestContext.Current.CancellationToken)
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
		Assert.Empty(errors);

		using MemoryStream assemblyBytes = new();
		EmitResult emit = compilation.Emit(assemblyBytes, cancellationToken: TestContext.Current.CancellationToken);
		Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
		return Assembly.Load(assemblyBytes.ToArray());
	}
}
