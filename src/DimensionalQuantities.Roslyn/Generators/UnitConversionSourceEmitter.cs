using System.Globalization;
using DimensionalQuantities.Numerics;
using DimensionalQuantities.Units;
using Microsoft.CodeAnalysis.CSharp;

namespace DimensionalQuantities.Roslyn.Generators;

internal static class UnitConversionSourceEmitter
{
	public static string Emit(IReadOnlyList<UnitConversionSpec> quantities, string namespaceName, string accessibility,
		string classNamePrefix = "")
	{
		int[] prefixes = SiPrefixes.Entries.Keys.OrderByDescending(exponent => exponent).ToArray();
		string extensions = string.Join("\n\n", quantities.Select(quantity => RenderQuantity(quantity, prefixes, accessibility, classNamePrefix)));
		if (namespaceName.Length == 0)
		{
			return extensions;
		}

		return $"""
			namespace {namespaceName};

			{extensions}
			""";
	}

	private static string RenderQuantity(UnitConversionSpec quantity, int[] prefixes, string accessibility, string classNamePrefix)
	{
		UnitExpression canonicalExpression = quantity.Units[0].Expression;
		ConversionDefinition generic = new("FromPrefixedUnits", "AsPrefixedUnits",
			GetTermParameters(canonicalExpression), GetTermScaleExponent(canonicalExpression),
			canonicalExpression.GetScale(), null);
		ConversionDefinition[] named = quantity.IncludeNamed
			? CollectConversions(quantity.Units, quantity.GenerateSiPrefixes ? prefixes : [])
				.Select(conversion => new ConversionDefinition($"From{conversion.Key}", $"As{conversion.Key}", [],
					FormatExponent(conversion.Value.Exponent), conversion.Value.Scale, conversion.Value.OffsetBeforeScale))
				.ToArray()
			: [];
		ConversionDefinition[] conversions = quantity.GeneratePrefixConversions && quantity.Units[0].OffsetBeforeScale is null
			? [generic, .. named] : named;
		string variableName = char.ToLowerInvariant(quantity.ExtensionName[0]) + quantity.ExtensionName[1..];
		string className = classNamePrefix + (quantity.ExtensionClassName ?? quantity.ExtensionName) + "Extensions";
		if (!SyntaxFacts.IsValidIdentifier(className) || !SyntaxFacts.IsValidIdentifier(variableName)
			|| conversions.Any(conversion => !SyntaxFacts.IsValidIdentifier(conversion.FactoryName)
				|| !SyntaxFacts.IsValidIdentifier(conversion.AccessorName)
				|| conversion.PrefixParameters.Any(parameter => !SyntaxFacts.IsValidIdentifier(parameter.Name))))
		{
			throw new InvalidOperationException($"Unit provider for {quantity.ExtensionName} produces an invalid C# identifier.");
		}

		string factories = string.Join("\n\t\t", conversions.Select(conversion => RenderFactory(quantity.ReceiverType, conversion)));
		string accessors = string.Join("\n\t\t", conversions.Select(conversion => RenderAccessor(variableName, conversion)));
		string deltaAccessors = string.Join("\n\t\t", conversions.Select(RenderDeltaAccessor));

		return $$"""
			{{accessibility}} static class {{className}}
			{
				extension({{quantity.ReceiverType}})
				{
					{{factories}}
				}

				extension({{quantity.ReceiverType}} {{variableName}})
				{
					{{accessors}}
				}

				extension(global::DimensionalQuantities.TypeSystem.Delta<{{quantity.ReceiverType}}> delta)
				{
					{{deltaAccessors}}
				}
			}
			""";
	}

	private static IReadOnlyDictionary<string, (Rational Exponent, double Scale, double? OffsetBeforeScale)> CollectConversions(
		IReadOnlyList<UnitDefinition> units, int[] generatedPrefixes)
	{
		Dictionary<string, (Rational Exponent, double Scale, double? OffsetBeforeScale)> conversions = new(StringComparer.Ordinal);
		foreach ((string name, Rational exponent, double scale, double? offset) in units.SelectMany(unit => GetConversions(unit, generatedPrefixes)))
		{
			if (conversions.TryGetValue(name, out (Rational Exponent, double Scale, double? OffsetBeforeScale) existing)
				&& existing != (exponent, scale, offset))
			{
				throw new InvalidOperationException($"Unit {name} has conflicting SI conversions.");
			}

			if (!conversions.ContainsKey(name))
			{
				conversions.Add(name, (exponent, scale, offset));
			}
		}

		return conversions;
	}

	private static IEnumerable<(string Name, Rational Exponent, double Scale, double? OffsetBeforeScale)> GetConversions(
		UnitDefinition unit, int[] generatedPrefixes)
		=> GetStandardConversions(unit, generatedPrefixes).Concat(unit.AdditionalConversions
			.Select(conversion => (conversion.Expression.GetName(unit.Expression), conversion.Expression.GetSiScaleExponent(),
				conversion.Expression.GetScale(), conversion.OffsetBeforeScale)));

