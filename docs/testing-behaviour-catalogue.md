# DimensionalQuantities testing behaviour catalogue

This catalogue defines the behaviours to cover when redesigning the compiler-tooling tests and their consumer integration tests. It covers generated quantity APIs, discovery, dimensional operations, diagnostics, alias fixes, unit providers and delivered packages. Runtime contracts used by those APIs are included. Standalone internal numerical utilities are outside the proposed rewrite unless they are needed to establish one of these behaviours.

The requirements come from current production code and the README at `881f0fcebfe0d205e26bb672ba71b865d0cbd87d`. Existing test names, assertions, helpers and test history were not used as requirements. The examples are proposed inputs and expected outcomes, not results of new executions. The previous Roslyn and consumer integration test suites have been removed for a fresh implementation from this catalogue. Runtime and extension unit-test suites remain outside that replacement. The fresh author must use this catalogue and production code, without consulting the deleted tests, their helpers or their Git history.

This is a catalogue of existing behaviour, not a product feature review. It does not authorise new functionality, broader syntax support or additional defensive machinery. A current implementation detail is not automatically a requirement. The review-decision section records accepted behaviour and excludes speculative extensions; only concrete failures of supported contracts warrant separate defect reports.

## Test allocation

There are two destinations. Both are run by the ordinary test runner; the distinction is whether a test needs an extra temporary consumer project.

| Destination | What belongs here |
| --- | --- |
| Normal testing framework | Microsoft.CodeAnalysis.Testing for generated source, diagnostics, fixes, compilation and semantic binding. Ordinary xUnit tests for directly callable public runtime code, real package archive/symbol inspection and loading the delivered fix provider into a workspace. Referenced assemblies/multiple compiler projects remain framework fixtures; they do not by themselves require a standalone project. |
| Ephemeral project | An isolated temporary consumer project for the representative generated-code executions below or for proving real PackageReference activation/dependencies. Invalid-build cases stop at the expected build failure. Package archive/symbol inspection and workspace loading remain normal tests against actual packaged assets. |

An ID ending in `-F` is a normal-framework check. An ID ending in `-E` needs an ephemeral project. Both refer to the original catalogue ID below. Mixed entries are split by observable outcome: for example, G09-F checks multiplication result dimensions/signatures, while G09-E checks the resulting numerical value. These are distinct assertions, not a requirement to duplicate the full scenario in both suites. Shared numerical rules have one owner; another behaviour that uses the same rule does not acquire a second execution requirement. C# lifting is checked through compiler semantics rather than a separate nullable execution matrix.

Executing an ordinary public runtime method does not require an ephemeral project. This applies to R01-R03, U15, the direct AsDelta portion of U14 and the handwritten RPM helper U22. Runtime-only portions of U12 can also use normal tests. When a generated body needs a numerical execution check, execute it through an ephemeral project. Not every generated body or source variation needs such a check; framework output/semantic checks and direct tests of already-compiled shared code cover the other cases. A public method already compiled into a shipped extension assembly can be called directly in a normal test, even if a generator originally produced it. Shipped Metric, Imperial, Nautical and CGS conversion values therefore stay in normal tests; their package delivery is covered by P01-P04.

Package checks must obtain their assets through the actual pack/restore route. P03-F/P04-F can inspect/load the resulting artifacts without another consumer project. An in-memory compiler test with hand-added source references cannot establish that a package delivers the correct assets. Conversely, multi-file or metadata binding alone stays in the normal framework. Preserve the agreed isolation: each case tests one behaviour, and inputs are not grouped to reduce build time.

### Normal testing framework

| Check | Behaviour to verify |
| --- | --- |
| R01-F | Call the public SI factory/accessor directly; check stored magnitude, zero defaults and unchanged values. |
| R02-F | Call runtime arithmetic directly; check values and return types. Use compiler cases for incompatible types. |
| R03-F | Call comparisons, Abs/Min/Max/Clamp directly, including reversed-bound exceptions; compile incompatible-type controls. |
| G01-F | Check all aliases, canonical target types, synonym identity and availability without preceding demand. |
| G02-F | Check canonical type-name requests, generated aliases and rejected spellings in each required type position. |
| G03-F | Check factory-only discovery, inferred types, method availability and unrelated member-use controls. |
| G04-F | Check bound user names, explicit aliases and the accepted reservation policy without replacing their meaning. |
| G05-F | Check intermediate/result types and binding throughout each arithmetic chain; numerical operator/power rules are covered by the execution checks below. |
| G06-F | Check quantity propagation across each required C# binding boundary, including multiple source files. |
| G07-F | Use referenced-assembly/metadata fixtures to check discovered types, local helpers, identity and further binding. Package delivery is P01-E/P02-E. |
| G08-F | Check genuine library symbol recognition, exact encoded dimensions and absence of lookalike-generated operations. |
| G09-F | Check result dimensions, operand signatures, ordering and partial cancellation. |
| G10-F | Check cancellation returns double, subsequent scalar binding and no duplicate/dimensionless declarations; verify the emitted multiplication expression. |
| G11-F | Check scalar/inverse types and numeric conversion forms; verify the emitted left/right SI division and result construction. |
| G12-F | Verify selected operators, lifted operation semantics and nullable result types for each required nullable/nonnullable combination; no nullable-specific body is emitted. |
| G13-F | Check the selected user operator/method symbol and absence of generated competing operations; no execution of consumer-written method bodies is required. |
| G14-F | Check exact rational result dimensions, actual return types and zeroth-power double signatures. |
| G15-F | Check explicit Power aliases/encodings, actual closed receiver signatures and unrelated/unreadable controls. |
| G16-F | Check on-demand English aliases, canonical targets, cross-file scope and non-request controls. |
| G17-F | Check all ten predefined helpers, including their signatures and emitted delegation to the correct Power<T>.Of call, plus availability/rejection cases. |
| G18-F | Check discovered helper chains, emitted delegation, further result dimensions and conditional-access binding; shared power values are G14-E/G19-E. |
| G20-F | Check declaration/signature uniqueness and absence of ambiguity across repeated/equivalent requests. |
| G21-F | Check compiler/DQ diagnostics, ordinary incomplete-source tolerance and absence of fabricated output. |
| G22-F | Check incompatible assignments, returns, arguments, branches and element types remain compiler errors. |
| D01-F | Check diagnostic identity, category, severity, message arguments and complete expected sets. |
| D02-F | Check incompatible binary-operator diagnostics, exact spans and valid controls. |
| D03-F | Check compound-assignment diagnostics, exact spans and valid controls. |
| D04-F | Check diagnostic suppression for successfully bound user operators and accepted native binding. |
| D05-F | Check diagnostic quantity identity, nullable/metadata cases and unrelated/unreadable controls. |
| D06-F | Check assignment/initialiser diagnostics and supported valid/incomplete controls. |
| D07-F | Check diagnostics for the supported block and expression-bodied return destinations. |
| D08-F | Check supported argument mapping, constructor/method diagnostics and ambiguous/unknown controls. |
| D09-F | Check readable preferred/canonical dimension names in diagnostic messages and alias properties. |
| D10-F | Check incomplete/open source and library-free compilations do not crash or receive false encoding errors. |
| D11-F | Check recognised generated-code exclusion for analysers and ordinary-source controls. |
| D12-F | Check raw canonical quantity suggestions, properties and exact generic-name spans. |
| D13-F | Check using-alias/dimensionless suggestion exclusions without bypassing encoding validation. |
| D14-F | Check noncanonical encoding warnings, canonical controls and suggestion precedence. |
| D15-F | Check invalid closed encoding errors, precedence and ordinary Roslyn recovery cases. |
| D16-F | Check Power encoding diagnostics independently of invocation and unrelated-type controls. |
| D17-F | Check declared-name collision warnings, locations, resulting output and accepted alias policy. |
| D18-F | Check actual displaced versus unchanged bindings using metadata/source fixtures. |
| D19-F | Check invalid unresolved English requests, locations/messages and valid requests. |
| D20-F | Check bound aliases and non-request syntax do not receive false English-request diagnostics. |
| D21-F | Check alias-fix eligibility, action title/equivalence key and unsupported diagnostic IDs. |
| D22-F | Check existing speculative type-identity safeguards and safe/unsafe alias visibility cases. |
| D23-F | Check ordinary type-use replacement, wrappers, qualification and exterior trivia under the agreed best-effort policy. |
| D24-F | Check the actual nameof-operation exclusion and ordinary invocation controls. |
| D25-F | Check actual Fix All application across eligible/ineligible occurrences and documents, using alias-specific actions. |
| U01-F | Check generated public accessibility, namespace/import binding and unmarked-provider absence using compiler fixtures. |
| U02-F | Check associations, receiver types, invalid-target diagnostics and unassociated declaration controls. |
| U03-F | Check same-runtime-type association consolidation, available alternatives and no duplicate overloads. |
| U04-F | Check alternative method availability, receiver/association rules, expected conversion expressions and invalid references. |
| U05-F | Check public method names/signatures for ordered factors, irregular plurals, powers, roots and inverse forms. |
| U06-F | Check both options and their four combinations, defaults, affine exclusions and fixed-prefix declarations. |
| U07-F | Check named-prefix APIs, absent variants and expected factor-placement/scale expressions; call shipped conversions directly for their values. |
| U08-F | Check generic-prefix signatures/defaults; call already-compiled shipped conversion methods directly for their values. |
| U09-F | Check identical-name coalescing and conflicting-semantics DQ0008 diagnostics. |
| U10-F | Check partial-provider deduplication, ordinary container identity and no valid-association output controls. |
| U11-F | Check shipped numerical values directly and independently expected custom scale/power expressions; the common emitter does not require another custom-conversion execution test. |
| U12-F | Check helper spellings, expected generated mass-scale expressions and direct runtime baseline; call shipped mass conversions directly. |
| U13-F | Call shipped Celsius/Fahrenheit/Rankine conversions directly; inspect expected custom affine factory/accessor expressions. |
| U14-F | Call AsDelta and shipped delta accessors directly; inspect expected custom scale-only accessor expressions and compile type controls. |
| U15-F | Call the public runtime SI-component APIs directly using explicit quantity types; check prefixes, dimensions and overall scale. |
| U16-F | Check generated literal text/semantic constant values and successful compilation under required cultures; no separate consumer execution is needed. |
| U17-F | Check invalid-factor DQ0008 cases, locations and accepted factor-boundary controls. |
| U18-F | Check existing affine restrictions/diagnostics and independent valid declarations. |
| U19-F | Check missing expressions, invalid generated identifiers and existing recovery/output behaviour. |
| U20-F | Check core/optional API presence and absence with controlled references/imports; actual core-only delivery is P01-E. |
| U21-F | Call the compiled Metric APIs directly for numerical results; use compiler fixtures for signatures, shared receivers and namespace/prefix rules. |
| U22-F | Call handwritten FromRpm/ToRpm directly; check numerical values, sign, zero and actual API spelling. |
| U23-F | Call the compiled Imperial APIs directly against customary definitions; use compiler fixtures for names, namespace and absent-prefix rules. |
| U24-F | Call the compiled Nautical APIs directly for length/area/speed values; check shared types and absent-prefix rules. |
| U25-F | Call the compiled CGS APIs directly for scales and synonyms; check shared types, namespace and absent-prefix rules. |
| U26-F | Use producer/consumer metadata fixtures to check public methods, shared types and no provider regeneration; real package composition is P02-E. |
| P03-F | Inspect genuinely packed archives, dependency metadata and matching DLL/PDB/symbol assets using normal tests. Runtime closure uses P01-E/P02-E; no new consumer project is needed for archive assertions. |
| P04-F | Discover/load the delivered fix provider from real package assets in a compatible workspace and invoke an ordinary supported fix. This is a normal workspace/package-asset test, not another consumer project or a broader host matrix. |

