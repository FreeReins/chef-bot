#!/usr/bin/env bash
# Works out the next release version from the Conventional Commits (https://www.conventionalcommits.org) on HEAD since
# the last vX.Y.Z tag:
#   feat: ...                                   a minor release
#   fix: ... or perf: ...                       a patch release
#   type!: ... or a "BREAKING CHANGE:" footer   a major release (a minor one while the version is below 1.0.0)
#   anything else (docs:, chore:, ci:, ...)     no release
# With no tag yet, the first release is INITIAL_VERSION (default 0.1.0).
# Prints the lines below, and adds them to $GITHUB_OUTPUT when run in GitHub Actions:
#   release=true|false  version=1.2.3  tag=v1.2.3  previous=v1.2.2
# Run it locally to see what the next release would be: .github/scripts/next-version.sh
set -euo pipefail

initial="${INITIAL_VERSION:-0.1.0}"

previous="$(git tag --list 'v*' --merged HEAD --sort=-v:refname | grep -E '^v[0-9]+\.[0-9]+\.[0-9]+$' | head -n 1 || true)"
range="${previous:+$previous..}HEAD"

bump=0 # 0 none, 1 patch, 2 minor, 3 major
while IFS= read -r -d $'\x1e' message; do
    message="${message#$'\n'}"
    subject="${message%%$'\n'*}"
    if [[ "$subject" =~ ^[a-zA-Z]+(\([^\)]*\))?!: ]] || grep -qE '^BREAKING[ -]CHANGE: ' <<< "$message"; then
        level=3
    elif [[ "$subject" =~ ^feat(\([^\)]*\))?: ]]; then
        level=2
    elif [[ "$subject" =~ ^(fix|perf)(\([^\)]*\))?: ]]; then
        level=1
    else
        level=0
    fi
    (( level > bump )) && bump=$level
done < <(git log --format='%B%x1e' "$range")

if (( bump == 0 )); then
    version=""
elif [[ -z "$previous" ]]; then
    version="$initial"
else
    IFS=. read -r major minor patch <<< "${previous#v}"
    if (( bump == 3 && major == 0 )); then bump=2; fi
    case $bump in
        3) version="$((major + 1)).0.0" ;;
        2) version="$major.$((minor + 1)).0" ;;
        1) version="$major.$minor.$((patch + 1))" ;;
    esac
fi

result="release=$([[ -n "$version" ]] && echo true || echo false)
version=$version
tag=${version:+v$version}
previous=$previous"
echo "$result"
if [[ -n "${GITHUB_OUTPUT:-}" ]]; then echo "$result" >> "$GITHUB_OUTPUT"; fi
