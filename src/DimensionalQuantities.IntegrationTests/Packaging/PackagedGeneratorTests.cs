using System.Reflection;
using Microsoft.Build.Utilities.ProjectCreation;

namespace DimensionalQuantities.IntegrationTests.Packaging;

[NotInParallel]
[ClassDataSource<PackageDeliveryFixture>(Shared = SharedType.PerClass)]
public sealed class PackagedGeneratorTests : IDisposable
{
	private readonly PackageDeliveryFixture _fixture;
	private readonly PackageConsumerWorkspace _workspace;

	public PackagedGeneratorTests(PackageDeliveryFixture fixture)
	{
		_fixture = fixture;
		_workspace = fixture.CreateWorkspace();
	}

	[Test]
	public async Task CorePackageProvidesBaseUnitsCatalogueAliasesAndArithmetic()
	{
		using PackageRepository repository = PackageRepository.Create(_workspace.Directory, new Uri("https://api.nuget.org/v3/index.json"));
		Package core = PackageTestSupport.AddPackedPackage(repository, _fixture.GetPackage("DimensionalQuantities"));
		ProjectCreator consumer = _workspace.CreateProject("CoreConsumer", """
			public static class Consumer
			{
				public static double ConvertLength() => Length.FromMetres(2.5).AsMetres();
				public static double ConvertCurrent() => Current.FromAmps(3).AsAmps();
				public static double CalculateArea() => (Length.FromMetres(2) * Length.FromMetres(3)).AsSiBaseUnit();
				public static double NamedArea() => Area.FromSiBaseUnit(6).AsSiBaseUnit();
			}
			""", core);
		_workspace.Build(consumer);

		using PackageAssemblyContext context = new(_workspace.OutputDirectory("CoreConsumer"));
		Type type = context.LoadFromAssemblyPath(Path.Combine(_workspace.OutputDirectory("CoreConsumer"), "CoreConsumer.dll"))
			.GetType("Consumer", throwOnError: true)!;
		using (Assert.Multiple())
		{
			await Assert.That(InvokeDouble(type, "ConvertLength")).IsEqualTo(2.5);
			await Assert.That(InvokeDouble(type, "ConvertCurrent")).IsEqualTo(3);
			await Assert.That(InvokeDouble(type, "CalculateArea")).IsEqualTo(6);
			await Assert.That(InvokeDouble(type, "NamedArea")).IsEqualTo(6);
		}
	}

	[Test]
	public async Task MetricPackageProvidesNamedConversionsAndArithmeticThroughCoreDependency()
	{
		using PackageRepository repository = PackageRepository.Create(_workspace.Directory, new Uri("https://api.nuget.org/v3/index.json"));
		PackageTestSupport.AddPackedPackage(repository, _fixture.GetPackage("DimensionalQuantities"));
		Package metric = PackageTestSupport.AddPackedPackage(repository, _fixture.GetPackage("DimensionalQuantities.Extensions.Metric"));
		ProjectCreator consumer = _workspace.CreateProject("MetricConsumer", """
			using DimensionalQuantities.Extensions.Metric;

			public static class Consumer
			{
				public static double ConvertVoltage(double volts) => Voltage.FromVolts(volts).AsVolts();
				public static double MultiplyVoltageCurrent(double volts, double amps)
				{
					Power power = Voltage.FromVolts(volts) * Current.FromAmps(amps);
					return power.AsWatts();
				}
			}
			""", metric);
		_workspace.Build(consumer);

		using PackageAssemblyContext context = new(_workspace.OutputDirectory("MetricConsumer"));
		Type type = context.LoadFromAssemblyPath(Path.Combine(_workspace.OutputDirectory("MetricConsumer"), "MetricConsumer.dll"))
			.GetType("Consumer", throwOnError: true)!;
		using (Assert.Multiple())
		{
			await Assert.That(InvokeDouble(type, "ConvertVoltage", 12.5)).IsEqualTo(12.5);
			await Assert.That(InvokeDouble(type, "MultiplyVoltageCurrent", 12.5, 2.0)).IsEqualTo(25);
		}
	}

	[Test]
	public async Task ImperialPackageProvidesConversionsThroughCoreDependency()
	{
		using PackageRepository repository = PackageRepository.Create(_workspace.Directory, new Uri("https://api.nuget.org/v3/index.json"));
		PackageTestSupport.AddPackedPackage(repository, _fixture.GetPackage("DimensionalQuantities"));
		Package imperial = PackageTestSupport.AddPackedPackage(repository, _fixture.GetPackage("DimensionalQuantities.Extensions.Imperial"));
		ProjectCreator consumer = _workspace.CreateProject("ImperialConsumer", """
			using DimensionalQuantities.Extensions.Imperial;

			public static class Consumer
			{
				public static double ConvertLength() => Length.FromFeet(6).AsSiBaseUnit();
				public static double ConvertTemperature() => Temperature.FromRankine(491.67).AsSiBaseUnit();
			}
			""", imperial);
		_workspace.Build(consumer);

		using PackageAssemblyContext context = new(_workspace.OutputDirectory("ImperialConsumer"));
		Type type = context.LoadFromAssemblyPath(Path.Combine(_workspace.OutputDirectory("ImperialConsumer"), "ImperialConsumer.dll"))
			.GetType("Consumer", throwOnError: true)!;
		using (Assert.Multiple())
		{
			await Assert.That(Math.Round(InvokeDouble(type, "ConvertLength"), 12)).IsEqualTo(Math.Round(1.8288, 12));
			await Assert.That(Math.Round(InvokeDouble(type, "ConvertTemperature"), 12)).IsEqualTo(Math.Round(273.15, 12));
		}
	}

