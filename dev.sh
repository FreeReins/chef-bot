#!/usr/bin/env bash
# Local development: ChefBot runs on this machine with hot reload, against a throwaway PostgreSQL container.
# Needs the .NET 10 SDK and Podman. The web page uses Node if this machine has it, otherwise Node in a container.
#
#   ./dev.sh          start the dev database if needed, then run the app with dotnet watch and the web page
#                     with live reload (Vite)
#   ./dev.sh web      build the web page once into src/ChefBot.Api/wwwroot, for a plain `dotnet run`
#   ./dev.sh web-test run the web page's accessibility tests in a Playwright container, then exit; arguments go
#                     to `playwright test`, for example ./dev.sh web-test --project desktop-dark
#   ./dev.sh web-shot /history
#                     screenshot an address of the web page, answered from the test stubs, in each test project
#                     (light, dark, phone) into src/ChefBot.Web/shots/, then exit
#   ./dev.sh test     run the tests again on every save
#   ./dev.sh db       start the dev database only
#   ./dev.sh psql     open a SQL prompt on the dev database
#   ./dev.sh stop     stop the dev database
#   ./dev.sh reset    delete the dev database, sign-in keys and backups, for example after changing an entity
#   ./dev.sh fresh    reset, then run the app: the setup wizard runs again (same as ./dev-fresh.sh)
#
# The dev database is separate from an installed ChefBot (deploy/podman). Its settings match the defaults in
# src/ChefBot.Api/appsettings.json, so a plain `dotnet run` works too while it is running.
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")"

container=chefbot-dev-db
volume=chefbot-dev-db
db_port=5432
# Where the dev copy keeps its session keys and backups: next to the built app (Data:SessionKeys, Data:Backups).
app_output=src/ChefBot.Api/bin/Debug/net10.0
web=src/ChefBot.Web
web_output=src/ChefBot.Api/wwwroot
web_container=chefbot-dev-web
web_test_modules=chefbot-web-test-modules
node_image=docker.io/library/node:24-alpine

port_in_use() {
    ss -Hltn "( sport = :$1 )" 2>/dev/null | grep -q .
}

container_exists() {
    podman container exists "$container"
}

start_db() {
    if ! container_exists; then
        if port_in_use "$db_port"; then
            echo "Port $db_port is already in use, so the dev database can't listen on it." >&2
            echo "Stop whatever is using it (ss -ltnp 'sport = :$db_port' shows what), then try again." >&2
            exit 1
        fi
        echo "Creating the dev database ($container)..."
        podman run --detach --name "$container" \
            --publish "127.0.0.1:$db_port:5432" \
            --env POSTGRES_USER=chefbot --env POSTGRES_PASSWORD=chefbot --env POSTGRES_DB=chefbot \
            --volume "$volume:/var/lib/postgresql/data" \
            docker.io/library/postgres:17-alpine >/dev/null
    elif [[ "$(podman inspect --format '{{.State.Running}}' "$container")" != "true" ]]; then
        podman start "$container" >/dev/null
    fi

    # Over TCP: on first start the image runs a temporary, socket-only server while it creates the database.
    printf 'Waiting for the dev database'
    for _ in $(seq 60); do
        if podman exec "$container" pg_isready --quiet -h 127.0.0.1 -U chefbot -d chefbot; then
            echo " ready."
            return
        fi
        printf '.'
        sleep 1
    done
    echo
    echo "The dev database didn't become ready. Its log: podman logs $container" >&2
    exit 1
}

reset_all() {
    podman rm --force "$container" >/dev/null 2>&1 || true
    podman volume rm "$volume" >/dev/null 2>&1 || true
    # Without the old keys, a browser's old sign-in cookie can't be read, so it starts signed out.
    rm -rf "$app_output/keys" "$app_output/backups"
    echo "Deleted the dev database, sign-in keys and backups."
}

# Runs npm in the web page's folder: with this machine's Node when there is one, otherwise in a Node container.
# Variables like CHEFBOT_API=... listed in web_env are passed to it; web_container_options go to podman run.
web_env=()
web_container_options=()
web_npm() {
    if command -v npm >/dev/null 2>&1; then
        (cd "$web" && exec env "${web_env[@]}" npm "$@")
    else
        mkdir -p "$web_output"
        local env_options=()
        for variable in "${web_env[@]}"; do env_options+=(--env "$variable"); done
        # keep-id: files it writes (node_modules, the build) belong to you. Host network: the dev server reaches the API.
        podman run --rm --init "${web_container_options[@]}" --userns=keep-id --network=host --env HOME=/tmp --env NPM_CONFIG_UPDATE_NOTIFIER=false \
            "${env_options[@]}" --volume "$PWD/$web:/source/$web:z" --volume "$PWD/$web_output:/source/$web_output:z" \
            --workdir "/source/$web" "$node_image" npm "$@"
    fi
}

build_web() {
    echo "Building the web page..."
    web_npm install --no-audit --no-fund --loglevel=error
    web_npm run build -- --logLevel warn
}

