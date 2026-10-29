#!/usr/bin/env bash
set -euo pipefail
project_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
export DOTNET_CLI_HOME="$project_root/.local/dotnet"
export NUGET_PACKAGES="$project_root/.local/nuget"
export DOTNET_NOLOGO=1
cd "$project_root"
exec dotnet "$@"
