using System.Globalization;
using System.Text;
using DimensionalQuantities.Units;

namespace DimensionalQuantities.DimensionAnalysis;

internal readonly record struct UnitExpression
{
	public UnitExpression(params UnitTerm[] terms)
	{
		Terms = terms;
	}

	public UnitTerm[] Terms { get; }

	public UnitExpression WithPrefix(int factorIndex, int prefixExponent)
	{
		UnitTerm[] terms = Terms.ToArray();
		terms[factorIndex] = terms[factorIndex] with { PrefixExponent = prefixExponent };
		return new(terms);
	}

	public int GetSiScaleExponent() => Terms.Sum(term => (term.PrefixExponent - term.Unit.SiBasePrefixExponent) * term.Power);

	public string GetName(UnitExpression? reference = null, int variableFactorIndex = -1)
	{
		StringBuilder numerator = new();
		StringBuilder denominator = new();
		bool hasNumerator = Terms.Any(term => term.Power > 0);
		bool hasDenominator = Terms.Any(term => term.Power < 0);
		int lastNumerator = Array.FindLastIndex(Terms, term => term.Power > 0);

		for (int index = 0; index < Terms.Length; index++)
		{
			UnitTerm term = Terms[index];
			StringBuilder destination = term.Power > 0 ? numerator : denominator;
			AppendPowerName(destination, Math.Abs(term.Power));
			bool generatedPrefix = index == variableFactorIndex ||
				(reference is { } original && term.PrefixExponent != original.Terms[index].PrefixExponent);
			AppendUnitName(destination, term, index == lastNumerator, generatedPrefix);
		}

		if (!hasNumerator)
		{
			return "Inverse" + denominator;
		}

		return hasDenominator ? numerator + "Per" + denominator : numerator.ToString();
	}

	public string GetSymbol()
	{
		StringBuilder numerator = new();
		StringBuilder denominator = new();
		bool hasNumerator = Terms.Any(term => term.Power > 0);
		bool hasDenominator = Terms.Any(term => term.Power < 0);
		foreach (UnitTerm term in Terms)
		{
			StringBuilder destination = term.Power > 0 ? numerator : denominator;
			if (term.PrefixExponent != 0)
			{
				destination.Append(SiPrefixes.Entries[term.PrefixExponent].Symbol);
			}

			destination.Append(term.Unit.Symbol);
			if (Math.Abs(term.Power) != 1 || !hasNumerator)
			{
				AppendSuperscript(destination, hasNumerator ? Math.Abs(term.Power) : term.Power);
			}
		}

		if (!hasNumerator)
		{
			return denominator.ToString();
		}

		return hasDenominator ? numerator + "/" + denominator : numerator.ToString();
	}

	public bool Equals(UnitExpression other) => (Terms ?? []).SequenceEqual(other.Terms ?? []);

	public override int GetHashCode()
	{
		HashCode hash = new();
		foreach (UnitTerm term in Terms ?? [])
		{
			hash.Add(term);
		}

		return hash.ToHashCode();
	}

	private static void AppendPowerName(StringBuilder name, int power)
	{
		if (power == 2)
		{
			name.Append("Square");
		}
		else if (power == 3)
		{
			name.Append("Cubic");
		}
		else if (power > 3)
		{
			name.Append("Power").Append(power.ToString(CultureInfo.InvariantCulture));
		}
	}

	private static void AppendUnitName(StringBuilder name, UnitTerm term, bool plural, bool generatedPrefix)
	{
		string unitName = plural ? term.Unit.NamePlural : term.Unit.NameSingular;
		if (term.PrefixExponent == 0)
		{
			name.Append(unitName);
			return;
		}

		name.Append(SiPrefixes.Entries[term.PrefixExponent].Name);
		name.Append(generatedPrefix ? unitName : char.ToLowerInvariant(unitName[0]) + unitName[1..]);
	}

	private static void AppendSuperscript(StringBuilder symbol, int exponent)
	{
		const string digits = "⁰¹²³⁴⁵⁶⁷⁸⁹";
		if (exponent < 0)
		{
			symbol.Append('⁻');
		}

		foreach (char digit in Math.Abs(exponent).ToString(CultureInfo.InvariantCulture))
		{
			symbol.Append(digits[digit - '0']);
		}
	}
}
