using DimensionalQuantities.Dimensions;
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

		if (symbol is not ILocalSymbol and not IFieldSymbol and not IPropertySymbol and not IMethodSymbol and not IParameterSymbol)
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

	public bool TryReadCollectionElementType(ISymbol? symbol, IErrorTypeSymbol elementType, out Dimension dimension)
	{
		dimension = default;
		SyntaxNode? declaration = symbol?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(_cancellationToken);
		TypeSyntax? type = declaration switch
		{
			ParameterSyntax parameter => parameter.Type,
			VariableDeclaratorSyntax variable when variable.Parent is VariableDeclarationSyntax variableDeclaration
				=> variableDeclaration.Type.IsVar ? variable.Initializer?.Value switch
				{
					ArrayCreationExpressionSyntax arrayCreation => arrayCreation.Type,
					ObjectCreationExpressionSyntax objectCreation => objectCreation.Type,
					_ => null
				} : variableDeclaration.Type,
			PropertyDeclarationSyntax property => property.Type,
			IndexerDeclarationSyntax indexer => indexer.Type,
			MethodDeclarationSyntax method => method.ReturnType,
			LocalFunctionStatementSyntax localFunction => localFunction.ReturnType,
			_ => null
		};
		if (type is null)
		{
			return false;
		}

		SemanticModel model = _compilation.GetSemanticModel(type.SyntaxTree);
		foreach (IdentifierNameSyntax name in type.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
		{
			if (name.Identifier.ValueText == elementType.Name
				&& SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(name, _cancellationToken).Type, elementType)
				&& QuantityNameDiscovery.TryRead(name, model, out dimension, _cancellationToken))
			{
				return true;
			}
		}

		return false;
	}

	private QuantityOperand AnalyseDeclaration(ISymbol symbol)
	{
		SyntaxNode? syntax = symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(_cancellationToken);
		return syntax switch
		{
			ParameterSyntax parameter when symbol is IParameterSymbol => ReadDeclaredType(parameter.Type),
			VariableDeclaratorSyntax variable when variable.Parent is VariableDeclarationSyntax declaration
				&& variable.Initializer is { Value: ExpressionSyntax initializer }
				=> AnalyseInitializer(declaration.Type, initializer),
			VariableDeclaratorSyntax variable when symbol is IFieldSymbol or ILocalSymbol
				&& variable.Parent is VariableDeclarationSyntax declaration
				=> ReadDeclaredType(declaration.Type),
			PropertyDeclarationSyntax property when GetPropertyValue(property) is ExpressionSyntax value
				=> AnalyseInitializer(property.Type, value),
			PropertyDeclarationSyntax property when IsAutoProperty(property) => ReadDeclaredType(property.Type),
			MethodDeclarationSyntax method when symbol is IMethodSymbol methodSymbol && methodSymbol.MethodKind == MethodKind.Ordinary
				=> AnalyseMethod(methodSymbol, method.ReturnType, method.ExpressionBody, method.Body),
			LocalFunctionStatementSyntax localFunction when symbol is IMethodSymbol methodSymbol && methodSymbol.MethodKind == MethodKind.LocalFunction
				=> AnalyseMethod(methodSymbol, localFunction.ReturnType, localFunction.ExpressionBody, localFunction.Body),
			_ => default
		};
	}

	private QuantityOperand ReadDeclaredType(TypeSyntax? type)
	{
		if (type is IdentifierNameSyntax name
			&& QuantityNameDiscovery.TryRead(name, _compilation.GetSemanticModel(name.SyntaxTree), out Dimension dimension, _cancellationToken))
		{
			return new(QuantityOperandKind.Quantity, dimension);
		}

		return default;
	}

	private static bool IsAutoProperty(PropertyDeclarationSyntax property) => property.ExpressionBody is null
		&& property.Initializer is null && property.AccessorList is not null
		&& property.AccessorList.Accessors.All(accessor => accessor.Body is null && accessor.ExpressionBody is null);

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

		if (initializer.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.DefaultLiteralExpression)
			|| initializer is ImplicitObjectCreationExpressionSyntax)
		{
			return ReadDeclaredType(type);
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
