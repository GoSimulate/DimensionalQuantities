using System.Reflection;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.Roslyn.Generators;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Discovery;

public sealed class QuantityDiscoveryCoordinatorTests
{
	private static readonly CSharpParseOptions _parseOptions = new(LanguageVersion.CSharp14);

	[Theory]
	[InlineData("var make = () => length * current; return (make() * time).AsSiBaseUnit();")]
	[InlineData("var pair = (length * current, time); return (pair.Item1 * pair.Item2).AsSiBaseUnit();")]
	[InlineData("var (value, duration) = (length * current, time); return (value * duration).AsSiBaseUnit();")]
	[InlineData("return (Identity(length * current) * time).AsSiBaseUnit();")]
	[InlineData("LengthCurrent? value = null; var fallback = length * current; return ((value ?? fallback) * time).AsSiBaseUnit();")]
	[InlineData("object value = length * current; return (((LengthCurrent)value) * time).AsSiBaseUnit();")]
	[InlineData("var values = new[] { length }.Select(item => item * current); return (values.First() * time).AsSiBaseUnit();")]
	[InlineData("var values = new[] { length * current }; double result = 0; foreach (var value in values) { result += (value * time).AsSiBaseUnit(); } return result;")]
	public void IntermediateResultsComposeThroughOrdinaryCSharp(string body)
	{
		string source = $$"""
			using System.Linq;
			public static class Consumer
			{
				private static T Identity<T>(T value) => value;
				public static double Value()
				{
					Length length = Length.FromMetres(2);
					Current current = Current.FromAmps(3);
					Time time = Time.FromSeconds(4);
					{{body}}
				}
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);
		double expected = 24;

		Assert.Equal(expected, result);
	}

	[Fact]
	public void AwaitedIntermediateResultsComposeWithArithmetic()
	{
		const string source = """
			using System.Threading.Tasks;
			public static class Consumer
			{
				private static async Task<LengthCurrent> Make()
				{
					await Task.Yield();
					return Length.FromMetres(2) * Current.FromAmps(3);
				}
				private static async Task<double> Calculate() => ((await Make()) * Time.FromSeconds(4)).AsSiBaseUnit();
				public static double Value() => Calculate().GetAwaiter().GetResult();
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);

		Assert.Equal(24, result);
	}

	[Fact]
	public void BlockGetterResultsComposeAcrossSourceFiles()
	{
		const string member = """
			public static class Measurements
			{
				public static LengthCurrent Value
				{
					get { return Length.FromMetres(2) * Current.FromAmps(3); }
				}
			}
			""";
		const string consumer = """
			public static class Consumer
			{
				public static double Value() => (Measurements.Value * Time.FromSeconds(4)).AsSiBaseUnit();
			}
			""";
		QuantityDiscoveryResult discovery = Discover(consumer, member);
		double result = Evaluate(discovery.Compilation);

		Assert.Equal(24, result);
	}

	[Theory]
	[InlineData("new LengthCurrent()")]
	[InlineData("default(LengthCurrent)")]
	[InlineData("Make()")]
	public void ExplicitIntermediateTypesSupplyDimensionsWithoutInitialisers(string expression)
	{
		string source = $$"""
			public static class Consumer
			{
				private static LengthCurrent Make() => default;
				public static double Value() => ({{expression}} * Time.FromSeconds(4)).AsSiBaseUnit();
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);

		Assert.Equal(0, result);
	}

	[Theory]
	[InlineData("FromSiBaseUnit(6000)")]
	[InlineData("FromPrefixedUnits(6, 3, 0)")]
	public void StandaloneCanonicalFactoryRequestsAQuantityAndItsConversions(string factory)
	{
		string source = $$"""
			public static class Consumer
			{
				public static double Value() => LengthCurrent.{{factory}}.AsPrefixedUnits(3, 0);
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);

		Assert.Equal(6, result);
	}

