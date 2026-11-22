#!/usr/bin/env bash
# PreToolUse hook for Bash, Edit and Write: stops two mistakes AGENTS.md used to spell out.
# - Commands that watch for changes and never exit (./dev.sh, ./dev.sh test, ./dev-fresh.sh, dotnet watch), and the
#   slow container test build.
# - Edits to src/ChefBot.Web/dist/, which is the web page's build output.
# Exit 2 blocks the call and shows the message to Claude.
set -euo pipefail

input=$(cat)
command=$(jq -r '.tool_input.command // empty' <<<"$input")
file=$(jq -r '.tool_input.file_path // empty' <<<"$input")

if [[ -n "$command" ]]; then
  if grep -Eq '(^|[;&|(][[:space:]]*)((ba)?sh[[:space:]]+)?([^[:space:];&|]*/)?(dev\.sh[[:space:]]*($|[;&|)]|(run|test|fresh)([[:space:];&|)]|$))|dev-fresh\.sh)|dotnet[[:space:]]+watch' <<<"$command"; then
    echo "That command watches for changes and never exits. Check work with dotnet build and dotnet run --project tests/ChefBot.Core.Tests, or ./dev.sh web and ./dev.sh web-test for the web page." >&2
    exit 2
  fi
  if grep -Eq 'podman[[:space:]]+build.*--target[[:space:]=]+test' <<<"$command"; then
    echo "The container test build runs the same tests much more slowly. Use dotnet run --project tests/ChefBot.Core.Tests." >&2
    exit 2
  fi
fi

if [[ "$file" == */src/ChefBot.Web/dist/* ]]; then
  echo "src/ChefBot.Web/dist/ is build output. Edit src/ChefBot.Web/src instead, then run ./dev.sh web." >&2
  exit 2
fi
