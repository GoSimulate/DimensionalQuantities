using System.Reflection;
using System.Globalization;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Roslyn.Generators;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.TypeSystem;
using DimensionalQuantities.Units;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Emission;

public sealed class UnitConversionSourceEmitterTests
{
	private static readonly CSharpParseOptions _parseOptions = new(LanguageVersion.Preview);
	[Theory]
	[InlineData("Length.FromMilliMetres(2500).AsSiBaseUnit()", 2.5)]
	[InlineData("Mass.FromKiloGrams(2.5).AsSiBaseUnit()", 2.5)]
	[InlineData("Mass.FromPrefixedUnits(2).AsSiBaseUnit()", 2)]
	[InlineData("Mass.FromSiBaseUnit(1).AsPrefixedUnits(gramPrefixExponent: 0)", 1000)]
	[InlineData("Length.FromMilliMetres(2500).AsDelta().AsMilliMetres()", 2500)]
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

	[Theory]
	[InlineData("Temperature.FromCelsius(20).AsCelsius()", 20)]
	[InlineData("Temperature.FromFahrenheit(68).AsCelsius()", 20)]
	[InlineData("(Temperature.FromCelsius(20) - Temperature.FromCelsius(10)).AsDelta().AsCelsius()", 10)]
	[InlineData("(Temperature.FromCelsius(20) - Temperature.FromCelsius(10)).AsDelta().AsFahrenheit()", 18)]
	[InlineData("(Temperature.FromCelsius(20) - Temperature.FromCelsius(10)).AsDelta().AsRankine()", 18)]
	[InlineData("(Temperature.FromCelsius(20) - Temperature.FromCelsius(20)).AsDelta().AsFahrenheit()", 0)]
	[InlineData("(Temperature.FromCelsius(10) - Temperature.FromCelsius(20)).AsDelta().AsFahrenheit()", -18)]
	public void AffineReadingsAndScaleOnlyDeltasUseTheirOwnConversionRules(string expression, double expected)
	{
		AtomicUnit celsius = new("Celsius", "Celsius", "°C");
		AtomicUnit rankine = new("Rankine", "Rankine", "°R", Scale: 5d / 9d);
		AtomicUnit fahrenheit = new("Fahrenheit", "Fahrenheit", "°F", Scale: 5d / 9d);
		UnitConversionSpec[] units =
		[
			new("Temperature", "Temperature",
			[
				new UnitDefinition("Celsius", new UnitExpression(new UnitTerm(celsius)), offsetBeforeScale: 273.15),
				new UnitDefinition("Rankine", new UnitExpression(new UnitTerm(rankine)),
					[new(new UnitExpression(new UnitTerm(fahrenheit)), 459.67)])
			], true, false, false)
		];
		CSharpCompilation compilation = Compile(
			$$"""
			public static class Consumer
			{
				public static double Value() => {{expression}};
			}
			""", units);
		Assert.Equal(expected, QuantitySourceEmitterTests.Execute<double>(compilation), 10);
	}

