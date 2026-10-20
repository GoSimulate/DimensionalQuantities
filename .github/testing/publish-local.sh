#!/usr/bin/env bash
set -euo pipefail
[[ "${ACT:-}" == true ]]
mkdir -p artifacts/local-feed
cp artifacts/packages/*.nupkg artifacts/local-feed/
