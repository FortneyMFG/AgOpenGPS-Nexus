#!/usr/bin/env bash
set -euo pipefail

if ! command -v dotnet >/dev/null 2>&1; then
  echo "error: dotnet CLI is required to build the UI" >&2
  exit 127
fi

repo_root="$(git rev-parse --show-toplevel)"
project_path="$repo_root/Nexus SourceCode/src/Aog.UI.Avalonia/Aog.UI.Avalonia.csproj"

echo "Building Aog.UI.Avalonia from $project_path"
dotnet build "$project_path" "$@"
