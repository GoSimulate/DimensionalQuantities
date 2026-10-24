# CI and releases

Pull requests require a conventional title. CI runs explicit restore, Release build,
test, pack, and package-layout checks. Tests use Microsoft Testing Platform with
.NET 10. Packing uses `--no-build` and the outputs of the preceding solution build.
CI uses package version `0.0.0-ci`; build and pack receive the same version and
repository metadata.

Use squash merges with a Conventional Commits title, or preserve conventional
commit messages when merging. The release workflow uses `ietf-tools/semver-action`
to calculate a version from the latest tag through the triggering commit SHA.
`fix:` and `perf:` produce a patch; `feat:` a minor; `!` or a `BREAKING CHANGE:`
footer a major. Other commits alone, or no new commits, skip the release job
successfully. Versions continue from existing `vX.Y.Z` tags; version updates are
not committed to the repository.

CD runs on pushes to `main` affecting production source or the CD workflow, and
can also be dispatched manually on `main`. The existing source filter excludes
test-only changes. Release runs are serialized. The release job checks out the
same SHA used for versioning, then explicitly restores, builds, tests, packs, and
validates the package. `requarks/changelog-action` generates release notes from
the previous tag to that SHA without writing a changelog file.

After validation, NuGet trusted publishing obtains a short-lived credential using
`NuGet/login` and the repository's `NUGET_USER` variable. Separate publish steps
push the package to nuget.org and the repository owner's GitHub Packages feed.
GitHub Packages uses `GITHUB_TOKEN`; no PAT is needed. Finally,
`ncipollo/release-action` creates the `vX.Y.Z` tag at the tested SHA and a GitHub
release with the notes and package attached. Only the release job receives
`contents: write`, `packages: write`, and `id-token: write` permissions.

## Local checks

Run the Restore, Build, Test, Pack, and Check NuGet package commands shown in
`.github/workflows/ci.yml` from the repository root. Set `VERSION=0.0.0-ci` and
`REPOSITORY_URL=https://github.com/GoSimulate/DimensionalQuantities` in the shell
environment first. Use the SDK pinned in `global.json`. These checks validate
build and package behavior; hosted authentication, action API calls, and
publication are verified by GitHub Actions.

## Recovering a failed release

Publishing to two feeds and creating a GitHub release are separate operations.
A failed build or test stops publication; either failed publish stops release
creation. Inspect the failed step and existing packages/tags/releases before
retrying. Both feeds use `--skip-duplicate`, allowing a failed run to be retried
when one feed already accepted the package. Before a tag exists, rerun the failed
run to retain its original commit. A new manual run targets the selected branch's
current commit and may include newer changes.

After a tag has been created, recover an incomplete release explicitly rather
than assuming another run will repair it: version calculation may find no new
commits and skip publication. Do not move published version tags or replace
published packages. There is no local alternate publishing mode or release
simulator.
