#!/usr/bin/env bash
# Stops ChefBot and removes the Quadlet services that install.sh set up for the current user.
#
#   deploy/podman/uninstall.sh                 stop ChefBot and remove its services; keep its data
#   deploy/podman/uninstall.sh --delete-data   also delete the database, sign-in keys, backups, the database
#                                              password, the settings file and the image (asks first)
#   deploy/podman/uninstall.sh --delete-data --yes   the same, without asking
#
# Kept data means running install.sh again brings ChefBot back exactly as it was.
set -euo pipefail

config_home="${XDG_CONFIG_HOME:-$HOME/.config}"
units="$config_home/containers/systemd/chefbot"
settings="$config_home/chefbot/chefbot.env"
volumes=(chefbot-db chefbot-keys chefbot-backups)
# Quadlet makes a service from each unit, including one per volume. Stopping those doesn't delete the volumes.
services=(chefbot-pod.service chefbot.service chefbot-db.service)
for volume in "${volumes[@]}"; do services+=("$volume-volume.service"); done

delete_data=false
assume_yes=false
for argument in "$@"; do
    case "$argument" in
        --delete-data) delete_data=true ;;
        --yes) assume_yes=true ;;
        *) sed -n '2,9p' "$0" | sed 's/^# \{0,1\}//'; exit 1 ;;
    esac
done

if $delete_data && ! $assume_yes; then
    echo "This deletes ChefBot's database (recipes, plans, pantry, accounts), its backups, sign-in keys,"
    echo "database password, settings ($settings) and image. It can't be undone."
    echo "To keep a backup, download one from Settings > Backups first."
    read -r -p "Type delete to go ahead: " answer
    if [[ "$answer" != delete ]]; then
        echo "Nothing was changed."
        exit 1
    fi
fi

echo "Stopping ChefBot..."
# Stopping the pod stops the app and the database with it.
systemctl --user stop "${services[@]}" 2>/dev/null || true

if [[ -d "$units" ]]; then
    rm -rf "$units"
    echo "Removed the services from $units."
fi
systemctl --user daemon-reload
systemctl --user reset-failed "${services[@]}" 2>/dev/null || true

# Whatever is still running, for example a pod started by hand or left by an older install.
if podman pod exists chefbot; then
    podman pod rm --force --time 10 chefbot >/dev/null
    echo "Removed the chefbot pod."
fi
for container in chefbot chefbot-db; do
    if podman container exists "$container"; then
        podman rm --force --time 10 "$container" >/dev/null
        echo "Removed the $container container."
    fi
done

if ! $delete_data; then
    echo
    echo "ChefBot is uninstalled. Its data is kept: the volumes ${volumes[*]}, the secret"
    echo "chefbot-db-password, $settings and the image localhost/chefbot:latest."
    echo "Run install.sh to bring it back, or uninstall.sh --delete-data to remove those too."
    exit 0
fi

for volume in "${volumes[@]}"; do
    if podman volume exists "$volume"; then
        podman volume rm "$volume" >/dev/null
        echo "Deleted the $volume volume."
    fi
done
if podman secret exists chefbot-db-password; then
    podman secret rm chefbot-db-password >/dev/null
    echo "Deleted the chefbot-db-password secret."
fi
if [[ -f "$settings" ]]; then
    rm "$settings"
    rmdir "$(dirname "$settings")" 2>/dev/null || true
    echo "Deleted $settings."
fi
if podman image exists localhost/chefbot:latest; then
    podman rmi localhost/chefbot:latest >/dev/null
    echo "Deleted the localhost/chefbot:latest image."
fi
echo
echo "ChefBot and its data are gone. Run install.sh to start again from the setup wizard."
