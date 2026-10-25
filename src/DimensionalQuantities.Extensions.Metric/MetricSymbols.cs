using DimensionalQuantities.Units.Declarations;

namespace DimensionalQuantities.Extensions.Metric;

internal static class MetricSymbols
{
	[UnitSymbol("Radian", "Radians", "rad")]
	internal sealed class Radian { }
	[UnitSymbol("Celsius", "Celsius", "°C")]
	[AffineUnit(273.15)]
	internal sealed class Celsius { }
	[UnitSymbol("Hertz", "Hertz", "Hz")]
	internal sealed class Hertz { }
	[UnitSymbol("Pascal", "Pascals", "Pa")]
	internal sealed class Pascal { }
	[UnitSymbol("Joule", "Joules", "J")]
	internal sealed class Joule { }
	[UnitSymbol("Newton", "Newtons", "N")]
	internal sealed class Newton { }
	[UnitSymbol("Watt", "Watts", "W")]
	internal sealed class Watt { }
	[UnitSymbol("Volt", "Volts", "V")]
	internal sealed class Volt { }
	[UnitSymbol("Ohm", "Ohms", "Ω")]
	internal sealed class Ohm { }
	[UnitSymbol("Henry", "Henries", "H")]
	internal sealed class Henry { }
	[UnitSymbol("Weber", "Webers", "Wb")]
	internal sealed class Weber { }
	[UnitSymbol("Tesla", "Teslas", "T")]
	internal sealed class Tesla { }
	[UnitSymbol("Siemens", "Siemens", "S")]
	internal sealed class Siemens { }
}
