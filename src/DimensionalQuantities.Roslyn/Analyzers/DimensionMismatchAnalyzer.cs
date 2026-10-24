using System.Collections.Immutable;
using DimensionalQuantities.Roslyn.Discovery;
using DimensionalQuantities.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DimensionalQuantities.Roslyn.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DimensionMismatchAnalyzer : DiagnosticAnalyzer
{
	internal static readonly DiagnosticDescriptor IncompatibleOperands = new(
		"DQ0001", "Incompatible quantity dimensions", "Cannot apply '{0}' to {1} and {2}",
		"DimensionalQuantities", DiagnosticSeverity.Error, isEnabledByDefault: true);

	internal static readonly DiagnosticDescriptor WrongDestination = new(
		"DQ0002", "Wrong quantity dimension", "Expected {0}, but expression has {1}",
		"DimensionalQuantities", DiagnosticSeverity.Error, isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [IncompatibleOperands, WrongDestination];

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationStartAction(start =>
		{
			if (start.Compilation.GetTypeByMetadataName(typeof(Quantity<>).FullName!) is null)
			{
				return;
			}

			QuantityTypeReader reader = new(start.Compilation);
			start.RegisterSyntaxNodeAction(node => BinaryDimensionAnalysis.Analyze(node, reader),
				SyntaxKind.AddExpression, SyntaxKind.SubtractExpression,
				SyntaxKind.LessThanExpression, SyntaxKind.GreaterThanExpression,
				SyntaxKind.LessThanOrEqualExpression, SyntaxKind.GreaterThanOrEqualExpression,
				SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression);
			start.RegisterSyntaxNodeAction(node => BinaryDimensionAnalysis.AnalyzeCompoundAssignment(node, reader),
				SyntaxKind.AddAssignmentExpression, SyntaxKind.SubtractAssignmentExpression);
			DestinationDimensionAnalysis.Register(start, reader);
		});
	}
}
