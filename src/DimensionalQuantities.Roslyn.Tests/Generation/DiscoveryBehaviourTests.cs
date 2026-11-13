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
	[InlineData("Mass", "TerminalHex0,TerminalHex1,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("Time", "TerminalHex0,TerminalHex0,TerminalHex1,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("Current", "TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex1,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("Temperature", "TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex1,TerminalHex0,TerminalHex0")]
	[InlineData("AmountOfSubstance", "TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex1,TerminalHex0")]
	[InlineData("LuminousIntensity", "TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex1")]
	[InlineData("Area", "TerminalHex2,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("Volume", "TerminalHex3,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("Speed", "TerminalHex1,TerminalHex0,DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("Acceleration", "TerminalHex1,TerminalHex0,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("Frequency", "TerminalHex0,TerminalHex0,DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("AngularVelocity", "TerminalHex0,TerminalHex0,DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("Force", "TerminalHex1,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("Density", "DimensionalQuantities.TypeSystem.Negative<TerminalHex3>,TerminalHex1,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("Pressure", "DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("Stress", "DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("YoungsModulus", "DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("Energy", "TerminalHex2,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("Torque", "TerminalHex2,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("Power", "TerminalHex2,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex3>,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("SpecificPower", "TerminalHex2,TerminalHex0,DimensionalQuantities.TypeSystem.Negative<TerminalHex3>,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("SpecificPowerLoss", "TerminalHex2,TerminalHex0,DimensionalQuantities.TypeSystem.Negative<TerminalHex3>,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("TemperatureCoefficient", "TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0,DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex0,TerminalHex0")]
	[InlineData("PowerDensity", "DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex3>,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("VolumetricPowerLoss", "DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex3>,TerminalHex0,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("ElectricCharge", "TerminalHex0,TerminalHex0,TerminalHex1,TerminalHex1,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("Voltage", "TerminalHex2,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex3>,DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("Capacitance", "DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex4,TerminalHex2,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("Resistance", "TerminalHex2,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex3>,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("Inductance", "TerminalHex2,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("MagneticFlux", "TerminalHex2,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("FluxLinkage", "TerminalHex2,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("MagneticFluxDensity", "TerminalHex0,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("MagneticPolarisation", "TerminalHex0,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("RemanentFluxDensity", "TerminalHex0,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("MagneticFieldStrength", "DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex0,TerminalHex0,TerminalHex1,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("CoerciveFieldStrength", "DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex0,TerminalHex0,TerminalHex1,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("CurrentDensity", "DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,TerminalHex0,TerminalHex0,TerminalHex1,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("Permeability", "TerminalHex1,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("Resistivity", "TerminalHex3,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex3>,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("Conductivity", "DimensionalQuantities.TypeSystem.Negative<TerminalHex3>,DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex3,TerminalHex2,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("MagneticReluctivity", "DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex2,TerminalHex2,TerminalHex0,TerminalHex0,TerminalHex0")]
	[InlineData("MagneticVectorPotential", "TerminalHex1,TerminalHex1,DimensionalQuantities.TypeSystem.Negative<TerminalHex2>,DimensionalQuantities.TypeSystem.Negative<TerminalHex1>,TerminalHex0,TerminalHex0,TerminalHex0")]
	public async Task DefaultAliasesHaveCanonicalTargets(string alias, string exponents)
	{
		QuantityGeneratorTest test = new() { TestCode = "class Consumer {}" };
		await test.RunAsync(TestContext.Current.CancellationToken);
		UsingDirectiveSyntax declaration = test.GeneratedSources.SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>()).Single(directive => directive.Alias?.Name.Identifier.ValueText == alias);
		ITypeSymbol target = Assert.IsAssignableFrom<ITypeSymbol>(test.Compilation.GetSemanticModel(declaration.SyntaxTree).GetSymbolInfo(declaration.Name!, TestContext.Current.CancellationToken).Symbol);
		Assert.Equal($"DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<{exponents}>>".Replace(" ", ""), target.ToDisplayString().Replace("DimensionalQuantities.TypeSystem.TerminalHex", "TerminalHex").Replace(" ", ""));
	}

	[Theory]
	[InlineData("LengthSquared", "Area")]
	[InlineData("Energy", "Torque")]
	[InlineData("Frequency", "AngularVelocity")]
	[InlineData("Pressure", "Stress")]
	[InlineData("Stress", "YoungsModulus")]
	[InlineData("SpecificPower", "SpecificPowerLoss")]
	[InlineData("PowerDensity", "VolumetricPowerLoss")]
	[InlineData("MagneticFlux", "FluxLinkage")]
	[InlineData("MagneticFluxDensity", "MagneticPolarisation")]
	[InlineData("MagneticPolarisation", "RemanentFluxDensity")]
	[InlineData("MagneticFieldStrength", "CoerciveFieldStrength")]
	public async Task SynonymsShareRuntimeIdentity(string left, string right)
	{
		QuantityGeneratorTest test = new() { TestCode = $"class Consumer {{ {left} A; {right} B; }}" };
		await test.RunAsync(TestContext.Current.CancellationToken);
		IFieldSymbol[] fields = test.Compilation.GetTypeByMetadataName("Consumer")!.GetMembers().OfType<IFieldSymbol>().ToArray();
		Assert.True(SymbolEqualityComparer.Default.Equals(fields[0].Type, fields[1].Type));
	}

	[Theory]
	[InlineData("LengthCurrent M(LengthCurrent x) => x;")]
	[InlineData("void M() { LengthCurrent x = default; }")]
	[InlineData("System.Collections.Generic.List<LengthCurrent> M() => null;")]
	[InlineData("LengthCurrent[] M() => null;")]
	[InlineData("LengthCurrent? M() => null;")]
	[InlineData("SquareRootLength M() => default;")]
	public async Task UnresolvedCanonicalTypePositionsRequestAliases(string member)
	{
		QuantityGeneratorTest test = new() { TestCode = $"class Consumer {{ {member} }}" };
		await test.RunAsync(TestContext.Current.CancellationToken);
		string generated = string.Join("\n", test.GeneratedSources);
		Assert.Contains(member.Contains("SquareRootLength") ? "global using SquareRootLength =" : "global using LengthCurrent =", generated);
	}

	[Theory]
	[InlineData("LengthCurrent.FromSiBaseUnit(6)")]
	[InlineData("LengthCurrent.FromPrefixedUnits(6)")]
	public async Task FactoryOnlyRequestsInferCanonicalQuantity(string expression)
	{
		QuantityGeneratorTest test = new() { TestCode = $"class Consumer {{ void M() {{ var result = {expression}; }} }}" };
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
		QuantityGeneratorTest test = new() { TestCode = "class LengthCurrent {} class Consumer { LengthCurrent Value; }" };
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
		QuantityGeneratorTest test = new() { TestCode = $"class Consumer {{ void M(Length l, Current i, Resistance r) {{ var result = {expression}; {expected} check = result; }} }}" };
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = test.Compilation.SyntaxTrees.First();
		SemanticModel model = test.Compilation.GetSemanticModel(tree);
		VariableDeclaratorSyntax[] variables = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();
		Assert.Multiple(
			() => Assert.True(SymbolEqualityComparer.Default.Equals(((ILocalSymbol)model.GetDeclaredSymbol(variables[0], TestContext.Current.CancellationToken)!).Type, ((ILocalSymbol)model.GetDeclaredSymbol(variables[1], TestContext.Current.CancellationToken)!).Type)),
			() => Assert.All(tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<BinaryExpressionSyntax>(), binary => Assert.NotNull(model.GetSymbolInfo(binary, TestContext.Current.CancellationToken).Symbol)));
	}

	[Theory]
	[InlineData("Length? l, Current? i", "l*i", "LengthCurrent?", true)]
	[InlineData("Length? l, Current i", "l*i", "LengthCurrent?", true)]
	[InlineData("Length l, Current? i", "l*i", "LengthCurrent?", true)]
	[InlineData("Length l, Current i", "l*i", "LengthCurrent", false)]
	[InlineData("Length? l, Length? i", "l/i", "double?", true)]
	[InlineData("Length? l, Length i", "l/i", "double?", true)]
	[InlineData("Length l, Length? i", "l/i", "double?", true)]
	[InlineData("double? l, Time? i", "l/i", "Frequency?", true)]
	[InlineData("double? l, Time i", "l/i", "Frequency?", true)]
	[InlineData("double l, Time? i", "l/i", "Frequency?", true)]
	[InlineData("Length? l, double? i", "l*i", "Length?", true)]
	[InlineData("Length? l, double i", "l*i", "Length?", true)]
	[InlineData("Length l, double? i", "l*i", "Length?", true)]
	[InlineData("Length? l, Current? i", "(l*i)/i", "Length?", true)]
	public async Task NullableOperationsUseCompilerLifting(string parameters, string expression, string result, bool lifted)
	{
		QuantityGeneratorTest test = new() { TestCode = $"class Consumer {{ {result} M({parameters}) => {expression}; }}" };
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
				generated => generated.ParameterList.DescendantNodes().OfType<NullableTypeSyntax>().Any()));
	}

	private static ITypeSymbol UnwrapNullable(ITypeSymbol type) => type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable ? nullable.TypeArguments[0] : type;

	[Fact]
	public async Task CancellationReturnsScalarAndEmitsMagnitudeMultiplication()
	{
		QuantityGeneratorTest test = new() { TestCode = "class Consumer { Length M(Length l, InverseLength inverse) { double scalar = l*inverse; return scalar*l; } }" };
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
		QuantityGeneratorTest test = new() { TestCode = "class Consumer { Frequency M(Time t) => 1/t; }" };
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.Contains(".FromSiBaseUnit(left / right.AsSiBaseUnit())", string.Join("\n", test.GeneratedSources));
	}

	[Theory]
	[InlineData("Squared", "TerminalHex2", "LengthSquared")]
	[InlineData("Cubed", "TerminalHex3", "LengthCubed")]
	[InlineData("Pow4", "TerminalHex4", "Length4")]
	[InlineData("Pow5", "TerminalHex5", "Length5")]
	[InlineData("Pow6", "TerminalHex6", "Length6")]
	[InlineData("SquareRoot", "Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex2>", "SquareRootLength")]
	[InlineData("CubeRoot", "Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex3>", "CubeRootLength")]
	[InlineData("Root4", "Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex4>", "Root4Length")]
	[InlineData("Root5", "Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex5>", "Root5Length")]
	[InlineData("Root6", "Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex6>", "Root6Length")]
	public async Task PredefinedHelpersDelegateToExactPower(string method, string exponent, string result)
	{
		QuantityGeneratorTest test = new() { TestCode = $"class Consumer {{ {result} M(Length l) => l.{method}(); }}" };
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.Contains($"{method}() => global::DimensionalQuantities.TypeSystem.Power<global::DimensionalQuantities.TypeSystem.{exponent}>.Of(value);", string.Join("\n", test.GeneratedSources));
		SyntaxTree tree = test.Compilation.SyntaxTrees.First();
		InvocationExpressionSyntax call = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
		IMethodSymbol selected = Assert.IsAssignableFrom<IMethodSymbol>(test.Compilation.GetSemanticModel(tree).GetSymbolInfo(call, TestContext.Current.CancellationToken).Symbol);
		IMethodSymbol declared = test.Compilation.GetTypeByMetadataName("Consumer")!.GetMembers("M").OfType<IMethodSymbol>().Single();
		Assert.True(SymbolEqualityComparer.Default.Equals(declared.ReturnType, selected.ReturnType));
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
		QuantityGeneratorTest test = new() { TestCode = $"using DimensionalQuantities.TypeSystem; class Consumer {{ {output} M({input} value) => Power<{exponent}>.Of(value); }}" };
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
		QuantityGeneratorTest test = new() { TestCode = "class Consumer { LengthCurrent? M(Length l, Current i) { var product = l*i; var square = product.Squared(); LengthCurrent? restored = square.SquareRoot(); return restored?.Squared().SquareRoot(); } }" };
		await test.RunAsync(TestContext.Current.CancellationToken);
		string generated = string.Join("\n", test.GeneratedSources);
		Assert.Multiple(
			() => Assert.Contains("public LengthSquaredCurrentSquared Squared()", generated),
			() => Assert.Contains("public LengthCurrent SquareRoot()", generated));
	}
}