### Ephemeral projects

Execution covers the distinct numerical rules below, not every source shape that requests them. Discovery paths, aliases, helper delegation and provider options stay in source/semantic framework checks. Shipped conversion values stay in direct normal tests. Package checks cover delivery separately. This narrows duplicated execution requirements; it does not combine unrelated behaviours into a shared test.

| Check | Behaviour to verify |
| --- | --- |
| G09-E | Execute representative generated multiplication and division calculations; dimension/operand-shape matrices stay in G09-F. |
| G14-E | Execute representative ordinary generated rational-power values; encoding/discovery variations stay in framework cases, and negative-base odd-root cases belong only to G19-E. |
| G19-E | Execute the distinct negative-base odd-denominator numerical cases and an even-root control; do not retest the full Math.Pow domain. |
| U08-E | Execute a representative generated parameterised compound-prefix formula; API/options/default-expression variations remain in framework cases. |
| P01-E | Restore only core; run a valid consumer and check an invalid build receives the packaged analyser diagnostic. Includes actual core-only API delivery from U20. |
| P02-E | Verify each optional package supplies matching core transitively and is usable by a consumer; use a representative cross-assembly composition for shared runtime identity. |

The engineering-alias table, power-helper list and conversion data are parameter data for these checks. Q01-Q14 are recorded scope decisions, not extra tests or a third destination. Apply them to the corresponding F/E cases.

A catalogue ID identifies a behaviour, not a mandated test method or count. Different semantic boundaries can require separate cases. Each case has isolated source and tests one behaviour. Combined inputs belong only where their interaction is the behaviour under test. There is no grouping or runtime-optimisation objective.

Compilation success alone does not prove the result has the correct type or value. A round trip alone does not prove a conversion has the correct physical scale. Generated text checks do not replace compiler-semantic checks of binding, nullable lifting or cross-assembly identity.

Full generated-source snapshots are not required. Verify the relevant declarations, expressions and binding. Formatting, generated container numbering and hint-file organisation are not public contracts. Diagnostic-only cases need no full source comparison; output behaviours still need their own relevant output assertions.

Tests must not use production emitters, dimension readers or conversion calculators to compute their expected answers. Internal helpers may be exercised as the subject of a test, but cannot also supply its oracle.

## Runtime contracts exposed through quantities

These contracts support the generated APIs. Existing runtime coverage can satisfy them; they are not a request to duplicate a separate runtime suite.

| ID | Behaviour and required cases | Production evidence |
| --- | --- | --- |
| R01 | A quantity stores a coherent SI magnitude. `Length.FromSiBaseUnit(2.5).AsSiBaseUnit()` is 2.5; default quantity values represent zero. Assigning through a same-dimension alias preserves the magnitude. Cover zero, negative and non-round finite values. | [runtime quantity][quantity]. |
| R02 | Same-dimension addition, subtraction and unary negation preserve the type and operate on SI magnitudes. Multiplication by a scalar in either order and division by a scalar preserve the dimension. Same-dimension division returns `double`. Aliases such as Energy/Torque participate as the same type. | [runtime operators][quantity-operators]. |
| R03 | Comparisons use SI magnitudes of the same dimension. `Abs`, `Min`, `Max` and `Clamp` return the appropriate same-dimension quantity. Clamp covers below, inside and above the inclusive bounds and throws `ArgumentException` for reversed bounds. Incompatible dimension arguments are rejected by the compiler; diagnostic wording belongs to D06-D08. | [runtime comparisons and helpers][quantity-helpers]. |

## Quantity discovery and generated operations

