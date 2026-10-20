# CI and releases

Pull requests require a conventional title, then run restore, Release build, all three xUnit v3 test projects, and
package-layout validation. Tests use Microsoft Testing Platform with .NET 10.
The scaffold currently permits empty suites (`--ignore-exit-code 8`);
remove that allowance when implementation tests are added. A push to `main` runs the same checks before release.
Use squash merges with a Conventional Commits 1.0 title, or preserve conventional
commit messages when merging. `fix:` and `perf:` produce a patch; `feat:` a minor;
`!` or a `BREAKING CHANGE:` footer a major. Other commits alone do not release.
The first release is 1.0.0. Versions are computed from Git tags, not committed
version-number updates. Use branch protection to require CI before merging.

The release action is cycjimmy/semantic-release-action, using semantic-release's
commit-analyzer, release-notes-generator, exec, and GitHub plugins. It packs one
DimensionalQuantities package containing the runtime and Roslyn tooling, tags
`vX.Y.Z`, publishes to the repository owner's GitHub Packages NuGet feed, and
creates a GitHub release with conventional-commit notes and the package attached.
The workflow uses GITHUB_TOKEN with contents:write and packages:write; no PAT is
needed. Repository metadata connects the package to GoSimulate/DimensionalQuantities.
A manual Release run can retry failures before tagging. After a tag has been
created, inspect and recover any incomplete publication explicitly.
Publishing/tagging/release creation are not an atomic transaction: inspect partial
results before recovering a failed publish.

## Local workflow checks

Run `python3 .github/testing/run-act.py` with Docker and act installed. It makes a
disposable repository, injects temporary xUnit tests to verify runner discovery,
and runs CI plus the release workflow using the real semantic-release action.
The fixture starts at v1.0.0 and adds a feat commit. Under act, Git pushes go only
to a local bare repository, NuGet publication copies to a local feed, and GitHub
release creation records JSON. Checks verify v1.1.0, generated notes and package
contents. Hosted authentication, permissions and GitHub API publication still
are not exercised by act. No GitHub token is used locally.

Use `--case patch`, `--case major`, or `--case none` with `--workflow release`
to check patch releases, breaking changes, and documentation-only commits.
