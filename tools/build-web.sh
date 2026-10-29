#!/usr/bin/env bash
set -euo pipefail
project_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
python3 "$project_root/tools/bootstrap_sources.py"
npm --prefix "$project_root/src/PKHeX.Everywhere.Engine.Sdk" ci --include=dev --cache "$project_root/.local/npm-cache"
npm --prefix "$project_root/src/PKHeX.Web.React" ci --include=dev --cache "$project_root/.local/npm-cache"
"$project_root/tools/dotnet.sh" publish src/PKHeX.Everywhere.Engine.Host -c Release -m:1 -nr:false -p:DisableTransitiveFrameworkReferenceDownloads=true
cd "$project_root/src/PKHeX.Web.React"
npm run build:site
printf '\nStatic site: %s\n' "$PWD/dist"
