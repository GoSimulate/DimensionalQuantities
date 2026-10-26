val allowedLicences = setOf(
    "MIT",
    "Apache-2.0",
    "0BSD",
    "BSD-1-Clause",
    "BSD-2-Clause",
    "BSD-3-Clause",
    "BSL-1.0",
    "ISC",
    "Zlib"
).map { it.toSpdx() as SpdxSingleLicenseExpression }.toSet()

fun PackageRule.LicenseRule.isOutsideAllowlist() = object : RuleMatcher {
    override val description = "isOutsideAllowlist($license)"
    override fun matches() = license !in allowedLicences
}

val licenceRules = ruleSet(ortResult, licenseInfoResolver, resolutionProvider) {
    packageRule("UNAPPROVED_PACKAGE_LICENCE") {
        require {
            -isExcluded()
        }

        // ORT also visits the repository's project entries, which are not consumed packages.
        if (pkg.metadata.id.type == "NuGet" && pkg.metadata.id.version.isNotEmpty()) {
            if (pkg.metadata.declaredLicensesProcessed.spdxExpression == null) {
                error(
                    "No recognised licence was declared for ${pkg.metadata.id.toCoordinates()}.",
                    "Review the package licence and record a justified curation before approving it."
                )
            }

            licenseRule("UNAPPROVED_PACKAGE_LICENCE", LicenseView.CONCLUDED_OR_DECLARED_OR_DETECTED) {
                require {
                    -isExcluded()
                    +isOutsideAllowlist()
                }

                error(
                    "The licence $license for ${pkg.metadata.id.toCoordinates()} is outside the pilot allowlist.",
                    "Review the licence before adding it to the allowlist or record a justified exception."
                )
            }
        }
    }
}

ruleViolations += licenceRules.violations