| ID | Behaviour and required cases | Production evidence |
| --- | --- | --- |
| G01 | Core consumers have all seven base aliases and the engineering aliases in the mapping below, without Metric or a preceding arithmetic expression. Canonical mechanical names and engineering synonyms for one dimension denote the same closed runtime type. For example, `LengthSquared`/Area and Energy/Torque are interchangeable; different dimensions are not. | [default discovery][defaults], [quantity planning][planning], [aliases][aliases]. |
| G02 | An unresolved canonical name in a type position requests that quantity. `LengthCurrent` in a parameter or return type and `SquareRootLength` in a declaration become usable aliases. Cover signatures, locals, generic arguments, arrays and nullable types. Reject noncanonical axis order, repeated axes and alternative spellings such as `CurrentLength`, `Length1` and `Root2Length`. | [name discovery][name-discovery], [canonical parser][name-parser]. |
| G03 | A recognised factory on an unresolved canonical name can request its type without a type declaration. `var x = LengthCurrent.FromSiBaseUnit(6)` and a valid `FromPrefixedUnits` call infer the proper quantity. An arbitrary unknown static member is not a factory request. | [discovery seeds][seeds]. |
| G04 | Bound user types, namespaces and aliases retain their meaning during name-based discovery. A user `LengthCurrent` or `using LengthCurrent = Example.Custom` is not replaced merely because its spelling is dimensional. Qualified user names are respected. Genuine arithmetic requests and collision diagnostics are a separate interaction; alias scope questions are Q01. | [name discovery][name-discovery], [alias reservations][reservations]. |
| G05 | Arithmetic chains discover the intermediates needed by later expressions. With current 3 A and resistance 4 SI, `i*i*r` binds through current-squared to Power and evaluates to 36. Cover parenthesised chains, repeated operations and mixed multiplication, division and powers. No particular discovery round count is required. | [discovery coordination][coordination], [operation discovery][bound-operations]. |
| G06 | Quantity types flow through ordinary C# binding. Cover explicit and inferred locals, compatible conditional/switch branches, fields/properties/helper returns and parameters, arrays/collections/spans/indexers, generic interface or constrained calls, out/ref results, patterns/deconstruction, awaited results and iterator/async-iterator elements. Use separate cases for distinct binding boundaries, including cross-file definitions. Incompatible branches remain errors; arbitrary reassignment-based type inference is not promised. | [documented discovery][readme-discovery], [bound operations][bound-operations], [discovery seeds][seeds]. |
| G07 | Referenced quantity values seed local aliases/conversions and further arithmetic. A producer returning a length-current quantity, directly or through a member/collection/containing object, can be consumed and multiplied by Time. Independent producers exchange the same runtime type. An unused quantity elsewhere in an assembly does not export all aliases. | [metadata discovery][metadata], [documented interoperability][readme-interoperability]. |
| G08 | Discovery recognises actual library symbols and all seven encoded exponents. Unrelated lookalike Quantity, Dimension, Power and number types do not acquire the library's operators or aliases. Unresolved/open dimensions are not guessed. Canonical negative and fractional exponents retain their exact dimension meaning. | [quantity reader][quantity-reader], [number reader][number-reader]. |
| G09 | Multiplication adds dimension exponents and multiplies SI magnitudes; division subtracts exponents and divides magnitudes. For length 6 and current 2, product is length-current 12 and ratio is length-per-current 3. Cover both written multiplication orders, reversed division, all axes and partial cancellation. Do not require unrequested reverse operators. | [arithmetic discovery][arithmetic-discovery], [arithmetic generation][arithmetic-emitter], [exact dimensions][dimensions]. |
| G10 | Complete dimensional cancellation returns `double`, including reciprocal multiplication and same-dimension division. A cancellation result remains usable as a scalar in subsequent arithmetic. There is no invented dimensionless quantity alias or duplicate generated same-dimension division operator. | [arithmetic generation][arithmetic-emitter], [runtime operators][quantity-operators]. |
| G11 | Scalar operations use normal C# numeric conversion rules. `2*l`, `l*2` and `l/2` preserve length; `1/Time.FromSiBaseUnit(0.02)` produces frequency 50. Cover standard implicit numeric conversions to double and nullable forms. Decimal is not silently accepted; a user conversion to double alone does not trigger generated scalar-inverse arithmetic. | [scalar recognition][arithmetic-discovery], [arithmetic generation][arithmetic-emitter]. |
| G12 | Nullable arithmetic lifts the dimensional operators. Non-null nullable length 2 and current 3 produce nullable length-current 6; null on either side produces null. Cover mixed nullable/nonnullable operands, nullable scalars, inverse division, cancellation to `double?` and chained lifted operations. | [nullable discovery][quantity-reader], [nullable contract][readme-nullable]. |
| G13 | Already bound user operations retain their selected implementation and result type. Existing multiply/divide operators and methods such as a consumer-defined `Squared()` must not be replaced by generated competitors. Their results can feed later quantity expressions when those results are genuine quantity types. | [arithmetic discovery][arithmetic-discovery], [power discovery][power-discovery], [helper discovery][helper-discovery]. |
| G14 | `Power<TExponent>.Of(quantity)` multiplies dimension exponents by an exact rational and computes the magnitude using double exponentiation. Square root of area 9 returns Length 3; power 2/3 of volume 8 returns Area approximately 4. Cover zero, one, negative, proper/improper rational exponents and quantities already containing rational/negative dimensions. Zeroth power returns `double` 1. | [power discovery][power-discovery], [power generation][power-emitter]. |
| G15 | Explicit exponent encodings and aliases to the actual Power type work. Terminal digits, hexadecimal composites, negatives and fractions specify powers; a type alias to `Power<TerminalHex2>` supports `.Of`. Preserve the actual closed exponent receiver type when equivalent values have different valid encodings. Unrelated Power APIs or undecodable custom marker types do not get guessed overloads. | [power discovery][power-discovery], [number reader][number-reader], [number encoding][number-encoding]. |
| G16 | Valid unresolved English exponent requests generate global aliases to canonical encodings. Cover integers, negative integers, named fractions and numerator/denominator forms such as NumberHalf, NumberTwoOverThree and NumberTwentyOne. Equivalent rational spellings share the canonical encoded type. The alias is usable across files. A standalone unresolved number name or a lookalike Power call is not sufficient demand. | [English request discovery][english-discovery], [English parser][english-parser], [alias generation][english-emitter]. |
| G17 | Base/catalogue quantities have the ten zero-argument power helpers listed below, without earlier demand. Each has the appropriate result dimension and numerical meaning. Misspellings, extra arguments and unrelated receivers do not receive invented helper overloads. | [default powers][defaults], [helper discovery][helper-discovery], [helper generation][helper-emitter]. |
| G18 | Named power/root calls on discovered quantities request their own further results. A length-current product can be squared and rooted, including after member, helper or metadata flow. Nullable receivers use conditional access, such as `x?.Squared()`, with null propagation. Direct nullable calls without conditional access are not promised equivalents. | [bound operations][bound-operations], [helper discovery][helper-discovery], [helper generation][helper-emitter]. |
| G19 | Rational powers of negative magnitudes preserve real odd-root results. For -8, exponent 1/3 gives -2, 2/3 gives approximately 4 and -1/3 gives approximately -0.5. An even reduced denominator follows Math.Pow and gives NaN for a negative finite base. Cover the distinct numerator/denominator parity and negative-exponent rules, with representative positive-zero/nonfinite controls. Reduction and encoding belong in framework cases; exhaustive Math.Pow overflow/underflow/input-domain checks are not required. Dimension algebra remains exact even when magnitude evaluation is approximate. Signed-zero preservation is outside scope under Q07. | [power numerical rule][power-emitter]. |
| G20 | Repetition and synonymous requests do not create duplicate or ambiguous declarations. Repeated operations across files, equivalent English exponents, catalogue synonyms and default/discovered helper interactions retain one usable signature per operation. Verify signatures/binding and meaningful output uniqueness, not source ordering or private class names. | [quantity planning][planning], [default powers][defaults], [power generation][power-emitter]. |
| G21 | Unsupported or incomplete expressions do not acquire fabricated result types or crash generation. Incompatible addition/comparison, nonquantity operands, unresolved overloads and malformed requests remain compiler failures. Independent valid quantity expressions in the same input can still be generated. Ordinary incomplete editor source is relevant; arbitrary resource-exhaustion guarantees are not. | [arithmetic discovery][arithmetic-discovery], [bound operations][bound-operations], [quantity reader][quantity-reader]. |
| G22 | Generation preserves dimensional type safety. A length-current result cannot be assigned to Length, passed as Time or returned as Area. Cover explicit intermediate annotations, incompatible branches, overload arguments and collection element types. Same-dimension aliases remain interchangeable. | [encoded quantity types][quantity-format], [quantity aliases][quantity-emitter]. |