	[Test]
	public async Task NauticalPackageProvidesConversionsThroughCoreDependency()
	{
		using PackageRepository repository = PackageRepository.Create(_workspace.Directory, new Uri("https://api.nuget.org/v3/index.json"));
		PackageTestSupport.AddPackedPackage(repository, _fixture.GetPackage("DimensionalQuantities"));
		Package nautical = PackageTestSupport.AddPackedPackage(repository, _fixture.GetPackage("DimensionalQuantities.Extensions.Nautical"));
		ProjectCreator consumer = _workspace.CreateProject("NauticalConsumer", """
			using DimensionalQuantities.Extensions.Nautical;

			public static class Consumer
			{
				public static double ConvertLength() => Length.FromNauticalMiles(1).AsSiBaseUnit();
				public static double ConvertSpeed() => Speed.FromKnots(1).AsSiBaseUnit();
			}
			""", nautical);
		_workspace.Build(consumer);

		using PackageAssemblyContext context = new(_workspace.OutputDirectory("NauticalConsumer"));
		Type type = context.LoadFromAssemblyPath(Path.Combine(_workspace.OutputDirectory("NauticalConsumer"), "NauticalConsumer.dll"))
			.GetType("Consumer", throwOnError: true)!;
		using (Assert.Multiple())
		{
			await Assert.That(InvokeDouble(type, "ConvertLength")).IsEqualTo(1852);
			await Assert.That(InvokeDouble(type, "ConvertSpeed")).IsEqualTo(1852d / 3600d);
		}
	}

	[Test]
	public async Task CgsEmagPackageProvidesConversionsThroughCoreDependency()
	{
		using PackageRepository repository = PackageRepository.Create(_workspace.Directory, new Uri("https://api.nuget.org/v3/index.json"));
		PackageTestSupport.AddPackedPackage(repository, _fixture.GetPackage("DimensionalQuantities"));
		Package cgsEmag = PackageTestSupport.AddPackedPackage(repository, _fixture.GetPackage("DimensionalQuantities.Extensions.CgsEmag"));
		ProjectCreator consumer = _workspace.CreateProject("CgsEmagConsumer", """
			using DimensionalQuantities.Extensions.CgsEmag;

			public static class Consumer
			{
				public static double ConvertFluxDensity() => MagneticFluxDensity.FromGauss(10_000).AsSiBaseUnit();
				public static double ConvertCurrent() => Current.FromAbamperes(1).AsSiBaseUnit();
				public static double ConvertCharge() => ElectricCharge.FromAbcoulombs(1).AsSiBaseUnit();
			}
			""", cgsEmag);
		_workspace.Build(consumer);

		using PackageAssemblyContext context = new(_workspace.OutputDirectory("CgsEmagConsumer"));
		Type type = context.LoadFromAssemblyPath(Path.Combine(_workspace.OutputDirectory("CgsEmagConsumer"), "CgsEmagConsumer.dll"))
			.GetType("Consumer", throwOnError: true)!;
		using (Assert.Multiple())
		{
			await Assert.That(InvokeDouble(type, "ConvertFluxDensity")).IsEqualTo(1);
			await Assert.That(InvokeDouble(type, "ConvertCurrent")).IsEqualTo(10);
			await Assert.That(InvokeDouble(type, "ConvertCharge")).IsEqualTo(10);
		}
	}

	[Test]
	public async Task MetricNamedConversionsRequireNamespaceImport()
	{
		using PackageRepository repository = PackageRepository.Create(_workspace.Directory, new Uri("https://api.nuget.org/v3/index.json"));
		PackageTestSupport.AddPackedPackage(repository, _fixture.GetPackage("DimensionalQuantities"));
		Package metric = PackageTestSupport.AddPackedPackage(repository, _fixture.GetPackage("DimensionalQuantities.Extensions.Metric"));
		ProjectCreator consumer = _workspace.CreateProject("MissingMetricImport", """
			public static class Consumer
			{
				public static double Convert() => Voltage.FromVolts(12.5).AsVolts();
			}
			""", metric);
		consumer.TryBuild(restore: true, out bool built, out BuildOutput output);
		using (output)
		{
			string log = output.GetConsoleLog();
			using (Assert.Multiple())
			{
				await Assert.That(built).IsFalse().Because(log);
				await Assert.That(output.ErrorEvents.Any(error => error.Code == "CS0117" && error.Message!.Contains("FromVolts", StringComparison.Ordinal))).IsTrue();
				await Assert.That(output.ErrorEvents.Any(error => error.Code is "CS8032" or "CS8785" or "AD0001")).IsFalse();
				await Assert.That(output.WarningEvents.Any(warning => warning.Code is "CS8032" or "CS8785" or "AD0001")).IsFalse();
			}
		}
	}

	private static double InvokeDouble(Type type, string method, params object[] arguments)
		=> (double)type.GetMethod(method)!.Invoke(null, arguments)!;

	public void Dispose() => _workspace.Dispose();
}
