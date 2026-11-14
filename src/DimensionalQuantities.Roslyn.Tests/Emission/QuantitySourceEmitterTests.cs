using System.Reflection;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.Roslyn.Generators;
using DimensionalQuantities.TypeSystem;
using DimensionalQuantities.Units;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Emission;

public sealed class QuantitySourceEmitterTests
{
	[Fact]
	public void StandaloneTemperatureUsesTheDimensionAlias()
	{
		string source = QuantitySourceEmitter.Emit([new(new(Temperature: 1), ["Temperature"], [], false)]);
		Assert.Contains("global using Temperature =", source);
	}

	[Fact]
	public void MechanicalQuantityUsesItsGenericPrefixConversion()
	{
		PlannedQuantity quantity = new(new(Mass: 2), ["MassSquared"],
			[new UnitDefinition("MassSquared", [new(AtomicUnits.Gram, power: 2, prefixExponent: 3)])], false);
		CSharpCompilation compilation = Compile(
			"""
			public static class Consumer
			{
				public static double Value() => MassSquared.FromPrefixedUnits(2, gramPrefixExponent: 0).AsSiBaseUnit();
			}
			""", QuantitySourceEmitter.Emit([quantity]));
		Assert.Equal(0.000002, Execute<double>(compilation), 12);
	}

	[Fact]
	public void QuantityWithoutNamedConversionsOnlyHasGenericPrefixConversions()
	{
		PlannedQuantity quantity = new(new(ElectricCurrent: 2), ["CurrentSquared"],
			[new UnitDefinition("CurrentSquared", [new(AtomicUnits.Amp, power: 2)])], false);
		CSharpCompilation compilation = Compile("", QuantitySourceEmitter.Emit([quantity]));
		string[] methods = compilation.GetTypeByMetadataName("DimensionalQuantities.Generated.CurrentSquaredExtensions")!
			.GetMembers().OfType<IMethodSymbol>().Select(method => method.Name).ToArray();
		Assert.Multiple(
			() => Assert.Contains("FromPrefixedUnits", methods),
			() => Assert.Contains("AsPrefixedUnits", methods),
			() => Assert.DoesNotContain(methods, method => method.StartsWith("FromSquare", StringComparison.Ordinal)),
			() => Assert.DoesNotContain(methods, method => method.StartsWith("AsSquare", StringComparison.Ordinal)));
	}

