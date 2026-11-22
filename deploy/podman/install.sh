#!/usr/bin/env bash
# Installs or updates ChefBot as a rootless Podman Quadlet service for the current user. ChefBot is one container
# that runs its own database, keeping everything in the chefbot-data volume.
#
#   install.sh           use the published image (ghcr.io), kept up to date by podman auto-update
#   install.sh --build   build the image from this checkout instead (needs the repository, not just this script)
#
# Works straight from the web, without cloning:
#   curl -fsSL https://raw.githubusercontent.com/Jamiethenextkevinbaconhamm/chef-bot/main/deploy/podman/install.sh | bash
#
# Run it again to update the services: settings and data are kept.
set -euo pipefail

published_image=ghcr.io/jamiethenextkevinbaconhamm/chef-bot:latest
example_url=https://raw.githubusercontent.com/Jamiethenextkevinbaconhamm/chef-bot/main/deploy/podman/chefbot.env.example
config_home="${XDG_CONFIG_HOME:-$HOME/.config}"
units="$config_home/containers/systemd/chefbot"
settings="$config_home/chefbot/chefbot.env"
here="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" 2>/dev/null && pwd || pwd)"

build=false
for argument in "$@"; do
    case "$argument" in
        --build) build=true ;;
        *) sed -n '2,11p' "$0" | sed 's/^# \{0,1\}//'; exit 1 ;;
    esac
done

if ! command -v podman >/dev/null 2>&1; then
    echo "ChefBot needs Podman 4.4 or later. Install it (for example: sudo apt install podman), then run this again." >&2
    exit 1
fi

if $build; then
    repo="$(cd "$here/../.." 2>/dev/null && pwd)"
    if [[ ! -f "$repo/Dockerfile" ]]; then
        echo "--build needs the ChefBot repository: run deploy/podman/install.sh --build from a clone." >&2
        exit 1
    fi
    echo "Building the ChefBot image..."
    podman build --tag localhost/chefbot:latest "$repo"
    image=localhost/chefbot:latest
    auto_update=local
else
    echo "Downloading the ChefBot image..."
    podman pull "$published_image"
    image=$published_image
    auto_update=registry
fi

if [[ ! -f "$settings" ]]; then
    mkdir -p "$(dirname "$settings")"
    if [[ -f "$here/chefbot.env.example" ]]; then
        cp "$here/chefbot.env.example" "$settings"
    else
        curl -fsSL "$example_url" -o "$settings"
    fi
    echo "Created $settings. Edit it to change HTTPS options; most settings are in the app."
fi

mkdir -p "$units"
# Left by an install from before ChefBot was one container.
rm -f "$units"/chefbot.pod "$units"/chefbot-db.container "$units"/chefbot-db.volume \
    "$units"/chefbot-keys.volume "$units"/chefbot-backups.volume

cat > "$units/chefbot-data.volume" <<'EOF'
# Everything ChefBot keeps: its database, sign-in keys, HTTPS certificate and backups.
[Volume]
VolumeName=chefbot-data
EOF

cat > "$units/chefbot.container" <<EOF
# ChefBot, written by install.sh. Its settings are in ~/.config/chefbot/chefbot.env.
[Unit]
Description=ChefBot

[Container]
ContainerName=chefbot
Image=$image
# podman auto-update (its timer: systemctl --user enable --now podman-auto-update.timer) replaces the image with a
# newer one and restarts ChefBot.
AutoUpdate=$auto_update
EnvironmentFile=%h/.config/chefbot/chefbot.env
Timezone=local
# HTTP, then HTTPS. To use other ports on this computer, change the left-hand number. If you change the HTTPS one,
# set Https__PublicPort in chefbot.env to match.
PublishPort=8080:8080
PublishPort=8443:8443
# To keep the data in a folder instead, create it, give it to the app's user with
#   podman unshare chown 1654 ~/chefbot-data
# and use this line in place of the one below:
#   Volume=%h/chefbot-data:/data:Z
Volume=chefbot-data.volume:/data
# To use your own ingredient list, starter recipes, list of known stores or starter plan styles, mount them over the
# built-in ones:
#   Volume=%h/.config/chefbot/ingredients.json:/app/config/ingredients.json:ro,Z
#   Volume=%h/.config/chefbot/stores.json:/app/config/stores.json:ro,Z
#   Volume=%h/.config/chefbot/plan-styles.json:/app/config/plan-styles.json:ro,Z
# Time for ChefBot to stop its database cleanly.
StopTimeout=30

[Service]
Restart=always

[Install]
# Start with your user session, or at boot once lingering is on (loginctl enable-linger).
WantedBy=default.target
EOF

systemctl --user daemon-reload
# The old pod, if this machine had ChefBot from before it was one container.
systemctl --user stop chefbot-pod.service chefbot-db.service 2>/dev/null || true
systemctl --user restart chefbot.service

if [[ "$auto_update" == registry ]] && ! systemctl --user is-enabled --quiet podman-auto-update.timer 2>/dev/null; then
    systemctl --user enable --now podman-auto-update.timer >/dev/null 2>&1 \
        && echo "Turned on podman-auto-update.timer: ChefBot updates itself daily when there's a new release."
fi

echo
echo "ChefBot is starting at http://localhost:8080 and https://localhost:8443"
echo "Logs: journalctl --user -u chefbot -f"
if [[ "$(loginctl show-user "$USER" --property=Linger --value 2>/dev/null)" != "yes" ]]; then
    echo "ChefBot stops when you log out. To keep it running and start it at boot, run:"
    echo "  loginctl enable-linger $USER"
fi
