using DimensionalQuantities.Numerics;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.Units;
using DimensionalQuantities.Units.Declarations;
using Microsoft.CodeAnalysis;

namespace DimensionalQuantities.Roslyn.Generators;

internal sealed class UnitProviderReader
{
	private readonly INamedTypeSymbol _providerAttribute;
	private readonly INamedTypeSymbol _quantityAttribute;
	private readonly INamedTypeSymbol _factorAttribute;
	private readonly INamedTypeSymbol _symbolAttribute;
	private readonly INamedTypeSymbol _additionalAttribute;
	private readonly QuantityTypeReader _quantityReader;

	public UnitProviderReader(Compilation compilation)
	{
		_providerAttribute = compilation.GetRequiredType(typeof(UnitProviderAttribute));
		_quantityAttribute = compilation.GetRequiredType(typeof(QuantityUnitAttribute));
		_factorAttribute = compilation.GetRequiredType(typeof(UnitFactorAttribute));
		_symbolAttribute = compilation.GetRequiredType(typeof(UnitSymbolAttribute));
		_additionalAttribute = compilation.GetRequiredType(typeof(AdditionalUnitAttribute));
		_quantityReader = new(compilation);
	}

	public IReadOnlyList<UnitConversionSpec> Read(INamedTypeSymbol provider, Action<Diagnostic> report)
	{
		AttributeData? marker = Find(provider, _providerAttribute);
		if (marker is null)
		{
			return [];
		}

		bool siPrefixes = GetNamed(marker, nameof(UnitProviderAttribute.GenerateSiPrefixes), false);
		bool prefixConversions = GetNamed(marker, nameof(UnitProviderAttribute.GeneratePrefixConversions), false);
		List<(ITypeSymbol Type, string Name, List<UnitDefinition> Units)> groups = [];
		foreach (INamedTypeSymbol declaration in provider.GetTypeMembers())
		{
			if (Find(declaration, _quantityAttribute) is not { } unitMarker
				|| !TryReadQuantity(declaration, unitMarker, provider, report, out INamedTypeSymbol? quantityType, out UnitDefinition unit))
			{
				continue;
			}

			int index = groups.FindIndex(group => SymbolEqualityComparer.Default.Equals(group.Type, quantityType));
			if (index < 0)
			{
				groups.Add((quantityType!, unit.QuantityName, [unit]));
			}
			else
			{
				groups[index].Units.Add(unit);
			}
		}

		return groups.Select(group => new UnitConversionSpec(group.Name,
			group.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), [.. group.Units], true,
			siPrefixes, prefixConversions)).ToArray();
	}

	private bool TryReadQuantity(INamedTypeSymbol declaration, AttributeData marker, INamedTypeSymbol provider,
		Action<Diagnostic> report, out INamedTypeSymbol? quantityType, out UnitDefinition unit)
	{
		unit = default;
		quantityType = marker.ConstructorArguments[0].Value as INamedTypeSymbol;
		if (quantityType is null || !_quantityReader.TryRead(quantityType, out _))
		{
			Report(report, marker, $"{declaration.Name} must name a valid Quantity<T>.");
			return false;
		}

		if (!TryReadExpression(declaration, report, out UnitExpression expression))
		{
			return false;
		}

		double offset = GetNamed(marker, nameof(QuantityUnitAttribute.Offset), 0d);
		if (!IsFinite(offset))
		{
			Report(report, marker, $"{declaration.Name} has a non-finite offset.");
			return false;
		}

		if (!TryReadAlternatives(declaration, provider, report, out UnitExpression[] alternatives))
		{
			return false;
		}

		string name = declaration.Name.EndsWith("Unit", StringComparison.Ordinal)
			? declaration.Name[..^4] : declaration.Name;
		if (name.Length == 0)
		{
			Report(report, marker, "A quantity unit declaration must have a name.");
			return false;
		}

		unit = new(name, expression, alternatives, offset);
		return true;
	}

	private bool TryReadAlternatives(INamedTypeSymbol declaration, INamedTypeSymbol provider,
		Action<Diagnostic> report, out UnitExpression[] alternatives)
	{
		List<UnitExpression> results = [];
		foreach (AttributeData addition in FindAll(declaration, _additionalAttribute))
		{
			if (addition.ConstructorArguments[0].Value is not INamedTypeSymbol additionalType
				|| !SymbolEqualityComparer.Default.Equals(additionalType.ContainingType, provider))
			{
				Report(report, addition, "An additional unit must be a nested type in this provider.");
				alternatives = [];
				return false;
			}

			if (!TryReadExpression(additionalType, report, out UnitExpression expression))
			{
				alternatives = [];
				return false;
			}

			results.Add(expression);
		}

		alternatives = [.. results];
		return true;
	}

	private bool TryReadExpression(INamedTypeSymbol declaration, Action<Diagnostic> report, out UnitExpression expression)
	{
		List<UnitTerm> terms = [];
		foreach (AttributeData factor in FindAll(declaration, _factorAttribute))
		{
			if (!TryReadFactor(factor, report, out UnitTerm term))
			{
				expression = default;
				return false;
			}

			terms.Add(term);
		}

		expression = new([.. terms]);
		if (terms.Count > 0)
		{
			return true;
		}

		Report(report, Find(declaration, _quantityAttribute) ?? declaration.GetAttributes().FirstOrDefault(),
			$"{declaration.Name} must contain at least one unit factor.");
		return false;
	}

	private bool TryReadFactor(AttributeData factor, Action<Diagnostic> report, out UnitTerm term)
	{
		term = default;
		if (factor.ConstructorArguments[0].Value is not INamedTypeSymbol symbolType
			|| Find(symbolType, _symbolAttribute) is not { } symbol)
		{
			Report(report, factor, "A unit factor must reference a type marked UnitSymbol.");
			return false;
		}

		int power = GetNamed(factor, nameof(UnitFactorAttribute.Power), 1);
		int denominator = GetNamed(factor, nameof(UnitFactorAttribute.PowerDenominator), 1);
		int prefix = GetNamed(factor, nameof(UnitFactorAttribute.PrefixExponent), 0);
		int basePrefix = GetNamed(symbol, nameof(UnitSymbolAttribute.SiBasePrefixExponent), 0);
		double scale = GetNamed(symbol, nameof(UnitSymbolAttribute.Scale), 1d);
		string? singular = symbol.ConstructorArguments[0].Value as string;
		string? plural = symbol.ConstructorArguments[1].Value as string;
		string? glyph = symbol.ConstructorArguments[2].Value as string;
		if (power == 0 || denominator <= 0 || (prefix != 0 && !SiPrefixes.Entries.ContainsKey(prefix))
			|| (basePrefix != 0 && !SiPrefixes.Entries.ContainsKey(basePrefix))
			|| !IsFinite(scale) || scale <= 0
			|| string.IsNullOrEmpty(singular) || string.IsNullOrEmpty(plural) || string.IsNullOrEmpty(glyph))
		{
			Report(report, factor, "A unit factor has an invalid power, prefix, symbol, or scale.");
			return false;
		}

		term = new(new AtomicUnit(singular!, plural!, glyph!, basePrefix, scale), new Rational(power, denominator), prefix);
		return true;
	}

	private static AttributeData? Find(ISymbol symbol, INamedTypeSymbol attributeType) =>
		FindAll(symbol, attributeType).FirstOrDefault();

	private static IEnumerable<AttributeData> FindAll(ISymbol symbol, INamedTypeSymbol attributeType) => symbol.GetAttributes()
		.Where(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType));

	private static T GetNamed<T>(AttributeData attribute, string name, T fallback)
	{
		foreach (KeyValuePair<string, TypedConstant> argument in attribute.NamedArguments)
		{
			if (argument.Key == name && argument.Value.Value is T value)
			{
				return value;
			}
		}

		return fallback;
	}

	private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

	private static void Report(Action<Diagnostic> report, AttributeData? attribute, string message) =>
		report(Diagnostic.Create(GenerationDiagnostics.InvalidUnitProvider,
			attribute?.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None, message));
}
