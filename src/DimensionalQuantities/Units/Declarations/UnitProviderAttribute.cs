namespace DimensionalQuantities.Units.Declarations;

/// <summary>Marks a type whose nested declarations define conversions for this assembly.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class UnitProviderAttribute : Attribute
{
	/// <summary>Generate named conversions for all supported SI prefixes.</summary>
	public bool GenerateSiPrefixes { get; set; }

	/// <summary>Generate parameterised prefix conversions.</summary>
	public bool GeneratePrefixConversions { get; set; }
}
