using DimensionalQuantities.Units.Declarations;
using DimensionalQuantities.Units.Symbols;

namespace DimensionalQuantities.Extensions.Metric;

[UnitProvider]
internal static class MetricTemperatureUnits
{
	[QuantityUnit(typeof(AbsoluteTemperature))]
	[UnitFactor(typeof(MetricSymbols.Celsius))]
	private sealed class TemperatureUnit { }
}
