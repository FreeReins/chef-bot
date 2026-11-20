#!/usr/bin/env bash
# PostToolUse hook for Edit|Write: tidies a C# file after Claude changes it.
# - Removes unused usings (IDE0005) with `dotnet format`.
# - Checks the namespace matches the folder (IDE0130). `dotnet format` can't fix that one, so it tells Claude instead.
set -euo pipefail

file=$(jq -r '.tool_response.filePath // .tool_input.file_path // empty')
[[ "$file" == *.cs && -f "$file" ]] || exit 0
[[ "$file" == */obj/* || "$file" == */bin/* ]] && exit 0

# The nearest project file above the edited file.
dir=$(dirname "$file")
project=""
while [[ "$dir" != "/" ]]; do
  project=$(find "$dir" -maxdepth 1 -name '*.csproj' -print -quit)
  [[ -n "$project" ]] && break
  dir=$(dirname "$dir")
done
[[ -n "$project" ]] || exit 0
project_dir=$(dirname "$project")

# `--include` only matches paths relative to the working directory.
(cd "$project_dir" && dotnet format style "$(basename "$project")" --diagnostics IDE0005 --severity warn \
  --include "$(realpath --relative-to="$project_dir" "$file")" -v q) >/dev/null 2>&1 || true

# Expected namespace: the project's root namespace plus the folder path below the project.
root=$(grep -oP '(?<=<RootNamespace>)[^<]+' "$project" || basename "$project" .csproj)
relative=$(realpath --relative-to="$project_dir" "$(dirname "$file")")
expected=$root
[[ "$relative" != "." ]] && expected="$root.${relative//\//.}"
actual=$(grep -m1 -oP '^namespace \K[\w.]+' "$file" || true)

if [[ -n "$actual" && "$actual" != "$expected" ]]; then
  jq -n --arg reason "$file declares namespace $actual but its folder means $expected (IDE0130). Change the namespace, and update usings that refer to it." \
    '{decision: "block", reason: $reason}'
fi
exit 0
