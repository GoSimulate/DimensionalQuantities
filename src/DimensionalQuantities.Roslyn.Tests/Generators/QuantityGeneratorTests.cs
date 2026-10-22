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

	[Fact]
	public void CatalogueAliasesAreAvailableToConsumers()
	{
		const string source = """
			public static class Consumer
			{
				public static global::System.Type[] Types() =>
				[
					typeof(Area), typeof(Volume), typeof(Frequency), typeof(AngularVelocity), typeof(Density),
					typeof(Pressure), typeof(Stress), typeof(YoungsModulus), typeof(Energy), typeof(Torque),
					typeof(Power), typeof(SpecificPower), typeof(SpecificPowerLoss), typeof(TemperatureCoefficient),
					typeof(PowerDensity), typeof(VolumetricPowerLoss), typeof(Voltage), typeof(Resistance),
					typeof(Inductance), typeof(MagneticFlux), typeof(FluxLinkage), typeof(MagneticFluxDensity),
					typeof(MagneticPolarisation), typeof(RemanentFluxDensity), typeof(MagneticFieldStrength),
					typeof(CoerciveFieldStrength), typeof(CurrentDensity), typeof(Permeability), typeof(Resistivity),
					typeof(Conductivity), typeof(MagneticReluctivity), typeof(MagneticVectorPotential)
				];
			}
			""";

		Compile(source);
	}

	[Fact]
	public void CatalogueUnitsHaveUnprefixedFactoriesAndAccessors()
	{
		const string source = """
			public static class Consumer
			{
				public static double UseUnits() =>
					Area.FromSquareMetres(1).AsSquareMetres() +
					Volume.FromCubicMetres(1).AsCubicMetres() +
					Frequency.FromHertz(1).AsHertz() +
					AngularVelocity.FromRadiansPerSecond(1).AsRadiansPerSecond() +
					Density.FromGramsPerCubicMetre(1).AsGramsPerCubicMetre() +
					Pressure.FromPascals(1).AsPascals() +
					Energy.FromJoules(1).AsJoules() +
					Torque.FromNewtonMetres(1).AsNewtonMetres() +
					Power.FromWatts(1).AsWatts() +
					SpecificPower.FromWattsPerKilogram(1).AsWattsPerKilogram() +
					TemperatureCoefficient.FromInverseKelvin(1).AsInverseKelvin() +
					PowerDensity.FromWattsPerCubicMetre(1).AsWattsPerCubicMetre() +
					Voltage.FromVolts(1).AsVolts() +
					Resistance.FromOhms(1).AsOhms() +
					Inductance.FromHenries(1).AsHenries() +
					MagneticFlux.FromWebers(1).AsWebers() +
					MagneticFluxDensity.FromTeslas(1).AsTeslas() +
					MagneticFieldStrength.FromAmpsPerMetre(1).AsAmpsPerMetre() +
					CurrentDensity.FromAmpsPerSquareMetre(1).AsAmpsPerSquareMetre() +
					Permeability.FromHenriesPerMetre(1).AsHenriesPerMetre() +
					Resistivity.FromOhmMetres(1).AsOhmMetres() +
					Conductivity.FromSiemensPerMetre(1).AsSiemensPerMetre() +
					MagneticReluctivity.FromMetresPerHenry(1).AsMetresPerHenry() +
					MagneticVectorPotential.FromWebersPerMetre(1).AsWebersPerMetre();
			}
			""";

		Compile(source);
	}

	[Theory]
	[InlineData("Area", "LengthSquared")]
	[InlineData("CurrentDensity", "CurrentPerLengthSquared")]
	[InlineData("Pressure", "Stress")]
	[InlineData("Pressure", "YoungsModulus")]
	[InlineData("Energy", "Torque")]
	public void CatalogueAliasesShareTheCanonicalDimensionType(string alias, string equivalent)
	{
		bool result = Evaluate<bool>($"typeof({alias}) == typeof({equivalent})");
		Assert.True(result);
	}

	[Fact]
	public void SharedUnitsAreAvailableThroughEveryAliasWithoutDuplicateHelpers()
	{
		const string source = """
			public static class Consumer
			{
				public static double UsePressureAliases()
				{
					Pressure pressure = Pressure.FromPascals(1);
					Stress stress = Stress.FromPascals(1);
					YoungsModulus modulus = YoungsModulus.FromKiloPascals(1);
					return pressure.AsPascals() + stress.AsPascals() + modulus.AsKiloPascals();
				}
			}
			""";

		Compile(source);
	}

	[Fact]
	public void PrefixesAreOnlyGeneratedForSupportedFactorPlacements()
	{
		const string source = "public static class Consumer { public static double Value() => TemperatureCoefficient.FromInverseKelvin(1).AsInverseKelvin(); }";
		CSharpCompilation result = Compile(source);
		INamedTypeSymbol extensions = result.GetTypeByMetadataName("DimensionalQuantities.Generated.QuantityExtensions")!;
		string[] names = extensions.GetMembers().OfType<IMethodSymbol>().Select(method => method.Name).ToArray();

		Assert.Multiple(
			() => Assert.DoesNotContain("FromInverseMilliKelvin", names),
			() => Assert.DoesNotContain("AsInverseMilliKelvin", names),
			() => Assert.DoesNotContain("FromMilliAmpsPerSquareMilliMetre", names),
			() => Assert.DoesNotContain("FromKiloGramsPerCubicCentiMetre", names));
	}

	[Fact]
	public void EverySiPrefixHasUsableFactoryAndAccessor()
	{
		string calls = string.Join("\n", _prefixes.Select(prefix =>
			$"Length value{prefix} = Length.From{prefix}Metres(1); double output{prefix} = value{prefix}.As{prefix}Metres();"));
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
	[InlineData("Length.FromMilliMetres(2500).AsSiBaseUnit()", 2.5)]
	[InlineData("Voltage.FromSiBaseUnit(2.5).AsKiloVolts()", 0.0025)]
	[InlineData("Mass.FromKiloGrams(2.5).AsSiBaseUnit()", 2.5)]
	[InlineData("Density.FromGramsPerCubicCentiMetre(1).AsSiBaseUnit()", 1000)]
	public void GeneratedConversionsApplyRepresentativeUnitScales(string expression, double expected)
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
