using DimensionalQuantities.Naming;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DimensionalQuantities.Roslyn.Discovery;

internal sealed class QuantityDeclarationResolver
{
	private readonly Compilation _compilation;
	private readonly Func<ExpressionSyntax, QuantityOperand> _analyse;
	private readonly Func<IReadOnlyList<ExpressionSyntax?>, string, QuantityOperand> _analyseNamed;
	private readonly CancellationToken _cancellationToken;
	private readonly Dictionary<ISymbol, QuantityOperand> _resolved = new(SymbolEqualityComparer.Default);
	private readonly HashSet<ISymbol> _inProgress = new(SymbolEqualityComparer.Default);

	public QuantityDeclarationResolver(Compilation compilation, Func<ExpressionSyntax, QuantityOperand> analyse,
		Func<IReadOnlyList<ExpressionSyntax?>, string, QuantityOperand> analyseNamed, CancellationToken cancellationToken)
	{
		_compilation = compilation;
		_analyse = analyse;
		_analyseNamed = analyseNamed;
		_cancellationToken = cancellationToken;
	}

	public QuantityOperand Resolve(ISymbol symbol)
	{
		if (symbol is IMethodSymbol { MethodKind: MethodKind.ReducedExtension, ReducedFrom: IMethodSymbol original })
		{
			symbol = original;
		}

		if (symbol is not ILocalSymbol and not IFieldSymbol and not IPropertySymbol and not IMethodSymbol)
		{
			return default;
		}

		if (_resolved.TryGetValue(symbol, out QuantityOperand cached))
		{
			return cached;
		}

		if (!_inProgress.Add(symbol))
		{
			return default;
		}

		try
		{
			QuantityOperand result = AnalyseDeclaration(symbol);
			_resolved.Add(symbol, result);
			return result;
		}
		finally
		{
			_inProgress.Remove(symbol);
		}
	}

	public static bool MatchesExpectedName(string typeName, QuantityOperand inferred) => inferred.Kind == QuantityOperandKind.Quantity
		&& typeName == DimensionNameGenerator.GetQuantityName(inferred.Dimension);

	private QuantityOperand AnalyseDeclaration(ISymbol symbol)
	{
		SyntaxNode? syntax = symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(_cancellationToken);
		return syntax switch
		{
			VariableDeclaratorSyntax variable when variable.Parent is VariableDeclarationSyntax declaration
				&& variable.Initializer is { Value: ExpressionSyntax initializer }
				=> AnalyseInitializer(declaration.Type, initializer),
			PropertyDeclarationSyntax property when GetPropertyValue(property) is ExpressionSyntax value
				=> AnalyseInitializer(property.Type, value),
			MethodDeclarationSyntax method when symbol is IMethodSymbol methodSymbol && methodSymbol.MethodKind == MethodKind.Ordinary
				=> AnalyseMethod(methodSymbol, method.ReturnType, method.ExpressionBody, method.Body),
			LocalFunctionStatementSyntax localFunction when symbol is IMethodSymbol methodSymbol && methodSymbol.MethodKind == MethodKind.LocalFunction
				=> AnalyseMethod(methodSymbol, localFunction.ReturnType, localFunction.ExpressionBody, localFunction.Body),
			_ => default
		};
	}

	private QuantityOperand AnalyseMethod(IMethodSymbol symbol, TypeSyntax returnType, ArrowExpressionClauseSyntax? expressionBody, BlockSyntax? body)
	{
		if (symbol.IsAsync || body is not null && DescendantsOutsideNestedFunctions(body).OfType<YieldStatementSyntax>().Any())
		{
			return default;
		}

		IReadOnlyList<ExpressionSyntax?> returns = expressionBody is not null
			? [expressionBody.Expression]
			: body is not null
				? DescendantsOutsideNestedFunctions(body).OfType<ReturnStatementSyntax>().Select(statement => statement.Expression).ToArray()
				: [];
		if (returns.Count == 0)
		{
			return default;
		}

		return AnalyseNamedValues(returnType, returns);
	}

	private static IEnumerable<SyntaxNode> DescendantsOutsideNestedFunctions(BlockSyntax body) => body.DescendantNodes(
		descendIntoChildren: node => node is not AnonymousFunctionExpressionSyntax and not LocalFunctionStatementSyntax);

	private QuantityOperand AnalyseInitializer(TypeSyntax type, ExpressionSyntax initializer)
	{
		SemanticModel model = _compilation.GetSemanticModel(type.SyntaxTree);
		if (type.IsVar && model.GetSymbolInfo(type, _cancellationToken).Symbol is null)
		{
			return _analyse(initializer);
		}

		return AnalyseNamedValues(type, [initializer]);
	}

	private QuantityOperand AnalyseNamedValues(TypeSyntax type, IReadOnlyList<ExpressionSyntax?> values)
	{
		SemanticModel model = _compilation.GetSemanticModel(type.SyntaxTree);
		if (type is not IdentifierNameSyntax name
			|| model.GetTypeInfo(name, _cancellationToken).Type is not IErrorTypeSymbol
			|| model.GetSymbolInfo(name, _cancellationToken) is not { Symbol: null, CandidateSymbols.IsEmpty: true })
		{
			return default;
		}

		return _analyseNamed(values, name.Identifier.ValueText);
	}

	private static ExpressionSyntax? GetPropertyValue(PropertyDeclarationSyntax property)
	{
		if (property.ExpressionBody is { Expression: ExpressionSyntax expression })
		{
			return expression;
		}

		AccessorDeclarationSyntax? getter = property.AccessorList?.Accessors.FirstOrDefault(accessor => accessor.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.GetAccessorDeclaration));
		return getter?.ExpressionBody?.Expression ?? property.Initializer?.Value;
	}
}
