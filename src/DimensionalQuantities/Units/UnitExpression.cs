using System.Globalization;
using System.Numerics;
using System.Text;
using DimensionalQuantities.Numerics;

namespace DimensionalQuantities.Units;

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

	public Rational GetSiScaleExponent()
	{
		Rational exponent = Rational.Zero;
		foreach (UnitTerm term in Terms)
		{
			exponent += (term.PrefixExponent - term.Unit.SiBasePrefixExponent) * term.Power;
		}

		return exponent;
	}

	public string GetName(UnitExpression? reference = null, int variableFactorIndex = -1)
	{
		StringBuilder numerator = new();
		StringBuilder denominator = new();
		bool hasNumerator = Terms.Any(term => term.Power > Rational.Zero);
		bool hasDenominator = Terms.Any(term => term.Power < Rational.Zero);
		int lastNumerator = Array.FindLastIndex(Terms, term => term.Power > Rational.Zero);

		for (int index = 0; index < Terms.Length; index++)
		{
			UnitTerm term = Terms[index];
			StringBuilder destination = term.Power > Rational.Zero ? numerator : denominator;
			Rational power = Rational.Abs(term.Power);
			AppendRootName(destination, power);
			AppendPowerName(destination, power.Numerator);
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
		bool hasNumerator = Terms.Any(term => term.Power > Rational.Zero);
		bool hasDenominator = Terms.Any(term => term.Power < Rational.Zero);
		foreach (UnitTerm term in Terms)
		{
			StringBuilder destination = term.Power > Rational.Zero ? numerator : denominator;
			if (term.PrefixExponent != 0)
			{
				destination.Append(SiPrefixes.Entries[term.PrefixExponent].Symbol);
			}

			destination.Append(term.Unit.Symbol);
			if (Rational.Abs(term.Power) != Rational.One || !hasNumerator)
			{
				AppendSuperscript(destination, hasNumerator ? Rational.Abs(term.Power) : term.Power);
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
		int hash = 17;
		foreach (UnitTerm term in Terms ?? [])
		{
			hash = unchecked(hash * 31 + term.GetHashCode());
		}

		return hash;
	}

	private static void AppendRootName(StringBuilder name, Rational power)
	{
		if (power.Denominator == 2)
		{
			name.Append("SquareRoot");
		}
		else if (power.Denominator == 3)
		{
			name.Append("CubeRoot");
		}
		else if (power.Denominator > 3)
		{
			name.Append("Root").Append(power.Denominator.ToString(CultureInfo.InvariantCulture));
		}
	}

	private static void AppendPowerName(StringBuilder name, BigInteger power)
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

	private static void AppendSuperscript(StringBuilder symbol, Rational exponent)
	{
		const string digits = "⁰¹²³⁴⁵⁶⁷⁸⁹";
		if (exponent < Rational.Zero)
		{
			symbol.Append('⁻');
		}

		foreach (char digit in BigInteger.Abs(exponent.Numerator).ToString(CultureInfo.InvariantCulture))
		{
			symbol.Append(digits[digit - '0']);
		}

		if (exponent.Denominator != BigInteger.One)
		{
			symbol.Append('⁄');
			foreach (char digit in exponent.Denominator.ToString(CultureInfo.InvariantCulture))
			{
				symbol.Append(digits[digit - '0']);
			}
		}
	}
}