	private static IEnumerable<(string Name, Rational Exponent, double Scale, double? OffsetBeforeScale)> GetStandardConversions(
		UnitDefinition unit, int[] generatedPrefixes)
	{
		UnitExpression expression = unit.Expression;
		int firstNumerator = Array.FindIndex(expression.Terms, term => term.Power > Rational.Zero);
		if (firstNumerator < 0 || generatedPrefixes.Length == 0 || unit.OffsetBeforeScale is not null)
		{
			yield return (expression.GetName(), expression.GetSiScaleExponent(), expression.GetScale(), unit.OffsetBeforeScale);
			yield break;
		}

		foreach (int prefix in generatedPrefixes.Prepend(0))
		{
			UnitExpression prefixed = expression.WithPrefix(firstNumerator, prefix);
			yield return (prefixed.GetName(expression, firstNumerator), prefixed.GetSiScaleExponent(), prefixed.GetScale(), null);
		}
	}

	private static PrefixParameterDefinition[] GetTermParameters(UnitExpression expression) => expression.Terms
		.Select(term => new PrefixParameterDefinition(GetPrefixParameterName(term), term.PrefixExponent)).ToArray();

	private static string GetTermScaleExponent(UnitExpression expression) => string.Join(" + ", expression.Terms.Select(FormatTermScaleExponent));

	private static string FormatTermScaleExponent(UnitTerm term)
	{
		string prefix = $"((double){GetPrefixParameterName(term)}";
		if (term.Unit.SiBasePrefixExponent != 0)
		{
			prefix += " - " + term.Unit.SiBasePrefixExponent.ToString(CultureInfo.InvariantCulture);
		}

		prefix += ")";
		if (term.Power == Rational.One)
		{
			return prefix;
		}

		string power = term.Power.Denominator.IsOne
			? term.Power.Numerator.ToString(CultureInfo.InvariantCulture)
			: $"({term.Power.Numerator.ToString(CultureInfo.InvariantCulture)}d / {term.Power.Denominator.ToString(CultureInfo.InvariantCulture)}d)";
		return $"{prefix} * {power}";
	}

	private static string GetPrefixParameterName(UnitTerm term) => char.ToLowerInvariant(term.Unit.NameSingular[0]) + term.Unit.NameSingular[1..] + "PrefixExponent";

	private static string RenderFactory(string quantityName, ConversionDefinition conversion)
	{
		string parameters = string.Concat(conversion.PrefixParameters.Select(parameter => $", int {parameter.Name} = {parameter.DefaultExponent.ToString(CultureInfo.InvariantCulture)}"));
		if (conversion.OffsetBeforeScale is double offset)
		{
			return $"/// <summary>Converts an affine reading to its absolute SI value. Arithmetic does not preserve affine offsets.</summary>\n\t\t\tpublic static {quantityName} {conversion.FactoryName}(double value{parameters}) => {quantityName}.FromSiBaseUnit((value + {FormatDouble(offset)}) * {GetScaleExpression(conversion)});";
		}

		if (conversion.Scale == 1)
		{
			return $"public static {quantityName} {conversion.FactoryName}(double value{parameters}) => {quantityName}.FromArbitrarySiComponents(value, overallScaleExponent: {conversion.ScaleExponentExpression});";
		}

		return $"public static {quantityName} {conversion.FactoryName}(double value{parameters}) => {quantityName}.FromSiBaseUnit(value * {FormatDouble(conversion.Scale)} * global::System.Math.Pow(10, {conversion.ScaleExponentExpression}));";
	}

	private static string RenderAccessor(string variableName, ConversionDefinition conversion)
	{
		string parameters = string.Join(", ", conversion.PrefixParameters.Select(parameter => $"int {parameter.Name} = {parameter.DefaultExponent.ToString(CultureInfo.InvariantCulture)}"));
		if (conversion.OffsetBeforeScale is double offset)
		{
			return $"/// <summary>Interprets this value as an absolute reading. Use AsDelta() before converting a difference.</summary>\n\t\t\tpublic double {conversion.AccessorName}({parameters}) => {variableName}.AsSiBaseUnit() / {GetScaleExpression(conversion)} - {FormatDouble(offset)};";
		}

		if (conversion.Scale == 1)
		{
			return $"public double {conversion.AccessorName}({parameters}) => {variableName}.AsArbitrarySiComponents(overallScaleExponent: {conversion.ScaleExponentExpression});";
		}

		return $"public double {conversion.AccessorName}({parameters}) => {variableName}.AsSiBaseUnit() / ({FormatDouble(conversion.Scale)} * global::System.Math.Pow(10, {conversion.ScaleExponentExpression}));";
	}

	private static string RenderDeltaAccessor(ConversionDefinition conversion)
	{
		string parameters = string.Join(", ", conversion.PrefixParameters.Select(parameter => $"int {parameter.Name} = {parameter.DefaultExponent.ToString(CultureInfo.InvariantCulture)}"));
		return $"/// <summary>Converts a difference using scale alone; affine offsets are omitted.</summary>\n\t\t\tpublic double {conversion.AccessorName}({parameters}) => delta.AsSiBaseUnit() / {GetScaleExpression(conversion)};";
	}

	private static string GetScaleExpression(ConversionDefinition conversion)
		=> $"({FormatDouble(conversion.Scale)} * global::System.Math.Pow(10, {conversion.ScaleExponentExpression}))";

	private static string FormatExponent(Rational exponent) => ((double)exponent).ToString("R", CultureInfo.InvariantCulture) + "d";
	private static string FormatDouble(double value) => value.ToString("R", CultureInfo.InvariantCulture) + "d";

	private sealed record ConversionDefinition(string FactoryName, string AccessorName, PrefixParameterDefinition[] PrefixParameters,
		string ScaleExponentExpression, double Scale, double? OffsetBeforeScale);

	private sealed record PrefixParameterDefinition(string Name, int DefaultExponent);
}
