using System.Globalization;
using System.Numerics;
using System.Text;
using DimensionalQuantities.Numerics;

namespace DimensionalQuantities.DimensionAnalysis;

internal static class DimensionNameGenerator
{
	public static string GetQuantityName(Dimension dimension) => GetName(dimension, static component => component.QuantityName);

	public static string GetUnitName(Dimension dimension) => GetName(dimension, static component => component.UnitNamePlural);

	public static string GetUnitExpression(Dimension dimension)
	{
		StringBuilder expression = new();
		foreach (DimensionComponent component in dimension.GetComponents())
		{
			if (component.Exponent == Rational.Zero)
			{
				continue;
			}

			expression.Append(component.UnitSymbol);
			if (component.Exponent != Rational.One)
			{
				expression.Append("<sup>").Append(component.Exponent.ToString(null, CultureInfo.InvariantCulture)).Append("</sup>");
			}
		}

		if (expression.Length == 0)
		{
			throw new NotSupportedException("Dimensionless names are not defined.");
		}

		return expression.ToString();
	}

	private static string GetName(Dimension dimension, Func<DimensionComponent, string> getFactorName)
	{
		StringBuilder numerator = new();
		StringBuilder denominator = new();

		foreach (DimensionComponent component in dimension.GetComponents())
		{
			Rational exponent = component.Exponent;
			if (exponent.Numerator.Sign > 0)
			{
				AppendFactor(numerator, getFactorName(component), exponent);
			}
			else if (exponent.Numerator.Sign < 0)
			{
				AppendFactor(denominator, getFactorName(component), -exponent);
			}
		}

		if (numerator.Length == 0)
		{
			if (denominator.Length == 0)
			{
				throw new NotSupportedException("Dimensionless names are not defined.");
			}

			return "Inverse" + denominator;
		}

		return denominator.Length == 0 ? numerator.ToString() : numerator + "Per" + denominator;
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
