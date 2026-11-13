using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Diagnostics;

public sealed class GeneratorDiagnosticTests
{
	[Theory]
	[InlineData("class {|#0:Length|} {}")]
	[InlineData("delegate void {|#0:Length|}();")]
	[InlineData("using {|#0:Length|} = System.String; class Measurement {}")]
	[InlineData("namespace Consumer { using {|#0:Length|} = System.String; }")]
	public async Task GeneratedAliasConflictReportsDeclaredIdentifierAndStillEmitsAlias(string source)
	{
		QuantityGeneratorTest test = new() { TestCode = source, CompilerDiagnostics = CompilerDiagnostics.None };
		test.ExpectedDiagnostics.Add(Collision());
		test.DiagnosticVerifier = (actual, expected, verifier) => Assert.Multiple(() => Assert.Equal("DimensionalQuantities", actual.Descriptor.Category), () => Assert.True(actual.Descriptor.IsEnabledByDefault));
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.Contains(test.GeneratedSources.SelectMany(tree => tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<UsingDirectiveSyntax>()), directive => directive.GlobalKeyword.Text == "global" && directive.Alias?.Name.Identifier.ValueText == "Length");
	}

	[Theory]
	[InlineData("class Length<T> {}")]
	[InlineData("namespace Consumer { class Length {} }")]
	public async Task NonGlobalOrGenericDeclarationIsNotGlobalCollision(string source)
	{
		QuantityGeneratorTest test = new() { TestCode = source };
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.NotEmpty(test.GeneratedSources);
	}

	[Fact]
	public async Task ImportedTypeUseActuallyChangesBinding()
	{
		const string source = "using Imported; namespace Imported { public class Length {} } class Measurement { {|#0:Length|} value; }";
		QuantityGeneratorTest test = new() { TestCode = source };
		test.ExpectedDiagnostics.Add(Collision());
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree input = test.Compilation.SyntaxTrees.First(tree => tree.GetText(TestContext.Current.CancellationToken).ToString().Contains("class Measurement"));
		IdentifierNameSyntax use = input.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<FieldDeclarationSyntax>().Single().Declaration.Type as IdentifierNameSyntax ?? throw new InvalidOperationException();
		Compilation before = test.Compilation.RemoveSyntaxTrees(test.GeneratedSources);
		Assert.Multiple(
			() => Assert.Equal("Imported.Length", before.GetSemanticModel(input).GetTypeInfo(use, TestContext.Current.CancellationToken).Type!.ToDisplayString()),
			() => Assert.StartsWith("DimensionalQuantities.TypeSystem.Quantity<", test.Compilation.GetSemanticModel(input).GetTypeInfo(use, TestContext.Current.CancellationToken).Type!.ToDisplayString()));
	}

	[Fact]
	public async Task QualifiedAndValueUsesKeepTheirMeaning()
	{
		QuantityGeneratorTest test = new() { TestCode = "namespace Imported { public class Length {} } class Measurement { Imported.Length value; int Apply(int Length) => Length; }" };
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.NotEmpty(test.GeneratedSources);
	}

	[Theory]
	[InlineData("NumberBananas")]
	[InlineData("NumberOneOverZero")]
	public async Task InvalidEnglishRequestReportsRequestedIdentifier(string exponent)
	{
		QuantityGeneratorTest test = new() { TestCode = $"using DimensionalQuantities.TypeSystem; class Calculation {{ object Apply(Length value) => Power<{{|#0:{exponent}|}}>.Of(value); }}", CompilerDiagnostics = CompilerDiagnostics.None };
		test.ExpectedDiagnostics.Add(new DiagnosticResult("DQ0007", DiagnosticSeverity.Error).WithLocation(0).WithArguments(exponent));
		test.DiagnosticVerifier = (actual, expected, verifier) => Assert.Multiple(() => Assert.Equal("DimensionalQuantities", actual.Descriptor.Category), () => Assert.True(actual.Descriptor.IsEnabledByDefault));
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.DoesNotContain(test.GeneratedSources.SelectMany(tree => tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<UsingDirectiveSyntax>()), directive => directive.Alias?.Name.Identifier.ValueText == exponent);
	}

	[Theory]
	[InlineData("Power<NumberBananas> value;")]
	[InlineData("object Apply(Length value) => Power<NumberBananas>.Other(value);")]
	[InlineData("object Apply(Length value) => Power<NumberBananas>.Of(value, value);")]
	[InlineData("object Apply(Length value) => Power<Bananas>.Of(value);")]
	[InlineData("object Apply(Length value) => Power<Words.NumberBananas>.Of(value);")]
	[InlineData("object Apply(Length value) => Power<NumberBananas<int>>.Of(value);")]
	public async Task ResemblingSyntaxDoesNotBecomeEnglishRequest(string member)
	{
		QuantityGeneratorTest test = new() { TestCode = "using DimensionalQuantities.TypeSystem; class Calculation { " + member + " }", CompilerDiagnostics = CompilerDiagnostics.None };
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.NotEmpty(test.GeneratedSources);
	}

	[Fact]
	public async Task BoundAliasIsNotAnUnresolvedEnglishRequest()
	{
		QuantityGeneratorTest test = new() { TestCode = "using DimensionalQuantities.TypeSystem; using NumberBananas = DimensionalQuantities.TypeSystem.TerminalHex2; class Calculation { object Apply(Length value) => Power<NumberBananas>.Of(value); }" };
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.Contains(test.GeneratedSources, tree => tree.ToString().Contains("Of("));
	}

