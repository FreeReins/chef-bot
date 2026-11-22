# Install with Docker Compose

This guide runs ChefBot with Docker Compose. It works anywhere Docker or Podman does: Linux, Windows, macOS and most
NAS systems. You need Docker with the Compose plugin (`docker compose version` prints a version), or Podman with
`podman compose`.

Each release is published as a container image for 64-bit x86 and ARM at
`ghcr.io/jamiethenextkevinbaconhamm/chef-bot`.

## Install

1. Make a folder for ChefBot and save this as `compose.yaml` in it:

   ```yaml
   services:
     db:
       image: postgres:17-alpine
       restart: unless-stopped
       environment:
         POSTGRES_DB: chefbot
         POSTGRES_USER: chefbot
         POSTGRES_PASSWORD: change-me
       volumes:
         - chefbot-db:/var/lib/postgresql/data
       healthcheck:
         test: ["CMD-SHELL", "pg_isready -h 127.0.0.1 -U chefbot -d chefbot"]
         interval: 5s
         retries: 12

     chefbot:
       image: ghcr.io/jamiethenextkevinbaconhamm/chef-bot:latest
       restart: unless-stopped
       depends_on:
         db:
           condition: service_healthy
       environment:
         ConnectionStrings__ChefBot: Host=db;Port=5432;Database=chefbot;Username=chefbot;Password=change-me
         TZ: America/Chicago
       ports:
         - "8080:8080"
         - "8443:8443"
       volumes:
         - chefbot-keys:/app/keys        # sign-in keys and the HTTPS certificate
         - chefbot-backups:/app/backups  # daily backups

   volumes:
     chefbot-db:
     chefbot-keys:
     chefbot-backups:
   ```

2. Change two things before the first start:
   - **The password.** Replace `change-me` in both places with the same long random password. PostgreSQL only
     takes it when the database is first created, so changing it later means changing it inside the database too.
   - **`TZ`.** Set it to your [time zone](https://en.wikipedia.org/wiki/List_of_tz_database_time_zones), such as
     `Europe/London`. ChefBot plans weeks, sends reminders and runs backups by local time.

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
docker compose logs -f chefbot     # follow the app's log
docker compose restart chefbot     # after changing a setting
docker compose pull && docker compose up -d   # update to the latest release
docker compose down                # stop it (data is kept)
```

## Keep the volumes

- **Keep `/app/keys` on a named volume.** Without it, replacing the container makes a new certificate that every
  browser has to accept again, and signs everyone out.
- **Keep `/app/backups` somewhere that's backed up**, or download a backup now and then. See
  [Backups](../admin/backups-and-restore.md).

## Use host folders instead of volumes

A folder on the host works too, but it must be writable by the container's user, id **1654**:

```sh
mkdir keys backups
sudo chown 1654 keys backups                 # Docker
podman unshare chown 1654 keys backups       # rootless Podman
```

```yaml
    volumes:
      - ./keys:/app/keys
      - ./backups:/app/backups
```

On Fedora and other systems with SELinux, add `:Z` to each mount (`./keys:/app/keys:Z`). If ChefBot can't write to a
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
git clone https://github.com/Jamiethenextkevinbaconhamm/chef-bot.git
docker build -t chefbot:local chef-bot
```

```yaml
    image: chefbot:local
```

## If something goes wrong

See [Install and startup problems](../troubleshooting/install-and-startup.md).

## Next

[First run](first-run.md): create your account and plan your first week.
