using JetBrains.Annotations;

namespace DimensionalQuantities;

/// <summary>A quantity whose dimension is encoded in its type and whose magnitude is stored in SI units.</summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public readonly record struct Quantity<TDimension>
{
	internal Quantity(double si)
	{
		Si = si;
	}

	internal double Si { get; }
}
