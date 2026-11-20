#!/usr/bin/env bash
# Installs or updates ChefBot as rootless Podman Quadlet services for the current user.
# Run it again after pulling changes: it rebuilds the image and restarts ChefBot. Settings, data and the
# database password are kept.
set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo="$(cd "$here/../.." && pwd)"
config_home="${XDG_CONFIG_HOME:-$HOME/.config}"
units="$config_home/containers/systemd/chefbot"
settings="$config_home/chefbot/chefbot.env"

echo "Building the ChefBot image (this also runs the tests)..."
podman build --target test "$repo"
podman build --tag localhost/chefbot:latest "$repo"

if ! podman secret exists chefbot-db-password; then
    echo "Creating a random database password as the podman secret chefbot-db-password."
    head -c 48 /dev/urandom | base64 | tr -d '/+=\n' | head -c 40 | podman secret create chefbot-db-password - >/dev/null
fi

if [[ ! -f "$settings" ]]; then
    mkdir -p "$(dirname "$settings")"
    cp "$here/chefbot.env.example" "$settings"
    echo "Created $settings. Edit it to connect Mealie or a webhook."
fi

mkdir -p "$units"
cp "$here"/*.pod "$here"/*.container "$here"/*.volume "$units/"
systemctl --user daemon-reload
# Restarting the pod (re)starts the database and the app, so a new image or changed unit is picked up.
systemctl --user restart chefbot-pod.service

echo
echo "ChefBot is starting at http://localhost:8080 and https://localhost:8443"
echo "Logs: journalctl --user -u chefbot -f"
if [[ "$(loginctl show-user "$USER" --property=Linger --value 2>/dev/null)" != "yes" ]]; then
    echo "ChefBot stops when you log out. To keep it running and start it at boot, run:"
    echo "  loginctl enable-linger $USER"
fi