### Quantity and power data

The seven base aliases are Length, Mass, Time, Current, Temperature, AmountOfSubstance and LuminousIntensity. Their coherent SI bases are metre, kilogram, second, ampere, kelvin, mole and candela. Named mass helpers still use grams with the appropriate scale correction.

In the engineering mapping, L is length, M mass, T time, I current and Theta temperature. Omitted axes have exponent zero. Each row also has the canonical mechanical alias dictated by the naming rules.

| Engineering aliases sharing one runtime type | Dimension |
| --- | --- |
| Area | `L^2` |
| Volume | `L^3` |
| Speed | `L T^-1` |
| Acceleration | `L T^-2` |
| Frequency, AngularVelocity | `T^-1` |
| Force | `L M T^-2` |
| Density | `L^-3 M` |
| Pressure, Stress, YoungsModulus | `L^-1 M T^-2` |
| Energy, Torque | `L^2 M T^-2` |
| Power | `L^2 M T^-3` |
| SpecificPower, SpecificPowerLoss | `L^2 T^-3` |
| TemperatureCoefficient | `Theta^-1` |
| PowerDensity, VolumetricPowerLoss | `L^-1 M T^-3` |
| ElectricCharge | `T I` |
| Voltage | `L^2 M T^-3 I^-1` |
| Capacitance | `L^-2 M^-1 T^4 I^2` |
| Resistance | `L^2 M T^-3 I^-2` |
| Inductance | `L^2 M T^-2 I^-2` |
| MagneticFlux, FluxLinkage | `L^2 M T^-2 I^-1` |
| MagneticFluxDensity, MagneticPolarisation, RemanentFluxDensity | `M T^-2 I^-1` |
| MagneticFieldStrength, CoerciveFieldStrength | `L^-1 I` |
| CurrentDensity | `L^-2 I` |
| Permeability | `L M T^-2 I^-2` |
| Resistivity | `L^3 M T^-3 I^-2` |
| Conductivity | `L^-3 M^-1 T^3 I^2` |
| MagneticReluctivity | `L^-1 M^-1 T^2 I^2` |
| MagneticVectorPotential | `L M T^-2 I^-1` |

The supported convenience powers are Squared=2, Cubed=3, Pow4=4, Pow5=5, Pow6=6, SquareRoot=1/2, CubeRoot=1/3, Root4=1/4, Root5=1/5 and Root6=1/6. These are case data for G17, not ten unrelated behaviour categories.

Canonical generated names order the seven axes consistently, use numerator/Per/denominator or Inverse forms, and express reduced rational powers using the documented root/power spelling. Canonical number encodings use least-significant hexadecimal digits first, a single sign and reduced fractions. Equivalent dimension calculations must yield the same closed runtime type; mechanically equal values represented by noncanonical user-written types are Q02.

## Diagnostics and alias fixes

### Diagnostic data

Default severity is the contract below. User configuration may override effective severity. DQ0001-DQ0005 come from analysers; DQ0006-DQ0008 come from generation. All use category `DimensionalQuantities` and are enabled by default.

| ID | Default severity | Message contract |
| --- | --- | --- |
| DQ0001 | Error | `Cannot apply '{operation}' to {leftDimension} and {rightDimension}` |
| DQ0002 | Error | `Expected {expectedDimension}, but expression has {actualDimension}` |
| DQ0003 | Info | `Use the readable alias '{alias}' for this quantity`; carries `AliasName` for the fix. |
| DQ0004 | Warning | `This encoding is noncanonical; equivalent values have distinct type identity` |
| DQ0005 | Error | `This closed type-system encoding is invalid` |
| DQ0006 | Warning | `Generated alias '{alias}' conflicts with a name in this source` |
| DQ0007 | Error | `English exponent '{name}' cannot be resolved` |
| DQ0008 | Error | Concrete unit-declaration/conversion failure. Its exact prose is not a separate physical behaviour requirement. |

Evidence: [mismatch descriptors][mismatch], [encoding descriptors][encoding], [generator descriptors][generator-diagnostics]. Compiler errors and DQ diagnostics are distinct observable results. Cases with deliberate C# errors must not suppress unexpected DQ diagnostics or accidentally claim successful compilation.

