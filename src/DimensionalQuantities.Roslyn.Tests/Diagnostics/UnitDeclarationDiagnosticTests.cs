using DimensionalQuantities.Roslyn.Tests.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace DimensionalQuantities.Roslyn.Tests.Diagnostics;

public sealed class UnitDeclarationDiagnosticTests
{
	private const string Imports = """
		using DimensionalQuantities.TypeSystem;
		using DimensionalQuantities.Units.Declarations;

		""";
	private const string LengthType = "Quantity<Dimension<TerminalHex1, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0, TerminalHex0>>";
	private const string InvalidFactorMessage = "A unit factor has an invalid power, prefix, symbol, or scale.";

	[Theory]
	[InlineData("", "", "A unit factor must reference a type marked UnitSymbol.")]
	[InlineData("""
		[UnitSymbol("", "Steps", "s")]
		""", "", InvalidFactorMessage)]
	[InlineData("""
		[UnitSymbol("Step", "", "s")]
		""", "", InvalidFactorMessage)]
	[InlineData("""
		[UnitSymbol("Step", "Steps", "")]
		""", "", InvalidFactorMessage)]
	[InlineData("""
		[UnitSymbol("Step", "Steps", "s", Scale = 0)]
		""", "", InvalidFactorMessage)]
	[InlineData("""
		[UnitSymbol("Step", "Steps", "s", Scale = -1)]
		""", "", InvalidFactorMessage)]
	[InlineData("""
		[UnitSymbol("Step", "Steps", "s", Scale = double.NaN)]
		""", "", InvalidFactorMessage)]
	[InlineData("""
		[UnitSymbol("Step", "Steps", "s", Scale = double.PositiveInfinity)]
		""", "", InvalidFactorMessage)]
	[InlineData("""
		[UnitSymbol("Step", "Steps", "s") ]
		""", ", Power = 0", InvalidFactorMessage)]
	[InlineData("""
		[UnitSymbol("Step", "Steps", "s") ]
		""", ", PowerDenominator = 0", InvalidFactorMessage)]
	[InlineData("""
		[UnitSymbol("Step", "Steps", "s") ]
		""", ", PowerDenominator = -2", InvalidFactorMessage)]
	[InlineData("""
		[UnitSymbol("Step", "Steps", "s") ]
		""", ", PrefixExponent = 4", InvalidFactorMessage)]
	[InlineData("""
		[UnitSymbol("Step", "Steps", "s", SiBasePrefixExponent = 4)]
		""", "", InvalidFactorMessage)]
	public async Task InvalidFactorReportsAtFactorAndDoesNotInventConversion(string symbol, string factorOptions, string message)
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
				{{Imports}}
				{{symbol}}
				class Step
				{
				}
				[UnitProvider]
				public class Routes
				{
					[QuantityUnit(typeof({{LengthType}}))]
					[{|#0:UnitFactor(typeof(Step){{factorOptions}})|}]
					public class RouteUnit
					{
					}
				}
				"""
		};
		test.ExpectedDiagnostics.Add(Invalid(message));
		test.DiagnosticVerifier = (actual, expected, verifier) => Assert.Multiple(() => Assert.Equal("DimensionalQuantities", actual.Descriptor.Category), () => Assert.True(actual.Descriptor.IsEnabledByDefault));
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.DoesNotContain(Methods(test), method => method.Identifier.ValueText == "FromSteps");
	}

	[Theory]
	[InlineData(", Power = 2", "0")]
	[InlineData(", Power = -1", "0")]
	[InlineData(", PowerDenominator = 2", "0")]
	[InlineData(", PrefixExponent = 3", "0")]
	[InlineData("", "double.NaN")]
	[InlineData("", "double.NegativeInfinity")]
	public async Task InvalidAffineFactorReportsAtFactor(string options, string offset)
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
				{{Imports}}
				[UnitSymbol("Step", "Steps", "s")]
				[AffineUnit({{offset}})]
				class Step
				{
				}
				[UnitProvider]
				public class Routes
				{
					[QuantityUnit(typeof({{LengthType}}))]
					[{|#0:UnitFactor(typeof(Step){{options}})|}]
					public class RouteUnit
					{
					}
				}
				"""
		};
		test.ExpectedDiagnostics.Add(Invalid("An affine unit must have a finite offset and be an unprefixed factor with power one."));
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.DoesNotContain(Methods(test), method => method.Identifier.ValueText == "FromSteps");
	}

	[Fact]
	public async Task MultipleAffineFactorsReportAtDeclarationAssociation()
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
				{{Imports}}
				[UnitSymbol("Step", "Steps", "s")]
				[AffineUnit(2)]
				class Step
				{
				}
				[UnitProvider]
				public class Routes
				{
					[{|#0:QuantityUnit(typeof({{LengthType}}))|}]
					[UnitFactor(typeof(Step))]
					[UnitFactor(typeof(Step))]
					public class RouteUnit
					{
					}
				}
				"""
		};
		test.ExpectedDiagnostics.Add(Invalid("An affine unit must be a single, unprefixed factor with power one."));
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.DoesNotContain(Methods(test), method => method.Identifier.ValueText == "FromSteps");
	}

	[Fact]
	public async Task MissingFactorReportsAssociationAndIndependentValidProviderStillGenerates()
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
				{{Imports}}
				[UnitProvider]
				public class Empty
				{
					[{|#0:QuantityUnit(typeof({{LengthType}}))|}]
					public class EmptyUnit
					{
					}
				}
				{{ValidProvider}}
				"""
		};
		test.ExpectedDiagnostics.Add(Invalid("EmptyUnit must contain at least one unit factor."));
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.Contains(Methods(test), method => method.Identifier.ValueText == "FromStrides");
	}