	[Theory]
	[InlineData("(Length.FromMetres(2) * Current.FromAmps(3)).Squared()", 144)]
	[InlineData("Power<NumberTwo>.Of(Length.FromMetres(2) * Current.FromAmps(3))", 144)]
	[InlineData("Power<NumberHalf>.Of(Length.FromMetres(3) * Current.FromAmps(3))", 12)]
	public void PowerResultsComposeThroughInferredDelegates(string expression, double expected)
	{
		string source = $$"""
			using DimensionalQuantities.TypeSystem;
			public static class Consumer
			{
				public static double Value()
				{
					var make = () => {{expression}};
					return (make() * Time.FromSeconds(4)).AsSiBaseUnit();
				}
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void ExistingMultiplicationOperatorKeepsItsBehaviour()
	{
		const string source = """
			public static class CustomOperators
			{
				extension(Length)
				{
					public static double operator *(Length length, Current current) => 42;
				}
			}
			public static class Consumer
			{
				public static double Value() => ((Length.FromMetres(2) * Current.FromAmps(3)) * Time.FromSeconds(4)).AsSiBaseUnit();
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);

		Assert.Equal(168, result);
	}

	[Fact]
	public void ExistingConvenienceMethodKeepsItsBehaviourAfterItsReceiverResolves()
	{
		const string source = """
			public static class CustomMethods
			{
				public static Time Squared(this LengthCurrent value) => Time.FromSeconds(7);
			}
			public static class Consumer
			{
				public static double Value() => ((Length.FromMetres(2) * Current.FromAmps(3)).Squared() * Current.FromAmps(3)).AsSiBaseUnit();
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);

		Assert.Equal(21, result);
	}

	[Theory]
	[InlineData("wrapper + wrapper")]
	[InlineData("wrapper - wrapper")]
	[InlineData("-wrapper")]
	public void BoundOperatorResultsComposeWithDimensionalArithmetic(string expression)
	{
		string source = $$"""
			public sealed class Wrapper
			{
				public static Current operator +(Wrapper left, Wrapper right) => Current.FromAmps(3);
				public static Current operator -(Wrapper left, Wrapper right) => Current.FromAmps(3);
				public static Current operator -(Wrapper value) => Current.FromAmps(3);
			}
			public static class Consumer
			{
				public static double Value()
				{
					Wrapper wrapper = new();
					return (({{expression}}) * Time.FromSeconds(4)).AsSiBaseUnit();
				}
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);

		Assert.Equal(12, result);
	}