| ID | Behaviour and required cases | Production evidence |
| --- | --- | --- |
| D01 | Each diagnostic has the correct identity, category, default severity and relevant message arguments. One complete expected set is checked for an ordinary triggering case, including absence of extra DQ diagnostics. Do not assume a universal diagnostic ordering. | descriptors above. |
| D02 | Unbound incompatible `+`, `-`, `<`, `>`, `<=`, `>=`, `==` and `!=` expressions produce DQ0001 over the whole expression, preserving operator spelling and operand order in the message. Matching dimensions and unrelated built-in operands do not. Multiplication/division combine dimensions instead of invoking this equality rule. | [binary analysis][binary-analysis]. |
| D03 | Unbound incompatible `+=` and `-=` produce DQ0001 over the assignment with the compound operator spelling. Matching dimensions are clear. Multiplicative assignments are not reinterpreted as requiring equal dimensions. | [binary analysis][binary-analysis]. |
| D04 | A successfully bound user operator suppresses a dimensional-mismatch claim, including an explicitly supported cross-dimension operator. Merely having unresolved/ambiguous candidates does not prove successful binding. Pair with G13 where generation could introduce a competitor. Valid native binding/conversion controls must remain accepted. | [binary analysis][binary-analysis]. |
| D05 | Mismatch diagnostics use genuine library quantity identity, including nullable quantities, aliases and metadata-origin values. Lookalike types, wrappers, collections, Delta wrappers, unreadable dimensions and error types are not guessed as direct quantities. Mathematically equal readable dimensions do not receive a false mismatch solely because their generic encoding differs; DQ0004 addresses noncanonical identity. | [quantity reader][quantity-reader], [destination analysis][destination-analysis]. |
| D06 | Incompatible assignment or initialisation produces DQ0002 at the source expression. Cover locals, fields, properties, ordinary assignments and independent initialisers in a multi-variable declaration. Inferred var, matching dimensions, nonquantity destinations and missing types do not produce a guessed mismatch. Valid same-dimension nullable/implicit assignments remain accepted; tests need not fabricate a cross-dimension conversion to cover a private guard. | [destination analysis][destination-analysis]. |
| D07 | Incompatible declared return destinations produce DQ0002 at the returned expression. Cover method/local-function/getter blocks and expression-bodied methods/properties/indexers. Matching returns and `return;` are clear. Additional destination contexts remain outside the required scope under Q05. | [destination analysis][destination-analysis]. |
| D08 | Incompatible method, delegate or constructor arguments produce DQ0002 at the offending expression. Cover positional and reordered named arguments, explicit and target-typed construction, and a single recoverable candidate method. Multiple ambiguous candidates or an unknown named argument must not yield guessed expected dimensions. Preserve the current argument-mapping coverage under Q05; no expanded-params support is required. | [argument analysis][destination-analysis]. |
| D09 | Dimension display names are stable and readable: preferred engineering alias when defined, canonical mechanical name otherwise, and Dimensionless for zero dimensions. Messages and `AliasName` use the same preferred name. | [display names][display-names], [engineering mapping][aliases]. |
| D10 | Ordinary incomplete editor code and open/unresolved generic arguments do not crash analysers or produce a false invalid-closed-encoding error. With the library absent, an unrelated compilation remains unaffected. Do not add counterfeit partial-library or resource-exhaustion obligations without a separate requirement. | [analyser setup and encoding guards][encoding], [mismatch setup][mismatch]. |
| D11 | DQ0001-DQ0005 are not reported on source Roslyn recognises as generated; equivalent ordinary user source is analysed. Verify actual generated-code classification rather than inventing a filename-only rule. Current generator diagnostic scope is retained under Q13. | [analyser registration][mismatch], [encoding registration][encoding]. |
| D12 | A canonical non-dimensionless raw library `Quantity<Dimension<...>>` produces DQ0003 at the Quantity generic itself, including qualified names and occurrences inside containers. The message and `AliasName` identify the readable alias. A suggestion can exist even when the fix is unsafe or unavailable. | [encoding analysis][encoding]. |
| D13 | An intentional using-alias declaration and a canonical dimensionless raw quantity do not receive DQ0003. Validation still reports invalid or noncanonical encodings inside alias declarations. Power types are not quantity-alias suggestions. | [encoding analysis][encoding]. |
| D14 | Valid noncanonical encodings produce DQ0004 at their enclosing Quantity or Power generic. Cover unreduced fractions, denominator-one fractions and zero fractions with valid generic constraints. Canonical equivalents do not warn. A noncanonical quantity gets no DQ0003 for that generic. | [canonicality inspection][canonicality], [encoding analysis][encoding], [number contract][number-encoding]. |
| D15 | Closed invalid quantity/power encodings produce DQ0005, not a readable-alias suggestion. Cover a non-dimension type, unsupported closed number-marker type, and malformed built-in encodings where Roslyn retains the bound generic. Invalid exponents take precedence over noncanonical exponents within one quantity. Do not demand separate analyser diagnostics for standalone Hex/Fraction/Dimension types. | [encoding analysis][encoding], [number reader][number-reader]. |
| D16 | Canonical Power types are accepted without an Of invocation; noncanonical/invalid bound Power types diagnose even in declarations. An unrelated consumer Power type is outside these rules. | [encoding analysis][encoding]. |
| D17 | A generated-name conflict with an explicit alias or nongeneric global type/delegate produces DQ0006 at the declared identifier. The warning does not suppress or rename a genuinely required generated alias. Generic and namespace-contained names are not automatically global declaration collisions. The accepted scope policy is Q01. | [collision diagnostics][collisions]. |
| D18 | A generated alias displacing an imported type produces DQ0006 at the affected unqualified use, including static member access where the receiver was a type. Qualified names, value identifiers, already-unresolved names and genuinely unchanged bindings do not warn. Establish the semantic before/after meaning, not just a matching spelling. | [displaced-use analysis][collisions]. |
| D19 | An invalid unresolved English exponent in a recognised real Power.Of request produces DQ0007 at that exponent identifier. Valid requests proceed without DQ0007. Cover malformed words and a zero denominator; message includes the requested spelling. | [English diagnostics][generator-diagnostics], [request recognition][english-discovery]. |
| D20 | Bound exponent names and actual aliases are not invalid unresolved requests. A lookalike Power API, wrong method/arity, non-Number prefix, qualified/generic exponent spelling or standalone unresolved Power type does not gain a DQ0007 diagnosis merely by resemblance. Closed bound invalid encodings remain DQ0005's domain. | [English request recognition][english-discovery]. |
| D21 | The alias fix applies to DQ0003 and uses its proposed alias. A genuine safe Length replacement offers `Use Length` with equivalence key `UseAlias:Length`. Other diagnostic families have no fix from this provider. | [fix provider][fix]. |
| D22 | A fix is offered only when the proposed alias resolves at the use site to the same quantity type. Missing, inaccessible, ambiguous or differently shadowed aliases withhold it. A visible same-type alias can be valid. The best-effort expression-context boundary is resolved in Q04; interior-comment handling is resolved in Q03. | [speculative binding][fix]. |
| D23 | In ordinary type-use contexts, the fix replaces the complete qualified quantity name while preserving nullable/array wrappers, containing generic types, declarations and surrounding syntax. Leading/trailing trivia survives. Check representative declarations and intentional text preservation. Expression-context replacements remain best-effort under Q04; there is no general post-fix binding/compilation guarantee. | [replacement bounds and edit][fix]. |
| D24 | The fix is withheld inside a real C# nameof operation, including nested occurrences, because the returned string could change. A user invocation merely spelled nameof is distinguished by actual operation binding. DQ0003 itself can remain present. | [nameof guard][fix]. |
| D25 | Fix All uses the selected alias-specific action consistently across multiple eligible occurrences/documents, preserving ineligible and nameof sites. A Length action does not silently select unrelated Time actions. Use the provider's actual BatchFixer; do not invent custom aggregation or scope guarantees beyond it. | [provider and equivalence key][fix]. |

## Unit providers and conversions

