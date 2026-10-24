using System.Globalization;
using DimensionalQuantities.Numerics;
using DimensionalQuantities.Units;

namespace DimensionalQuantities.Roslyn.Generators;

internal static class UnitConversionEmitter
{
	public static string Emit(IReadOnlyList<UnitConversionSpec> quantities, string namespaceName, string accessibility)
	{
		int[] prefixes = SiPrefixes.Entries.Keys.OrderByDescending(exponent => exponent).ToArray();
		string extensions = string.Join("\n\n", quantities.Select(quantity => RenderQuantity(quantity, prefixes, accessibility)));
		return $"""
			namespace {namespaceName};

			{extensions}
			""";
	}

	private static string RenderQuantity(UnitConversionSpec quantity, int[] prefixes, string accessibility)
	{
		UnitExpression canonicalExpression = quantity.Units[0].Expression;
		ConversionDefinition generic = new("FromPrefixedUnits", "AsPrefixedUnits",
			GetTermParameters(canonicalExpression), GetTermScaleExponent(canonicalExpression));
		ConversionDefinition[] named = quantity.IncludeNamed
			? CollectConversions(quantity.Units, prefixes)
				.Select(conversion => new ConversionDefinition($"From{conversion.Key}", $"As{conversion.Key}", [], FormatExponent(conversion.Value)))
				.ToArray()
			: [];
		ConversionDefinition[] conversions = [generic, .. named];
		string variableName = char.ToLowerInvariant(quantity.Name[0]) + quantity.Name[1..];
		string factories = string.Join("\n\t\t", conversions.Select(conversion => RenderFactory(quantity.ReceiverType, conversion)));
		string accessors = string.Join("\n\t\t", conversions.Select(conversion => RenderAccessor(variableName, conversion)));

		return $$"""
			{{accessibility}} static class {{quantity.Name}}Extensions
			{
				extension({{quantity.ReceiverType}})
				{
					{{factories}}
				}

				extension({{quantity.ReceiverType}} {{variableName}})
				{
					{{accessors}}
				}
			}
			""";
	}

	private static IReadOnlyDictionary<string, Rational> CollectConversions(IReadOnlyList<UnitDefinition> units, int[] prefixes)
	{
		Dictionary<string, Rational> conversions = new(StringComparer.Ordinal);
		foreach ((string name, Rational scaleExponent) in units.SelectMany(unit => GetConversions(unit, prefixes)))
		{
			if (conversions.TryGetValue(name, out Rational existingExponent) && existingExponent != scaleExponent)
			{
				throw new InvalidOperationException($"Unit {name} has conflicting SI scales.");
			}

			if (!conversions.ContainsKey(name))
			{
				conversions.Add(name, scaleExponent);
			}
		}

		return conversions;
	}

	private static IEnumerable<(string Name, Rational ScaleExponent)> GetConversions(UnitDefinition unit, int[] prefixes)
		=> GetStandardConversions(unit.Expression, prefixes).Concat(unit.AdditionalConversions
			.Select(expression => (expression.GetName(unit.Expression), expression.GetSiScaleExponent())));

	private static IEnumerable<(string Name, Rational ScaleExponent)> GetStandardConversions(UnitExpression expression, int[] prefixes)
	{
		int firstNumerator = Array.FindIndex(expression.Terms, term => term.Power > Rational.Zero);
		if (firstNumerator < 0)
		{
			yield return (expression.GetName(), expression.GetSiScaleExponent());
			yield break;
		}

		foreach (int prefix in prefixes.Prepend(0))
		{
			UnitExpression prefixed = expression.WithPrefix(firstNumerator, prefix);
			yield return (prefixed.GetName(expression, firstNumerator), prefixed.GetSiScaleExponent());
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
		return $"public static {quantityName} {conversion.FactoryName}(double value{parameters}) => {quantityName}.FromArbitrarySiComponents(value, overallScaleExponent: {conversion.ScaleExponentExpression});";
	}

	private static string RenderAccessor(string variableName, ConversionDefinition conversion)
	{
		string parameters = string.Join(", ", conversion.PrefixParameters.Select(parameter => $"int {parameter.Name} = {parameter.DefaultExponent.ToString(CultureInfo.InvariantCulture)}"));
		return $"public double {conversion.AccessorName}({parameters}) => {variableName}.AsArbitrarySiComponents(overallScaleExponent: {conversion.ScaleExponentExpression});";
	}

	private static string FormatExponent(Rational exponent) => ((double)exponent).ToString("R", CultureInfo.InvariantCulture) + "d";

	private sealed record ConversionDefinition(string FactoryName, string AccessorName, PrefixParameterDefinition[] PrefixParameters, string ScaleExponentExpression);

	private sealed record PrefixParameterDefinition(string Name, int DefaultExponent);
}