	[Theory]
	[InlineData("choose ? length * current : length * time", "CS0173")]
	[InlineData("choose switch { true => length * current, false => length * time }", "CS8506")]
	public void ConflictingBranchesDoNotSupplyDimensionsToFurtherArithmetic(string expression, string expectedDiagnostic)
	{
		string source = $$"""
			public static class Consumer
			{
				public static object Value(bool choose, Length length, Current current, Time time)
				{
					var value = {{expression}};
					return value * current;
				}
			}
			""";
		QuantityDiscoveryResult result = Discover(source);
		Diagnostic[] errors = result.Compilation.GetDiagnostics(TestContext.Current.CancellationToken)
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

		Assert.Multiple(
			() => Assert.Contains(errors, diagnostic => diagnostic.Id == expectedDiagnostic),
			() => Assert.Null(result.Compilation.Assembly.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentSquaredExtensions")),
			() => Assert.Null(result.Compilation.Assembly.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeCurrentExtensions")));
	}

	[Fact]
	public void AmbiguousMethodCallsRemainCompilerErrors()
	{
		const string source = """
			public static class Consumer
			{
				private static LengthCurrent Make(int first, long second) => default;
				private static LengthTime Make(long first, int second) => default;
				public static object Value(Current current) => Make(1, 1) * current;
			}
			""";
		QuantityDiscoveryResult result = Discover(source);
		Diagnostic[] errors = result.Compilation.GetDiagnostics(TestContext.Current.CancellationToken)
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

		Assert.Multiple(
			() => Assert.Contains(errors, diagnostic => diagnostic.Id == "CS0121"),
			() => Assert.Null(result.Compilation.Assembly.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthCurrentSquaredExtensions")),
			() => Assert.Null(result.Compilation.Assembly.GetTypeByMetadataName("DimensionalQuantities.Generated.LengthTimeCurrentExtensions")));
	}

	[Theory]
	[InlineData("var value = value * current; return value;", "CS0841")]
	[InlineData("return (length + current) * time;", "CS0019")]
	[InlineData("return LengthCurrent.FromBananas(6);", "CS0103")]
	public void InvalidConsumerCodeRemainsACompilerError(string body, string expectedDiagnostic)
	{
		string source = $$"""
			public static class Consumer
			{
				public static object Value(Length length, Current current, Time time) { {{body}} }
			}
			""";
		QuantityDiscoveryResult result = Discover(source);
		Diagnostic[] errors = result.Compilation.GetDiagnostics(TestContext.Current.CancellationToken)
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

		Assert.Contains(errors, diagnostic => diagnostic.Id == expectedDiagnostic);
	}

	[Fact]
	public void LongDependentExpressionsCompleteWithoutAnArbitrarySmallPassLimit()
	{
		string expression = string.Join(" * ", Enumerable.Repeat("Current.FromAmps(1)", 20));
		string source = $$"""
			public static class Consumer
			{
				public static double Value() => (Length.FromMetres(2) * {{expression}}).AsSiBaseUnit();
			}
			""";
		QuantityDiscoveryResult discovery = Discover(source);
		double result = Evaluate(discovery.Compilation);

		Assert.Equal(2, result);
	}

	[Fact]
	public void EmptyInputNeedsNoDynamicSource()
	{
		QuantityDiscoveryResult result = Discover();

		Assert.Empty(result.Sources);
	}

	[Fact]
	public void CancellationStopsDiscovery()
	{
		using CancellationTokenSource cancellation = new();
		cancellation.Cancel();
		QuantityGenerationPlan defaults = DefaultQuantityDiscovery.Discover();
		CSharpCompilation compilation = CreateCompilation([]);

		Assert.Throws<OperationCanceledException>(() => QuantityDiscoveryCoordinator.Discover(compilation, [], defaults, cancellation.Token));
	}

	private static QuantityDiscoveryResult Discover(params string[] sources)
	{
		CSharpCompilation original = CreateCompilation(sources);
		QuantityGenerationPlan defaults = DefaultQuantityDiscovery.Discover();
		SyntaxTree[] defaultTrees =
		[
			CSharpSyntaxTree.ParseText(QuantitySourceEmitter.Emit([.. defaults.KnownQuantities, .. defaults.AdditionalQuantities]), _parseOptions, path: "Quantities.g.cs"),
			CSharpSyntaxTree.ParseText(PowerSourceEmitter.Emit(defaults, "DefaultPowerExtensions"), _parseOptions, path: "DefaultPowers.g.cs"),
			CSharpSyntaxTree.ParseText(PowerMethodSourceEmitter.Emit(defaults, "DefaultPowerMethodExtensions"), _parseOptions, path: "DefaultPowerMethods.g.cs")
		];
		return QuantityDiscoveryCoordinator.Discover(original.AddSyntaxTrees(defaultTrees), [.. original.SyntaxTrees], defaults, TestContext.Current.CancellationToken);
	}

	private static CSharpCompilation CreateCompilation(string[] sources)
	{
		string[] platformAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = platformAssemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		return CSharpCompilation.Create("DiscoveryConsumer", sources.Select((source, index) => CSharpSyntaxTree.ParseText(source, _parseOptions, path: $"Consumer{index}.cs")),
			references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
	}

	private static double Evaluate(CSharpCompilation compilation)
	{
		using MemoryStream bytes = new();
		EmitResult emitted = compilation.Emit(bytes, cancellationToken: TestContext.Current.CancellationToken);
		if (!emitted.Success)
		{
			throw new InvalidOperationException(string.Join(Environment.NewLine, emitted.Diagnostics));
		}
		Assembly assembly = Assembly.Load(bytes.ToArray());
		return (double)assembly.GetType("Consumer")!.GetMethod("Value")!.Invoke(null, null)!;
	}
}
