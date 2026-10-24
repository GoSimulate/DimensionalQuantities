using DimensionalQuantities.Numerics;
using DimensionalQuantities.Units;
using JetBrains.Annotations;
using MathematicalDimension = DimensionalQuantities.Dimensions.Dimension;

namespace DimensionalQuantities.TypeSystem;

/// <summary>A quantity whose dimension is encoded in its type and whose magnitude is stored in SI units.</summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public readonly record struct Quantity<TDimension>
{
	// Each closed quantity type intentionally caches its own decoded dimension.
	// ReSharper disable once StaticMemberInGenericType
	private static readonly Lazy<MathematicalDimension> _dimension = new(ParseDimension);

	internal Quantity(double valueInSiBaseUnit)
	{
		ValueInSiBaseUnit = valueInSiBaseUnit;
	}

	internal double ValueInSiBaseUnit { get; }

	/// <summary>Creates a quantity from a magnitude expressed in coherent SI base units for its dimension.</summary>
	public static Quantity<TDimension> FromSiBaseUnit(double value) => new(value);

	/// <summary>Creates a quantity from SI base components with the selected decimal prefixes.</summary>
	public static Quantity<TDimension> FromArbitrarySiComponents(
		double value,
		int lengthPrefixExponent = SiBasePrefixExponents.Length,
		int massPrefixExponent = SiBasePrefixExponents.Mass,
		int timePrefixExponent = SiBasePrefixExponents.Time,
		int electricCurrentPrefixExponent = SiBasePrefixExponents.ElectricCurrent,
		int temperaturePrefixExponent = SiBasePrefixExponents.Temperature,
		int amountOfSubstancePrefixExponent = SiBasePrefixExponents.AmountOfSubstance,
		int luminousIntensityPrefixExponent = SiBasePrefixExponents.LuminousIntensity,
		double overallScaleExponent = 0)
	{
		double exponent = GetScaleExponent(lengthPrefixExponent, massPrefixExponent, timePrefixExponent, electricCurrentPrefixExponent,
			temperaturePrefixExponent, amountOfSubstancePrefixExponent, luminousIntensityPrefixExponent, overallScaleExponent);
		return new(value * Math.Pow(10, exponent));
	}

	/// <summary>Returns the magnitude in coherent SI base units for its dimension.</summary>
	public double AsSiBaseUnit() => ValueInSiBaseUnit;

	/// <summary>Returns the magnitude in SI base components with the selected decimal prefixes.</summary>
	public double AsArbitrarySiComponents(
		int lengthPrefixExponent = SiBasePrefixExponents.Length,
		int massPrefixExponent = SiBasePrefixExponents.Mass,
		int timePrefixExponent = SiBasePrefixExponents.Time,
		int electricCurrentPrefixExponent = SiBasePrefixExponents.ElectricCurrent,
		int temperaturePrefixExponent = SiBasePrefixExponents.Temperature,
		int amountOfSubstancePrefixExponent = SiBasePrefixExponents.AmountOfSubstance,
		int luminousIntensityPrefixExponent = SiBasePrefixExponents.LuminousIntensity,
		double overallScaleExponent = 0)
	{
		double exponent = GetScaleExponent(lengthPrefixExponent, massPrefixExponent, timePrefixExponent, electricCurrentPrefixExponent,
			temperaturePrefixExponent, amountOfSubstancePrefixExponent, luminousIntensityPrefixExponent, overallScaleExponent);
		return ValueInSiBaseUnit / Math.Pow(10, exponent);
	}

	public static Quantity<TDimension> operator +(Quantity<TDimension> left, Quantity<TDimension> right) => new(left.ValueInSiBaseUnit + right.ValueInSiBaseUnit);
	public static Quantity<TDimension> operator -(Quantity<TDimension> left, Quantity<TDimension> right) => new(left.ValueInSiBaseUnit - right.ValueInSiBaseUnit);
	public static Quantity<TDimension> operator -(Quantity<TDimension> quantity) => new(-quantity.ValueInSiBaseUnit);
	public static Quantity<TDimension> operator *(Quantity<TDimension> quantity, double scalar) => new(quantity.ValueInSiBaseUnit * scalar);
	public static Quantity<TDimension> operator *(double scalar, Quantity<TDimension> quantity) => new(scalar * quantity.ValueInSiBaseUnit);
	public static Quantity<TDimension> operator /(Quantity<TDimension> quantity, double scalar) => new(quantity.ValueInSiBaseUnit / scalar);
	public static double operator /(Quantity<TDimension> left, Quantity<TDimension> right) => left.ValueInSiBaseUnit / right.ValueInSiBaseUnit;

	/// <summary>Compares two quantities of the same dimension by their SI magnitudes.</summary>
	public static bool operator <(Quantity<TDimension> left, Quantity<TDimension> right) => left.ValueInSiBaseUnit < right.ValueInSiBaseUnit;

	/// <summary>Compares two quantities of the same dimension by their SI magnitudes.</summary>
	public static bool operator >(Quantity<TDimension> left, Quantity<TDimension> right) => left.ValueInSiBaseUnit > right.ValueInSiBaseUnit;

	/// <summary>Compares two quantities of the same dimension by their SI magnitudes.</summary>
	public static bool operator <=(Quantity<TDimension> left, Quantity<TDimension> right) => left.ValueInSiBaseUnit <= right.ValueInSiBaseUnit;

	/// <summary>Compares two quantities of the same dimension by their SI magnitudes.</summary>
	public static bool operator >=(Quantity<TDimension> left, Quantity<TDimension> right) => left.ValueInSiBaseUnit >= right.ValueInSiBaseUnit;

	/// <summary>Returns the absolute magnitude of a quantity.</summary>
	public static Quantity<TDimension> Abs(Quantity<TDimension> quantity) => new(Math.Abs(quantity.ValueInSiBaseUnit));

	/// <summary>Returns the smaller of two quantities of the same dimension.</summary>
	public static Quantity<TDimension> Min(Quantity<TDimension> left, Quantity<TDimension> right) => new(Math.Min(left.ValueInSiBaseUnit, right.ValueInSiBaseUnit));

	/// <summary>Returns the larger of two quantities of the same dimension.</summary>
	public static Quantity<TDimension> Max(Quantity<TDimension> left, Quantity<TDimension> right) => new(Math.Max(left.ValueInSiBaseUnit, right.ValueInSiBaseUnit));

	/// <summary>Restricts a quantity to the inclusive range between two quantities of the same dimension.</summary>
	public static Quantity<TDimension> Clamp(Quantity<TDimension> value, Quantity<TDimension> min, Quantity<TDimension> max)
	{
		if (min > max)
		{
			throw new ArgumentException("The minimum must be less than or equal to the maximum.", nameof(min));
		}

		if (value < min)
		{
			return min;
		}

		if (value > max)
		{
			return max;
		}

		return value;
	}

	private static double GetScaleExponent(int length, int mass, int time, int electricCurrent, int temperature, int amountOfSubstance, int luminousIntensity, double overall)
	{
		MathematicalDimension dimension = _dimension.Value;
		Rational exponent = (length - SiBasePrefixExponents.Length) * dimension.Length + (mass - SiBasePrefixExponents.Mass) * dimension.Mass
			+ (time - SiBasePrefixExponents.Time) * dimension.Time + (electricCurrent - SiBasePrefixExponents.ElectricCurrent) * dimension.ElectricCurrent
			+ (temperature - SiBasePrefixExponents.Temperature) * dimension.Temperature + (amountOfSubstance - SiBasePrefixExponents.AmountOfSubstance) * dimension.AmountOfSubstance
			+ (luminousIntensity - SiBasePrefixExponents.LuminousIntensity) * dimension.LuminousIntensity;
		return (double)exponent + overall;
	}

	private static MathematicalDimension ParseDimension()
	{
		Type type = typeof(TDimension);
		if (!type.IsConstructedGenericType || type.GetGenericTypeDefinition() != typeof(Dimension<,,,,,,>))
		{
			throw new ArgumentException($"Type '{type}' is not a dimension encoding.");
		}

		Type[] exponents = type.GetGenericArguments();
		return new(
			TypeSystemNumberCodec.Parse(exponents[0]), TypeSystemNumberCodec.Parse(exponents[1]), TypeSystemNumberCodec.Parse(exponents[2]),
			TypeSystemNumberCodec.Parse(exponents[3]), TypeSystemNumberCodec.Parse(exponents[4]), TypeSystemNumberCodec.Parse(exponents[5]),
			TypeSystemNumberCodec.Parse(exponents[6]));
	}
}
