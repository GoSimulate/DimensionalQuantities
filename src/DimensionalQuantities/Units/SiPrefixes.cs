namespace DimensionalQuantities.Units;

internal static class SiPrefixes
{
	public const int Quetta = 30;
	public const int Ronna = 27;
	public const int Yotta = 24;
	public const int Zetta = 21;
	public const int Exa = 18;
	public const int Peta = 15;
	public const int Tera = 12;
	public const int Giga = 9;
	public const int Mega = 6;
	public const int Kilo = 3;
	public const int Hecto = 2;
	public const int Deca = 1;
	public const int Deci = -1;
	public const int Centi = -2;
	public const int Milli = -3;
	public const int Micro = -6;
	public const int Nano = -9;
	public const int Pico = -12;
	public const int Femto = -15;
	public const int Atto = -18;
	public const int Zepto = -21;
	public const int Yocto = -24;
	public const int Ronto = -27;
	public const int Quecto = -30;

	public static readonly Dictionary<int, SiPrefix> Entries = new()
	{
		[Quetta] = new("Quetta", "Q"),
		[Ronna] = new("Ronna", "R"),
		[Yotta] = new("Yotta", "Y"),
		[Zetta] = new("Zetta", "Z"),
		[Exa] = new("Exa", "E"),
		[Peta] = new("Peta", "P"),
		[Tera] = new("Tera", "T"),
		[Giga] = new("Giga", "G"),
		[Mega] = new("Mega", "M"),
		[Kilo] = new("Kilo", "k"),
		[Hecto] = new("Hecto", "h"),
		[Deca] = new("Deca", "da"),
		[Deci] = new("Deci", "d"),
		[Centi] = new("Centi", "c"),
		[Milli] = new("Milli", "m"),
		[Micro] = new("Micro", "µ"),
		[Nano] = new("Nano", "n"),
		[Pico] = new("Pico", "p"),
		[Femto] = new("Femto", "f"),
		[Atto] = new("Atto", "a"),
		[Zepto] = new("Zepto", "z"),
		[Yocto] = new("Yocto", "y"),
		[Ronto] = new("Ronto", "r"),
		[Quecto] = new("Quecto", "q")
	};
}
