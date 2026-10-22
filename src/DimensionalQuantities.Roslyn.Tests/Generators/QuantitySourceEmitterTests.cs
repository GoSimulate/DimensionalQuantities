using System.Reflection;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.Roslyn.Generators;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generators;

public sealed class QuantitySourceEmitterTests
{
	private static readonly CSharpParseOptions _parseOptions = new(LanguageVersion.CSharp14);
	private static readonly string[] _prefixes =
	[
		"Quetta", "Ronna", "Yotta", "Zetta", "Exa", "Peta", "Tera", "Giga", "Mega", "Kilo", "Hecto", "Deca",
		"Deci", "Centi", "Milli", "Micro", "Nano", "Pico", "Femto", "Atto", "Zepto", "Yocto", "Ronto", "Quecto"
	];

	[Fact]
	public void DiscoveredMechanicalQuantityUsesItsGenericPrefixConversion()
	{
		Dimension mass = new(Mass: 1);
		Dimension massSquared = new(Mass: 2);
		ArithmeticOperation operation = new(ArithmeticOperationKind.Multiply, mass, mass, massSquared);
		QuantityGenerationPlan plan = QuantityDiscovery.Discover([operation]);
		const string source = "public static class Consumer { public static double Value() => MassSquared.FromPrefixedUnits(2, gramPrefixExponent: 0).AsSiBaseUnit(); }";
		Assembly assembly = CompileAndEmit(source, plan.KnownQuantities, plan.AdditionalQuantities);
		Type consumer = assembly.GetType("Consumer")!;
		double result = (double)consumer.GetMethod("Value")!.Invoke(null, null)!;

		Assert.Equal(0.000002, result, 12);
	}

	[Fact]
	public void DiscoveredQuantityOnlyHasGenericPrefixConversions()
	{
		Dimension current = new(ElectricCurrent: 1);
		Dimension currentSquared = new(ElectricCurrent: 2);
		ArithmeticOperation operation = new(ArithmeticOperationKind.Multiply, current, current, currentSquared);
		QuantityGenerationPlan plan = QuantityDiscovery.Discover([operation]);
		CSharpCompilation compilation = Compile(QuantitySourceEmitter.Emit(plan.AdditionalQuantities));
		INamedTypeSymbol extensions = compilation.GetTypeByMetadataName("DimensionalQuantities.Generated.CurrentSquaredExtensions")!;
		string[] methods = extensions.GetMembers().OfType<IMethodSymbol>().Select(method => method.Name).ToArray();

		Assert.Multiple(
			() => Assert.Contains("FromPrefixedUnits", methods),
			() => Assert.Contains("AsPrefixedUnits", methods),
			() => Assert.DoesNotContain(methods, method => method.StartsWith("FromSquare", StringComparison.Ordinal)),
			() => Assert.DoesNotContain(methods, method => method.StartsWith("AsSquare", StringComparison.Ordinal)));
	}

	[Fact]
	public void VoltageAndMechanicalAliasAreTheSameType()
	{
		const string expression = "typeof(Voltage) == typeof(LengthSquaredMassPerTimeCubedCurrent)";
		bool result = Evaluate<bool>(expression);
		Assert.True(result);
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
		INamespaceSymbol generated = result.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthExtensions")!.ContainingNamespace;
		string[] names = generated.GetTypeMembers().SelectMany(type => type.GetMembers().OfType<IMethodSymbol>()).Select(method => method.Name).ToArray();

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

	[Theory]
	[InlineData("Voltage.FromPrefixedUnits(2, -3).AsSiBaseUnit()", 0.002)]
	[InlineData("CurrentDensity.FromPrefixedUnits(1, ampPrefixExponent: 0, metrePrefixExponent: -3).AsSiBaseUnit()", 1000000)]
	[InlineData("SpecificPower.FromPrefixedUnits(1, wattPrefixExponent: 3, gramPrefixExponent: 3).AsSiBaseUnit()", 1000)]
	[InlineData("TemperatureCoefficient.FromPrefixedUnits(1, kelvinPrefixExponent: -3).AsSiBaseUnit()", 1000)]
	[InlineData("Mass.FromPrefixedUnits(2).AsSiBaseUnit()", 2)]
	[InlineData("Torque.FromPrefixedUnits(2).AsSiBaseUnit()", 2)]
	public void PrefixedUnitFactoriesUseCanonicalUnitFactors(string expression, double expected)
	{
		double result = Evaluate<double>(expression);
		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("Voltage.FromSiBaseUnit(0.002).AsPrefixedUnits(-3)", 2)]
	[InlineData("CurrentDensity.FromSiBaseUnit(1000000).AsPrefixedUnits(ampPrefixExponent: 0, metrePrefixExponent: -3)", 1)]
	[InlineData("Mass.FromSiBaseUnit(1).AsPrefixedUnits(gramPrefixExponent: 0)", 1000)]
	[InlineData("Torque.FromSiBaseUnit(2).AsPrefixedUnits()", 2)]
	public void PrefixedUnitAccessorsUseCanonicalUnitFactors(string expression, double expected)
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
		Assembly assembly = CompileAndEmit(source, QuantityDiscovery.Discover([]).KnownQuantities, []);
		MethodInfo method = assembly.GetType("Consumer")!.GetMethod("Value")!;
		return Assert.IsType<T>(method.Invoke(null, null));
	}

	private static CSharpCompilation Compile(string source)
	{
		SyntaxTree generated = CSharpSyntaxTree.ParseText(
			QuantitySourceEmitter.Emit(QuantityDiscovery.Discover([]).KnownQuantities),
			_parseOptions,
			path: "KnownQuantities.g.cs");
		CSharpCompilation compilation = CreateInputCompilation(source).AddSyntaxTrees(generated);
		Diagnostic[] errors = compilation.GetDiagnostics(TestContext.Current.CancellationToken)
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
		Assert.Empty(errors);
		return compilation;
	}

	private static Assembly CompileAndEmit(string source, IReadOnlyList<PlannedQuantity> known, IReadOnlyList<PlannedQuantity> additional)
	{
		SyntaxTree knownQuantities = CSharpSyntaxTree.ParseText(QuantitySourceEmitter.Emit(known), _parseOptions, path: "KnownQuantities.g.cs");
		SyntaxTree additionalQuantities = CSharpSyntaxTree.ParseText(QuantitySourceEmitter.Emit(additional), _parseOptions, path: "AdditionalQuantities.g.cs");
		CSharpCompilation compilation = CreateInputCompilation(source).AddSyntaxTrees(knownQuantities, additionalQuantities);
		Diagnostic[] errors = compilation.GetDiagnostics(TestContext.Current.CancellationToken)
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
		Assert.Empty(errors);

		using MemoryStream assemblyBytes = new();
		EmitResult emit = compilation.Emit(assemblyBytes, cancellationToken: TestContext.Current.CancellationToken);
		Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
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
