using DimensionalQuantities.Units.Declarations;
using DimensionalQuantities.Units.Symbols;

namespace DimensionalQuantities.Extensions.Metric;

[UnitProvider]
internal static class MetricTemperatureUnits
{
	[QuantityUnit(typeof(Temperature))]
	[UnitFactor(typeof(MetricSymbols.Celsius))]
	private sealed class TemperatureUnit { }
}
