#!/usr/bin/env bash
set -euo pipefail
[[ "${ACT:-}" == true ]]
# act copies the disposable fixture checkout into the job, including its history.
git config --global --add safe.directory "$PWD"
git clone --bare . /tmp/dimensional-quantities-remote.git
git remote set-url origin file:///tmp/dimensional-quantities-remote.git
