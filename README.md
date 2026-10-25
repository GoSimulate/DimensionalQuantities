# DimensionalQuantities

[![CI](https://img.shields.io/github/actions/workflow/status/GoSimulate/DimensionalQuantities/ci.yml?branch=main&style=for-the-badge&logo=github&label=CI)](https://github.com/GoSimulate/DimensionalQuantities/actions/workflows/ci.yml)
[![CD](https://img.shields.io/github/actions/workflow/status/GoSimulate/DimensionalQuantities/cd.yml?branch=main&style=for-the-badge&logo=github&label=CD)](https://github.com/GoSimulate/DimensionalQuantities/actions/workflows/cd.yml)
[![NuGet](https://img.shields.io/nuget/v/DimensionalQuantities?style=for-the-badge&logo=nuget)](https://www.nuget.org/packages/DimensionalQuantities/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/DimensionalQuantities?style=for-the-badge)](https://www.nuget.org/packages/DimensionalQuantities/)
[![License](https://img.shields.io/github/license/GoSimulate/DimensionalQuantities?style=for-the-badge)](LICENSE)

A C# library for dimensional quantities, with generated type aliases, SI unit conversions, and compile-time dimensional arithmetic.

> [!WARNING]
> We developed this library for our own internal use and are sharing it because we believe it may be useful to the wider developer community. Issues and pull requests are welcome, but we make no support commitments or warranties, and we cannot guarantee that we will respond to issues or review or merge pull requests.

## Usage

The runtime library targets .NET Standard 2.0. Consumers need a C# 14 compiler for the generated extension blocks. The `DimensionalQuantities` NuGet package includes the runtime library, source generator, analysers and an alias quick fix.

Core includes the seven SI base quantities' conversion helpers. Add `DimensionalQuantities.Extensions.Metric` for catalogue conversions such as volts, watts and pascals, and import its namespace:

```csharp
using DimensionalQuantities.Extensions.Metric;
```

Metric depends on the matching core version, so referencing Metric also supplies core and its compiler tooling. Quantity names and dimensional arithmetic are available with core alone.

Add `DimensionalQuantities.Extensions.Imperial` for British and US customary conversions. It includes feet, miles, stones, pounds-force, US and Imperial liquid measures, Fahrenheit, mechanical horsepower and the International Table British thermal unit. Import `DimensionalQuantities.Extensions.Imperial` to use its helpers:

```csharp
Length depth = Length.FromFeet(6);
Volume fuel = Volume.FromImperialGallons(2);
Energy heat = Energy.FromBritishThermalUnitsIT(1);

double metres = depth.AsSiBaseUnit(); // 1.8288
```

`BritishThermalUnitIT` is the International Table definition (exactly 1055.05585262 J); other BTU definitions differ. US and Imperial liquid measures are named separately. Imperial helpers do not generate SI-prefixed variants.

Add `DimensionalQuantities.Extensions.Nautical` for international nautical miles, fathoms, square nautical miles and knots. Import its namespace to use the helpers:

```csharp
using DimensionalQuantities.Extensions.Nautical;

Length passage = Length.FromNauticalMiles(12);
Speed vesselSpeed = Speed.FromKnots(8);
```

A nautical mile is exactly 1852 m, and a knot is one nautical mile per hour. Nautical helpers do not generate SI-prefixed variants.

Add `DimensionalQuantities.Extensions.CgsEmag` for practical CGS mechanical, electromagnetic (EMU), and magnetic conversions. Import its namespace to use helpers such as:

```csharp
using DimensionalQuantities.Extensions.CgsEmag;

MagneticFluxDensity field = MagneticFluxDensity.FromGauss(10_000);
MagneticFieldStrength strength = MagneticFieldStrength.FromOersteds(1);
Energy work = Energy.FromErgs(1_000_000);
```

It includes dynes, ergs, baryes, gals, the common `ab` electrical units, maxwells, gauss and oersteds. These are named conversions into coherent SI quantities; the package does not model a separate CGS electromagnetic dimension system or generate SI-prefixed variants. See [NIST's CGS unit definitions](https://www.nist.gov/pml/special-publication-811/nist-guide-si-chapter-5-units-outside-si) for the distinction between EMU, ESU and Gaussian systems.

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

The generator derives dimensions through arithmetic and generates the required operators and intermediate aliases. Quantities also support addition, subtraction and equality checks with quantities of the same dimension:

```csharp
Current current = Current.FromAmps(3);
Resistance resistance = Resistance.FromOhms(4);

CurrentSquared squared = current * current;
Power heating = current * current * resistance; // 36 W
Current doubled = 2 * current;
Current halved = current / 2;
Current total = current + doubled;
double ratio = doubled / current; // 2: matching dimensions cancel
Frequency frequency = 1 / Time.FromSeconds(0.02); // 50 Hz
```

Known dimensions use names from the engineering catalogue, such as `Power`, `Voltage`, `Permeability` and `MagneticFluxDensity`. Other dimensions receive mechanical names such as `CurrentSquared` or `TimePerCurrent`. Discovered quantities have SI base-unit and generic prefix conversion helpers.

Aliases with the same dimensions share a type. For example, `Energy` and `Torque` are interchangeable: the library checks dimensions, not the physical meaning of a calculation. Magnitudes are stored as `double` in coherent SI base units.

### Comparisons and numerical helpers

Quantities with matching dimensions support `<`, `>`, `<=` and `>=`, along with `Abs`, `Min`, `Max` and `Clamp`:

```csharp
Current measured = Current.FromAmps(-3);
Current limit = Current.FromAmps(10);

Current magnitude = Current.Abs(measured); // 3 A
bool withinLimit = magnitude <= limit;
Current smaller = Current.Min(magnitude, limit);
Current larger = Current.Max(magnitude, limit);
Current bounded = Current.Clamp(measured, Current.FromAmps(0), limit); // 0 A
```

Comparisons follow ordinary `double` behaviour, including comparisons with NaN. The numerical helpers follow the corresponding `Math` behaviour.

### Powers and roots

SI base and catalogue quantities have `Squared()`, `Cubed()`, `Pow4()` through `Pow6()`, and the corresponding `SquareRoot()`, `CubeRoot()`, `Root4()` through `Root6()` helpers. Helpers for intermediate quantities are generated when called.

For other integer or rational powers, use `Power<TExponent>.Of(...)`. English exponent aliases such as `NumberTwo`, `NumberHalf` and `NumberTwoOverThree` are generated on demand:

```csharp
using DimensionalQuantities.TypeSystem;

Length length = Length.FromMetres(3);
Area area = length.Squared();
Volume volume = length.Cubed();
Length recovered = Power<NumberHalf>.Of(area); // 3 m
```

Fractional dimensions are supported. Results whose dimensions cancel completely, including a zeroth power, are returned as `double`.

### Nullable quantities

Arithmetic supports nullable quantity operands and nullable scalars. Missing values propagate as `null`; power and root helpers can be called through `?.`:

```csharp
Length? length = Length.FromMetres(3);
Current? current = null;

LengthCurrent? product = length * current; // null
Area? area = length?.Squared();            // 9 m²
Length? recovered = area?.SquareRoot();    // 3 m
```

### Discovery through your code

Intermediate quantities can flow through:

- `var` locals and explicitly typed locals.
- Conditional (`?:`) and switch expressions whose value branches have matching dimensions.
- Fields, properties and helper method returns, including declarations in other source files.
- Parameters, arrays, generic collections, spans and indexers.
- Generic interface members, constrained generic calls, `out` variables and `ref` returns.
- Pattern variables and deconstruction results.
- Awaited results, iterator elements and async iterator elements.

Canonical compound names can also be used directly in type declarations, without first writing an expression that produces them:

```csharp
static LengthCurrent Combine(Length length, Current current) => length * current;

LengthCurrent combined = Combine(Length.FromMetres(2), Current.FromAmps(3));
var timed = combined * Time.FromSeconds(4);
double value = timed.AsSiBaseUnit(); // 24
```

The generator recognises canonical names such as `LengthCurrent`, `LengthPerTime` and `SquareRootLength`. Canonical-name discovery leaves existing bound types and explicit aliases alone. Incompatible dimensions remain compiler errors.

### Sharing quantities between packages

Generated names are aliases for types in the shared runtime library. Independently compiled packages can therefore exchange the same quantity through method parameters, return values, collections and containing objects.

A consuming project with the generator can discover quantities from referenced values, generate local conversion helpers, and use those values in further arithmetic. Quantity aliases are generated separately in each compilation; they are not exported by a referenced assembly.

### Diagnostics and quick fixes

The analysers and generator report dimensional mistakes and problems with generated names or type encodings:

| Diagnostic | Severity | Meaning |
| --- | --- | --- |
| DQ0001 | Error | Arithmetic or comparison operands have incompatible dimensions. |
| DQ0002 | Error | An assignment, return value or argument has the wrong dimensions for its destination. |
| DQ0003 | Info | A raw `Quantity<...>` type can be written using a readable quantity alias. |
| DQ0004 | Warning | A type-system number uses a noncanonical encoding: equivalent numeric values can still be different C# types. |
| DQ0005 | Error | A closed type-system encoding is invalid. |
| DQ0006 | Warning | A generated alias conflicts with a declared name or shadows an imported type. |
| DQ0007 | Error | An unresolved English exponent in `Power<Number...>.Of(...)` cannot be parsed, for example `NumberTwoOverZero`. |
| DQ0008 | Error | A unit provider declaration is invalid, for example a missing unit symbol or a zero power denominator. |

DQ0003 has an IDE quick fix to replace the raw type with its readable alias, such as `Length`. The fix is offered only when the alias resolves to the same type; it is withheld inside `nameof`, where replacing the type could change the resulting string.

Alias collision warnings do not suppress or rename generated aliases. Use qualified names or distinct explicit aliases to disambiguate your own types.

### Defining an extension

An extension project references core and declares its units using attributes. The generator compiles public conversion helpers into that extension's assembly; consumers reference the extension and import its namespace.

```csharp
using DimensionalQuantities.Units.Declarations;

namespace Example.Units;

[UnitProvider]
internal static class CustomUnits
{
    [QuantityUnit(typeof(Length))]
    [UnitFactor(typeof(CustomSymbols.Foot))]
    private sealed class LengthUnit;

    [QuantityUnit(typeof(Pressure))]
    [QuantityUnit(typeof(Stress))]
    [QuantityUnit(typeof(YoungsModulus))]
    [UnitFactor(typeof(CustomSymbols.Pascal))]
    private sealed class PascalUnit;
}

internal static class CustomSymbols
{
    [UnitSymbol("Foot", "Feet", "ft", Scale = 0.3048)]
    internal sealed class Foot;

    [UnitSymbol("Pascal", "Pascals", "Pa")]
    internal sealed class Pascal;
}
```

Consumers can then use `Length.FromFeet(6)` and `length.AsFeet()` with `using Example.Units;`. The Pascal declaration associates the same unit with Pressure, Stress and Young’s modulus; their shared underlying type receives one set of conversion helpers.

`Scale` converts each atomic unit to SI. Ordered `UnitFactor` attributes describe compound units, with `Power`, `PowerDenominator` and `PrefixExponent` controlling each factor. An optional `[UnitConversion(Offset = ...)]` on the unit declaration supplies an offset added after scaling into SI and subtracted before converting back; it applies to every associated quantity. A provider can opt into named SI prefixes and parameterised prefix conversions with `GenerateSiPrefixes` and `GeneratePrefixConversions`; both default to false. `AdditionalUnit` references another factor declaration for an alternative conversion.

### Current scope

When upgrading from the combined package, add the Metric package and namespace wherever you use catalogue conversions, including their `FromPrefixedUnits` and `AsPrefixedUnits` helpers. Their names and conversion behaviour are unchanged. Core-only consumers can use `FromSiBaseUnit`, `AsSiBaseUnit`, `FromArbitrarySiComponents` and `AsArbitrarySiComponents` for every quantity.

Discovery uses the types and operations that the compiler can resolve as generated quantities become available. It supports async methods and iterators with quantity return or element types, but does not infer a new variable type from arbitrary reassignment chains.

The library is under active development; the API and supported expression patterns are still evolving.

## Licence

[MIT](LICENSE), copyright GoSim.
