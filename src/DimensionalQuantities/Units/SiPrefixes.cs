namespace DimensionalQuantities.Units;

internal static class SiPrefixes
{
	public static readonly Dictionary<int, SiPrefix> Entries = new()
	{
		[30] = new("Quetta", "Q"),
		[27] = new("Ronna", "R"),
		[24] = new("Yotta", "Y"),
		[21] = new("Zetta", "Z"),
		[18] = new("Exa", "E"),
		[15] = new("Peta", "P"),
		[12] = new("Tera", "T"),
		[9] = new("Giga", "G"),
		[6] = new("Mega", "M"),
		[3] = new("Kilo", "k"),
		[2] = new("Hecto", "h"),
		[1] = new("Deca", "da"),
		[-1] = new("Deci", "d"),
		[-2] = new("Centi", "c"),
		[-3] = new("Milli", "m"),
		[-6] = new("Micro", "µ"),
		[-9] = new("Nano", "n"),
		[-12] = new("Pico", "p"),
		[-15] = new("Femto", "f"),
		[-18] = new("Atto", "a"),
		[-21] = new("Zepto", "z"),
		[-24] = new("Yocto", "y"),
		[-27] = new("Ronto", "r"),
		[-30] = new("Quecto", "q")
	};
}