| ID | Behaviour and required cases | Production evidence |
| --- | --- | --- |
| U01 | A real UnitProvider publishes public factory/accessor extensions in its namespace, even when its ordinary provider and symbol declarations are internal/private. A separate consumer imports the namespace and calls the methods without accessing declaration types. Unmarked declarations do not publish conversions. Global namespace and ordinary nongeneric nesting are relevant boundaries. | [provider discovery/generation][providers], [conversion surface][conversion-emitter], [extension contract][readme-extension]. |
| U02 | Only valid associated quantity declarations create receiver conversion sets. Multiple associations can target distinct valid closed quantities. Unassociated factor/symbol declarations supply data rather than independent methods. Invalid or unrelated quantity targets produce DQ0008. No expansion to deeper/generic declaration shapes is required under Q08. | [association reading][provider-reader], [quantity identity][quantity-reader]. |
| U03 | Associations that name the same runtime quantity type consolidate into one callable surface. Pressure/Stress/YoungsModulus share Pascal methods; Energy/Torque share their declared alternatives. Repeated identical associations do not create ambiguous duplicates. Distinct noncanonical runtime types are not merged merely by numeric dimension. | [association consolidation][provider-reader], [conversion collection][conversion-emitter]. |
| U04 | AdditionalUnit contributes a valid alternative expression to the owning receiver; it need not have its own association. A Feet receiver with an Inch alternative provides both methods, with 12 inches equal to one foot. An alternative outside the provider or with invalid factors diagnoses. Recursive alternatives are not a new requirement under Q08. | [alternative reading][provider-reader], [alternative generation][conversion-emitter]. |
| U05 | Public conversion names express ordered factors and powers rather than declaration-class spelling. Cover irregular Feet, square/cubic and rational powers, mixed numerator/Per/denominator and inverse-only forms. Amp times metre^-2 yields AmpsPerSquareMetre. Invalid generated identifiers produce DQ0008. | [unit naming][unit-expression], [method construction][conversion-emitter]. |
| U06 | GenerateSiPrefixes and GeneratePrefixConversions default off and are independently selectable. Their four combinations expose the appropriate named and parameterised APIs. Affine expressions do not get automatic prefix families; explicitly declared fixed prefixes remain meaningful when automatic generation is off. | [provider options][provider-options], [conversion selection][conversion-emitter]. |
| U07 | Named prefixes cover the supported finite SI set, plus the unprefixed form. Automatic compound prefix variation applies to the first positive factor; other factors retain their declared prefixes. A reciprocal-only expression does not gain invented numerator-prefix variants. Explicit alternative units can supply separately prefixed denominator spellings. | [prefix data][prefixes], [prefix expansion][conversion-emitter], [factor naming][unit-expression]. |
| U08 | Parameterised prefixes accept decimal exponents for each declared unit factor, weighted by that factor's power and SI base-prefix adjustment. Voltage at -3 means millivolts; metre at -3 in a denominator squared means a factor of 10^6. Runtime prefix arguments need not belong to the finite named-prefix set. Declared defaults, reverse conversion and delta accessors agree. Retain first-expression selection under Q10. | [generic method construction][conversion-emitter], [unit scaling][unit-expression]. |
| U09 | Identical named conversions for one receiver coalesce; the same generated name with different scale, decimal exponent or affine offset reports DQ0008 instead of choosing a definition silently. No additional cross-provider coordination is required under Q09. | [conflict detection][conversion-emitter]. |
| U10 | Partial providers are not emitted repeatedly. Providers with the same short name in ordinary different namespaces/containing types can coexist when their public method signatures do not conflict. A provider with no valid associated declarations publishes no callable conversion surface. Private generated class-name mangling is not a contract. | [provider identity][providers]. |
| U11 | Linear conversion applies the product of atomic scales and decimal-prefix factors, each raised to its signed rational power. Accessors apply the inverse. Cover 6 ft=1.8288 m, square-foot scale 0.3048^2, mile/hour scale 1609.344/3600, rational and reciprocal factors, and zero/negative readings. Assert both directions against independent numerical expectations. | [unit scaling][unit-expression], [conversion formulas][conversion-emitter]. |
| U12 | Mass prefixes account for kilogram as coherent SI mass. 1000 grams and one kilogram both store 1 SI; powers and denominator occurrences apply the same correction. Cover automatic KiloGrams versus explicitly declared Kilograms spellings and compound density or specific-power defaults. | [base prefix exponents][base-prefixes], [runtime prefix formula][quantity-prefixes]. |
| U13 | Absolute affine conversion applies `(reading + offsetBeforeScale) * scale` and reverses as `SI/scale - offset`. Celsius 20 and Fahrenheit 68 store 293.15 K. Celsius -273.15 and Fahrenheit -459.67 store zero. Rankine is linear. Cover non-unit scale, zero offset, negative readings and cross-package accessors. No absolute-zero input rejection is promised. | [affine formulas][conversion-emitter], [temperature contract][readme-temperature]. |
| U14 | AsDelta preserves the stored SI magnitude and changes its interpretation, without tracking subtraction provenance. Delta accessors omit affine offsets. A 10 K difference is 10 Celsius and 18 Fahrenheit/Rankine; an absolute Celsius-20 reading reinterpreted as a delta is 293.15 Celsius. Cover negative/zero/default deltas, prefixes and wrong-dimension accessors. There is no implied delta factory. | [delta runtime][delta], [delta accessors][conversion-emitter], [documented provenance][readme-temperature]. |
| U15 | Runtime SI and arbitrary-component factories/accessors are available without optional conversions or generated aliases. Each chosen base-component prefix is weighted by its dimension exponent; irrelevant components have zero effect; overallScaleExponent is additive. For current density, a -3 length prefix gives 10^6 SI. Defaults are coherent SI, including kilogram. | [runtime prefix API/formula][quantity-prefixes]. |
| U16 | Generated numerical literals are culture independent and preserve the supplied finite double constants. A comma-decimal generator culture must still produce valid C# with the correct scale/offset. Cover small/large finite constants and rational exponent expressions. No aggregate-overflow guarantee is added under Q11. | [literal formatting][conversion-emitter]. |
| U17 | Invalid factors report DQ0008 at useful source locations, without fabricated conversion semantics. Cover missing real UnitSymbol, empty symbol spellings, nonpositive/nonfinite scale, zero power, nonpositive denominator, and unsupported declared named prefixes/base prefixes. A positive finite scale, negative numerator power and positive denominator remain valid. Ordinary compiler attribute errors are separate. | [factor validation][provider-reader], [symbol attributes][symbol-attribute], [factor attributes][factor-attribute]. |
| U18 | Affine declarations require a finite offset and one unprefixed factor of power one. Affine powers, inverses, prefixed factors, multiple factors and nonfinite offsets diagnose with DQ0008. A valid independent declaration remains useful. Additional affine encoding forms are not required under Q08. | [affine validation][provider-reader]. |
| U19 | Missing factor expressions, unusable public method names and conflicting conversions fail through DQ0008 rather than a generator exception or invented defaults. An independent valid provider can still generate. Reader-level and emission-level errors need useful locations; arbitrary diagnostic ordering or every incidental partial-output choice is not a requirement. No special declaration-label handling is added under Q08. | [declaration validation][provider-reader], [emission diagnostics][providers]. |
| U20 | Core provides the seven base quantities' named SI helpers, all generic runtime SI APIs, aliases and arithmetic. Optional named catalogue methods require their package and namespace. Core alone must not supply FromVolts, FromCelsius, FromFeet, FromKnots or FromGauss. Existing engineering aliases do not thereby acquire every optional generic named-factor conversion. | [core planning][planning], [quantity emission][quantity-emitter], [package contract][readme-core]. |
| U21 | Metric supplies its documented mechanical, electrical and magnetic named conversions, prefixes and explicit compound alternatives on shared types. Cover volts*amps=watts, Pascal/Joule synonyms, 1 g/cm^3=1000 SI, 1 A/mm^2=10^6 SI and reciprocal-only units. Celsius is a separate affine family, without automatic prefixes. | [Metric declarations][metric], [Metric temperature][metric-temperature]. |
| U22 | Metric's handwritten angular helper uses its actual FromRpm/ToRpm API. 60 rpm stores 2*pi rad/s; pi rad/s converts to 30 rpm; zero and negative speeds preserve sign. Do not invent AsRpm, delta-RPM or prefixed-RPM APIs. | [RPM helper][rpm]. |
| U23 | Imperial uses distinct customary definitions on shared SI types, with no automatic SI-prefix family. Cover length/area/volume powers, speed/force/pressure, short versus long tons, US versus Imperial liquid volume, mechanical horsepower and IT BTU definitions. Fahrenheit/Rankine follow U13-U14. Representative numerical data is below. | [Imperial declarations][imperial], [Imperial symbols][imperial-symbols]. |
| U24 | Nautical converts nautical miles, fathoms, square nautical miles and knots into the same Length/Area/Speed types. One nautical mile is 1852 m and one knot is 1852/3600 m/s. Helpers require the namespace; automatic prefixed nautical methods are absent. | [Nautical declarations][nautical], [Nautical symbols][nautical-symbols]. |
| U25 | CgsEmag converts its named mechanical, ab-electrical and magnetic units into coherent SI types; it does not introduce a separate electromagnetic dimension system. Cover magnetic and electrical scales, synonyms such as Biot/Abampere, extreme scales and the absence of automatic prefixes. | [CGS declarations][cgs], [CGS symbols][cgs-symbols]. |
| U26 | Provider libraries publish compiled public conversion methods targeting shared runtime types. Consumers need the assembly/package and namespace, not private declaration sources or producer-local aliases. Metadata references do not regenerate the provider's declarations locally. Fresh consumer tooling can still generate its own aliases and further arithmetic. | [provider generation][providers], [documented extension consumption][readme-extension], G07. |

### Conversion data boundaries

Named SI prefixes span exponents -30 through 30 at the supported SI positions: +/-1, +/-2, +/-3, then multiples of three through +/-30. Zero is unprefixed. Case data includes the extreme Quetta/Ronna and Ronto/Quecto names, not arbitrary prefix-name invention. Parameterised runtime exponents follow U08 instead.

These independent numerical examples anchor the conversion families; round trips are additional evidence, not substitutes for these scale checks.

| Family | Representative expected values |
| --- | --- |
| Linear custom or customary | 6 feet = 1.8288 m; one square foot = 0.3048^2 m^2; one mile/hour = 1609.344/3600 m/s. |
| Custom rational factor | A symbol scale of 4 at power 1/2 gives factor 2; at power -1/2 gives factor 0.5. |
| Prefixes and mass | 1 gram = 0.001 kg; 1 gram/cm^3 = 1000 kg/m^3; 1 A/mm^2 = 1,000,000 A/m^2. |
| Absolute temperature | 20 Celsius = 68 Fahrenheit = 293.15 K; 491.67 Rankine = 273.15 K. |
| Temperature difference | 10 K = 10 Celsius difference = 18 Fahrenheit difference = 18 Rankine difference. |
| Customary liquid volumes | US gallon = 0.003785411784 m^3; Imperial gallon = 0.00454609 m^3. |
| Customary mass and pressure | Short ton = 907.18474 kg; long ton = 1016.0469088 kg; psi = 4.4482216152605 / 0.0254^2 Pa. |
| Energy | IT BTU = 1055.05585262 J; 10^7 ergs = 1 J. |
| Nautical | Nautical mile = 1852 m; fathom = 1.8288 m; knot = 1852/3600 m/s. |
| Magnetic/electrical CGS | 10,000 gauss = 1 T; oersted = 79.57747154594767 A/m; abampere or biot = 10 A; abvolt = 10^-8 V; abfarad = 10^9 F; maxwell = 10^-8 Wb. |

