using System.Collections.Immutable;
using System.Composition.Hosting;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Loader;
using DimensionalQuantities.IntegrationTests.Delivery;
using DimensionalQuantities.IntegrationTests.Fixtures;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace DimensionalQuantities.IntegrationTests;

[Collection(typeof(PackageDeliveryCollection))]
public sealed class DeliveredCodeFixTests
{
	private readonly PackageDeliveryFixture _fixture;

	public DeliveredCodeFixTests(PackageDeliveryFixture fixture)
	{
		_fixture = fixture;
	}

	[Fact]
	public async Task PackagedMefExportLoadsAndReplacesRawQuantityWithSafeAlias()
	{
		using PackageSandbox sandbox = _fixture.CreateConsumer();
		string extracted = Path.Combine(sandbox.Root, "extracted");
		ZipFile.ExtractToDirectory(sandbox.ArchivePath("DimensionalQuantities"), extracted);
		string codeFixPath = Path.Combine(extracted, "analyzers/dotnet/cs/DimensionalQuantities.Roslyn.CodeFixes.dll");
		AssemblyLoadContext loadContext = new("delivered-fix-" + Guid.NewGuid(), isCollectible: true);
		loadContext.Resolving += (_, name) => AssemblyLoadContext.Default.LoadFromAssemblyName(name);
		try
		{
			Assembly delivered = loadContext.LoadFromAssemblyPath(codeFixPath);
			using CompositionHost exports = new ContainerConfiguration().WithAssembly(delivered).CreateContainer();
			CodeFixProvider provider = Assert.Single(exports.GetExports<CodeFixProvider>());
			Assert.Multiple(
				() => Assert.Equal(codeFixPath, delivered.Location),
				() => Assert.Equal(["DQ0003"], provider.FixableDiagnosticIds));

			const string source = """
				using DimensionalQuantities.TypeSystem;
				using Length = DimensionalQuantities.TypeSystem.Quantity<DimensionalQuantities.TypeSystem.Dimension<
					DimensionalQuantities.TypeSystem.TerminalHex1, DimensionalQuantities.TypeSystem.TerminalHex0,
					DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0,
					DimensionalQuantities.TypeSystem.TerminalHex0, DimensionalQuantities.TypeSystem.TerminalHex0,
					DimensionalQuantities.TypeSystem.TerminalHex0>>;
				class Example
				{
					Quantity<Dimension<TerminalHex1, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0>> value;
				}
				""";
			using AdhocWorkspace workspace = new();
			Project project = workspace.AddProject("Consumer", LanguageNames.CSharp)
				.WithParseOptions(new CSharpParseOptions(LanguageVersion.Preview))
				.WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
				.AddMetadataReferences(((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
					.Where(path => !Path.GetFileName(path).StartsWith("DimensionalQuantities", StringComparison.Ordinal))
					.Select(path => MetadataReference.CreateFromFile(path)))
				.AddMetadataReference(MetadataReference.CreateFromFile(Path.Combine(extracted, "lib/netstandard2.0/DimensionalQuantities.dll")));
			Document document = project.AddDocument("Consumer.cs", SourceText.From(source));
			Compilation compilation = (await document.Project.GetCompilationAsync(TestContext.Current.CancellationToken))!;
			Diagnostic[] setupErrors = [.. compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)];
			if (setupErrors.Length > 0)
			{
				throw new InvalidOperationException("Invalid code-fix fixture:" + Environment.NewLine + string.Join(Environment.NewLine, setupErrors.Select(diagnostic => diagnostic.ToString())));
			}
			SyntaxNode root = (await document.GetSyntaxRootAsync(TestContext.Current.CancellationToken))!;
			GenericNameSyntax quantity = root.DescendantNodes().OfType<FieldDeclarationSyntax>().Single().Declaration.Type.DescendantNodesAndSelf().OfType<GenericNameSyntax>().First();
			Diagnostic diagnostic = Diagnostic.Create(new DiagnosticDescriptor("DQ0003", "Alias", "Use alias", "Usage", DiagnosticSeverity.Info, true),
				quantity.GetLocation(), ImmutableDictionary<string, string?>.Empty.Add("AliasName", "Length"));
			List<CodeAction> actions = [];
			await provider.RegisterCodeFixesAsync(new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), TestContext.Current.CancellationToken));
			CodeAction action = Assert.Single(actions);
			ApplyChangesOperation operation = Assert.Single((await action.GetOperationsAsync(TestContext.Current.CancellationToken)).OfType<ApplyChangesOperation>());
			string updated = (await operation.ChangedSolution.GetDocument(document.Id)!.GetTextAsync(TestContext.Current.CancellationToken)).ToString();
			Compilation updatedCompilation = (await operation.ChangedSolution.GetDocument(document.Id)!.Project.GetCompilationAsync(TestContext.Current.CancellationToken))!;
			Assert.Multiple(
				() => Assert.Equal("Use Length", action.Title),
				() => Assert.Contains("""
					class Example
					{
						Length value;
					}
					""", updated),
				() => Assert.Empty(updatedCompilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(d => d.Severity == DiagnosticSeverity.Error)));
		}
		finally { loadContext.Unload(); }
	}
}