	[Theory]
	[InlineData("Quetta", 30)]
	[InlineData("Ronna", 27)]
	[InlineData("Yotta", 24)]
	[InlineData("Zetta", 21)]
	[InlineData("Exa", 18)]
	[InlineData("Peta", 15)]
	[InlineData("Tera", 12)]
	[InlineData("Giga", 9)]
	[InlineData("Mega", 6)]
	[InlineData("Kilo", 3)]
	[InlineData("Hecto", 2)]
	[InlineData("Deca", 1)]
	[InlineData("Deci", -1)]
	[InlineData("Centi", -2)]
	[InlineData("Milli", -3)]
	[InlineData("Micro", -6)]
	[InlineData("Nano", -9)]
	[InlineData("Pico", -12)]
	[InlineData("Femto", -15)]
	[InlineData("Atto", -18)]
	[InlineData("Zepto", -21)]
	[InlineData("Yocto", -24)]
	[InlineData("Ronto", -27)]
	[InlineData("Quecto", -30)]
	public void NamedPrefixesUseIndependentlySpecifiedDecimalExponents(string prefix, int exponent)
	{
		string generated = EmitNamedPrefixes();
		CSharpCompilation compilation = QuantitySourceEmitterTests.Compile(
			$$"""
			using Custom;
			class Consumer
			{
				Length M() => Length.From{{prefix}}Metres(1);
			}
			""", generated);
		string source = string.Join("\n", compilation.SyntaxTrees.Skip(1).Where(tree => tree.ToString().Contains("namespace Custom;")));
		MethodDeclarationSyntax method = compilation.SyntaxTrees.Skip(1).SelectMany(tree => tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()).Single(method => method.Identifier.ValueText == $"From{prefix}Metres" && method.SyntaxTree.ToString().Contains("namespace Custom;"));
		Assert.Multiple(
			() => Assert.Contains($"From{prefix}Metres(double value)", source),
			() => Assert.Contains($"overallScaleExponent: {exponent}d", method.ToString()));
	}
	[Theory]
	[InlineData("Quetta", 30)]
	[InlineData("Ronna", 27)]
	[InlineData("Yotta", 24)]
	[InlineData("Zetta", 21)]
	[InlineData("Exa", 18)]
	[InlineData("Peta", 15)]
	[InlineData("Tera", 12)]
	[InlineData("Giga", 9)]
	[InlineData("Mega", 6)]
	[InlineData("Kilo", 3)]
	[InlineData("Hecto", 2)]
	[InlineData("Deca", 1)]
	[InlineData("Deci", -1)]
	[InlineData("Centi", -2)]
	[InlineData("Milli", -3)]
	[InlineData("Micro", -6)]
	[InlineData("Nano", -9)]
	[InlineData("Pico", -12)]
	[InlineData("Femto", -15)]
	[InlineData("Atto", -18)]
	[InlineData("Zepto", -21)]
	[InlineData("Yocto", -24)]
	[InlineData("Ronto", -27)]
	[InlineData("Quecto", -30)]
	public void NamedPrefixAccessorBindsAndUsesIndependentExponent(string prefix, int exponent)
	{
		string generated = EmitNamedPrefixes();
		CSharpCompilation compilation = QuantitySourceEmitterTests.Compile(
			$$"""
			using Custom;
			class Consumer
			{
				double M(Length value) => value.As{{prefix}}Metres();
			}
			""", generated);
		SyntaxTree tree = compilation.SyntaxTrees.First();
		InvocationExpressionSyntax call = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
		IMethodSymbol selected = Assert.IsAssignableFrom<IMethodSymbol>(compilation.GetSemanticModel(tree).GetSymbolInfo(call, TestContext.Current.CancellationToken).Symbol);
		string declaration = selected.DeclaringSyntaxReferences.Single().GetSyntax(TestContext.Current.CancellationToken).ToString();
		Assert.Multiple(
			() => Assert.Equal("Custom", selected.ContainingNamespace.ToDisplayString()),
			() => Assert.Equal(SpecialType.System_Double, selected.ReturnType.SpecialType),
			() => Assert.Empty(selected.Parameters),
			() => Assert.Contains($"distance.AsArbitrarySiComponents(overallScaleExponent: {exponent}d)", declaration));
	}

	[Theory]
	[InlineData("FromSiBaseUnit((value + 10d) * (2d * global::System.Math.Pow(10, 0d)))")]
	[InlineData("""
		reading.AsSiBaseUnit() / (2d * global::System.Math.Pow(10, 0d)) - 10d;
		""")]
	[InlineData("""
		delta.AsSiBaseUnit() / (2d * global::System.Math.Pow(10, 0d));
		""")]
	public void AffineConversionEmitsExpectedFormula(string formula)
	{
		UnitConversionSpec spec = new("Reading", "Temperature",
			[new UnitDefinition("Reading", new UnitExpression(new UnitTerm(new AtomicUnit("Degree", "Degrees", "d", Scale: 2))), offsetBeforeScale: 10)], true);
		string source = UnitConversionSourceEmitter.Emit([spec], "Custom", "public");
		Assert.Contains(formula, source);
	}

	[Theory]
	[InlineData("FromKiloDegrees")]
	[InlineData("FromPrefixedUnits")]
	public void AffineConversionDoesNotPublishPrefixFamily(string method)
	{
		UnitConversionSpec spec = new("Reading", "Temperature",
			[new UnitDefinition("Reading", new UnitExpression(new UnitTerm(new AtomicUnit("Degree", "Degrees", "d", Scale: 2))), offsetBeforeScale: 10)], true);
		string source = UnitConversionSourceEmitter.Emit([spec], "Custom", "public");
		Assert.DoesNotContain(method, source);
	}


