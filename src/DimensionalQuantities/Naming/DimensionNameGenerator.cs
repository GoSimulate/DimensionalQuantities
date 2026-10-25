using System.Globalization;
using System.Numerics;
using System.Text;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Numerics;

namespace DimensionalQuantities.Naming;

internal static class DimensionNameGenerator
{
	public static string GetUnitExpression(Dimension dimension)
	{
		DimensionComponent[] components = dimension.GetComponents().ToArray();
		if (components.Length == 0)
		{
			throw new NotSupportedException("Dimensionless names are not defined.");
		}

		StringBuilder expression = new();
		foreach (DimensionComponent component in components)
		{
			expression.Append(component.Unit.Expression.GetSymbol());
			if (component.Exponent != Rational.One)
			{
				expression.Append("<sup>").Append(component.Exponent.ToString(null, CultureInfo.InvariantCulture)).Append("</sup>");
			}
		}

		return expression.ToString();
	}

	public static string GetQuantityName(Dimension dimension)
	{
		DimensionComponent[] components = dimension.GetComponents().ToArray();
		if (components.Length == 0)
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
				AppendFactor(numerator, component.Unit.Name, exponent);
			}
			else
			{
				AppendFactor(denominator, component.Unit.Name, -exponent);
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
