using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generation;

public sealed class BindingBoundaryTests
{
	[Theory]
	[InlineData("""
		double M(LengthCurrent input)
		{
			LengthCurrent explicitLocal = input;
			var result = explicitLocal * Time.FromSiBaseUnit(4);
			return result.AsSiBaseUnit();
		}
		""", "new Consumer().M(LengthCurrent.FromSiBaseUnit(6))", 24)]
	[InlineData("""
		double M(LengthCurrent input)
		{
			var inferred = input;
			var result = inferred * Time.FromSiBaseUnit(4);
			return result.AsSiBaseUnit();
		}
		""", "new Consumer().M(LengthCurrent.FromSiBaseUnit(6))", 24)]
	[InlineData("""
		double M(LengthCurrent a, LengthCurrent b, bool choose)
		{
			var value = choose ? a : b;
			var result = value * Time.FromSiBaseUnit(4);
			return result.AsSiBaseUnit();
		}
		""", "new Consumer().M(LengthCurrent.FromSiBaseUnit(6), LengthCurrent.FromSiBaseUnit(9), true)", 24)]
	[InlineData("""
		double M(LengthCurrent a, LengthCurrent b, int choose)
		{
			var value = choose switch
			{
				0 => a,
				_ => b
			};
			var result = value * Time.FromSiBaseUnit(4);
			return result.AsSiBaseUnit();
		}
		""", "new Consumer().M(LengthCurrent.FromSiBaseUnit(6), LengthCurrent.FromSiBaseUnit(9), 0)", 24)]
	[InlineData("""
		LengthCurrent field = LengthCurrent.FromSiBaseUnit(6);
		double M()
		{
			var result = field * Time.FromSiBaseUnit(4);
			return result.AsSiBaseUnit();
		}
		""", "new Consumer().M()", 24)]
	[InlineData("""
		LengthCurrent Property
		{
			get;
			set;
		} = LengthCurrent.FromSiBaseUnit(6);
		double M()
		{
			var result = Property * Time.FromSiBaseUnit(4);
			return result.AsSiBaseUnit();
		}
		""", "new Consumer().M()", 24)]
	[InlineData("""
		LengthCurrent Get() => LengthCurrent.FromSiBaseUnit(6);
		double M()
		{
			var result = Get() * Time.FromSiBaseUnit(4);
			return result.AsSiBaseUnit();
		}
		""", "new Consumer().M()", 24)]
	[InlineData("""
		double M(LengthCurrent parameter)
		{
			var result = parameter * Time.FromSiBaseUnit(4);
			return result.AsSiBaseUnit();
		}
		""", "new Consumer().M(LengthCurrent.FromSiBaseUnit(6))", 24)]
	[InlineData("""
		double M(LengthCurrent[] values)
		{
			var result = values[0] * Time.FromSiBaseUnit(4);
			return result.AsSiBaseUnit();
		}
		""", "new Consumer().M(new[] { LengthCurrent.FromSiBaseUnit(6) })", 24)]
	[InlineData("""
		double M(System.Collections.Generic.List<LengthCurrent> values)
		{
			var result = values[0] * Time.FromSiBaseUnit(4);
			return result.AsSiBaseUnit();
		}
		""", "new Consumer().M(new System.Collections.Generic.List<LengthCurrent> { LengthCurrent.FromSiBaseUnit(6) })", 24)]
	[InlineData("""
		double M(System.Span<LengthCurrent> values)
		{
			var result = values[0] * Time.FromSiBaseUnit(4);
			return result.AsSiBaseUnit();
		}
		""", "new Consumer().M(new System.Span<LengthCurrent>(new[] { LengthCurrent.FromSiBaseUnit(6) }))", 24)]
	[InlineData("""
		LengthCurrent this[int index] => LengthCurrent.FromSiBaseUnit(6);
		double M()
		{
			var result = this[0] * Time.FromSiBaseUnit(4);
			return result.AsSiBaseUnit();
		}
		""", "new Consumer().M()", 24)]
	[InlineData("""
		T Identity<T>(T value) => value;
		double M(LengthCurrent input)
		{
			var result = Identity(input) * Time.FromSiBaseUnit(4);
			return result.AsSiBaseUnit();
		}
		""", "new Consumer().M(LengthCurrent.FromSiBaseUnit(6))", 24)]
	[InlineData("""
		interface IValue<T>
		{
			T Get();
		}
		double M(IValue<LengthCurrent> value)
		{
			var result = value.Get() * Time.FromSiBaseUnit(4);
			return result.AsSiBaseUnit();
		}
		sealed class Provider : IValue<LengthCurrent>
		{
			public LengthCurrent Get() => LengthCurrent.FromSiBaseUnit(6);
		}
		""", "new Consumer().M(new Provider())", 24)]
	[InlineData("""
		interface IValue
		{
			LengthCurrent Get();
		}
		double M<T>(T value) where T : IValue
		{
			var result = value.Get() * Time.FromSiBaseUnit(4);
			return result.AsSiBaseUnit();
		}
		sealed class Provider : IValue
		{
			public LengthCurrent Get() => LengthCurrent.FromSiBaseUnit(6);
		}
		""", "new Consumer().M(new Provider())", 24)]
	[InlineData("""
		bool TryGet(out LengthCurrent value)
		{
			value = Length.FromSiBaseUnit(2) * Current.FromSiBaseUnit(3);
			return true;
		}
		double M()
		{
			if (TryGet(out var value))
			{
				var result = value * Time.FromSiBaseUnit(4);
				return result.AsSiBaseUnit();
			}
			throw new System.InvalidOperationException();
		}
		""", "new Consumer().M()", 24)]
	[InlineData("""
		ref LengthCurrent Get(ref LengthCurrent value) => ref value;
		double M(LengthCurrent value)
		{
			var result = Get(ref value) * Time.FromSiBaseUnit(4);
			return result.AsSiBaseUnit();
		}
		""", "new Consumer().M(LengthCurrent.FromSiBaseUnit(6))", 24)]
	[InlineData("""
		double M(object input)
		{
			if (input is LengthCurrent value)
			{
				var result = value * Time.FromSiBaseUnit(4);
				return result.AsSiBaseUnit();
			}
			throw new System.InvalidOperationException();
		}
		""", "new Consumer().M((object)LengthCurrent.FromSiBaseUnit(6))", 24)]
	[InlineData("""
		double M((LengthCurrent, int) input)
		{
			var (value, other) = input;
			var result = value * Time.FromSiBaseUnit(4);
			return result.AsSiBaseUnit();
		}
		""", "new Consumer().M((LengthCurrent.FromSiBaseUnit(6), 0))", 24)]
	[InlineData("""
		static async System.Threading.Tasks.Task<LengthCurrent> Make()
		{
			await System.Threading.Tasks.Task.Yield();
			return Length.FromSiBaseUnit(2) * Current.FromSiBaseUnit(3);
		}
		async System.Threading.Tasks.Task<double> M(System.Threading.Tasks.Task<LengthCurrent> task)
		{
			var value = await task;
			var result = value * Time.FromSiBaseUnit(4);
			return result.AsSiBaseUnit();
		}
		""", "new Consumer().M(Make()).GetAwaiter().GetResult()", 24)]
	[InlineData("""
		System.Collections.Generic.IEnumerable<LengthCurrent> Values()
		{
			yield return LengthCurrent.FromSiBaseUnit(6);
			yield return LengthCurrent.FromSiBaseUnit(9);
		}
		double M()
		{
			LengthTimeCurrent total = default;
			foreach (var value in Values())
			{
				var result = value * Time.FromSiBaseUnit(4);
				total += result;
			}
			return total.AsSiBaseUnit();
		}
		""", "new Consumer().M()", 60)]
	[InlineData("""
		async System.Threading.Tasks.Task<double> M(System.Collections.Generic.IAsyncEnumerable<LengthCurrent> values)
		{
			LengthTimeCurrent total = default;
			await foreach (var value in values)
			{
				var result = value * Time.FromSiBaseUnit(4);
				total += result;
			}
			return total.AsSiBaseUnit();
		}
		static async System.Collections.Generic.IAsyncEnumerable<LengthCurrent> Values()
		{
			await System.Threading.Tasks.Task.Yield();
			yield return LengthCurrent.FromSiBaseUnit(6);
			yield return LengthCurrent.FromSiBaseUnit(9);
		}
		""", "new Consumer().M(Values()).GetAwaiter().GetResult()", 60)]
	public async Task QuantitiesFlowThroughCSharpBindingBoundaries(string members, string execute, double expected)
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
				public class Consumer
				{
					{{members}}

