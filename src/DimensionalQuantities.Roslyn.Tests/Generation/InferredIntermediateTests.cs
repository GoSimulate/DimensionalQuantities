using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Generation;

public sealed class InferredIntermediateTests
{
	private const string ProductType = "DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>>";
	private const string ResultType = "DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0>>";

	[Theory]
	[InlineData("var value = length * current; var result = value * time;", "length * current")]
	[InlineData("var make = () => length * current; var result = make() * time;", "length * current")]
	[InlineData("var pair = (length * current, time); var result = pair.Item1 * pair.Item2;", "length * current")]
	[InlineData("var (value, duration) = (length * current, time); var result = value * duration;", "length * current")]
	[InlineData("var result = Identity(length * current) * time;", "length * current")]
	[InlineData("var values = new[] { length }.Select(item => item * current); var result = values.First() * time;", "item * current")]
	[InlineData("var values = new[] { length * current }; var result = values[0] * time;", "length * current")]
	[InlineData("var values = new[] { length * current }; foreach (var value in values) { var result = value * time; }", "length * current")]
	[InlineData("var value = choose ? length * current : current * length; var result = value * time;", "length * current")]
	[InlineData("var value = choose switch { true => length * current, false => current * length }; var result = value * time;", "length * current")]
	public async Task GeneratedIntermediateFlowsThroughInferredBinding(string statements, string productExpression)
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
				using System.Linq;
				class Consumer
				{
				    private static T Identity<T>(T value) => value;
				    void Calculate(Length length, Current current, Time time, bool choose)
				    {
				        {{statements}}
				    }
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		SyntaxTree tree = test.Compilation.SyntaxTrees.First();
		SemanticModel model = test.Compilation.GetSemanticModel(tree);
		SyntaxNode root = tree.GetRoot(TestContext.Current.CancellationToken);
		BinaryExpressionSyntax product = root.DescendantNodes().OfType<BinaryExpressionSyntax>().Single(expression => expression.ToString() == productExpression);
		VariableDeclaratorSyntax result = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().Single(variable => variable.Identifier.ValueText == "result");
		ILocalSymbol resultSymbol = Assert.IsAssignableFrom<ILocalSymbol>(model.GetDeclaredSymbol(result, TestContext.Current.CancellationToken));
		Assert.Multiple(
			() => Assert.Equal(ProductType, model.GetTypeInfo(product, TestContext.Current.CancellationToken).Type!.ToDisplayString()),
			() => Assert.Equal(ResultType, resultSymbol.Type.ToDisplayString()),
			() => Assert.All(root.DescendantNodes().OfType<BinaryExpressionSyntax>().Where(expression => expression.IsKind(SyntaxKind.MultiplyExpression)), expression =>
			{
				IBinaryOperation operation = Assert.IsAssignableFrom<IBinaryOperation>(model.GetOperation(expression, TestContext.Current.CancellationToken));
				IMethodSymbol selected = Assert.IsAssignableFrom<IMethodSymbol>(operation.OperatorMethod);
				Assert.Multiple(
					() => Assert.True(SymbolEqualityComparer.Default.Equals(operation.Type, selected.ReturnType)),
					() => Assert.True(SymbolEqualityComparer.Default.Equals(operation.LeftOperand.Type, selected.Parameters[0].Type)),
					() => Assert.True(SymbolEqualityComparer.Default.Equals(operation.RightOperand.Type, selected.Parameters[1].Type)));
			}));
	}
}
