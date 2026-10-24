using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace DimensionalQuantities.Roslyn.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(QuantityAliasCodeFixProvider))]
[Shared]
public sealed class QuantityAliasCodeFixProvider : CodeFixProvider
{
	public override ImmutableArray<string> FixableDiagnosticIds => ["DQ0003"];

	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		Diagnostic diagnostic = context.Diagnostics[0];
		if (!diagnostic.Properties.TryGetValue("AliasName", out string? alias) || alias is null or "")
		{
			return;
		}

		SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		SemanticModel? model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
		if (root is null || model is null
			|| root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<GenericNameSyntax>() is not GenericNameSyntax generic)
		{
			return;
		}

		NameSyntax name = GetOutermostName(generic);
		if (name.Ancestors().OfType<InvocationExpressionSyntax>()
			.Any(invocation => model.GetOperation(invocation, context.CancellationToken) is INameOfOperation))
		{
			return;
		}

		ITypeSymbol? originalType = model.GetTypeInfo(name, context.CancellationToken).Type;
		TypeSyntax replacement = SyntaxFactory.ParseTypeName(alias);
		ITypeSymbol? replacementType = model.GetSpeculativeTypeInfo(name.SpanStart, replacement,
			SpeculativeBindingOption.BindAsTypeOrNamespace).Type;
		if (originalType is null || !SymbolEqualityComparer.Default.Equals(originalType, replacementType))
		{
			return;
		}

		context.RegisterCodeFix(CodeAction.Create($"Use {alias}", cancellationToken => ReplaceAsync(context.Document, name, replacement, cancellationToken),
			$"UseAlias:{alias}"), diagnostic);
	}

	private static NameSyntax GetOutermostName(NameSyntax name)
	{
		while (name.Parent is NameSyntax parent)
		{
			name = parent;
		}

		return name;
	}

	private static async Task<Document> ReplaceAsync(Document document, NameSyntax original,
		TypeSyntax replacement, CancellationToken cancellationToken)
	{
		SyntaxNode root = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!;
		SyntaxNode updated = root.ReplaceNode(original, replacement.WithTriviaFrom(original));
		return document.WithSyntaxRoot(updated);
	}
}
