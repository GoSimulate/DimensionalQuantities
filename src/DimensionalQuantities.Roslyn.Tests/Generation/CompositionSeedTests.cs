using DimensionalQuantities.Roslyn.Tests.Framework;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generation;

public sealed class CompositionSeedTests
{
	[Theory]
	[InlineData("new LengthCurrent()")]
	[InlineData("default(LengthCurrent)")]
	[InlineData("Make()")]
	public async Task ExplicitIntermediateTypesSupplyDimensionsWithoutInitialisers(string expression)
	{
		string source = $$"""
			public static class Consumer
			{
				private static LengthCurrent Make() => default;
				public static LengthTimeCurrent Calculate() => {{expression}} * Time.FromSeconds(4);
				public static double Execute() => Calculate().AsSiBaseUnit();
			}
			""";
		QuantityGeneratorTest test = new()
		{
			TestCode = source
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		double result = GeneratedCodeExecution.Execute(test.Compilation);

		Assert.Equal(0, result);
	}

	[Theory]
	[InlineData("FromSiBaseUnit(6000)")]
	[InlineData("FromPrefixedUnits(6, 3, 0)")]
	public async Task StandaloneCanonicalFactoryRequestsAQuantityAndItsConversions(string factory)
	{
		string source = $$"""
			public static class Consumer
			{
				public static double Execute() => LengthCurrent.{{factory}}.AsPrefixedUnits(3, 0);
			}
			""";
		QuantityGeneratorTest test = new()
		{
			TestCode = source
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		double result = GeneratedCodeExecution.Execute(test.Compilation);

		Assert.Equal(6, result);
	}

	[Theory]
	[InlineData("""
		public LengthCurrent Stored = Length.FromMetres(2) * Current.FromAmps(3);
		""")]
	[InlineData("""
		public LengthCurrent Stored => Length.FromMetres(2) * Current.FromAmps(3);
		""")]
	[InlineData("""
		public LengthCurrent Stored
		{
			get => Length.FromMetres(2) * Current.FromAmps(3);
		}
		""")]
	[InlineData("""
		public LengthCurrent Stored
		{
			get;
		} = Length.FromMetres(2) * Current.FromAmps(3);
		""")]
	[InlineData("""
		private LengthCurrent Seed = Length.FromMetres(2) * Current.FromAmps(3);
		public LengthCurrent Stored => Seed;
		""")]
	public async Task IntermediateFieldsAndPropertiesComposeWithArithmetic(string declaration)
	{
		string source = $$"""
			public class Inputs
			{
				{{declaration}}
			}
			public static class Consumer
			{
				public static LengthTimeCurrent Calculate() => new Inputs().Stored * Time.FromSeconds(4);
				public static double Execute() => Calculate().AsSiBaseUnit();
			}
			""";
		QuantityGeneratorTest test = new()
		{
			TestCode = source
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		double result = GeneratedCodeExecution.Execute(test.Compilation);

		Assert.Equal(24, result);
	}

	[Theory]
	[InlineData("""
		public static LengthCurrent Field;
		""", "Field")]
	[InlineData("""
		public static LengthCurrent Field = default;
		""", "Field")]
	[InlineData("""
		public static LengthCurrent Property
		{
			get;
		} = new();
		""", "Property")]
	[InlineData("""
		public static LengthCurrent Property
		{
			get;
			set;
		}
		""", "Property")]
	[InlineData("""
		public static LengthCurrent[] Values = new LengthCurrent[1];
		""", "Values[0]")]
	[InlineData("""
		public static System.Collections.Generic.List<LengthCurrent> Values = new() { default };
		""", "Values[0]")]
	public async Task TypeOnlyMemberAndCollectionValuesParticipateInArithmetic(string declaration, string operand)
	{
		string source = $$"""
			public static class Consumer
			{
				{{declaration}}
				public static LengthTimeCurrent Calculate() => {{operand}} * Time.FromSeconds(4);
				public static double Execute() => Calculate().AsSiBaseUnit();
			}
			""";
		QuantityGeneratorTest test = new()
		{
			TestCode = source
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		double result = GeneratedCodeExecution.Execute(test.Compilation);

		Assert.Equal(0, result);
	}

	[Theory]
	[InlineData("new LengthCurrent[1]")]
	[InlineData("""
		new System.Collections.Generic.List<LengthCurrent> { default }
		""")]
	public async Task InferredCollectionsRetainTheirExplicitElementDimensions(string initializer)
	{
		string source = $$"""
			public static class Consumer
			{
				public static LengthTimeCurrent Calculate()
				{
					var values = {{initializer}};
					return values[0] * Time.FromSeconds(4);
				}
				public static double Execute() => Calculate().AsSiBaseUnit();
			}
			""";
		QuantityGeneratorTest test = new()
		{
			TestCode = source
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		double result = GeneratedCodeExecution.Execute(test.Compilation);

		Assert.Equal(0, result);
	}

	[Fact]
	public async Task ExplicitLocalCanBeAssignedAfterItsDeclaration()
	{
		const string source = """
			public static class Consumer
			{
				public static LengthTimeCurrent Calculate()
				{
					LengthCurrent value;
					value = LengthCurrent.FromSiBaseUnit(6);
					return value * Time.FromSeconds(4);
				}
				public static double Execute() => Calculate().AsSiBaseUnit();
			}
			""";
		QuantityGeneratorTest test = new()
		{
			TestCode = source
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		double result = GeneratedCodeExecution.Execute(test.Compilation);

		Assert.Equal(24, result);
	}

	[Theory]
	[InlineData("default")]
	[InlineData("new()")]
	public async Task ExplicitDefaultQuantityLocalParticipatesInArithmetic(string initializer)
	{
		string source = $$"""
			public static class Consumer
			{
				public static LengthTimeCurrent Calculate()
				{
					LengthCurrent value = {{initializer}};
					return value * Time.FromSeconds(4);
				}
				public static double Execute() => Calculate().AsSiBaseUnit();
			}
			""";
		QuantityGeneratorTest test = new()
		{
			TestCode = source
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		double result = GeneratedCodeExecution.Execute(test.Compilation);

		Assert.Equal(0, result);
	}
}
