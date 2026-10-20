#!/usr/bin/env bash
set -euo pipefail
if [[ -n "${ACT:-}" ]]; then
  echo 'External publishing is disabled under act.' >&2
  exit 1
fi
: "${GITHUB_TOKEN:?GITHUB_TOKEN is required}"
: "${GITHUB_REPOSITORY_OWNER:?GITHUB_REPOSITORY_OWNER is required}"
dotnet nuget push 'artifacts/packages/*.nupkg' \
  --source "https://nuget.pkg.github.com/${GITHUB_REPOSITORY_OWNER}/index.json" \
  --api-key "$GITHUB_TOKEN" --skip-duplicate
