using DimensionalQuantities.Numerics;

namespace DimensionalQuantities.DimensionAnalysis;

/// <summary>An internal value describing the seven rational SI exponents.</summary>
internal readonly record struct Dimension(
	Rational Length = default,
	Rational Mass = default,
	Rational Time = default,
	Rational ElectricCurrent = default,
	Rational Temperature = default,
	Rational AmountOfSubstance = default,
	Rational LuminousIntensity = default)
{
	public static Dimension operator *(Dimension left, Dimension right) => left.Multiply(right);
	public static Dimension operator /(Dimension left, Dimension right) => left.Divide(right);

	/// <summary>Pairs every SI exponent with its quantity and unit names, in canonical SI axis order.</summary>
	public IEnumerable<DimensionComponent> GetComponents()
	{
		yield return new(Length, "Length", "Metre", "Metres");
		yield return new(Mass, "Mass", "Kilogram", "Kilograms");
		yield return new(Time, "Time", "Second", "Seconds");
		yield return new(ElectricCurrent, "Current", "Amp", "Amps");
		yield return new(Temperature, "Temperature", "Kelvin", "Kelvin");
		yield return new(AmountOfSubstance, "AmountOfSubstance", "Mole", "Moles");
		yield return new(LuminousIntensity, "LuminousIntensity", "Candela", "Candelas");
	}

	/// <summary>Adds corresponding exponents.</summary>
	public Dimension Multiply(Dimension other) => new(
		Length + other.Length,
		Mass + other.Mass,
		Time + other.Time,
		ElectricCurrent + other.ElectricCurrent,
		Temperature + other.Temperature,
		AmountOfSubstance + other.AmountOfSubstance,
		LuminousIntensity + other.LuminousIntensity);

	/// <summary>Subtracts corresponding exponents.</summary>
	public Dimension Divide(Dimension other) => new(
		Length - other.Length,
		Mass - other.Mass,
		Time - other.Time,
		ElectricCurrent - other.ElectricCurrent,
		Temperature - other.Temperature,
		AmountOfSubstance - other.AmountOfSubstance,
		LuminousIntensity - other.LuminousIntensity);

	/// <summary>Scales exponents by an exact rational power.</summary>
	public Dimension Pow(Rational power) => new(Length * power, Mass * power, Time * power, ElectricCurrent * power, Temperature * power, AmountOfSubstance * power, LuminousIntensity * power);

	/// <summary>
	/// Divides exponents by a positive root degree.
	/// Nonpositive degrees throw ArgumentOutOfRangeException.
	/// </summary>
	public Dimension Root(int degree)
	{
		if (degree <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(degree));
		}

		return Pow(new(1, degree));
	}
}
