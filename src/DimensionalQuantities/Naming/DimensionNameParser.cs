using System.Globalization;
using System.Numerics;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Numerics;

namespace DimensionalQuantities.Naming;

internal static class DimensionNameParser
{
	private static readonly (string Name, Dimension Basis)[] _axes =
	[
		Basis(new(Length: 1)),
		Basis(new(Mass: 1)),
		Basis(new(Time: 1)),
		Basis(new(ElectricCurrent: 1)),
		Basis(new(Temperature: 1)),
		Basis(new(AmountOfSubstance: 1)),
		Basis(new(LuminousIntensity: 1))
	];

	public static bool TryParse(string? name, out Dimension dimension)
	{
		dimension = default;
		if (name is null || name.Length == 0)
		{
			return false;
		}

		Dimension parsed = default;
		if (name.StartsWith("Inverse", StringComparison.Ordinal))
		{
			if (!TryParseSide(name.Substring("Inverse".Length), -1, ref parsed))
			{
				return false;
			}
		}
		else
		{
			int separator = name.IndexOf("Per", StringComparison.Ordinal);
			if (separator < 0)
			{
				if (!TryParseSide(name, 1, ref parsed))
				{
					return false;
				}
			}
			else if (!TryParseSide(name.Substring(0, separator), 1, ref parsed)
				|| !TryParseSide(name.Substring(separator + "Per".Length), -1, ref parsed))
			{
				return false;
			}
		}

		if (parsed != default && DimensionNameGenerator.GetQuantityName(parsed) == name)
		{
			dimension = parsed;
			return true;
		}

		return false;
	}

	private static bool TryParseSide(string text, int sign, ref Dimension dimension)
	{
		int position = 0;
		int previousAxis = -1;
		while (position < text.Length)
		{
			if (!TryReadRootDegree(text, ref position, out BigInteger degree))
			{
				return false;
			}

			int axis = Array.FindIndex(_axes, previousAxis + 1, candidate => StartsWithAt(text, position, candidate.Name));
			if (axis < 0)
			{
				return false;
			}

			position += _axes[axis].Name.Length;
			if (!TryReadPower(text, ref position, out BigInteger power))
			{
				return false;
			}
			dimension = dimension * _axes[axis].Basis.Pow(new Rational(sign * power, degree));
			previousAxis = axis;
		}

		return previousAxis >= 0;
	}

	private static bool TryReadRootDegree(string text, ref int position, out BigInteger degree)
	{
		degree = 1;
		if (StartsWithAt(text, position, "SquareRoot"))
		{
			degree = 2;
			position += "SquareRoot".Length;
		}
		else if (StartsWithAt(text, position, "CubeRoot"))
		{
			degree = 3;
			position += "CubeRoot".Length;
		}
		else if (StartsWithAt(text, position, "Root"))
		{
			position += "Root".Length;
			return TryReadNumber(text, ref position, out degree) && degree >= 2;
		}

		return true;
	}

	private static bool TryReadPower(string text, ref int position, out BigInteger power)
	{
		power = 1;
		if (StartsWithAt(text, position, "Squared"))
		{
			power = 2;
			position += "Squared".Length;
		}
		else if (StartsWithAt(text, position, "Cubed"))
		{
			power = 3;
			position += "Cubed".Length;
		}
		else if (position < text.Length && char.IsDigit(text[position]))
		{
			return TryReadNumber(text, ref position, out power);
		}

		return true;
	}

	private static bool TryReadNumber(string text, ref int position, out BigInteger value)
	{
		int start = position;
		while (position < text.Length && char.IsDigit(text[position]))
		{
			position++;
		}

		value = default;
		return position > start && BigInteger.TryParse(text.Substring(start, position - start), NumberStyles.None, CultureInfo.InvariantCulture, out value);
	}

	private static bool StartsWithAt(string text, int position, string prefix) => text.Length - position >= prefix.Length
		&& string.Compare(text, position, prefix, 0, prefix.Length, StringComparison.Ordinal) == 0;

	private static (string Name, Dimension Basis) Basis(Dimension dimension) =>
		(dimension.GetComponents().Single().Unit.QuantityName, dimension);
}
