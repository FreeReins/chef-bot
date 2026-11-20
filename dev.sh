#!/usr/bin/env bash
# Local development: ChefBot runs on this machine with hot reload, against a throwaway PostgreSQL container.
# Needs the .NET 10 SDK and Podman.
#
#   ./dev.sh          start the dev database if needed, then run the app with dotnet watch
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

run_app() {
    start_db

    local http_port=8080
    if port_in_use "$http_port"; then
        http_port=5080
        echo "Port 8080 is in use (an installed ChefBot?), so the dev copy uses $http_port."
        if port_in_use "$http_port"; then
            echo "Port $http_port is in use too. Stop one of them and try again." >&2
            exit 1
        fi
    fi

    echo "ChefBot will be at http://localhost:$http_port"
    # Development turns on hot reload's browser refresh. The schedule is off so the dev copy never stages plans
    # on its own, and edits that hot reload can't apply restart the app instead of asking.
    ASPNETCORE_ENVIRONMENT=Development \
    DOTNET_WATCH_RESTART_ON_RUDE_EDIT=1 \
    Https__Mode=Off \
    Https__HttpPort="$http_port" \
    Schedule__Enabled=false \
        exec dotnet watch --project src/ChefBot.Api
}

case "${1:-run}" in
    run)
        run_app
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
        sed -n '2,14p' "$0" | sed 's/^# \{0,1\}//'
        exit 1
        ;;
esac
