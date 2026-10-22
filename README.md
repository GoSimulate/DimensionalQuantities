# DimensionalQuantities

A C# library for dimensional quantities, with generated type aliases, SI unit conversions, and compile-time dimensional arithmetic.

> [!WARNING]
> We developed this library for our own internal use and are sharing it because we believe it may be useful to the wider developer community. Issues and pull requests are welcome, but we make no support commitments or warranties, and we cannot guarantee that we will respond to issues or review or merge pull requests.

## Usage

Requires .NET 10 and C# 14. The `DimensionalQuantities` NuGet package includes both the runtime library and its source generator.

```csharp
Voltage voltage = Voltage.FromVolts(12);
Current current = Current.FromAmps(3);

Resistance resistance = voltage / current;
Power power = voltage * current;

double ohms = resistance.AsOhms(); // 4
double watts = power.AsWatts();    // 36
```

### SI prefixes and compound units

Named helpers handle conversion into and out of SI prefixes, including powers of prefixed units:

```csharp
Length length = Length.FromMilliMetres(2500);
double metres = length.AsMetres(); // 2.5

Density density = Density.FromGramsPerCubicCentiMetre(1);
double kilogramsPerCubicMetre = density.AsSiBaseUnit(); // 1000
```

For more control, `FromPrefixedUnits` and `AsPrefixedUnits` accept decimal prefix exponents for each unit factor. Powers are taken into account automatically:

```csharp
// 1 A/mm² = 1,000,000 A/m²
CurrentDensity density = CurrentDensity.FromPrefixedUnits(
    1, ampPrefixExponent: 0, metrePrefixExponent: -3);

double ampsPerSquareMillimetre = density.AsPrefixedUnits(
    ampPrefixExponent: 0, metrePrefixExponent: -3); // 1
```

Named compound units remain a single factor where appropriate: `Voltage.FromPrefixedUnits(2, -3)` means 2 millivolts, without requiring you to know the SI dimensions of a volt.

### Arithmetic and generated intermediate quantities

The generator follows multiplication and division through nested expressions, derives their dimensions, and generates the required operators and intermediate aliases:

```csharp
Current current = Current.FromAmps(3);
Resistance resistance = Resistance.FromOhms(4);

CurrentSquared squared = current * current;
Power heating = current * current * resistance; // 36 W
Current doubled = 2 * current;
Current halved = current / 2;
Frequency frequency = 1 / Time.FromSeconds(0.02); // 50 Hz
```

Known dimensions use names from the engineering catalogue, such as `Power`, `Voltage`, `Permeability` and `MagneticFluxDensity`. Other dimensions receive mechanical names such as `CurrentSquared` or `TimePerCurrent`. Discovered quantities have SI base-unit and generic prefix conversion helpers.

Aliases with the same dimensions share a type. For example, `Energy` and `Torque` are interchangeable: the library checks dimensions, not the physical meaning of a calculation. Magnitudes are stored as `double` in coherent SI base units.

### Current scope

Arithmetic discovery currently supports multiplication and division with known quantity operands, scalar factors, and nested expressions. Roots, powers, dimensionless results and inference through unresolved intermediate local variables are not yet supported by discovery.

The library is under active development; the API and supported expression patterns are still evolving.

## Licence

[MIT](LICENSE), copyright GoSim.