Catalogue membership and alias mappings are parameter data. Test each distinct conversion mechanism and preserve the independently specified definitions of shipped units. Do not make one giant consumer that happens to make every conversion compile, or derive all expected values by asking the production catalogue to calculate them.

## Package and assembly delivery

| ID | Behaviour and required cases | Production evidence |
| --- | --- | --- |
| P01 | A clean PackageReference to core delivers the runtime and activates quantity generation and analysers under a compatible compiler. A valid calculation compiles and runs; an invalid dimensional expression receives its DQ diagnostic. No repository ProjectReference, manual analyser wiring, inherited repository props or stale generated files are allowed to supply the result. | [core package layout][core-project], [consumer requirement][readme-core]. |
| P02 | Metric, Imperial, Nautical and CgsEmag each supply the exact matching core version transitively. Each can be the consumer's sole direct package reference; several compatible extensions still share one runtime type identity. Verify the exact dependency range and namespace absent/present separately from package absent/present. | [Metric package dependency][metric-project] and corresponding production extension projects. |
| P03 | Advertised package assets and symbols correspond to the shipped binaries: netstandard2.0 runtime/extension assets, compiler DLL dependencies and DLL/PDB pairs, portable symbol packages and available source provenance. Consumer applications do not gain Roslyn/Workspaces/annotation build tooling as application runtime requirements just to use quantities. Archive success does not substitute for execution. Source Link completeness is not assumed from a repository URL property. | [core assets][core-project], [tooling assets][tooling-project], [code-fix assets][codefix-project]. |
| P04 | The bundled alias-fix provider is loadable by a compatible workspace host and offers its documented safe fix. A CLI generator smoke build alone cannot prove that the IDE/code-fix assembly and exports load. No expanded host matrix is requested under Q14. | [code-fix export][fix], [bundled assets][core-project]. |

## Review decisions and implementation boundaries

Q01-Q07 record the agreed behaviour. Q08-Q14 originally mixed hypothetical limitations with possible product extensions; those extensions are excluded from this rewrite. They are not a backlog or further decisions for the user. Tests should cover the existing supported contracts without turning every private guard or unsupported spelling into a requirement.

| ID | Recorded contract or scope boundary | Production evidence |
| --- | --- | --- |
| Q01 | Resolved: retain the current conservative alias policy. Top-level explicit aliases reserve their spelling compilation-wide for unresolved-name/factory/English-exponent discovery; predefined and arithmetic-required aliases remain generated. Namespace aliases are protected by normal binding rather than global reservation. Any explicit alias matching a generated name receives DQ0006, even with the same target or namespace-local scope, and suppresses displaced-use checks for that spelling throughout its file. These rules are accepted behaviour, not a requested scope correction. | [reservations][reservations], [name discovery][name-discovery], [English discovery][english-discovery], [collisions][collisions]. |
| Q02 | Resolved: retain the current behaviour. Generated values and type encodings are normalised to canonical forms. Manually written noncanonical encodings receive DQ0004; their mathematical equality does not make them the same C# type. No automatic conversion or arithmetic-interoperability guarantee is required for such manually constructed quantity types. | [quantity reader][quantity-reader], [canonicality][canonicality], [quantity formatting][quantity-format]. |
| Q03 | Resolved: retain the current replacement behaviour. Comments inside the generic/qualified type being replaced may be discarded. Do not withhold the fix or add relocation/preservation logic solely for interior comments. Leading/trailing trivia remains preserved under D23. | [replacement edit][fix]. |
| Q04 | Resolved: the alias quick fix is best-effort, not guaranteed to preserve every expression binding or compilation result. Retain the existing speculative type-identity and nameof checks; do not add general rewritten-document semantic/compilation validation. Rare cases such as a value named Length shadowing the replacement in a static factory expression may need manual cleanup. Acceptance of this limitation is not a requirement to reproduce the failure in tests. | [speculative type check and outer-name selection][fix]. |
| Q05 | Resolved: retain the current DQ0002 scope: assignments/initialisers, supported return destinations, and method/constructor arguments. No expansion is required for params elements, constructor initialisers, indexer/attribute arguments, collection/deconstruction elements or additional lambda/async/yield destinations. Ordinary C# errors remain sufficient where DQ0002 is not supplied. | [registered destination syntax and mapping][destination-analysis]. |
| Q06 | Resolved: retain the documented invocation-based discovery forms: ordinary quantity helper calls, Power<T>.Of(quantity), and conditional access for nullable helper receivers. No new discovery support is required for method-group-only requests, qualified unresolved factory spellings or direct nullable Power/helper alternatives. Already-generated methods remain usable through normal C# binding. Do not add machinery or tests promising these additional forms. | [power discovery][power-discovery], [helper discovery][helper-discovery], [discovery seeds][seeds]. |
| Q07 | Resolved: signed-zero preservation is outside the required mathematical contract. Retain the current power implementation; do not add special handling or assertions distinguishing positive/negative zero or resulting infinity signs solely for this case. Ordinary finite negative-base rational-power behaviour remains G19. | [power numerical rule][power-emitter]. |
| Q08 | Outside rewrite scope: no expansion of provider declaration shapes is requested. Cover the documented ordinary providers and existing supported forms. Do not add requirements for generic containers, deeper/recursive declaration discovery, special handling of unusual internal labels or additional affine encodings merely because the implementation has limits. | [provider shape handling][providers], [declaration/factor validation][provider-reader]. |
| Q09 | Retain existing conversion-conflict handling within a receiver conversion set. Do not add cross-provider coordination, automatic disambiguation or parameter-name repair. Only a reproduced failure of documented valid provider composition should be raised as a defect; hypothetical collisions are not new acceptance requirements. | [conversion collection and parameters][conversion-emitter]. |
| Q10 | Retain the existing first-expression selection for parameterised prefix conversions. Cover the callable API produced by supported declarations. No new canonical-selection scheme or declaration-reordering invariance is requested without a demonstrated contradiction of an existing contract. | [association grouping][provider-reader], [first expression selection][conversion-emitter]. |
| Q11 | No new aggregate-overflow validation or universal finite-result guarantee is requested. Cover ordinary supported conversion scales and existing invalid-factor diagnostics. Hypothetical extreme combined constants do not authorise new diagnostics or resilience machinery. | [unit scale product][unit-expression], [literal formatting][conversion-emitter], [factor checks][provider-reader]. |
| Q12 | Retain the current provider-author responsibility for matching unit factors to quantity dimensions; no dimensional metadata schema or new physical-consistency validation is requested. QuantityUnit associations must resolve under the current provider-generation contract. Do not require additional support for associations depending on later unresolved-alias discovery. | [symbol schema][symbol-attribute], [quantity association reading][provider-reader], [generation sequence][generator]. |
| Q13 | Retain current generator input selection and diagnostic scope. The explicit analyser generated-code exclusion remains D11. Do not add a blanket generator exclusion or broader cross-generator visibility contract. Raise only a reproduced violation of an existing supported input/diagnostic behaviour. | [generator input selection][generator], [collision traversal][collisions], [English diagnostics][generator-diagnostics]. |
| Q14 | Verify existing advertised package delivery and compatibility, not an expanded host matrix or new debugging features. No new compiler/IDE support or Source Link guarantee is requested. If a clean consumer under an explicitly documented supported compiler cannot load the shipped tooling, report that concrete compatibility defect separately. | [runtime package][core-project], [Roslyn dependency][tooling-project], [Workspaces dependency][codefix-project], [README requirement][readme-core]. |

D18 covers warnings for actual changed bindings. The comparer used by [collision analysis][collisions] does not create an additional acceptance requirement or a separate product-design question.

## Exclusions from the proposed rewrite

- Same-driver edit history, incremental cache reuse and stale-output lifecycle guarantees remain outside scope, as previously agreed. Fresh input states still have to satisfy the contracts above.
- Discovery iteration budgets, mutable collection choices, helper call order, generated private class names, hint-file layout and incidental diagnostic ordering are not acceptance requirements.
- This catalogue does not add performance targets, test grouping, broad counterfeit-runtime resilience or speculative resource-exhaustion requirements.
- Existing tests and history are not implementation instructions for the fresh test author. A later coverage review can consult the old suite to find omissions, but should not silently promote its assumptions into requirements.

