using System.Text.Json;
using DimensionalQuantities.IntegrationTests.Delivery;
using DimensionalQuantities.IntegrationTests.Fixtures;
using Xunit;

namespace DimensionalQuantities.IntegrationTests;

[Collection(typeof(PackageDeliveryCollection))]
public sealed class PackageReferenceTests
{
	private readonly PackageDeliveryFixture _fixture;

	public PackageReferenceTests(PackageDeliveryFixture fixture)
	{
		_fixture = fixture;
	}

	[Fact]
	public async Task CorePackageActivatesGeneratorAndRunsWithoutToolingRuntimeDependencies()
	{
		using PackageSandbox sandbox = _fixture.CreateConsumer();
		sandbox.WriteConsumer("""
			Console.WriteLine((Length.FromMetres(6) * Current.FromAmps(2)).AsSiBaseUnit());
			""", "DimensionalQuantities");
		string output = await sandbox.ExecuteConsumer();
		using JsonDocument dependencies = JsonDocument.Parse(File.ReadAllText(Path.Combine(sandbox.Consumer, "bin/Release/net11.0/Consumer.deps.json")));
		Assert.Multiple(
			() => Assert.Equal("12", output),
			() => Assert.Equal(["Consumer", "DimensionalQuantities"], dependencies.RootElement.GetProperty("libraries").EnumerateObject().Select(p => p.Name.Split('/')[0]).Order().ToArray()));
	}

	[Fact]
	public async Task CorePackageDeliversDimensionalMismatchAnalyzer()
	{
		using PackageSandbox sandbox = _fixture.CreateConsumer();
		sandbox.WriteConsumer("""
			Console.WriteLine(Length.FromMetres(1) + Time.FromSeconds(1));
			""", "DimensionalQuantities");
		PackageSandbox.ProcessResult result = await sandbox.BuildConsumer();
		Assert.Multiple(
			() => Assert.NotEqual(0, result.ExitCode),
			() => Assert.Contains("error DQ0001:", result.Output));
	}

	[Theory]
	[InlineData("Metric", "Voltage.FromVolts(2)")]
	[InlineData("Imperial", "Length.FromFeet(2)")]
	[InlineData("Nautical", "Length.FromNauticalMiles(2)")]
	[InlineData("CgsEmag", "MagneticFluxDensity.FromGauss(2)")]
	public async Task OptionalPackageAloneSuppliesMatchingCoreAndCallableConversions(string extension, string expression)
	{
		string id = "DimensionalQuantities.Extensions." + extension;
		using PackageSandbox sandbox = _fixture.CreateConsumer();
		sandbox.WriteConsumer($$"""
			using {{id}};
			Console.WriteLine(({{expression}}).AsSiBaseUnit() > 0);
			""", id);
		string output = await sandbox.ExecuteConsumer();
		using JsonDocument assets = JsonDocument.Parse(File.ReadAllText(Path.Combine(sandbox.Consumer, "obj/project.assets.json")));
		string[] direct = assets.RootElement.GetProperty("project").GetProperty("frameworks").EnumerateObject().Single().Value.GetProperty("dependencies").EnumerateObject().Select(p => p.Name).ToArray();
		Assert.Multiple(
			() => Assert.Equal("True", output),
			() => Assert.True(assets.RootElement.GetProperty("libraries").TryGetProperty("DimensionalQuantities/" + sandbox.Version, out _)),
			() => Assert.Equal([id], direct));
	}

	[Theory]
	[InlineData("Metric", "Voltage.FromVolts(2)")]
	[InlineData("Imperial", "Length.FromFeet(2)")]
	[InlineData("Nautical", "Length.FromNauticalMiles(2)")]
	[InlineData("CgsEmag", "MagneticFluxDensity.FromGauss(2)")]
	public async Task OptionalConversionsRequireNamespaceImport(string extension, string expression)
	{
		string id = "DimensionalQuantities.Extensions." + extension;
		using PackageSandbox sandbox = _fixture.CreateConsumer();
		sandbox.WriteConsumer($$"""
			Console.WriteLine(({{expression}}).AsSiBaseUnit());
			""", id);
		PackageSandbox.ProcessResult result = await sandbox.BuildConsumer();
		Assert.Multiple(
			() => Assert.NotEqual(0, result.ExitCode),
			() => Assert.Contains("error CS0117:", result.Output));
	}

	[Theory]
	[InlineData("Voltage.FromVolts(2)")]
	[InlineData("Length.FromFeet(2)")]
	[InlineData("Length.FromNauticalMiles(2)")]
	[InlineData("MagneticFluxDensity.FromGauss(2)")]
	public async Task CorePackageDoesNotSupplyOptionalConversions(string expression)
	{
		using PackageSandbox sandbox = _fixture.CreateConsumer();
		sandbox.WriteConsumer($$"""
			Console.WriteLine(({{expression}}).AsSiBaseUnit());
			""", "DimensionalQuantities");
		PackageSandbox.ProcessResult result = await sandbox.BuildConsumer();
		Assert.Multiple(
			() => Assert.NotEqual(0, result.ExitCode),
			() => Assert.Contains("error CS0117:", result.Output));
	}

	[Fact]
	public async Task CompatibleOptionalPackagesComposeThroughOneRuntimeType()
	{
		using PackageSandbox sandbox = _fixture.CreateConsumer();
		string[] ids = ["DimensionalQuantities.Extensions.Metric", "DimensionalQuantities.Extensions.Imperial", "DimensionalQuantities.Extensions.Nautical", "DimensionalQuantities.Extensions.CgsEmag"];
		sandbox.WriteConsumer("""
			using DimensionalQuantities.Extensions.Metric;
			using DimensionalQuantities.Extensions.Imperial;
			using DimensionalQuantities.Extensions.Nautical;
			using DimensionalQuantities.Extensions.CgsEmag;
			Length total = Length.FromFeet(1) + Length.FromNauticalMiles(1);
			Energy work = Energy.FromJoules(2) + Energy.FromErgs(10_000_000);
			Console.WriteLine(total.AsSiBaseUnit() > 1852 && work.AsSiBaseUnit() == 3);
			""", ids);
		Assert.Equal("True", await sandbox.ExecuteConsumer());
	}
}
