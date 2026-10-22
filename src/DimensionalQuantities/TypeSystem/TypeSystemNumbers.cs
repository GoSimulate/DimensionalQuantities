using JetBrains.Annotations;

namespace DimensionalQuantities.TypeSystem;

/*
 * This file deliberately violates our standard for single files per type because it's just a bunch of empty type definitions.
 *
 * The point of these types is to allow us to use generic type signatures to let us represent numbers.
 * They are markers, not calculators: the generator will do the maths and choose the types representing the result.
 *
 * TerminalHex0 through TerminalHexF represent the hexadecimal digits 0 through 15.
 * Hex<TDigit, TRest> means (16 * TRest) + TDigit, so the outermost digit is the LEAST significant:
 *
 *   TerminalHex0                                              = 0
 *   TerminalHexC                                              = 12    (0xC)
 *   Hex<TerminalHex0, TerminalHex1>                           = 16    (0x10)
 *   Hex<TerminalHexB, TerminalHex2>                           = 43    (0x2B): 16 * 2 + 11
 *   Hex<TerminalHex3, Hex<TerminalHex2, TerminalHex1>>        = 291   (0x123): 16 * (16 * 1 + 2) + 3
 *   Negative<TerminalHexC>                                    = -12
 *   Fraction<TerminalHex1, TerminalHex2>                      = 1/2
 *   Fraction<Negative<TerminalHex3>, TerminalHex2>            = -3/2
 *
 * Base 16 keeps nesting small: each Hex adds a digit rather than a single increment to the number.
 * The constraints reject invalid shapes such as leading zeros, Negative<TerminalHex0>, and zero denominators.
 * The generator must also emit one representation per value: reduce 2/4 to 1/2, encode 2/1 as TerminalHex2,
 * and encode 0/2 as TerminalHex0. C# considers alternative spellings to be different types even if they mean
 * the same number, which would otherwise make equivalent dimensions incompatible across assemblies.
 */


/// <summary>A number encoded in a type signature.</summary>
[UsedImplicitly]
public interface ITypeSystemNumber;

/// <summary>An integer encoded in a type signature.</summary>
[UsedImplicitly]
public interface ITypeSystemInteger : ITypeSystemNumber;

/// <summary>A strictly positive integer encoded in a type signature.</summary>
[UsedImplicitly]
public interface IPositiveTypeSystemInteger : ITypeSystemInteger;

/// <summary>A single hexadecimal digit.</summary>
[UsedImplicitly]
public interface IHexDigit : ITypeSystemInteger;

/// <summary>A terminal hexadecimal digit with value 0.</summary>
[UsedImplicitly]
public readonly struct TerminalHex0 : IHexDigit;

/// <summary>A terminal hexadecimal digit with value 1.</summary>
[UsedImplicitly]
public readonly struct TerminalHex1 : IHexDigit, IPositiveTypeSystemInteger;

/// <summary>A terminal hexadecimal digit with value 2.</summary>
[UsedImplicitly]
public readonly struct TerminalHex2 : IHexDigit, IPositiveTypeSystemInteger;

/// <summary>A terminal hexadecimal digit with value 3.</summary>
[UsedImplicitly]
public readonly struct TerminalHex3 : IHexDigit, IPositiveTypeSystemInteger;

/// <summary>A terminal hexadecimal digit with value 4.</summary>
[UsedImplicitly]
public readonly struct TerminalHex4 : IHexDigit, IPositiveTypeSystemInteger;

/// <summary>A terminal hexadecimal digit with value 5.</summary>
[UsedImplicitly]
public readonly struct TerminalHex5 : IHexDigit, IPositiveTypeSystemInteger;

/// <summary>A terminal hexadecimal digit with value 6.</summary>
[UsedImplicitly]
public readonly struct TerminalHex6 : IHexDigit, IPositiveTypeSystemInteger;

/// <summary>A terminal hexadecimal digit with value 7.</summary>
[UsedImplicitly]
public readonly struct TerminalHex7 : IHexDigit, IPositiveTypeSystemInteger;

/// <summary>A terminal hexadecimal digit with value 8.</summary>
[UsedImplicitly]
public readonly struct TerminalHex8 : IHexDigit, IPositiveTypeSystemInteger;

/// <summary>A terminal hexadecimal digit with value 9.</summary>
[UsedImplicitly]
public readonly struct TerminalHex9 : IHexDigit, IPositiveTypeSystemInteger;

/// <summary>A terminal hexadecimal digit with value 10.</summary>
[UsedImplicitly]
public readonly struct TerminalHexA : IHexDigit, IPositiveTypeSystemInteger;

/// <summary>A terminal hexadecimal digit with value 11.</summary>
[UsedImplicitly]
public readonly struct TerminalHexB : IHexDigit, IPositiveTypeSystemInteger;

/// <summary>A terminal hexadecimal digit with value 12.</summary>
[UsedImplicitly]
public readonly struct TerminalHexC : IHexDigit, IPositiveTypeSystemInteger;

/// <summary>A terminal hexadecimal digit with value 13.</summary>
[UsedImplicitly]
public readonly struct TerminalHexD : IHexDigit, IPositiveTypeSystemInteger;

/// <summary>A terminal hexadecimal digit with value 14.</summary>
[UsedImplicitly]
public readonly struct TerminalHexE : IHexDigit, IPositiveTypeSystemInteger;

/// <summary>A terminal hexadecimal digit with value 15.</summary>
[UsedImplicitly]
public readonly struct TerminalHexF : IHexDigit, IPositiveTypeSystemInteger;

/// <summary>
/// Encodes 16 times the remaining magnitude plus the least significant digit.
/// Canonical encodings use a TerminalHex digit for TDigit and have no leading zeros.
/// </summary>
[UsedImplicitly]
public readonly struct Hex<TDigit, TRest> : IPositiveTypeSystemInteger
	where TDigit : struct, IHexDigit
	where TRest : struct, IPositiveTypeSystemInteger;

/// <summary>Encodes the negation of a nonzero unsigned magnitude.</summary>
[UsedImplicitly]
public readonly struct Negative<TMagnitude> : ITypeSystemInteger
	where TMagnitude : struct, IPositiveTypeSystemInteger;

/// <summary>
/// Encodes a reduced rational exponent with a signed numerator and a positive denominator.
/// Canonical encodings represent integers directly, without a Fraction wrapper.
/// </summary>
[UsedImplicitly]
public readonly struct Fraction<TNumerator, TDenominator> : ITypeSystemNumber
	where TNumerator : struct, ITypeSystemInteger
	where TDenominator : struct, IPositiveTypeSystemInteger;
