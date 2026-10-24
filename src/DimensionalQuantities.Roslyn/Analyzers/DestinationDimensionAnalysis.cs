using DimensionalQuantities.Dimensions;
using DimensionalQuantities.Roslyn.Discovery;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DimensionalQuantities.Roslyn.Analyzers;

internal static class DestinationDimensionAnalysis
{
	public static void Register(CompilationStartAnalysisContext context, QuantityTypeReader reader)
	{
		context.RegisterSyntaxNodeAction(node => AnalyzeAssignment(node, reader), SyntaxKind.SimpleAssignmentExpression);
		context.RegisterSyntaxNodeAction(node => AnalyzeInitializer(node, reader), SyntaxKind.EqualsValueClause);
		context.RegisterSyntaxNodeAction(node => AnalyzeReturn(node, reader), SyntaxKind.ReturnStatement, SyntaxKind.ArrowExpressionClause);
		context.RegisterSyntaxNodeAction(node => AnalyzeArgument(node, reader), SyntaxKind.Argument);
	}

	private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context, QuantityTypeReader reader)
	{
		AssignmentExpressionSyntax assignment = (AssignmentExpressionSyntax)context.Node;
		ITypeSymbol? target = context.SemanticModel.GetTypeInfo(assignment.Left, context.CancellationToken).Type;
		ReportIfDifferent(context, reader, target, assignment.Right);
	}

	private static void AnalyzeInitializer(SyntaxNodeAnalysisContext context, QuantityTypeReader reader)
	{
		EqualsValueClauseSyntax initializer = (EqualsValueClauseSyntax)context.Node;
		ITypeSymbol? target = initializer.Parent switch
		{
			VariableDeclaratorSyntax variable => (context.SemanticModel.GetDeclaredSymbol(variable, context.CancellationToken) as ILocalSymbol)?.Type
				?? (context.SemanticModel.GetDeclaredSymbol(variable, context.CancellationToken) as IFieldSymbol)?.Type,
			PropertyDeclarationSyntax property => (context.SemanticModel.GetDeclaredSymbol(property, context.CancellationToken) as IPropertySymbol)?.Type,
			_ => null
		};
		ReportIfDifferent(context, reader, target, initializer.Value);
	}

	private static void AnalyzeReturn(SyntaxNodeAnalysisContext context, QuantityTypeReader reader)
	{
		ExpressionSyntax? expression = context.Node switch
		{
			ReturnStatementSyntax statement => statement.Expression,
			ArrowExpressionClauseSyntax arrow => arrow.Expression,
			_ => null
		};
		if (expression is null)
		{
			return;
		}

		ITypeSymbol? target = context.Node switch
		{
			ArrowExpressionClauseSyntax { Parent: PropertyDeclarationSyntax property } =>
				(context.SemanticModel.GetDeclaredSymbol(property, context.CancellationToken) as IPropertySymbol)?.Type,
			ArrowExpressionClauseSyntax { Parent: IndexerDeclarationSyntax indexer } =>
				(context.SemanticModel.GetDeclaredSymbol(indexer, context.CancellationToken) as IPropertySymbol)?.Type,
			_ => (context.SemanticModel.GetEnclosingSymbol(context.Node.SpanStart, context.CancellationToken) as IMethodSymbol)?.ReturnType
		};
		ReportIfDifferent(context, reader, target, expression);
	}

	private static void AnalyzeArgument(SyntaxNodeAnalysisContext context, QuantityTypeReader reader)
	{
		ArgumentSyntax argument = (ArgumentSyntax)context.Node;
		SyntaxNode? call = argument.Parent?.Parent;
		if (call is not InvocationExpressionSyntax && call is not BaseObjectCreationExpressionSyntax)
		{
			return;
		}

		SymbolInfo binding = context.SemanticModel.GetSymbolInfo(call, context.CancellationToken);
		IMethodSymbol? method = binding.Symbol as IMethodSymbol;
		if (method is null && binding.CandidateSymbols.Length == 1)
		{
			method = binding.CandidateSymbols[0] as IMethodSymbol;
		}

		if (method is null)
		{
			return;
		}

		BaseArgumentListSyntax? arguments = call switch
		{
			InvocationExpressionSyntax invocation => invocation.ArgumentList,
			BaseObjectCreationExpressionSyntax creation => creation.ArgumentList,
			_ => null
		};
		IParameterSymbol? parameter = argument.NameColon is not null
			? method.Parameters.FirstOrDefault(candidate => candidate.Name == argument.NameColon.Name.Identifier.ValueText)
			: method.Parameters.ElementAtOrDefault(arguments?.Arguments.IndexOf(argument) ?? -1);
		ReportIfDifferent(context, reader, parameter?.Type, argument.Expression);
	}

	private static void ReportIfDifferent(SyntaxNodeAnalysisContext context, QuantityTypeReader reader,
		ITypeSymbol? target, ExpressionSyntax expression)
	{
		ITypeSymbol? source = context.SemanticModel.GetTypeInfo(expression, context.CancellationToken).Type;
		if (!reader.TryReadIncludingNullable(target, out Dimension expected, context.CancellationToken)
			|| !reader.TryReadIncludingNullable(source, out Dimension actual, context.CancellationToken)
			|| expected == actual || target is null || source is null)
		{
			return;
		}

		Microsoft.CodeAnalysis.CSharp.Conversion conversion = context.SemanticModel.Compilation is CSharpCompilation compilation
			? compilation.ClassifyConversion(source, target) : default;
		if (conversion.IsImplicit)
		{
			return;
		}

		context.ReportDiagnostic(Diagnostic.Create(DimensionMismatchAnalyzer.WrongDestination, expression.GetLocation(),
			DimensionNames.Get(expected), DimensionNames.Get(actual)));
	}
}