# The web page's accessibility tests (src/ChefBot.Web/tests), always in Playwright's image: it has Node, Chromium and
# the libraries Chromium needs, at the version package.json pins. The image is Ubuntu, while node_modules in the
# source folder may come from the Alpine Node container, whose native Vite packages don't run there; so the tests
# keep their own node_modules in a volume. Rootless Podman runs the container's root as you, so results belong to you.
web_test() {
    local version
    version="$(sed -n 's/.*"@playwright\/test": "\([0-9.]*\)".*/\1/p' "$web/package.json")"
    if [[ -z "$version" ]]; then
        echo "Pin @playwright/test to an exact version in $web/package.json, so the container image can match it." >&2
        exit 1
    fi
    podman run --rm --init --ipc=host --env NPM_CONFIG_UPDATE_NOTIFIER=false --env CI=1 --env SHOT_PATH \
        --volume "$PWD/$web:/source/$web:z" --volume "$web_test_modules:/source/$web/node_modules" \
        --workdir "/source/$web" "mcr.microsoft.com/playwright:v$version-noble" \
        sh -c 'npm install --no-audit --no-fund --loglevel=error && exec npx playwright test "$@"' web-test "$@"
}

run_app() {
    start_db

    local http_port https_port web_port
    http_port="$(pick_port 8080 5080 HTTP)"
    https_port="$(pick_port 8443 5443 HTTPS)"
    web_port="$(pick_port 5173 5174 "live-reload web page")"

    # Built once, so the app's own addresses serve a current page too.
    build_web
    # The live-reload copy of the page passes API calls through to the app. It stops when the app does.
    web_env=("CHEFBOT_API=http://localhost:$http_port" "CHEFBOT_WEB_PORT=$web_port")
    # Named, so a dev server left behind by a dev.sh that was killed is replaced, not doubled.
    web_container_options=(--name "$web_container" --replace)
    web_npm run dev -- --clearScreen false &
    local web_pid=$!
    trap 'kill "$web_pid" 2>/dev/null; podman rm --force --time 2 "$web_container" >/dev/null 2>&1 || true' EXIT

    echo "Edit the web page at http://localhost:$web_port: it reloads as you save files in $web."
    echo "ChefBot will be at http://localhost:$http_port and https://localhost:$https_port"
    # HTTPS works like an installed ChefBot: a self-signed certificate, kept with the dev copy's other keys, so
    # the HTTPS settings and the setup wizard's HTTPS step can be tried out. ./dev.sh reset deletes it.
    # Development turns on hot reload's browser refresh. The schedule is off so the dev copy never stages plans
    # on its own, and edits that hot reload can't apply restart the app instead of asking.
    ASPNETCORE_ENVIRONMENT=Development \
    DOTNET_WATCH_RESTART_ON_RUDE_EDIT=1 \
    Https__Mode=SelfSigned \
    Https__HttpPort="$http_port" \
    Https__Port="$https_port" \
    Https__PublicPort="$https_port" \
    Schedule__Enabled=false \
        dotnet watch --project src/ChefBot.Api
}

# Prints the preferred port, or the fallback when an installed ChefBot (or anything else) already uses it.
pick_port() {
    local preferred=$1 fallback=$2 name=$3
    if ! port_in_use "$preferred"; then
        echo "$preferred"
        return
    fi
    if port_in_use "$fallback"; then
        echo "$name ports $preferred and $fallback are both in use. Stop one of them and try again." >&2
        exit 1
    fi
    echo "Port $preferred is in use (an installed ChefBot?), so the dev copy uses $fallback for $name." >&2
    echo "$fallback"
}

case "${1:-run}" in
    run)
        run_app
        ;;
    web)
        build_web
        echo "Built into $web_output."
        ;;
    web-test)
        shift
        web_test "$@"
        ;;
    web-shot)
        if [[ -z "${2:-}" ]]; then
            echo "Say which address to screenshot, for example: ./dev.sh web-shot /history" >&2
            exit 1
        fi
        SHOT_PATH="$2" web_test --reporter=line "${@:3}"
        echo "Screenshots are in $web/shots/."
        ;;
    test)
        exec dotnet watch --project tests/ChefBot.Core.Tests run
        ;;
    db)
        start_db
        echo "Connection: Host=localhost;Port=$db_port;Database=chefbot;Username=chefbot;Password=chefbot"
        ;;
    psql)
        start_db
        exec podman exec -it "$container" psql -U chefbot -d chefbot
        ;;
    stop)
        if container_exists; then
            podman stop "$container" >/dev/null
            echo "Stopped the dev database. Its data is kept."
        else
            echo "There is no dev database."
        fi
        ;;
    reset)
        reset_all
        echo "The next ./dev.sh creates an empty database and the setup wizard runs again."
        ;;
    fresh)
        reset_all
        run_app
        ;;
    *)
        sed -n '2,18p' "$0" | sed 's/^# \{0,1\}//'
        exit 1
        ;;
esac
