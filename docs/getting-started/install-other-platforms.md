# Install on other platforms

ChefBot runs anywhere containers do. Every platform below uses the same
[compose file](install-docker-compose.md#with-docker-compose) and the same rules, so read that page first. This one
says where each platform wants the file. On Windows you can also skip containers altogether: see
[Install on Windows](install-windows.md).

> [!NOTE]
> Menu names change between versions of these products. If yours look different, look for "Compose", "Stack",
> "Project" or "Install via YAML".

## What every platform needs

- **One container:** `ghcr.io/jamiethenextkevinbaconhamm/chef-bot:latest`. Its database runs inside it.
- **One place to keep data:** `/data`. Use a named volume, or a host folder owned by user id **1654**.
- **Two ports:** 8080 (HTTP) and 8443 (HTTPS). If you change the HTTPS one, set `Https__PublicPort`.
- **`TZ`** set to your time zone.

## Docker Desktop (Windows and macOS)

On Windows, [the ChefBot installer](install-windows.md) is simpler: it needs no Docker and starts with Windows.

1. Install [Docker Desktop](https://www.docker.com/products/docker-desktop/).
2. Make a folder, save the compose file in it as `compose.yaml`, and set `TZ`.
3. Open a terminal in that folder and run `docker compose up -d`.
4. Open <http://localhost:8080>.

To reach it from your phone, use the computer's address on your network, and allow ports 8080 and 8443 through the
computer's firewall when asked. ChefBot only runs while the computer is on and Docker Desktop is running.

## Podman Desktop (Windows, macOS and Linux)

Install [Podman Desktop](https://podman-desktop.io) and its Compose extension, then run `podman compose up -d` in the
folder with `compose.yaml`. On Linux with systemd, [Install with Podman](install-podman.md) is the better choice: it
starts ChefBot at boot and restarts it if it stops.

## Synology (Container Manager)

1. Install **Container Manager** from the Package Center.
2. In **File Station**, make a folder such as `docker/chefbot`.
3. In Container Manager, open **Project → Create**. Name it `chefbot`, choose the folder, choose **Create
   docker-compose.yml**, and paste the compose file. Set `TZ`.
4. Finish the wizard. It downloads the image and starts the container.
5. Open `http://<your NAS>:8080`.

If 8080 or 8443 is already used on the NAS, change the left-hand port numbers in the compose file.

## UGREEN NAS (UGOS Pro)

1. Install **Docker** from the **App Center**.
2. In **Files**, make a folder such as `docker/chefbot`.
3. Open Docker, go to **Project** and press **Create**. Name it `chefbot`, choose the folder as its storage path, and
   paste the compose file. Set `TZ`.
4. Press **Deploy now**. Docker downloads the image and starts the container.
5. Open `http://<your NAS>:8080`.

If 8080 or 8443 is already used on the NAS, change the left-hand port numbers in the compose file. To keep the data
in a folder under `docker/chefbot` instead of a named volume, see
[Use a host folder instead of a volume](install-docker-compose.md#use-a-host-folder-instead-of-a-volume): the folder
must be writable by user id 1654.

To reach ChefBot away from home, UGOS Pro's **App Center** also has Tailscale. See
[Reach ChefBot over Tailscale](../admin/tailscale.md).

## Unraid

The simplest way is the **Docker Compose Manager** plugin from Community Applications:

1. Install **Docker Compose Manager**.
2. Under **Docker → Compose**, add a stack called `chefbot`, edit its compose file, and paste the compose file. Change
   `TZ`.
3. Press **Compose Up**.

To keep the data in your appdata share, replace the named volume with a folder,
`/mnt/user/appdata/chefbot:/data`, and run `chown 1654 /mnt/user/appdata/chefbot` once from the terminal.

## TrueNAS SCALE

On TrueNAS SCALE 24.10 or later:

1. Open **Apps → Discover Apps**, then the menu next to **Custom App**, and choose **Install via YAML**.
2. Name it `chefbot` and paste the compose file. Set `TZ`.
3. Save. TrueNAS starts the container.

For a dataset instead of the volume, mount it at `/data` and make it writable by user id 1654.

## Portainer

1. Open **Stacks → Add stack**.
2. Name it `chefbot`, choose **Web editor**, and paste the compose file. Set `TZ`.
3. Press **Deploy the stack**.

## Raspberry Pi

Use a **64-bit** system, such as Raspberry Pi OS (64-bit), on a Raspberry Pi 4 or 5.

1. Install Docker: `curl -fsSL https://get.docker.com | sh`, then `sudo usermod -aG docker $USER` and log in again.
2. Follow [Install with Docker Compose](install-docker-compose.md).

An SD card can wear out under a database. Keep the data on an SSD if you can, and
[download backups](../admin/backups-and-restore.md) regularly.

## Proxmox

Run ChefBot in a small Debian or Ubuntu VM, or an LXC container with nesting turned on. Install Docker inside it,
then follow [Install with Docker Compose](install-docker-compose.md).

## After installing

Continue with [First run](first-run.md). For problems, see
[Install and startup problems](../troubleshooting/install-and-startup.md). To reach ChefBot away from home, see
[Reach ChefBot over Tailscale](../admin/tailscale.md) or [Behind a reverse proxy](../admin/reverse-proxy.md).

Got ChefBot running on a platform that isn't here, or found a step that has changed?
[Suggest an edit](https://github.com/FreeReins/chef-bot/edit/main/docs/getting-started/install-other-platforms.md).