	[Theory]
	[InlineData("Length", "TerminalHex1,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0", 1, 0, 0, 0, 0, 0, 0)]
	[InlineData("Mass", "TerminalHex0,TerminalHex1,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0", 0, 1, 0, 0, 0, 0, 0)]
	[InlineData("Time", "TerminalHex0,TerminalHex0,TerminalHex1,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0", 0, 0, 1, 0, 0, 0, 0)]
	[InlineData("Current", "TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex1,TerminalHex0,TerminalHex0,TerminalHex0", 0, 0, 0, 1, 0, 0, 0)]
	[InlineData("Temperature", "TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex1,TerminalHex0,TerminalHex0", 0, 0, 0, 0, 1, 0, 0)]
	[InlineData("AmountOfSubstance", "TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex1,TerminalHex0", 0, 0, 0, 0, 0, 1, 0)]
	[InlineData("LuminousIntensity", "TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex1", 0, 0, 0, 0, 0, 0, 1)]
	[InlineData("Area", "TerminalHex2,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0", 2, 0, 0, 0, 0, 0, 0)]
	[InlineData("Volume", "TerminalHex3,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0", 3, 0, 0, 0, 0, 0, 0)]
	[InlineData("Speed", "TerminalHex1,TerminalHex0,DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0", 1, 0, -1, 0, 0, 0, 0)]
	[InlineData("Acceleration", "TerminalHex1,TerminalHex0,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0", 1, 0, -2, 0, 0, 0, 0)]
	[InlineData("Frequency", "TerminalHex0,TerminalHex0,DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0", 0, 0, -1, 0, 0, 0, 0)]
	[InlineData("AngularVelocity", "TerminalHex0,TerminalHex0,DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0", 0, 0, -1, 0, 0, 0, 0)]
	[InlineData("Force", "TerminalHex1,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0", 1, 1, -2, 0, 0, 0, 0)]
	[InlineData("Density", "DimensionalQuantities.TypeSystem.Negative<TerminalHex3>,TerminalHex1,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0", -3, 1, 0, 0, 0, 0, 0)]
	[InlineData("Pressure", "DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0", -1, 1, -2, 0, 0, 0, 0)]
	[InlineData("Stress", "DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0", -1, 1, -2, 0, 0, 0, 0)]
	[InlineData("YoungsModulus", "DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0", -1, 1, -2, 0, 0, 0, 0)]
	[InlineData("Energy", "TerminalHex2,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0", 2, 1, -2, 0, 0, 0, 0)]
	[InlineData("Torque", "TerminalHex2,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0", 2, 1, -2, 0, 0, 0, 0)]
	[InlineData("Power", "TerminalHex2,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex3>,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0", 2, 1, -3, 0, 0, 0, 0)]
	[InlineData("SpecificPower", "TerminalHex2,TerminalHex0,DimensionalQuantities.TypeSystem.Negative<TerminalHex3>,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0", 2, 0, -3, 0, 0, 0, 0)]
	[InlineData("SpecificPowerLoss", "TerminalHex2,TerminalHex0,DimensionalQuantities.TypeSystem.Negative<TerminalHex3>,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0", 2, 0, -3, 0, 0, 0, 0)]
	[InlineData("TemperatureCoefficient", "TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0,DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex0,TerminalHex0", 0, 0, 0, 0, -1, 0, 0)]
	[InlineData("PowerDensity", "DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex3>,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0", -1, 1, -3, 0, 0, 0, 0)]
	[InlineData("VolumetricPowerLoss", "DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex3>,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0", -1, 1, -3, 0, 0, 0, 0)]
	[InlineData("ElectricCharge", "TerminalHex0,TerminalHex0,TerminalHex1,TerminalHex1,TerminalHex0,TerminalHex0,TerminalHex0", 0, 0, 1, 1, 0, 0, 0)]
	[InlineData("Voltage", "TerminalHex2,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex3>,DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex0,TerminalHex0,TerminalHex0", 2, 1, -3, -1, 0, 0, 0)]
	[InlineData("Capacitance", "DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex4,TerminalHex2,TerminalHex0,TerminalHex0,TerminalHex0", -2, -1, 4, 2, 0, 0, 0)]
	[InlineData("Resistance", "TerminalHex2,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex3>,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,TerminalHex0,TerminalHex0,TerminalHex0", 2, 1, -3, -2, 0, 0, 0)]
	[InlineData("Inductance", "TerminalHex2,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,TerminalHex0,TerminalHex0,TerminalHex0", 2, 1, -2, -2, 0, 0, 0)]
	[InlineData("MagneticFlux", "TerminalHex2,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex0,TerminalHex0,TerminalHex0", 2, 1, -2, -1, 0, 0, 0)]
	[InlineData("FluxLinkage", "TerminalHex2,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex0,TerminalHex0,TerminalHex0", 2, 1, -2, -1, 0, 0, 0)]
	[InlineData("MagneticFluxDensity", "TerminalHex0,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex0,TerminalHex0,TerminalHex0", 0, 1, -2, -1, 0, 0, 0)]
	[InlineData("MagneticPolarisation", "TerminalHex0,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex0,TerminalHex0,TerminalHex0", 0, 1, -2, -1, 0, 0, 0)]
	[InlineData("RemanentFluxDensity", "TerminalHex0,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex0,TerminalHex0,TerminalHex0", 0, 1, -2, -1, 0, 0, 0)]
	[InlineData("MagneticFieldStrength", "DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex0,TerminalHex0,TerminalHex1,TerminalHex0,TerminalHex0,TerminalHex0", -1, 0, 0, 1, 0, 0, 0)]
	[InlineData("CoerciveFieldStrength", "DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex0,TerminalHex0,TerminalHex1,TerminalHex0,TerminalHex0,TerminalHex0", -1, 0, 0, 1, 0, 0, 0)]
	[InlineData("CurrentDensity", "DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,TerminalHex0,TerminalHex0,TerminalHex1,TerminalHex0,TerminalHex0,TerminalHex0", -2, 0, 0, 1, 0, 0, 0)]
	[InlineData("Permeability", "TerminalHex1,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,TerminalHex0,TerminalHex0,TerminalHex0", 1, 1, -2, -2, 0, 0, 0)]
	[InlineData("Resistivity", "TerminalHex3,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex3>,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,TerminalHex0,TerminalHex0,TerminalHex0", 3, 1, -3, -2, 0, 0, 0)]
	[InlineData("Conductivity", "DimensionalQuantities.TypeSystem.Negative<TerminalHex3>,DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex3,TerminalHex2,TerminalHex0,TerminalHex0,TerminalHex0", -3, -1, 3, 2, 0, 0, 0)]
	[InlineData("MagneticReluctivity", "DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex2,TerminalHex2,TerminalHex0,TerminalHex0,TerminalHex0", -1, -1, 2, 2, 0, 0, 0)]
	[InlineData("MagneticVectorPotential", "TerminalHex1,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex0,TerminalHex0,TerminalHex0", 1, 1, -2, -1, 0, 0, 0)]
	public void ExplicitAliasesHaveCanonicalTargets(string alias, string exponents, int length, int mass, int time,
		int current, int temperature, int amount, int intensity)
	{
		PlannedQuantity quantity = new(new(length, mass, time, current, temperature, amount, intensity), [alias], [], false);
		CSharpCompilation compilation = Compile(
			$$"""
			class Consumer
			{
				{{alias}} Value;
			}
			""", QuantitySourceEmitter.Emit([quantity]));
		IFieldSymbol field = compilation.GetTypeByMetadataName("Consumer")!.GetMembers("Value").OfType<IFieldSymbol>().Single();
		Assert.Equal($"DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<{exponents}>>".Replace(" ", ""),
			field.Type.ToDisplayString().Replace("DimensionalQuantities.TypeSystem.TerminalHex", "TerminalHex").Replace(" ", ""));
	}

