using System.Text.Json;
using DimensionalQuantities.IntegrationTests.Delivery;
using Xunit;

namespace DimensionalQuantities.IntegrationTests;

public sealed class PackageInteroperabilityTests
{
	[Fact]
	public async Task IndependentPackagesShareQuantitiesAndConsumerCanExtendArithmetic()
	{
		using PackageSandbox sandbox = new();
		await sandbox.Pack("DimensionalQuantities.Extensions.Metric");
		await sandbox.PackProducer("ProjectA", """
			namespace PackageA;

			public static class Producer
			{
				public static Voltage Create() => Voltage.FromSiBaseUnit(6);
				public static LengthCurrent CreateIntermediate() => Length.FromMetres(2) * Current.FromAmps(3);
				public static List<Voltage> Values() => [Create(), Create()];
				public static Measurement Measure() => new();
			}

			public sealed class Measurement
			{
				public Voltage Value => Producer.Create();
			}
			""", "DimensionalQuantities");
		await sandbox.PackProducer("ProjectB", """
			using DimensionalQuantities.Extensions.Metric;

			namespace PackageB;

			public static class Consumer
			{
				public static Voltage Create() => Voltage.FromVolts(10);
				public static LengthCurrent CreateIntermediate() => Length.FromMetres(5) * Current.FromAmps(2);
				public static double ReadIntermediate(LengthCurrent value) => value.AsSiBaseUnit();
				public static double Read(Voltage value) => value.AsVolts();
				public static double Sum(IEnumerable<Voltage> values)
				{
					double total = 0;
					foreach (Voltage value in values)
					{
						total += value.AsVolts();
					}

					return total;
				}
			}
			""", "DimensionalQuantities.Extensions.Metric");
		sandbox.WriteConsumer("""
			using System.Text.Json;
			using DimensionalQuantities.Extensions.Metric;
			using PackageA;

			Type produced = typeof(Producer).GetMethod("Create")!.ReturnType;
			Type independentlyProduced = typeof(PackageB.Consumer).GetMethod("Create")!.ReturnType;
			Type accepted = typeof(PackageB.Consumer).GetMethod("Read")!.GetParameters().Single().ParameterType;
			Type intermediateFromA = typeof(Producer).GetMethod("CreateIntermediate")!.ReturnType;
			Type intermediateFromB = typeof(PackageB.Consumer).GetMethod("CreateIntermediate")!.ReturnType;
			Type intermediateAccepted = typeof(PackageB.Consumer).GetMethod("ReadIntermediate")!.GetParameters().Single().ParameterType;
			LengthCurrent intermediate = Producer.CreateIntermediate();
			LengthTimeCurrent extended = intermediate * Time.FromSeconds(4);

			Console.WriteLine(JsonSerializer.Serialize(new
			{
				SharedTypes = new
				{
					NamedProducers = produced == independentlyProduced,
					NamedConsumer = produced == accepted,
					IntermediateProducers = intermediateFromA == intermediateFromB,
					IntermediateConsumer = intermediateFromA == intermediateAccepted,
					IntermediateRuntime = intermediateFromA.Assembly == typeof(Length).Assembly,
					ExtendedRuntime = extended.GetType().Assembly == typeof(Length).Assembly
				},
				Values = new
				{
					Direct = PackageB.Consumer.Read(Producer.Create()),
					Collection = PackageB.Consumer.Sum(Producer.Values()),
					Contained = PackageB.Consumer.Read(Producer.Measure().Value),
					Prefixed = Producer.Create().AsPrefixedUnits(3),
					Product = (Producer.Create() * Current.FromAmps(4)).AsWatts(),
					Intermediate = PackageB.Consumer.ReadIntermediate(intermediate),
					IndependentIntermediate = PackageB.Consumer.ReadIntermediate(PackageB.Consumer.CreateIntermediate()),
					IntermediateProduct = extended.AsSiBaseUnit()
				}
			}));
			""", "ProjectA", "ProjectB", "DimensionalQuantities.Extensions.Metric");

		using JsonDocument result = JsonDocument.Parse(await sandbox.ExecuteConsumer());
		JsonElement sharedTypes = result.RootElement.GetProperty("SharedTypes");
		JsonElement values = result.RootElement.GetProperty("Values");
		Assert.Multiple(
			() => Assert.True(sharedTypes.GetProperty("NamedProducers").GetBoolean()),
			() => Assert.True(sharedTypes.GetProperty("NamedConsumer").GetBoolean()),
			() => Assert.True(sharedTypes.GetProperty("IntermediateProducers").GetBoolean()),
			() => Assert.True(sharedTypes.GetProperty("IntermediateConsumer").GetBoolean()),
			() => Assert.True(sharedTypes.GetProperty("IntermediateRuntime").GetBoolean()),
			() => Assert.True(sharedTypes.GetProperty("ExtendedRuntime").GetBoolean()),
			() => Assert.Equal(6d, values.GetProperty("Direct").GetDouble()),
			() => Assert.Equal(12d, values.GetProperty("Collection").GetDouble()),
			() => Assert.Equal(6d, values.GetProperty("Contained").GetDouble()),
			() => Assert.Equal(0.006d, values.GetProperty("Prefixed").GetDouble(), 12),
			() => Assert.Equal(24d, values.GetProperty("Product").GetDouble()),
			() => Assert.Equal(6d, values.GetProperty("Intermediate").GetDouble()),
			() => Assert.Equal(10d, values.GetProperty("IndependentIntermediate").GetDouble()),
			() => Assert.Equal(24d, values.GetProperty("IntermediateProduct").GetDouble()));
	}
}
