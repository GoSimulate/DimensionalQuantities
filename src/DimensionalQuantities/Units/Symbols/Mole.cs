using DimensionalQuantities.Units.Declarations;

namespace DimensionalQuantities.Units.Symbols;

/// <summary>The SI mole symbol for unit-provider declarations.</summary>
[UnitSymbol(SiUnitMetadata.Mole.Singular, SiUnitMetadata.Mole.Plural, SiUnitMetadata.Mole.Symbol)]
public sealed class Mole
{
	private Mole() { }
}