	[Fact]
	public async Task ValidEnglishRequestGeneratesRequestedAlias()
	{
		QuantityGeneratorTest test = new() { TestCode = "using DimensionalQuantities.TypeSystem; class Calculation { object Apply(Length value) => Power<NumberTwo>.Of(value); }" };
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.Contains(test.GeneratedSources.SelectMany(tree => tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<UsingDirectiveSyntax>()), directive => directive.Alias?.Name.Identifier.ValueText == "NumberTwo" && directive.Name!.ToString().EndsWith("TerminalHex2"));
	}

	[Fact]
	public async Task StaticTypeReceiverDisplacementReportsReceiverIdentifier()
	{
		QuantityGeneratorTest test = new() { TestCode = "using Imported; namespace Imported { public class Length { public static string FromSiBaseUnit(double value) => \"old type\"; } } class Measurement { object value = {|#0:Length|}.FromSiBaseUnit(1); }" };
		test.ExpectedDiagnostics.Add(Collision());
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree input = test.Compilation.SyntaxTrees.First(tree => tree.ToString().Contains("class Measurement"));
		MemberAccessExpressionSyntax access = input.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single().Expression as MemberAccessExpressionSyntax ?? throw new InvalidOperationException();
		Compilation before = test.Compilation.RemoveSyntaxTrees(test.GeneratedSources);
		Assert.Multiple(
			() => Assert.Equal("Imported.Length", before.GetSemanticModel(input).GetSymbolInfo(access.Expression, TestContext.Current.CancellationToken).Symbol!.ToDisplayString()),
			() => Assert.StartsWith("DimensionalQuantities.TypeSystem.Quantity<", test.Compilation.GetSemanticModel(input).GetSymbolInfo(access.Expression, TestContext.Current.CancellationToken).Symbol!.ToDisplayString()));
	}

	[Fact]
	public async Task LookalikePowerDoesNotRequestAnEnglishExponent()
	{
		QuantityGeneratorTest test = new() { TestCode = "namespace Consumer { class Power<T> { public static object Of(object value) => value; } class Calculation { object Apply(Length value) => Power<NumberBananas>.Of(value); } }", CompilerDiagnostics = CompilerDiagnostics.None };
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.NotEmpty(test.GeneratedSources);
	}

	[Fact]
	public async Task MetadataImportedTypeDisplacementHasVerifiedBeforeAndAfterMeaning()
	{
		QuantityGeneratorTest library = new() { TestCode = "namespace Imported { public class Length {} }" };
		await library.RunAsync(TestContext.Current.CancellationToken);
		using MemoryStream assembly = new();
		Microsoft.CodeAnalysis.Emit.EmitResult emitted = library.Compilation.Emit(assembly, cancellationToken: TestContext.Current.CancellationToken);
		if (!emitted.Success)
		{
			throw new InvalidOperationException(string.Join("\n", emitted.Diagnostics));
		}

		QuantityGeneratorTest test = new() { TestCode = "using Imported; class Measurement { {|#0:Length|} value; }" };
		test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromImage(assembly.ToArray()));
		test.ExpectedDiagnostics.Add(Collision());
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree input = test.Compilation.SyntaxTrees.First(tree => tree.ToString().Contains("class Measurement"));
		TypeSyntax use = input.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<FieldDeclarationSyntax>().Single().Declaration.Type;
		Compilation before = test.Compilation.RemoveSyntaxTrees(test.GeneratedSources);
		Assert.Multiple(
			() => Assert.Equal("Imported.Length", before.GetSemanticModel(input).GetTypeInfo(use, TestContext.Current.CancellationToken).Type!.ToDisplayString()),
			() => Assert.StartsWith("DimensionalQuantities.TypeSystem.Quantity<", test.Compilation.GetSemanticModel(input).GetTypeInfo(use, TestContext.Current.CancellationToken).Type!.ToDisplayString()));
	}

	[Fact]
	public async Task SameTargetExplicitAliasStillReportsAcceptedCollisionPolicy()
	{
		QuantityGeneratorTest test = new() { TestCode = "using DimensionalQuantities.TypeSystem; namespace Consumer { using {|#0:Length|} = Quantity<Dimension<TerminalHex1, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0>>; }" };
		test.ExpectedDiagnostics.Add(Collision());
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.Contains(test.GeneratedSources.SelectMany(tree => tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<UsingDirectiveSyntax>()), directive => directive.Alias?.Name.Identifier.ValueText == "Length");
	}


	[Fact]
	public async Task BoundConcreteExponentIsNotAnUnresolvedEnglishRequest()
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = "using DimensionalQuantities.TypeSystem; class NumberBananas {} class Consumer { object M(Length value) => Power<NumberBananas>.Of(value); }",
			CompilerDiagnostics = CompilerDiagnostics.None
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = test.Compilation.SyntaxTrees.First();
		IdentifierNameSyntax exponent = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<IdentifierNameSyntax>().Single(identifier => identifier.Identifier.ValueText == "NumberBananas");
		Assert.Multiple(
			() => Assert.True(SymbolEqualityComparer.Default.Equals(test.Compilation.GetTypeByMetadataName("NumberBananas"), test.Compilation.GetSemanticModel(tree).GetSymbolInfo(exponent, TestContext.Current.CancellationToken).Symbol)),
			() => Assert.DoesNotContain(test.GeneratedSources.SelectMany(source => source.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>()), alias => alias.Alias?.Name.Identifier.ValueText == "NumberBananas"));
	}
	private static DiagnosticResult Collision() => new DiagnosticResult("DQ0006", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("Length");
}
