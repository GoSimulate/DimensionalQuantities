#!/usr/bin/env bash
set -euo pipefail
version="${1:?Supply the package version}"
repository_url="${GITHUB_SERVER_URL:-https://github.com}/${GITHUB_REPOSITORY:?GITHUB_REPOSITORY is required}"
mkdir -p artifacts/packages
rm -f artifacts/packages/*.nupkg
dotnet pack src/DimensionalQuantities/DimensionalQuantities.csproj \
  -c Release -o artifacts/packages \
  -p:Version="$version" -p:ContinuousIntegrationBuild=true \
  -p:RepositoryUrl="$repository_url"
python3 .github/scripts/check-package.py "$version"