					public static double Execute() => {{execute}};
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = test.Compilation.SyntaxTrees.First();
		VariableDeclaratorSyntax result = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<VariableDeclaratorSyntax>().Single(variable => variable.Identifier.ValueText == "result");
		ILocalSymbol symbol = Assert.IsAssignableFrom<ILocalSymbol>(test.Compilation.GetSemanticModel(tree).GetDeclaredSymbol(result, TestContext.Current.CancellationToken));
		Assert.Multiple(
			() => Assert.Equal("DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>>", symbol.Type.ToDisplayString()),
			() => Assert.Equal(expected, GeneratedCodeExecution.Execute(test.Compilation), 12));
	}

	[Fact]
	public async Task HelperReturnsAcrossFilesSeedFurtherArithmetic()
	{
		QuantityGeneratorTest test = new();
		test.TestState.Sources.Add(("Producer.cs", """
			static class Producer
			{
				public static LengthCurrent Get() => default;
			}
			"""));
		test.TestState.Sources.Add(("Consumer.cs", """
			class Consumer
			{
				LengthTimeCurrent M() => Producer.Get() * Time.FromSiBaseUnit(1);
			}
			"""));
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = test.Compilation.SyntaxTrees.Single(tree => tree.FilePath.EndsWith("Consumer.cs"));
		BinaryExpressionSyntax binary = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<BinaryExpressionSyntax>().Single();
		Assert.Multiple(
			() => Assert.Contains("global using LengthTimeCurrent =", string.Join("\n", test.GeneratedSources)),
			() => Assert.NotNull(test.Compilation.GetSemanticModel(tree).GetSymbolInfo(binary, TestContext.Current.CancellationToken).Symbol));
	}

