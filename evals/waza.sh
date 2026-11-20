#!/usr/bin/env bash
# Runs Microsoft waza in a container against this repo. Needs only Podman.
#
#   evals/waza.sh --version
#   evals/waza.sh tokens count AGENTS.md
#   evals/waza.sh preflight evals/agents-md/eval.yaml --context-dir evals/agents-md/context/with
#   evals/waza.sh rebuild        rebuild the image, for example after changing evals/Containerfile
#
# The container has no network, so only offline commands work (tokens, preflight, check). Real runs
# (waza run) need network access and a Copilot sign-in, which aren't set up yet.
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

image=localhost/chefbot-waza
cache=chefbot-waza-cache

if [[ "${1:-}" == "rebuild" ]]; then
    podman build --tag "$image" --file evals/Containerfile evals
    exit
fi

if ! podman image exists "$image"; then
    echo "Building the waza image ($image)..." >&2
    podman build --quiet --tag "$image" --file evals/Containerfile evals >/dev/null
fi

# keep-id: files waza writes (results/) belong to you. :Z lets SELinux allow the mount, :U hands the cache
# volume to your user.
exec podman run --rm --interactive $([[ -t 0 ]] && echo --tty) \
    --network none \
    --userns keep-id \
    --volume "$PWD:/work:Z" \
    --volume "$cache:/cache:Z,U" \
    "$image" "$@"