	[Theory]
	[InlineData("fr-FR", 0.00000000000000000004, "4E-20")]
	[InlineData("de-DE", 4e20, "4E+20")]
	public void NumericalLiteralsRemainInvariant(string culture, double scale, string literal)
	{
		CultureInfo previous = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
			UnitConversionSpec spec = new("LengthUnit", "Length",
				[new UnitDefinition("LengthUnit", [new(new AtomicUnit("Unit", "Units", "u", Scale: scale))])], true);
			string generated = UnitConversionSourceEmitter.Emit([spec], "Custom", "public");
			SyntaxTree tree = CSharpSyntaxTree.ParseText(generated, _parseOptions, cancellationToken: TestContext.Current.CancellationToken);
			Assert.Multiple(
				() => Assert.Contains($"value * {literal}d *", generated),
				() => Assert.Contains(tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<LiteralExpressionSyntax>(), expression => expression.Token.Value is double value && value == scale));
		}
		finally
		{
			CultureInfo.CurrentCulture = previous;
		}
	}
	[Theory]
	[InlineData(false, "FromFootMetres")]
	[InlineData(true, "FromMetreFeet")]
	public void PositiveFactorsKeepOrderAndPluraliseLastNumerator(bool metreFirst, string factory)
	{
		AtomicUnit foot = new("Foot", "Feet", "ft", Scale: 0.3048);
		UnitTerm[] terms = metreFirst ? [new(AtomicUnits.Metre), new(foot)] : [new(foot), new(AtomicUnits.Metre)];
		UnitConversionSpec spec = new("AreaUnit", "Area", [new UnitDefinition("AreaUnit", terms)], true, false, false);
		string generated = UnitConversionSourceEmitter.Emit([spec], "Custom", "public");
		CSharpCompilation compilation = QuantitySourceEmitterTests.Compile(
			$$"""
			using Custom;
			public static class Consumer
			{
				public static double Value() => Area.{{factory}}(1).AsSiBaseUnit();
			}
			""", QuantitySourceEmitter.Emit([new(new(Length: 2), ["Area"], [], false)]), generated);
		Assert.Multiple(
			() => Assert.Contains($"{factory}(double value)", generated),
			() => Assert.Contains("value * 0.3048d *", generated),
			() => Assert.Equal(0.3048, QuantitySourceEmitterTests.Execute<double>(compilation)));
	}

	[Theory]
	[InlineData(1, "FromFeet")]
	[InlineData(2, "FromSquareFeet")]
	[InlineData(3, "FromCubicFeet")]
	[InlineData(4, "FromPower4Feet")]
	[InlineData(-1, "FromInverseFoot")]
	public void OrderedPowerNamesUseIrregularPluralOnlyInNumerator(int power, string factory)
	{
		UnitConversionSpec spec = new("Distance", "Length",
			[new UnitDefinition("Distance", [new(new AtomicUnit("Foot", "Feet", "ft"), power: power)])], true, false, false);
		string generated = UnitConversionSourceEmitter.Emit([spec], "Custom", "public");
		SyntaxTree tree = CSharpSyntaxTree.ParseText(generated, _parseOptions, cancellationToken: TestContext.Current.CancellationToken);
		Assert.Single(tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<MethodDeclarationSyntax>(),
			method => method.Identifier.ValueText == factory);
	}

	private static string EmitNamedPrefixes()
	{
		UnitConversionSpec spec = new("Distance", "Length", [new UnitDefinition("Distance", [new(AtomicUnits.Metre)])], true);
		return $$"""
			global using Length = {{QuantityTypeNameFormatter.Format(new Dimension(Length: 1))}};
			{{UnitConversionSourceEmitter.Emit([spec], "Custom", "public")}}
			""";
	}

	private static CSharpCompilation Compile(string source, UnitConversionSpec[]? units = null)
	{
		units ??=
		[
			new("Length", "Length", [new UnitDefinition("Length", [new(AtomicUnits.Metre)])], true),
			new("Mass", "Mass", [new UnitDefinition("Mass", [new(AtomicUnits.Gram, prefixExponent: 3)])], true)
		];
		string lengthType = QuantityTypeNameFormatter.Format(new Dimension(Length: 1));
		string massType = QuantityTypeNameFormatter.Format(new Dimension(Mass: 1));
		string temperatureType = QuantityTypeNameFormatter.Format(new Dimension(Temperature: 1));
		string generatedSource = $$"""
			global using Length = {{lengthType}};
			global using Mass = {{massType}};
			global using Temperature = {{temperatureType}};
			global using DimensionalQuantities.Generated;

			{{UnitConversionSourceEmitter.Emit(units, "DimensionalQuantities.Generated", "internal")}}
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
