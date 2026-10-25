using DimensionalQuantities.Units.Declarations;

namespace DimensionalQuantities.Units.Symbols;

/// <summary>The gram symbol for unit-provider declarations; coherent SI mass uses a kilogram.</summary>
[UnitSymbol(SiUnitMetadata.Gram.Singular, SiUnitMetadata.Gram.Plural, SiUnitMetadata.Gram.Symbol, SiBasePrefixExponent = SiBasePrefixExponents.Mass)]
public sealed class Gram
{
	private Gram() { }
}
