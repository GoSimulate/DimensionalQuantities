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
	[InlineData(
		"""
			var value = length * current;
			var result = value * time;
			Capture(result.AsSiBaseUnit());
			""", "length * current")]
	[InlineData(
		"""
			var make = () => length * current;
			var result = make() * time;
			Capture(result.AsSiBaseUnit());
			""", "length * current")]
	[InlineData(
		"""
			var pair = (length * current, time);
			var result = pair.Item1 * pair.Item2;
			Capture(result.AsSiBaseUnit());
			""", "length * current")]
	[InlineData(
		"""
			var (value, duration) = (length * current, time);
			var result = value * duration;
			Capture(result.AsSiBaseUnit());
			""", "length * current")]
	[InlineData(
		"""
			var result = Identity(length * current) * time;
			Capture(result.AsSiBaseUnit());
			""", "length * current")]
	[InlineData(
		"""
			var values = new[] { length }.Select(item => item * current);
			var result = values.First() * time;
			Capture(result.AsSiBaseUnit());
			""", "item * current")]
	[InlineData(
		"""
			var values = new[] { length * current };
			var result = values[0] * time;
			Capture(result.AsSiBaseUnit());
			""", "length * current")]
	[InlineData(
		"""
			var values = new[] { length * current };
			foreach (var value in values)
			{
				var result = value * time;
				Capture(result.AsSiBaseUnit());
			}
			""", "length * current")]
	[InlineData(
		"""
			var value = choose ? length * current : current * length;
			var result = value * time;
			Capture(result.AsSiBaseUnit());
			""", "length * current")]
	[InlineData(
		"""
			var value = choose switch
			{
				true => length * current,
				false => current * length
			};
			var result = value * time;
			Capture(result.AsSiBaseUnit());
			""", "length * current")]
	[InlineData(
		"""
			LengthCurrent? value = null;
			var fallback = length * current;
			var result = (value ?? fallback) * time;
			Capture(result.AsSiBaseUnit());
			""", "length * current")]
	[InlineData(
		"""
			object value = length * current;
			var result = ((LengthCurrent)value) * time;
			Capture(result.AsSiBaseUnit());
			""", "length * current")]
	[InlineData(
		"""
			var values = new[] { length * current };
			LengthTimeCurrent result = default;
			foreach (var value in values)
			{
				result += value * time;
			}
			Capture(result.AsSiBaseUnit());
			""", "length * current")]
	public async Task GeneratedIntermediateFlowsThroughInferredBinding(string statements, string productExpression)
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
				using System.Linq;
				public static class Consumer
				{
					private static double captured;
					private static void Capture(double value) => captured = value;
					private static T Identity<T>(T value) => value;
					public static double Execute()
					{
						Calculate(Length.FromSiBaseUnit(2), Current.FromSiBaseUnit(3), Time.FromSiBaseUnit(4), true);
						return captured;
					}

					private static void Calculate(Length length, Current current, Time time, bool choose)
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
			}),
			() => Assert.Equal(24, GeneratedCodeExecution.Execute(test.Compilation), 12));
	}
}
