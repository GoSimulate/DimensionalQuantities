using DimensionalQuantities.Roslyn.Discovery;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Discovery;

public sealed class ArithmeticDiscoveryTests
{
	[Theory]
	[InlineData("a * b", new[] { "a * b" })]
	[InlineData("(a * b) / c", new[] { "(a * b) / c" })]
	[InlineData("a * (b / c)", new[] { "a * (b / c)" })]
	[InlineData("Use(a * b) * c", new[] { "Use(a * b) * c", "a * b" })]
	[InlineData("a * b + c / d", new[] { "a * b", "c / d" })]
	[InlineData("a + b", new string[] { })]
	public void SelectsOutermostArithmeticThroughParentheses(string source, string[] expected)
	{
		ExpressionSyntax expression = SyntaxFactory.ParseExpression(source);
		string[] result = expression.DescendantNodesAndSelf().OfType<BinaryExpressionSyntax>()
			.Where(ArithmeticDiscovery.IsRoot).Select(binary => binary.ToString()).ToArray();

		Assert.Equal(expected, result);
	}
}