## Production evidence

[quantity]: ../src/DimensionalQuantities/TypeSystem/Quantity.cs#L8
[quantity-operators]: ../src/DimensionalQuantities/TypeSystem/Quantity.cs#L65
[quantity-helpers]: ../src/DimensionalQuantities/TypeSystem/Quantity.cs#L73
[quantity-prefixes]: ../src/DimensionalQuantities/TypeSystem/Quantity.cs#L26
[delta]: ../src/DimensionalQuantities/TypeSystem/Delta.cs#L3
[dimensions]: ../src/DimensionalQuantities/Dimensions/Dimension.cs#L66
[aliases]: ../src/DimensionalQuantities/Units/EngineeringQuantityAliases.cs#L7
[defaults]: ../src/DimensionalQuantities.Roslyn/Generators/DefaultQuantityDiscovery.cs#L8
[planning]: ../src/DimensionalQuantities.Roslyn/Generators/QuantityDiscovery.cs#L38
[generator]: ../src/DimensionalQuantities.Roslyn/Generators/QuantityGenerator.cs#L12
[quantity-emitter]: ../src/DimensionalQuantities.Roslyn/Generators/QuantitySourceEmitter.cs#L7
[quantity-format]: ../src/DimensionalQuantities.Roslyn/Generators/QuantityTypeNameFormatter.cs#L9
[name-discovery]: ../src/DimensionalQuantities.Roslyn/Discovery/QuantityNameDiscovery.cs#L11
[name-parser]: ../src/DimensionalQuantities/Naming/DimensionNameParser.cs#L10
[seeds]: ../src/DimensionalQuantities.Roslyn/Discovery/DiscoverySeedCollector.cs#L12
[reservations]: ../src/DimensionalQuantities.Roslyn/Discovery/ExplicitAliasReservation.cs#L8
[coordination]: ../src/DimensionalQuantities.Roslyn/Discovery/QuantityDiscoveryCoordinator.cs#L12
[bound-operations]: ../src/DimensionalQuantities.Roslyn/Discovery/BoundOperationDiscovery.cs#L18
[metadata]: ../src/DimensionalQuantities.Roslyn/Discovery/MetadataQuantityDiscovery.cs#L19
[quantity-reader]: ../src/DimensionalQuantities.Roslyn/Discovery/QuantityTypeReader.cs#L14
[number-reader]: ../src/DimensionalQuantities.Roslyn/Discovery/TypeSystemNumberReader.cs#L14
[number-encoding]: ../src/DimensionalQuantities/TypeSystem/TypeSystemNumbers.cs#L5
[arithmetic-discovery]: ../src/DimensionalQuantities.Roslyn/Discovery/ArithmeticDiscovery.cs#L13
[arithmetic-emitter]: ../src/DimensionalQuantities.Roslyn/Generators/ArithmeticSourceEmitter.cs#L21
[power-discovery]: ../src/DimensionalQuantities.Roslyn/Discovery/PowerDiscovery.cs#L23
[power-emitter]: ../src/DimensionalQuantities.Roslyn/Generators/PowerSourceEmitter.cs#L37
[helper-discovery]: ../src/DimensionalQuantities.Roslyn/Discovery/PowerMethodDiscovery.cs#L9
[helper-emitter]: ../src/DimensionalQuantities.Roslyn/Generators/PowerMethodSourceEmitter.cs#L24
[english-discovery]: ../src/DimensionalQuantities.Roslyn/Discovery/EnglishExponentDiscovery.cs#L20
[english-parser]: ../src/DimensionalQuantities/Numerics/EnglishRationalParser.cs#L7
[english-emitter]: ../src/DimensionalQuantities.Roslyn/Generators/EnglishExponentAliasEmitter.cs#L7
[mismatch]: ../src/DimensionalQuantities.Roslyn/Analyzers/DimensionMismatchAnalyzer.cs#L13
[binary-analysis]: ../src/DimensionalQuantities.Roslyn/Analyzers/BinaryDimensionAnalysis.cs#L11
[destination-analysis]: ../src/DimensionalQuantities.Roslyn/Analyzers/DestinationDimensionAnalysis.cs#L12
[encoding]: ../src/DimensionalQuantities.Roslyn/Analyzers/QuantityEncodingAnalyzer.cs#L17
[canonicality]: ../src/DimensionalQuantities.Roslyn/Analyzers/CanonicalTypeEncoding.cs#L10
[display-names]: ../src/DimensionalQuantities.Roslyn/Analyzers/DimensionNames.cs#L9
[generator-diagnostics]: ../src/DimensionalQuantities.Roslyn/Generators/GenerationDiagnostics.cs#L11
[collisions]: ../src/DimensionalQuantities.Roslyn/Generators/AliasCollisionDiagnostics.cs#L12
[fix]: ../src/DimensionalQuantities.Roslyn.CodeFixes/QuantityAliasCodeFixProvider.cs#L12
[providers]: ../src/DimensionalQuantities.Roslyn/Generators/UnitProviderGenerationCoordinator.cs#L10
[provider-reader]: ../src/DimensionalQuantities.Roslyn/Generators/UnitProviderReader.cs#L30
[conversion-emitter]: ../src/DimensionalQuantities.Roslyn/Generators/UnitConversionSourceEmitter.cs#L27
[provider-options]: ../src/DimensionalQuantities/Units/Declarations/UnitProviderAttribute.cs#L3
[symbol-attribute]: ../src/DimensionalQuantities/Units/Declarations/UnitSymbolAttribute.cs#L3
[factor-attribute]: ../src/DimensionalQuantities/Units/Declarations/UnitFactorAttribute.cs#L3
[unit-expression]: ../src/DimensionalQuantities/Units/UnitExpression.cs#L24
[prefixes]: ../src/DimensionalQuantities/Units/SiPrefixes.cs#L5
[base-prefixes]: ../src/DimensionalQuantities/Units/SiBasePrefixExponents.cs#L5
[metric]: ../src/DimensionalQuantities.Extensions.Metric/MetricUnits.cs#L6
[metric-temperature]: ../src/DimensionalQuantities.Extensions.Metric/MetricTemperatureUnits.cs#L6
[rpm]: ../src/DimensionalQuantities.Extensions.Metric/AngularVelocityRpmExtensions.cs#L3
[imperial]: ../src/DimensionalQuantities.Extensions.Imperial/ImperialUnits.cs#L6
[imperial-symbols]: ../src/DimensionalQuantities.Extensions.Imperial/ImperialSymbols.cs#L7
[nautical]: ../src/DimensionalQuantities.Extensions.Nautical/NauticalUnits.cs#L5
[nautical-symbols]: ../src/DimensionalQuantities.Extensions.Nautical/NauticalSymbols.cs#L7
[cgs]: ../src/DimensionalQuantities.Extensions.CgsEmag/CgsEmagUnits.cs#L5
[cgs-symbols]: ../src/DimensionalQuantities.Extensions.CgsEmag/CgsEmagSymbols.cs#L7
[core-project]: ../src/DimensionalQuantities/DimensionalQuantities.csproj#L3
[tooling-project]: ../src/DimensionalQuantities.Roslyn/DimensionalQuantities.Roslyn.csproj#L3
[codefix-project]: ../src/DimensionalQuantities.Roslyn.CodeFixes/DimensionalQuantities.Roslyn.CodeFixes.csproj#L3
[metric-project]: ../src/DimensionalQuantities.Extensions.Metric/DimensionalQuantities.Extensions.Metric.csproj#L20
[readme-core]: ../README.md#getting-started
[readme-nullable]: ../README.md#nullable-quantities
[readme-discovery]: ../README.md#discovery-through-your-code
[readme-interoperability]: ../README.md#sharing-quantities-between-packages
[readme-temperature]: ../README.md#affine-temperature-units-and-deltas
[readme-extension]: ../README.md#defining-an-extension