	[Fact]
	public async Task EmptyPublicConversionNameReportsAssociation()
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
				{{Imports}}
				[UnitSymbol("Step", "Steps", "s")]
				class Step
				{
				}
				[UnitProvider]
				public class Routes
				{
					[{|#0:QuantityUnit(typeof({{LengthType}}))|}]
					[UnitFactor(typeof(Step))]
					public class Unit
					{
					}
				}
				{{ValidProvider}}
				"""
		};
		test.ExpectedDiagnostics.Add(Invalid("A quantity unit declaration must have a name."));
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.Contains(Methods(test), method => method.Identifier.ValueText == "FromStrides");
	}

	[Fact]
	public async Task PositiveFiniteScaleWithNegativeRationalPowerIsAccepted()
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
				{{Imports}}
				[UnitSymbol("Step", "Steps", "s", Scale = 2)]
				class Step
				{
				}
				[UnitProvider]
				public class Routes
				{
					[QuantityUnit(typeof({{LengthType}}))]
					[UnitFactor(typeof(Step), Power = -1, PowerDenominator = 2)]
					public class RouteUnit
					{
					}
				}
				"""
		};
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.Contains(Methods(test), method => method.Identifier.ValueText == "FromInverseSquareRootStep");
	}

	[Fact]
	public async Task InvalidPublicMethodIdentifierReportsProviderAndLeavesIndependentProviderUseful()
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
				{{Imports}}
				[UnitSymbol("bad-unit", "bad-units", "b")]
				class BadSymbol
				{
				}
				[UnitProvider]
				public class {|#0:Broken|}
				{
					[QuantityUnit(typeof({{LengthType}}))]
					[UnitFactor(typeof(BadSymbol))]
					public class BrokenUnit
					{
					}
				}
				{{ValidProvider}}
				"""
		};
		test.ExpectedDiagnostics.Add(Invalid("Unit provider for Broken produces an invalid C# identifier."));
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.Contains(Methods(test), method => method.Identifier.ValueText == "FromStrides");
	}

	[Fact]
	public async Task ConflictingConversionSemanticsReportProvider()
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
				{{Imports}}
				[UnitSymbol("Step", "Steps", "s", Scale = 2)]
				class DoubleStep
				{
				}
				[UnitSymbol("Step", "Steps", "s", Scale = 3)]
				class TripleStep
				{
				}
				[UnitProvider]
				public class {|#0:Broken|}
				{
					[QuantityUnit(typeof({{LengthType}}))]
					[UnitFactor(typeof(DoubleStep))]
					public class FirstUnit
					{
					}
					[QuantityUnit(typeof({{LengthType}}))]
					[UnitFactor(typeof(TripleStep))]
					public class SecondUnit
					{
					}
				}
				{{ValidProvider}}
				"""
		};
		test.ExpectedDiagnostics.Add(Invalid("Unit Steps has conflicting SI conversions."));
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.Contains(Methods(test), method => method.Identifier.ValueText == "FromStrides");
	}

	[Fact]
	public async Task AffineBasePrefixIsRejected()
	{
		QuantityGeneratorTest test = new()
		{
			TestCode = $$"""
				{{Imports}}
				[UnitSymbol("Step", "Steps", "s", SiBasePrefixExponent = 3)]
				[AffineUnit(2)]
				class Step
				{
				}
				[UnitProvider]
				public class Routes
				{
					[QuantityUnit(typeof({{LengthType}}))]
					[{|#0:UnitFactor(typeof(Step))|}]
					public class RouteUnit
					{
					}
				}
				{{ValidProvider}}
				"""
		};
		test.ExpectedDiagnostics.Add(Invalid("An affine unit must have a finite offset and be an unprefixed factor with power one."));
		await test.RunAsync(TestContext.Current.CancellationToken);
		Assert.Contains(Methods(test), method => method.Identifier.ValueText == "FromStrides");
	}

	private static readonly string ValidProvider = $$"""
		[UnitSymbol("Stride", "Strides", "str")]
		class Stride
		{
		}
		[UnitProvider]
		public class Walks
		{
			[QuantityUnit(typeof({{LengthType}}))]
			[UnitFactor(typeof(Stride))]
			public class WalkUnit
			{
			}
		}
		""";
	private static IEnumerable<MethodDeclarationSyntax> Methods(QuantityGeneratorTest test) => test.GeneratedSources.SelectMany(tree => tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<MethodDeclarationSyntax>());
	private static DiagnosticResult Invalid(string message) => new DiagnosticResult("DQ0008", DiagnosticSeverity.Error).WithLocation(0).WithArguments(message);
}
