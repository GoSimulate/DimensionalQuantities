using System.Reflection;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Roslyn.Generators;
using DimensionalQuantities.TypeSystem;
using DimensionalQuantities.Units;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generators;

public sealed class UnitConversionSourceEmitterTests
{
	private static readonly CSharpParseOptions _parseOptions = new(LanguageVersion.Preview);
	private static readonly UnitConversionSpec[] _units =
	[
		new("Length", "Length", [new UnitDefinition("Length", [new(AtomicUnits.Metre, prefixExponent: SiBasePrefixExponents.Length)])], true),
		new("Mass", "Mass", [new UnitDefinition("Mass", [new(AtomicUnits.Gram, prefixExponent: SiBasePrefixExponents.Mass)])], true)
	];
	private static readonly string[] _prefixes =
	[
		"Quetta", "Ronna", "Yotta", "Zetta", "Exa", "Peta", "Tera", "Giga", "Mega", "Kilo", "Hecto", "Deca",
		"Deci", "Centi", "Milli", "Micro", "Nano", "Pico", "Femto", "Atto", "Zepto", "Yocto", "Ronto", "Quecto"
	];

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

		CSharpCompilation compilation = Compile(source);
		Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken)
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
	}

	[Theory]
	[InlineData("Length.FromMilliMetres(2500).AsSiBaseUnit()", 2.5)]
	[InlineData("Mass.FromKiloGrams(2.5).AsSiBaseUnit()", 2.5)]
	[InlineData("Mass.FromPrefixedUnits(2).AsSiBaseUnit()", 2)]
	[InlineData("Mass.FromSiBaseUnit(1).AsPrefixedUnits(gramPrefixExponent: 0)", 1000)]
	public void BaseUnitPrefixesKeepTheirSiScales(string expression, double expected)
	{
		string source = $$"""
			public static class Consumer
			{
				public static double Value() => {{expression}};
			}
			""";
		CSharpCompilation compilation = Compile(source);
		using MemoryStream assemblyBytes = new();
		EmitResult emit = compilation.Emit(assemblyBytes, cancellationToken: TestContext.Current.CancellationToken);
		if (!emit.Success)
		{
			throw new InvalidOperationException(string.Join(Environment.NewLine, emit.Diagnostics.Select(diagnostic => diagnostic.ToString())));
		}

		Assembly assembly = Assembly.Load(assemblyBytes.ToArray());
		MethodInfo method = assembly.GetType("Consumer")!.GetMethod("Value")!;
		double result = (double)method.Invoke(null, null)!;
		Assert.Equal(expected, result);
	}

	private static CSharpCompilation Compile(string source)
	{
		string lengthType = QuantityTypeNameFormatter.Format(new Dimension(Length: 1));
		string massType = QuantityTypeNameFormatter.Format(new Dimension(Mass: 1));
		string generatedSource = $"""
			global using Length = {lengthType};
			global using Mass = {massType};
			global using DimensionalQuantities.Generated;

			{UnitConversionSourceEmitter.Emit(_units, "DimensionalQuantities.Generated", "internal")}
			""";
		string[] platformAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = platformAssemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		CSharpCompilation compilation = CSharpCompilation.Create("UnitConversionSourceEmitterConsumer",
			[
				CSharpSyntaxTree.ParseText(source, _parseOptions, path: "Consumer.cs"),
				CSharpSyntaxTree.ParseText(generatedSource, _parseOptions, path: "Conversions.g.cs")
			], references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		return compilation;
	}
}
