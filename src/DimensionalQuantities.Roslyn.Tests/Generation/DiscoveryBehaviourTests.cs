using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generation;

public sealed class DiscoveryBehaviourTests
{
	[Theory]
	[InlineData("Length", "TerminalHex1,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("MagneticFlux", "TerminalHex2,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex0,TerminalHex0,TerminalHex0")]
	public async Task DefaultAliasesHaveCanonicalTargets(string alias, string exponents)
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = """
				class Consumer
				{
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		UsingDirectiveSyntax declaration = test.GeneratedSources.SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>()).Single(directive => directive.Alias?.Name.Identifier.ValueText == alias);
		ITypeSymbol target = Assert.IsAssignableFrom<ITypeSymbol>(test.Compilation.GetSemanticModel(declaration.SyntaxTree).GetSymbolInfo(declaration.Name!, TestContext.Current.CancellationToken).Symbol);
		Assert.Equal($"DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<{exponents}>>".Replace(" ", ""), target.ToDisplayString().Replace("DimensionalQuantities.TypeSystem.TerminalHex", "TerminalHex").Replace(" ", ""));
	}

	[Theory]
	[InlineData("LengthSquared", "Area")]
	[InlineData("Energy", "Torque")]
	public async Task SynonymsShareRuntimeIdentity(string left, string right)
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
				class Consumer
				{
					{{left}} A;
					{{right}} B;
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		IFieldSymbol[] fields = test.Compilation.GetTypeByMetadataName("Consumer")!.GetMembers().OfType<IFieldSymbol>().ToArray();
		Assert.True(SymbolEqualityComparer.Default.Equals(fields[0].Type, fields[1].Type));
	}

	[Theory]
	[InlineData("""
		LengthCurrent M(LengthCurrent x) => x;
		""")]
	[InlineData("""
		void M()
		{
			LengthCurrent x = default;
		}
		""")]
	[InlineData("""
		System.Collections.Generic.List<LengthCurrent> M() => null;
		""")]
	[InlineData("""
		LengthCurrent[] M() => null;
		""")]
	[InlineData("""
		LengthCurrent? M() => null;
		""")]
	[InlineData("""
		SquareRootLength M() => default;
		""")]
	public async Task UnresolvedCanonicalTypePositionsRequestAliases(string member)
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
				class Consumer
				{
					{{member}}
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		string generated = string.Join("\n", test.GeneratedSources);
		Assert.Contains(member.Contains("SquareRootLength") ? "global using SquareRootLength =" : "global using LengthCurrent =", generated);
	}

	[Theory]
	[InlineData("LengthCurrent.FromSiBaseUnit(6)")]
	[InlineData("LengthCurrent.FromPrefixedUnits(6)")]
	public async Task FactoryOnlyRequestsInferCanonicalQuantity(string expression)
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
				class Consumer
				{
					void M()
					{
						var result = {{expression}};
					}
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = test.Compilation.SyntaxTrees.First();
		VariableDeclaratorSyntax variable = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
		ITypeSymbol type = ((ILocalSymbol)test.Compilation.GetSemanticModel(tree).GetDeclaredSymbol(variable, TestContext.Current.CancellationToken)!).Type;
		Assert.Multiple(
			() => Assert.Equal("Quantity", type.Name),
			() => Assert.Contains("TerminalHex1, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex1", type.ToDisplayString()));
	}

	[Fact]
	public async Task BoundConsumerTypeRetainsItsMeaning()
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = """
				class LengthCurrent
				{
				}

				class Consumer
				{
					LengthCurrent Value;
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		IFieldSymbol field = test.Compilation.GetTypeByMetadataName("Consumer")!.GetMembers("Value").OfType<IFieldSymbol>().Single();
		Assert.Multiple(
			() => Assert.True(SymbolEqualityComparer.Default.Equals(test.Compilation.GetTypeByMetadataName("LengthCurrent"), field.Type)),
			() => Assert.DoesNotContain("global using LengthCurrent =", string.Join("\n", test.GeneratedSources)));
	}

	[Theory]
	[InlineData("i*i*r", "Power")]
	[InlineData("(i*i)*r", "Power")]
	[InlineData("(l*i)/i", "Length")]
	[InlineData("l*i", "LengthCurrent")]
	[InlineData("i*l", "LengthCurrent")]
	[InlineData("l/i", "LengthPerCurrent")]
	[InlineData("i/l", "CurrentPerLength")]
	public async Task ArithmeticBindsToExpectedDimension(string expression, string expected)
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
				class Consumer
				{
					void M(Length l, Current i, Resistance r)
					{
						var result = {{expression}};
						{{expected}} check = result;
					}
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = test.Compilation.SyntaxTrees.First();
		SemanticModel model = test.Compilation.GetSemanticModel(tree);
		VariableDeclaratorSyntax[] variables = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();
		Assert.Multiple(
			() => Assert.True(SymbolEqualityComparer.Default.Equals(((ILocalSymbol)model.GetDeclaredSymbol(variables[0], TestContext.Current.CancellationToken)!).Type, ((ILocalSymbol)model.GetDeclaredSymbol(variables[1], TestContext.Current.CancellationToken)!).Type)),
			() => Assert.All(tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<BinaryExpressionSyntax>(), binary => Assert.NotNull(model.GetSymbolInfo(binary, TestContext.Current.CancellationToken).Symbol)));
	}

	[Theory]
	[InlineData("Length? l, Current? i", "l*i", "LengthCurrent?", true, 18)]
	[InlineData("Length? l, Current i", "l*i", "LengthCurrent?", true, 18)]
	[InlineData("Length l, Current? i", "l*i", "LengthCurrent?", true, 18)]
	[InlineData("Length l, Current i", "l*i", "LengthCurrent", false, 18)]
	[InlineData("Length? l, Length? i", "l/i", "double?", true, 2)]
	[InlineData("Length? l, Length i", "l/i", "double?", true, 2)]
	[InlineData("Length l, Length? i", "l/i", "double?", true, 2)]
	[InlineData("double? l, Time? i", "l/i", "Frequency?", true, 2)]
	[InlineData("double? l, Time i", "l/i", "Frequency?", true, 2)]
	[InlineData("double l, Time? i", "l/i", "Frequency?", true, 2)]
	[InlineData("Length? l, double? i", "l*i", "Length?", true, 18)]
	[InlineData("Length? l, double i", "l*i", "Length?", true, 18)]
	[InlineData("Length l, double? i", "l*i", "Length?", true, 18)]
	[InlineData("Length? l, Current? i", "(l*i)/i", "Length?", true, 6)]
	public async Task NullableOperationsUseCompilerLifting(string parameters, string expression, string result, bool lifted, double expected)
	{
		string[] types = parameters.Split(',').Select(parameter => parameter.Trim().Split(' ')[0]).ToArray();
		string leftValue = types[0].TrimEnd('?') == "double" ? "6d" : $"{types[0].TrimEnd('?')}.FromSiBaseUnit(6)";
		string rightValue = types[1].TrimEnd('?') == "double" ? "3d" : $"{types[1].TrimEnd('?')}.FromSiBaseUnit(3)";
		string magnitude = result.StartsWith("double") ? "" : lifted ? "?.AsSiBaseUnit()" : ".AsSiBaseUnit()";
		string fallback = lifted ? " ?? -1" : "";
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
				public class Consumer
				{
					{{result}} M({{parameters}}) => {{expression}};
					public static double Execute() => new Consumer().M({{leftValue}}, {{rightValue}}){{magnitude}}{{fallback}};
					public static double ExecuteNullLeft() => new Consumer().M({{(types[0].EndsWith('?') ? "null" : leftValue)}}, {{rightValue}}){{magnitude}}{{fallback}};
					public static double ExecuteNullRight() => new Consumer().M({{leftValue}}, {{(types[1].EndsWith('?') ? "null" : rightValue)}}){{magnitude}}{{fallback}};
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = test.Compilation.SyntaxTrees.First();
		BinaryExpressionSyntax binary = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<BinaryExpressionSyntax>().First();
		IBinaryOperation operation = Assert.IsAssignableFrom<IBinaryOperation>(test.Compilation.GetSemanticModel(tree).GetOperation(binary, TestContext.Current.CancellationToken));
		IMethodSymbol selected = Assert.IsAssignableFrom<IMethodSymbol>(operation.OperatorMethod);
		IMethodSymbol declaration = test.Compilation.GetTypeByMetadataName("Consumer")!.GetMembers("M").OfType<IMethodSymbol>().Single();
		Assert.Multiple(
			() => Assert.Equal(lifted, operation.IsLifted),
			() => Assert.True(SymbolEqualityComparer.Default.Equals(declaration.ReturnType, operation.Type)),
			() => Assert.True(SymbolEqualityComparer.Default.Equals(UnwrapNullable(declaration.ReturnType), selected.ReturnType)),
			() => Assert.True(SymbolEqualityComparer.Default.Equals(UnwrapNullable(operation.LeftOperand.Type!), selected.Parameters[0].Type)),
			() => Assert.True(SymbolEqualityComparer.Default.Equals(UnwrapNullable(operation.RightOperand.Type!), selected.Parameters[1].Type)),
			() => Assert.DoesNotContain(test.GeneratedSources.SelectMany(source => source.GetRoot().DescendantNodes().OfType<OperatorDeclarationSyntax>()),
				generated => generated.ParameterList.DescendantNodes().OfType<NullableTypeSyntax>().Any()),
			() => Assert.Equal(expected, GeneratedCodeExecution.Execute(test.Compilation), 12),
			() => Assert.Equal(types[0].EndsWith('?') ? -1 : expected, GeneratedCodeExecution.Execute(test.Compilation, "ExecuteNullLeft"), 12),
			() => Assert.Equal(types[1].EndsWith('?') ? -1 : expected, GeneratedCodeExecution.Execute(test.Compilation, "ExecuteNullRight"), 12));
	}

	private static ITypeSymbol UnwrapNullable(ITypeSymbol type) => type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable ? nullable.TypeArguments[0] : type;

	[Fact]
	public async Task CancellationReturnsScalarAndEmitsMagnitudeMultiplication()
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = """
				class Consumer
				{
					Length M(Length l, InverseLength inverse)
					{
						double scalar = l*inverse;
						return scalar*l;
					}
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		string generated = string.Join("\n", test.GeneratedSources);
		Assert.Multiple(
			() => Assert.Contains("public static double operator *", generated),
			() => Assert.Contains("left.AsSiBaseUnit() * right.AsSiBaseUnit()", generated),
			() => Assert.DoesNotContain("global using Dimensionless =", generated),
			() => Assert.DoesNotContain("operator /(Length left, Length right)", generated));
	}

	[Fact]
	public async Task ScalarInverseDividesByStoredMagnitude()
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = """
				class Consumer
				{
					Frequency M(Time t) => 1/t;
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.Contains(".FromSiBaseUnit(left / right.AsSiBaseUnit())", string.Join("\n", test.GeneratedSources));
	}

	[Theory]
	[InlineData("Squared", "TerminalHex2", "LengthSquared")]
	[InlineData("SquareRoot", "Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex2>", "SquareRootLength")]
	public async Task PredefinedHelpersDelegateToExactPower(string method, string exponent, string result)
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
				class Consumer
				{
					{{result}} M(Length l) => l.{{method}}();
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = test.Compilation.SyntaxTrees.First();
		InvocationExpressionSyntax call = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
		IMethodSymbol selected = Assert.IsAssignableFrom<IMethodSymbol>(test.Compilation.GetSemanticModel(tree).GetSymbolInfo(call, TestContext.Current.CancellationToken).Symbol);
		IMethodSymbol declared = test.Compilation.GetTypeByMetadataName("Consumer")!.GetMembers("M").OfType<IMethodSymbol>().Single();
		Assert.Multiple(
			() => Assert.Contains($"{method}() => global::DimensionalQuantities.TypeSystem.Power<global::DimensionalQuantities.TypeSystem.{exponent}>.Of(value);", string.Join("\n", test.GeneratedSources)),
			() => Assert.True(SymbolEqualityComparer.Default.Equals(declared.ReturnType, selected.ReturnType)));
	}

	[Theory]
	[InlineData("NumberHalf", "Area", "Length")]
	[InlineData("NumberTwoOverThree", "Volume", "Area")]
	[InlineData("NumberZero", "Length", "double")]
	[InlineData("NumberOne", "Length", "Length")]
	[InlineData("NumberMinusOne", "Length", "InverseLength")]
	[InlineData("NumberThree", "SquareRootLength", "SquareRootLengthCubed")]
	[InlineData("NumberHalf", "InverseLength", "InverseSquareRootLength")]
	[InlineData("NumberTwentyOne", "Length", "Length21")]
	public async Task EnglishPowersProduceExactResultTypes(string exponent, string input, string output)
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
				using DimensionalQuantities.TypeSystem;

				class Consumer
				{
					{{output}} M({{input}} value) => Power<{{exponent}}>.Of(value);
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = test.Compilation.SyntaxTrees.First();
		InvocationExpressionSyntax call = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
		IMethodSymbol symbol = Assert.IsAssignableFrom<IMethodSymbol>(test.Compilation.GetSemanticModel(tree).GetSymbolInfo(call, TestContext.Current.CancellationToken).Symbol);
		IMethodSymbol declared = test.Compilation.GetTypeByMetadataName("Consumer")!.GetMembers("M").OfType<IMethodSymbol>().Single();
		Assert.Multiple(
			() => Assert.True(SymbolEqualityComparer.Default.Equals(declared.ReturnType, symbol.ReturnType)),
			() => Assert.Contains($"global using {exponent} =", string.Join("\n", test.GeneratedSources)));
	}

	[Fact]
	public async Task DiscoveredHelperChainSupportsConditionalAccess()
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = """
				class Consumer
				{
					LengthCurrent? M(Length l, Current i)
					{
						var product = l*i;
						var square = product.Squared();
						LengthCurrent? restored = square.SquareRoot();
						return restored?.Squared().SquareRoot();
					}
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		string generated = string.Join("\n", test.GeneratedSources);
		Assert.Multiple(
			() => Assert.Contains("public LengthSquaredCurrentSquared Squared()", generated),
			() => Assert.Contains("public LengthCurrent SquareRoot()", generated));
	}
}
