using Assembly = System.Reflection.Assembly;
using DimensionalQuantities.Extensions.Nautical;

namespace DimensionalQuantities.Extensions.Nautical.Tests;

public sealed class NauticalConversionsTests
{
	[Test]
	public async Task NauticalUnitsConvertToExpectedSiValues()
	{
		double[] result =
		[
			Length.FromNauticalMiles(1).AsSiBaseUnit(),
			Length.FromFathoms(1).AsSiBaseUnit(),
			Area.FromSquareNauticalMiles(1).AsSiBaseUnit(),
			Speed.FromKnots(1).AsSiBaseUnit(),
		];

		double[] expected = [1852d, 1.8288d, 3_429_904d, 1852d / 3600d];
		await Assert.That(result).IsEquivalentTo(expected, TUnit.Assertions.Enums.CollectionOrdering.Matching);
	}

	[Test]
	public async Task NauticalUnitsConvertBackFromSiValues()
	{
		double[] result =
		[
			Length.FromSiBaseUnit(1852).AsNauticalMiles(),
			Length.FromSiBaseUnit(1.8288).AsFathoms(),
			Area.FromSiBaseUnit(3_429_904).AsSquareNauticalMiles(),
			Speed.FromSiBaseUnit(1852d / 3600d).AsKnots(),
		];

		await Assert.That(result).IsEquivalentTo(new double[] { 1d, 1d, 1d, 1d }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
	}

	[Test]
	public async Task NauticalUnitsDoNotGenerateSiPrefixedConversions()
	{
		string[] names = Assembly.Load("DimensionalQuantities.Extensions.Nautical").GetExportedTypes()
			.Where(type => type.Namespace == "DimensionalQuantities.Extensions.Nautical")
			.SelectMany(type => type.GetMethods()).Select(method => method.Name).ToArray();

		using (Assert.Multiple())
		{
			await Assert.That(names.Contains("FromKiloNauticalMiles")).IsFalse();
			await Assert.That(names.Contains("FromMilliKnots")).IsFalse();
			await Assert.That(names.Contains("FromPrefixedUnits")).IsFalse();
			await Assert.That(names.Contains("AsPrefixedUnits")).IsFalse();
		}
	}
}
