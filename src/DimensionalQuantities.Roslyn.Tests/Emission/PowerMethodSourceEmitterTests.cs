using System.Reflection;
using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Numerics;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.Roslyn.Generators;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Emission;

public sealed class PowerMethodSourceEmitterTests
{
	[Theory]
	[InlineData("Squared", 2, 1, "TerminalHex2")]
	[InlineData("Cubed", 3, 1, "TerminalHex3")]
	[InlineData("Pow4", 4, 1, "TerminalHex4")]
	[InlineData("Pow5", 5, 1, "TerminalHex5")]
	[InlineData("Pow6", 6, 1, "TerminalHex6")]
	[InlineData("SquareRoot", 1, 2, "Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex2>")]
	[InlineData("CubeRoot", 1, 3, "Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex3>")]
	[InlineData("Root4", 1, 4, "Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex4>")]
	[InlineData("Root5", 1, 5, "Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex5>")]
	[InlineData("Root6", 1, 6, "Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex6>")]
	public void ConvenienceMethodDelegatesToTheEncodedPowerOverload(string method, int numerator, int denominator, string expectedExponent)
	{
		Dimension operand = new(Length: 1);
		Rational exponent = new(numerator, denominator);
		PowerMethodOperation operation = new(operand, exponent, operand.Pow(exponent), method);
		QuantityGenerationPlan plan = new(
			[new(operand, ["Length"], [], false)], [new(operation.Result, ["Result"], [], false)], [],
			[operation.ToPowerOperation()], [operation]);
		string exponentType = TypeSystemTypeNameFormatter.Format(TypeSystemNumberCodec.CreateType(exponent));
		string resultType = plan.GetQuantityName(operation.Result);
		string source = $$"""
			namespace DimensionalQuantities.Generated;
			public static class StubPowerExtensions
			{
				extension(global::DimensionalQuantities.TypeSystem.Power<{{exponentType}}>)
				{
					public static {{resultType}} Of(Length value) => {{resultType}}.FromSiBaseUnit(value.AsSiBaseUnit() + 100);
				}
			}
			public static class Consumer
			{
				public static {{resultType}} Convert(Length value) => value.{{method}}();
				public static double Value() => Convert(Length.FromSiBaseUnit(9)).AsSiBaseUnit();
			}
			""";
		CSharpParseOptions options = new(LanguageVersion.Preview);
		string[] assemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = assemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		SyntaxTree[] trees =
		[
			CSharpSyntaxTree.ParseText(source, options, cancellationToken: TestContext.Current.CancellationToken),
			CSharpSyntaxTree.ParseText(QuantitySourceEmitter.Emit(plan.KnownQuantities), options, cancellationToken: TestContext.Current.CancellationToken),
			CSharpSyntaxTree.ParseText(QuantitySourceEmitter.Emit(plan.AdditionalQuantities), options, cancellationToken: TestContext.Current.CancellationToken),
			CSharpSyntaxTree.ParseText(PowerMethodSourceEmitter.Emit(plan), options, cancellationToken: TestContext.Current.CancellationToken)
		];
		CSharpCompilation compilation = CSharpCompilation.Create("DelegatingPowerConsumer", trees, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		using MemoryStream assemblyBytes = new();
		EmitResult emit = compilation.Emit(assemblyBytes, cancellationToken: TestContext.Current.CancellationToken);
		if (!emit.Success)
		{
			throw new InvalidOperationException(string.Join(Environment.NewLine, emit.Diagnostics));
		}
		Assembly assembly = Assembly.Load(assemblyBytes.ToArray());

		double result = (double)assembly.GetType("DimensionalQuantities.Generated.Consumer")!.GetMethod("Value")!.Invoke(null, null)!;

		IMethodSymbol declared = compilation.GetTypeByMetadataName("DimensionalQuantities.Generated.Consumer")!
			.GetMembers("Convert").OfType<IMethodSymbol>().Single();
		MethodDeclarationSyntax declaration = (MethodDeclarationSyntax)declared.DeclaringSyntaxReferences.Single().GetSyntax(TestContext.Current.CancellationToken);
		IMethodSymbol selected = (IMethodSymbol)compilation.GetSemanticModel(declaration.SyntaxTree)
			.GetSymbolInfo(declaration.ExpressionBody!.Expression, TestContext.Current.CancellationToken).Symbol!;
		Assert.Multiple(
			() => Assert.True(SymbolEqualityComparer.Default.Equals(declared.ReturnType, selected.ReturnType)),
			() => Assert.Equal(109, result),
			() => Assert.Contains($"{method}() => global::DimensionalQuantities.TypeSystem.Power<global::DimensionalQuantities.TypeSystem.{expectedExponent}>.Of(value);", PowerMethodSourceEmitter.Emit(plan)));
	}

	[Theory]
	[InlineData("Squared", 2, 1, -3, 9)]
	[InlineData("CubeRoot", 1, 3, -8, -2)]
	[InlineData("Root5", 1, 5, -32, -2)]
	[InlineData("SquareRoot", 1, 2, -9, double.NaN)]
	[InlineData("Root4", 1, 4, -16, double.NaN)]
	public void EmittedMethodsUseTheRealOddRootAndPreserveEvenRootBehaviour(string method, int numerator, int denominator, double value, double expected)
	{
		Dimension operand = new(Length: 1);
		Rational exponent = new(numerator, denominator);
		Dimension resultDimension = operand.Pow(exponent);
		PowerMethodOperation operation = new(operand, exponent, resultDimension, method);
		QuantityGenerationPlan plan = new(
			[new(operand, ["Length"], [], false)], [new(operation.Result, ["Result"], [], false)], [],
			[operation.ToPowerOperation()], [operation]);
		string source = $$"""
			public static class Consumer
			{
				public static double Value() => Length.FromSiBaseUnit({{value.ToString(System.Globalization.CultureInfo.InvariantCulture)}}).{{method}}().AsSiBaseUnit();
			}
			""";
		CSharpParseOptions options = new(LanguageVersion.Preview);
		string[] assemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
		IEnumerable<PortableExecutableReference> references = assemblies.Select(path => MetadataReference.CreateFromFile(path))
			.Append(MetadataReference.CreateFromFile(typeof(Quantity<>).Assembly.Location));
		SyntaxTree[] trees =
		[
			CSharpSyntaxTree.ParseText(source, options, cancellationToken: TestContext.Current.CancellationToken),
			CSharpSyntaxTree.ParseText(QuantitySourceEmitter.Emit(plan.KnownQuantities), options, cancellationToken: TestContext.Current.CancellationToken),
			CSharpSyntaxTree.ParseText(QuantitySourceEmitter.Emit(plan.AdditionalQuantities), options, cancellationToken: TestContext.Current.CancellationToken),
			CSharpSyntaxTree.ParseText(PowerMethodSourceEmitter.Emit(plan), options, cancellationToken: TestContext.Current.CancellationToken),
			CSharpSyntaxTree.ParseText(PowerSourceEmitter.Emit(plan), options, cancellationToken: TestContext.Current.CancellationToken)
		];
		CSharpCompilation compilation = CSharpCompilation.Create("PowerMethodConsumer", trees, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		using MemoryStream assemblyBytes = new();
		EmitResult emit = compilation.Emit(assemblyBytes, cancellationToken: TestContext.Current.CancellationToken);
		if (!emit.Success)
		{
			throw new InvalidOperationException(string.Join(Environment.NewLine, emit.Diagnostics));
		}
		Assembly assembly = Assembly.Load(assemblyBytes.ToArray());

		double result = (double)assembly.GetType("Consumer")!.GetMethod("Value")!.Invoke(null, null)!;

		Assert.Equal(expected, result, 12);
	}
}
