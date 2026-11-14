using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generation;

public sealed class ConditionalHelperDiscoveryTests
{
	[Theory]
	[InlineData("value?.Squared()", "Squared", "TerminalHex2", "TerminalHex2", 4, 144)]
	[InlineData("value?.SquareRoot()", "SquareRoot", "Fraction<TerminalHex1, TerminalHex2>", "Fraction<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex2>", 48, 12)]
	[InlineData("holder?.Value?.Squared()", "Squared", "TerminalHex2", "TerminalHex2", 2, 36)]
	[InlineData("value?.Squared().Squared()", "Squared", "TerminalHex4", "TerminalHex2", 2, 1296)]
	public async Task ConditionalCallAloneRequestsDiscoveredHelper(string expression, string helper, string finalExponent, string delegatedExponent, double lengthValue, double expected)
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
				class Holder
				{
					public LengthCurrent? Value { get; set; }
				}

				public class Consumer
				{
					double M(Length length, Current current, Holder holder, bool hasValue)
					{
						LengthCurrent? value = hasValue ? length * current : null;
						var result = {{expression}};
						return result?.AsSiBaseUnit() ?? -1;
					}
					public static double Execute()
					{
						Length length = Length.FromSiBaseUnit({{lengthValue}});
						Current current = Current.FromSiBaseUnit(3);
						Holder holder = new() { Value = length * current };
						return new Consumer().M(length, current, holder, true);
					}

					public static double ExecuteNull() => new Consumer().M(default, default, null, false);
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = test.Compilation.SyntaxTrees.First();
		SemanticModel model = test.Compilation.GetSemanticModel(tree);
		SyntaxNode root = tree.GetRoot(TestContext.Current.CancellationToken);
		VariableDeclaratorSyntax result = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().Single(variable => variable.Identifier.ValueText == "result");
		ILocalSymbol symbol = Assert.IsAssignableFrom<ILocalSymbol>(model.GetDeclaredSymbol(result, TestContext.Current.CancellationToken));
		INamedTypeSymbol nullable = Assert.IsAssignableFrom<INamedTypeSymbol>(symbol.Type);
		INamedTypeSymbol quantity = Assert.IsAssignableFrom<INamedTypeSymbol>(nullable.TypeArguments.Single());
		INamedTypeSymbol dimension = Assert.IsAssignableFrom<INamedTypeSymbol>(quantity.TypeArguments.Single());
		InvocationExpressionSyntax conditionalCall = root.DescendantNodes().OfType<InvocationExpressionSyntax>().Single(call => call.Expression is MemberBindingExpressionSyntax binding && binding.Name.Identifier.ValueText == helper);
		IMethodSymbol selected = Assert.IsAssignableFrom<IMethodSymbol>(model.GetSymbolInfo(conditionalCall, TestContext.Current.CancellationToken).Symbol);
		Assert.Multiple(
			() => Assert.Equal(SpecialType.System_Nullable_T, nullable.OriginalDefinition.SpecialType),
			() => Assert.Equal($"{finalExponent},TerminalHex0,TerminalHex0,{finalExponent},TerminalHex0,TerminalHex0,TerminalHex0",
				string.Join(",", dimension.TypeArguments.Select(argument => argument.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)))),
			() => Assert.Equal(helper, selected.Name),
			() => Assert.True(SymbolEqualityComparer.Default.Equals(test.Compilation.Assembly, selected.ContainingAssembly)),
			() => Assert.All(selected.DeclaringSyntaxReferences, reference => Assert.Contains(reference.SyntaxTree, test.GeneratedSources)),
			() => Assert.Contains($"{helper}() => global::DimensionalQuantities.TypeSystem.Power<global::DimensionalQuantities.TypeSystem.{delegatedExponent}>.Of(value);", string.Join("\n", test.GeneratedSources)),
			() => Assert.Equal(expected, GeneratedCodeExecution.Execute(test.Compilation), 12),
			() => Assert.Equal(-1, GeneratedCodeExecution.Execute(test.Compilation, "ExecuteNull")));
	}
}
