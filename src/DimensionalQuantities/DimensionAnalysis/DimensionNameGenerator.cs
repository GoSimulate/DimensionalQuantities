using System.Collections.Immutable;
using System.Globalization;
using System.Numerics;
using System.Text;
using DimensionalQuantities.Numerics;

namespace DimensionalQuantities.DimensionAnalysis;

internal static class DimensionNameGenerator
{
	public static string GetQuantityName(Dimension dimension) => GetName(dimension, static component => component.Unit.QuantityName);

	public static string GetUnitName(Dimension dimension) => GetName(dimension, static component => component.Unit.SiUnitNamePlural);

	public static string GetUnitExpression(Dimension dimension)
	{
		ImmutableArray<DimensionComponent> components = dimension.GetComponents().ToImmutableArray();
		if (components.IsEmpty)
		{
			throw new NotSupportedException("Dimensionless names are not defined.");
		}

		StringBuilder expression = new();
		foreach (DimensionComponent component in components)
		{
			expression.Append(component.Unit.SiUnitSymbol);
			if (component.Exponent != Rational.One)
			{
				expression.Append("<sup>").Append(component.Exponent.ToString(null, CultureInfo.InvariantCulture)).Append("</sup>");
			}
		}

		return expression.ToString();
	}

	private static string GetName(Dimension dimension, Func<DimensionComponent, string> getFactorName)
	{
		ImmutableArray<DimensionComponent> components = dimension.GetComponents().ToImmutableArray();
		if (components.IsEmpty)
		{
			throw new NotSupportedException("Dimensionless names are not defined.");
		}

		bool hasNumerator = components.Any(component => component.Exponent.Numerator.Sign > 0);
		bool hasDenominator = components.Any(component => component.Exponent.Numerator.Sign < 0);
		StringBuilder numerator = new();
		StringBuilder denominator = new();

		foreach (DimensionComponent component in components)
		{
			Rational exponent = component.Exponent;
			if (exponent.Numerator.Sign > 0)
			{
				AppendFactor(numerator, getFactorName(component), exponent);
			}
			else
			{
				AppendFactor(denominator, getFactorName(component), -exponent);
			}
		}

		if (!hasNumerator)
		{
			return "Inverse" + denominator;
		}

		return hasDenominator ? numerator + "Per" + denominator : numerator.ToString();
	}

	private static void AppendFactor(StringBuilder name, string factorName, Rational exponent)
	{
		BigInteger degree = exponent.Denominator;
		if (degree == 2)
		{
			name.Append("SquareRoot");
		}
		else if (degree == 3)
		{
			name.Append("CubeRoot");
		}
		else if (degree > 3)
		{
			name.Append("Root").Append(degree.ToString(CultureInfo.InvariantCulture));
		}

		name.Append(factorName);
		BigInteger power = exponent.Numerator;
		if (power == 2)
		{
			name.Append("Squared");
		}
		else if (power == 3)
		{
			name.Append("Cubed");
		}
		else if (power > 3)
		{
			name.Append(power.ToString(CultureInfo.InvariantCulture));
		}
	}
}