	[Fact]
	public async Task MetadataReturnSeedsLocalHelpersAndArithmetic()
	{
		QuantityGeneratorTest producer = new()
		{
			TestCode = """
				public static class Producer
				{
					public static LengthCurrent Get() => default;
				}
				"""
		};
		await producer.RunAsync(TestContext.Current.CancellationToken);
		using MemoryStream assembly = new();
		EmitResult emit = producer.Compilation.WithAssemblyName("QuantityProducer").Emit(assembly, cancellationToken: TestContext.Current.CancellationToken);
		if (!emit.Success)
		{
			throw new InvalidOperationException(string.Join("\n", emit.Diagnostics));
		}

		QuantityGeneratorTest consumer = new()
		{
			TestCode = """
				class Consumer
				{
					LengthSquaredCurrentSquared M() => Producer.Get().Squared();
				}
				"""
		};
		consumer.TestState.AdditionalReferences.Add(MetadataReference.CreateFromImage(assembly.ToArray()));
		await consumer.RunAsync(TestContext.Current.CancellationToken);
		IMethodSymbol producerMethod = consumer.Compilation.GetTypeByMetadataName("Producer")!.GetMembers("Get").OfType<IMethodSymbol>().Single();
		Assert.Multiple(
			() => Assert.Equal("QuantityProducer", producerMethod.ContainingAssembly.Name),
			() => Assert.Contains("global using LengthCurrent =", string.Join("\n", consumer.GeneratedSources)),
			() => Assert.Contains("public LengthSquaredCurrentSquared Squared()", string.Join("\n", consumer.GeneratedSources)));
	}
}
