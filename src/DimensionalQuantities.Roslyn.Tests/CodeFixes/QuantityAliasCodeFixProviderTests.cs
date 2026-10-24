using System.Collections.Immutable;
using DimensionalQuantities.Roslyn.Analyzers;
using DimensionalQuantities.Roslyn.CodeFixes;
using DimensionalQuantities.Roslyn.Tests.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.CodeFixes;

public sealed class QuantityAliasCodeFixProviderTests
{
	private const string LengthType = "global::DimensionalQuantities.TypeSystem.Quantity<global::DimensionalQuantities.TypeSystem.Dimension<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex0, "
		+ "global::DimensionalQuantities.TypeSystem.TerminalHex0, global::DimensionalQuantities.TypeSystem.TerminalHex0, global::DimensionalQuantities.TypeSystem.TerminalHex0, "
		+ "global::DimensionalQuantities.TypeSystem.TerminalHex0, global::DimensionalQuantities.TypeSystem.TerminalHex0>>";

	private const string LengthCurrentType = "global::DimensionalQuantities.TypeSystem.Quantity<global::DimensionalQuantities.TypeSystem.Dimension<global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex0, "
		+ "global::DimensionalQuantities.TypeSystem.TerminalHex0, global::DimensionalQuantities.TypeSystem.TerminalHex1, global::DimensionalQuantities.TypeSystem.TerminalHex0, "
		+ "global::DimensionalQuantities.TypeSystem.TerminalHex0, global::DimensionalQuantities.TypeSystem.TerminalHex0>>";

	[Theory]
	[InlineData("public class Consumer { public TYPE Value { get; } }", "public class Consumer { public Length Value { get; } }")]
	[InlineData("public class Consumer { public TYPE? Value { get; } }", "public class Consumer { public Length? Value { get; } }")]
	[InlineData("public class Consumer { public /* measurement */ TYPE Value { get; } }", "public class Consumer { public /* measurement */ Length Value { get; } }")]
	[InlineData("public class Consumer { public TYPE Value { get; } }", "public class Consumer { public LengthCurrent Value { get; } }", LengthCurrentType)]
	public async Task FixUsesAliasAndPreservesSurroundingSource(string template, string expected, string? rawType = null)
	{
		string source = template.Replace("TYPE", rawType ?? LengthType);
		using AdhocWorkspace workspace = new();
		Document document = CreateDocument(workspace, source);
		Compilation compilation = (await document.Project.GetCompilationAsync(TestContext.Current.CancellationToken))!;
		Diagnostic diagnostic = Assert.Single(await compilation.WithAnalyzers([new QuantityEncodingAnalyzer()]).GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken));
		List<CodeAction> actions = [];
		QuantityAliasCodeFixProvider provider = new();
		await provider.RegisterCodeFixesAsync(new(document, diagnostic, (action, _) => actions.Add(action), TestContext.Current.CancellationToken));
		CodeAction action = Assert.Single(actions);
		ImmutableArray<CodeActionOperation> operations = await action.GetOperationsAsync(TestContext.Current.CancellationToken);
		ApplyChangesOperation changes = Assert.Single(operations.OfType<ApplyChangesOperation>());
		Document changed = changes.ChangedSolution.GetDocument(document.Id)!;
		string result = (await changed.GetTextAsync(TestContext.Current.CancellationToken)).ToString();
		CSharpCompilation finalCompilation = AnalyzerTestCompilation.Create(result);
		Assert.Multiple(
			() => Assert.Equal(expected, result),
			() => Assert.DoesNotContain(finalCompilation.GetDiagnostics(TestContext.Current.CancellationToken), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
	}

	[Fact]
	public async Task NameOfDoesNotOfferFixThatChangesTheReturnedName()
	{
		string source = $"public class Consumer {{ public string Value() => nameof({LengthType}); }}";
		using AdhocWorkspace workspace = new();
		Document document = CreateDocument(workspace, source);
		Compilation compilation = (await document.Project.GetCompilationAsync(TestContext.Current.CancellationToken))!;
		Diagnostic diagnostic = Assert.Single(await compilation.WithAnalyzers([new QuantityEncodingAnalyzer()]).GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken));
		List<CodeAction> actions = [];
		QuantityAliasCodeFixProvider provider = new();
		await provider.RegisterCodeFixesAsync(new(document, diagnostic, (action, _) => actions.Add(action), TestContext.Current.CancellationToken));
		Assert.Empty(actions);
	}

	[Fact]
	public async Task ShadowedAliasDoesNotOfferUnsafeFix()
	{
		string source = $"namespace ConsumerNamespace {{ public class Length {{ }} public class Consumer {{ public {LengthType} Value {{ get; }} }} }}";
		using AdhocWorkspace workspace = new();
		Document document = CreateDocument(workspace, source);
		Compilation compilation = (await document.Project.GetCompilationAsync(TestContext.Current.CancellationToken))!;
		ImmutableArray<Diagnostic> diagnostics = await compilation.WithAnalyzers([new QuantityEncodingAnalyzer()]).GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);
		List<CodeAction> actions = [];
		QuantityAliasCodeFixProvider provider = new();
		foreach (Diagnostic diagnostic in diagnostics.Where(diagnostic => diagnostic.Id == "DQ0003"))
		{
			await provider.RegisterCodeFixesAsync(new(document, diagnostic, (action, _) => actions.Add(action), TestContext.Current.CancellationToken));
		}
		Assert.Empty(actions);
	}

	private static Document CreateDocument(AdhocWorkspace workspace, string source)
	{
		CSharpCompilation compilation = AnalyzerTestCompilation.Create(source);
		Project project = workspace.AddProject(ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), "Consumer", "Consumer", LanguageNames.CSharp,
			compilationOptions: compilation.Options, parseOptions: new CSharpParseOptions(LanguageVersion.Preview), metadataReferences: compilation.References));
		Document userDocument = workspace.AddDocument(project.Id, "Consumer.cs", SourceText.From(source));
		foreach (SyntaxTree tree in compilation.SyntaxTrees.Where(tree => tree.FilePath != "Consumer.cs"))
		{
			workspace.AddDocument(project.Id, Path.GetFileName(tree.FilePath), tree.GetText());
		}
		return workspace.CurrentSolution.GetDocument(userDocument.Id)!;
	}
}
