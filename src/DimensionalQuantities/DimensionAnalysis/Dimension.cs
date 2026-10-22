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
	private static readonly UnitDefinition _lengthUnit = new("Length", [new(AtomicUnits.Metre)]);
	private static readonly UnitDefinition _massUnit = new("Mass", [new(AtomicUnits.Gram, PrefixExponent: 3)]);
	private static readonly UnitDefinition _timeUnit = new("Time", [new(AtomicUnits.Second)]);
	private static readonly UnitDefinition _electricCurrentUnit = new("Current", [new(AtomicUnits.Amp)]);
	private static readonly UnitDefinition _temperatureUnit = new("Temperature", [new(AtomicUnits.Kelvin)]);
	private static readonly UnitDefinition _amountOfSubstanceUnit = new("AmountOfSubstance", [new(AtomicUnits.Mole)]);
	private static readonly UnitDefinition _luminousIntensityUnit = new("LuminousIntensity", [new(AtomicUnits.Candela)]);

	public static Dimension operator *(Dimension left, Dimension right) => left.Multiply(right);
	public static Dimension operator /(Dimension left, Dimension right) => left.Divide(right);

	/// <summary>Pairs nonzero SI exponents with their quantity and unit names, in canonical SI axis order.</summary>
	public IEnumerable<DimensionComponent> GetComponents()
	{
		if (Length != Rational.Zero)
		{
			yield return new(Length, _lengthUnit);
		}

		if (Mass != Rational.Zero)
		{
			yield return new(Mass, _massUnit);
		}

		if (Time != Rational.Zero)
		{
			yield return new(Time, _timeUnit);
		}

		if (ElectricCurrent != Rational.Zero)
		{
			yield return new(ElectricCurrent, _electricCurrentUnit);
		}

		if (Temperature != Rational.Zero)
		{
			yield return new(Temperature, _temperatureUnit);
		}

		if (AmountOfSubstance != Rational.Zero)
		{
			yield return new(AmountOfSubstance, _amountOfSubstanceUnit);
		}

		if (LuminousIntensity != Rational.Zero)
		{
			yield return new(LuminousIntensity, _luminousIntensityUnit);
		}
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
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(degree);

		return Pow(new(1, degree));
	}
}
