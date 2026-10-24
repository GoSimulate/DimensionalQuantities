using System.Reflection;
using DimensionalQuantities.Extensions.Nautical;
using Xunit;

namespace DimensionalQuantities.Extensions.Nautical.Tests;

public sealed class NauticalConversionsTests
{
	[Fact]
	public void NauticalUnitsConvertToExpectedSiValues()
	{
		double[] result =
		[
			Length.FromNauticalMiles(1).AsSiBaseUnit(),
			Length.FromFathoms(1).AsSiBaseUnit(),
			Area.FromSquareNauticalMiles(1).AsSiBaseUnit(),
			Speed.FromKnots(1).AsSiBaseUnit(),
		];

		double[] expected = [1852d, 1.8288d, 3_429_904d, 1852d / 3600d];
		Assert.Equal(expected, result);
	}

	[Fact]
	public void NauticalUnitsConvertBackFromSiValues()
	{
		double[] result =
		[
			Length.FromSiBaseUnit(1852).AsNauticalMiles(),
			Length.FromSiBaseUnit(1.8288).AsFathoms(),
			Area.FromSiBaseUnit(3_429_904).AsSquareNauticalMiles(),
			Speed.FromSiBaseUnit(1852d / 3600d).AsKnots(),
		];

		Assert.Equal([1d, 1d, 1d, 1d], result);
	}

	[Fact]
	public void NauticalUnitsDoNotGenerateSiPrefixedConversions()
	{
		string[] names = Assembly.Load("DimensionalQuantities.Extensions.Nautical").GetExportedTypes()
			.Where(type => type.Namespace == "DimensionalQuantities.Extensions.Nautical")
			.SelectMany(type => type.GetMethods()).Select(method => method.Name).ToArray();

		Assert.Multiple(
			() => Assert.DoesNotContain("FromKiloNauticalMiles", names),
			() => Assert.DoesNotContain("FromMilliKnots", names),
			() => Assert.DoesNotContain("FromPrefixedUnits", names),
			() => Assert.DoesNotContain("AsPrefixedUnits", names));
	}
}
