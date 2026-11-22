# Install with Docker

This guide runs ChefBot with Docker or Podman. It works anywhere they do: Linux, Windows, macOS and most NAS
systems. ChefBot is one container, with its own database inside, and keeps everything in one volume.

Each release is published as a container image for 64-bit x86 and ARM at
`ghcr.io/jamiethenextkevinbaconhamm/chef-bot`.

## The quickest way

```sh
docker run -d --name chefbot --restart unless-stopped \
  -p 8080:8080 -p 8443:8443 -v chefbot-data:/data -e TZ=America/Chicago \
  ghcr.io/jamiethenextkevinbaconhamm/chef-bot:latest
```

Set `TZ` to your [time zone](https://en.wikipedia.org/wiki/List_of_tz_database_time_zones), such as
`Europe/London`: ChefBot plans weeks, sends reminders and runs backups by local time. With Podman, use `podman`
in place of `docker`. Then open `http://<this computer's address>:8080`.

## With Docker Compose

Compose keeps the settings in a file, which makes changing and updating them easier. You need Docker with the
Compose plugin (`docker compose version` prints a version), or Podman with `podman compose`.

1. Make a folder for ChefBot and download the compose file into it:

   ```sh
   curl -O https://raw.githubusercontent.com/Jamiethenextkevinbaconhamm/chef-bot/main/deploy/compose/compose.yaml
   ```

   Or save this as `compose.yaml`:

   ```yaml
   services:
     chefbot:
       image: ghcr.io/jamiethenextkevinbaconhamm/chef-bot:latest
       restart: unless-stopped
       stop_grace_period: 30s
       environment:
         TZ: America/Chicago
       ports:
         - "8080:8080"
         - "8443:8443"
       volumes:
         - chefbot-data:/data   # the database, sign-in keys, HTTPS certificate and backups

   volumes:
     chefbot-data:
   ```

2. Set `TZ` to your [time zone](https://en.wikipedia.org/wiki/List_of_tz_database_time_zones).

3. Start it:

   ```sh
   docker compose up -d
   ```

4. Open `http://<this computer's address>:8080`. ChefBot sends you on to `https://…:8443`. The browser warns about
   the certificate the first time: choose to continue. See [HTTPS and certificates](../admin/https-and-certificates.md).

The setup wizard starts. Continue with [First run](first-run.md).

## Day-to-day commands

Run these in the folder that holds `compose.yaml`:

```sh
docker compose ps                  # is it running?
docker compose logs -f chefbot     # follow the log
docker compose restart chefbot     # after changing a setting
docker compose pull && docker compose up -d   # update to the latest release
docker compose down                # stop it (data is kept)
```

## Keep the volume

Everything ChefBot keeps is in `/data`: its database, the sign-in keys, the HTTPS certificate and the backups. Keep it
on a named volume (as above) or a host folder, so it survives replacing the container. Without it, an update starts
again from the setup wizard. Back it up, or download a backup now and then: see
[Backups](../admin/backups-and-restore.md).

Give the container time to stop: ChefBot stops its database cleanly when asked to stop. `stop_grace_period: 30s`
does that for Compose; with `docker stop`, add `-t 30`.

## Use a host folder instead of a volume

A folder on the host works too, but it must be writable by the container's user, id **1654**:

```sh
mkdir data
sudo chown 1654 data                 # Docker
podman unshare chown 1654 data       # rootless Podman
```

```yaml
    volumes:
      - ./data:/data
```

On Fedora and other systems with SELinux, add `:Z` to the mount (`./data:/data:Z`). If ChefBot can't write to the
folder, it stops at startup with a message that names the folder and says how to fix it.

## Use other ports

Change the left-hand number of each port. When HTTPS is published on another port, tell ChefBot, so its links and
redirects use it:

```yaml
    environment:
      Https__PublicPort: "443"
    ports:
      - "80:8080"
      - "443:8443"
```

## Other settings

Any setting in the [settings reference](../admin/settings.md#settings-in-the-environment) can be added under
`environment:`, with `__` between the parts of its name. Most settings are changed in ChefBot's web page instead.

## Build the image yourself

To run code that isn't released yet, or on a machine that can't reach GitHub's registry, build the image from the
repository and use it in place of the `ghcr.io/…` one:

```sh
git clone https://github.com/FreeReins/chef-bot.git
docker build -t chefbot:local chef-bot
```

```yaml
    image: chefbot:local
```

## If something goes wrong

See [Install and startup problems](../troubleshooting/install-and-startup.md).

## Next

[First run](first-run.md): create your account and plan your first week.