	[Theory]
	[InlineData("Area", "LengthSquared", 2, 0, 0, 0)]
	[InlineData("Voltage", "LengthSquaredMassPerTimeCubedCurrent", 2, 1, -3, -1)]
	[InlineData("CurrentDensity", "CurrentPerLengthSquared", -2, 0, 0, 1)]
	[InlineData("Energy", "Torque", 2, 1, -2, 0)]
	[InlineData("Frequency", "AngularVelocity", 0, 0, -1, 0)]
	[InlineData("Pressure", "Stress", -1, 1, -2, 0)]
	[InlineData("Pressure", "YoungsModulus", -1, 1, -2, 0)]
	[InlineData("Stress", "YoungsModulus", -1, 1, -2, 0)]
	[InlineData("SpecificPower", "SpecificPowerLoss", 2, 0, -3, 0)]
	[InlineData("PowerDensity", "VolumetricPowerLoss", -1, 1, -3, 0)]
	[InlineData("MagneticFlux", "FluxLinkage", 2, 1, -2, -1)]
	[InlineData("MagneticFluxDensity", "MagneticPolarisation", 0, 1, -2, -1)]
	[InlineData("MagneticPolarisation", "RemanentFluxDensity", 0, 1, -2, -1)]
	[InlineData("MagneticFieldStrength", "CoerciveFieldStrength", -1, 0, 0, 1)]
	public void SynonymsShareRuntimeIdentity(string left, string right, int length, int mass, int time, int current)
	{
		PlannedQuantity quantity = new(new(length, mass, time, current), [left, right], [], false);
		CSharpCompilation compilation = Compile(
			$$"""
			public static class Consumer
			{
				public static bool Value() => typeof({{left}}) == typeof({{right}});
			}
			""", QuantitySourceEmitter.Emit([quantity]));
		Assert.True(Execute<bool>(compilation));
	}

	internal static CSharpCompilation Compile(string consumer, params string[] fragments)
	{
		CSharpParseOptions options = new(LanguageVersion.Preview);
		string[] paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = paths.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		CSharpCompilation compilation = CSharpCompilation.Create("EmitterConsumer" + Guid.NewGuid().ToString("N"),
			new[] { consumer }.Concat(fragments).Select((source, index) => CSharpSyntaxTree.ParseText(source, options, path: $"Fragment{index}.cs")),
			references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		Diagnostic[] errors = compilation.GetDiagnostics(TestContext.Current.CancellationToken)
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
		if (errors.Length != 0)
		{
			throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
		}
		return compilation;
	}

	internal static T Execute<T>(CSharpCompilation compilation, string consumer = "Consumer")
	{
		using MemoryStream bytes = new();
		Microsoft.CodeAnalysis.Emit.EmitResult result = compilation.Emit(bytes, cancellationToken: TestContext.Current.CancellationToken);
		if (!result.Success)
		{
			throw new InvalidOperationException(string.Join(Environment.NewLine, result.Diagnostics));
		}
		Assembly assembly = Assembly.Load(bytes.ToArray());
		return (T)assembly.GetType(consumer)!.GetMethod("Value")!.Invoke(null, null)!;
	}
}
