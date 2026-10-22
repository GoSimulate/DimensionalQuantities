using System.Collections.Immutable;
using System.Reflection;
using DimensionalQuantities.Roslyn.Generators;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generators;

public sealed class QuantityGeneratorTests
{
	private static readonly CSharpParseOptions _parseOptions = new(LanguageVersion.CSharp14);
	private static readonly string[] _prefixes =
	[
		"Quetta", "Ronna", "Yotta", "Zetta", "Exa", "Peta", "Tera", "Giga", "Mega", "Kilo", "Hecto", "Deca",
		"Deci", "Centi", "Milli", "Micro", "Nano", "Pico", "Femto", "Atto", "Zepto", "Yocto", "Ronto", "Quecto"
	];

	[Fact]
	public void BaseAliasesHaveUnprefixedFactoriesAndAccessors()
	{
		const string source = """
			public static class Consumer
			{
				public static double UseBaseQuantities()
				{
					Length length = Length.FromMetres(1);
					Mass mass = Mass.FromGrams(1000);
					Time time = Time.FromSeconds(1);
					Current current = Current.FromAmps(1);
					Temperature temperature = Temperature.FromKelvin(1);
					AmountOfSubstance amount = AmountOfSubstance.FromMoles(1);
					LuminousIntensity intensity = LuminousIntensity.FromCandelas(1);
					return length.AsMetres() + mass.AsGrams() + time.AsSeconds() + current.AsAmps()
						+ temperature.AsKelvin() + amount.AsMoles() + intensity.AsCandelas();
				}
			}
			""";

		Compile(source);
	}

	[Fact]
	public void VoltageAndMechanicalAliasAreTheSameType()
	{
		const string expression = "typeof(Voltage) == typeof(LengthSquaredMassPerTimeCubedCurrent)";
		bool result = Evaluate<bool>(expression);
		Assert.True(result);
	}

	[Theory]
	[InlineData("Length", "Metres")]
	[InlineData("Mass", "Grams")]
	[InlineData("Time", "Seconds")]
	[InlineData("Current", "Amps")]
	[InlineData("Temperature", "Kelvin")]
	[InlineData("AmountOfSubstance", "Moles")]
	[InlineData("LuminousIntensity", "Candelas")]
	[InlineData("Voltage", "Volts")]
	public void EverySiPrefixHasUsableFactoryAndAccessor(string quantity, string unit)
	{
		string calls = string.Join("\n", _prefixes.Select(prefix =>
			$"{quantity} value{prefix} = {quantity}.From{prefix}{unit}(1); double output{prefix} = value{prefix}.As{prefix}{unit}();"));
		string source = $$"""
			public static class Consumer
			{
				public static void UsePrefixes()
				{
					{{calls}}
				}
			}
			""";

		Compile(source);
	}

	[Theory]
	[InlineData("Length.FromMilliMetres(2500)", 2.5)]
	[InlineData("Mass.FromGrams(2500)", 2.5)]
	[InlineData("Mass.FromKiloGrams(2.5)", 2.5)]
	[InlineData("Mass.FromMilliGrams(2500000)", 2.5)]
	[InlineData("Time.FromKiloSeconds(2.5)", 2500)]
	[InlineData("Current.FromMilliAmps(2500)", 2.5)]
	[InlineData("Temperature.FromMilliKelvin(2500)", 2.5)]
	[InlineData("AmountOfSubstance.FromKiloMoles(2.5)", 2500)]
	[InlineData("LuminousIntensity.FromMilliCandelas(2500)", 2.5)]
	[InlineData("Voltage.FromMilliVolts(2500)", 2.5)]
	[InlineData("Voltage.FromKiloVolts(2.5)", 2500)]
	[InlineData("Voltage.FromRontoVolts(1)", 1e-27)]
	[InlineData("Voltage.FromQuettaVolts(1)", 1e30)]
	[InlineData("Voltage.FromQuectoVolts(1)", 1e-30)]
	public void FactoryStoresCoherentSiMagnitude(string factory, double expected)
	{
		double result = Evaluate<double>($"{factory}.AsSiBaseUnit()");
		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("Mass.FromSiBaseUnit(2.5).AsGrams()", 2500)]
	[InlineData("Mass.FromSiBaseUnit(2.5).AsKiloGrams()", 2.5)]
	[InlineData("Mass.FromSiBaseUnit(2.5).AsMilliGrams()", 2500000)]
	[InlineData("Voltage.FromSiBaseUnit(2.5).AsMilliVolts()", 2500)]
	[InlineData("Voltage.FromSiBaseUnit(2.5).AsKiloVolts()", 0.0025)]
	[InlineData("Voltage.FromSiBaseUnit(2.5).AsDeciVolts()", 25)]
	[InlineData("Voltage.FromSiBaseUnit(2.5).AsHectoVolts()", 0.025)]
	public void AccessorConvertsFromCoherentSiMagnitude(string expression, double expected)
	{
		double result = Evaluate<double>(expression);
		Assert.Equal(expected, result);
	}

	private static T Evaluate<T>(string expression)
	{
		string source = $$"""
			public static class Consumer
			{
				public static global::{{typeof(T).FullName}} Value() => {{expression}};
			}
			""";
		CSharpCompilation compilation = Compile(source);
		using MemoryStream assemblyBytes = new();
		EmitResult emitResult = compilation.Emit(assemblyBytes, cancellationToken: TestContext.Current.CancellationToken);
		Assert.True(emitResult.Success, string.Join(Environment.NewLine, emitResult.Diagnostics));
		Assembly assembly = Assembly.Load(assemblyBytes.ToArray());
		MethodInfo method = assembly.GetType("Consumer")!.GetMethod("Value")!;
		return Assert.IsType<T>(method.Invoke(null, null));
	}

	private static CSharpCompilation Compile(string source)
	{
		string[] platformAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = platformAssemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		CSharpCompilation input = CSharpCompilation.Create(
			"GeneratedQuantityConsumer",
			[CSharpSyntaxTree.ParseText(source, _parseOptions)],
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		GeneratorDriver driver = CSharpGeneratorDriver.Create([new QuantityGenerator().AsSourceGenerator()], parseOptions: _parseOptions);
		driver.RunGeneratorsAndUpdateCompilation(input, out Compilation output, out ImmutableArray<Diagnostic> generatorDiagnostics);
		Diagnostic[] compilationErrors = output.GetDiagnostics(TestContext.Current.CancellationToken)
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
		Assert.Multiple(
			() => Assert.True(generatorDiagnostics.IsEmpty, string.Join(Environment.NewLine, generatorDiagnostics)),
			() => Assert.True(compilationErrors.Length == 0, string.Join(Environment.NewLine, compilationErrors)));
		return (CSharpCompilation)output;
	}
}
