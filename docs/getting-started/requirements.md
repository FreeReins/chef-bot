# What you need

This page lists what ChefBot needs to run, so you can choose where to install it.

## A machine to run it on

ChefBot is one container, or one Windows program, with its PostgreSQL database inside. Any always-on computer on
your home network works: a home server, a NAS, a mini PC, a Raspberry Pi 4 or 5 with a 64-bit system, or the
desktop you use every day.

- **Processor:** 64-bit x86 (`amd64`) or 64-bit ARM (`arm64`). 32-bit systems aren't supported.
- **Memory and disk:** small. The app and database fit comfortably on hardware that runs a few other home services.
  Backups grow with your recipe photos: each photo can add up to 5 MB.
- **Software**, one of these:
  - Linux with **Podman 5 or later and systemd**, such as Fedora, Bazzite or another Fedora Atomic desktop. This is
    the main supported setup. See [Install with Podman](install-podman.md).
  - **Docker** or Podman, on Linux, Windows, macOS or a NAS. See [Install with Docker](install-docker-compose.md).
  - **Windows 10 or 11** (64-bit x86), with no containers at all. See [Install on Windows](install-windows.md).

## Network

| Port | Used for |
|---|---|
| `8080` | Plain HTTP. Sends visitors on to HTTPS once **Always use HTTPS** is on |
| `8443` | HTTPS, with a certificate ChefBot makes itself |
| `54329` | The database, on the same computer only (127.0.0.1). Nothing outside can reach it. Change it with `Database__Port` if something else uses it |

You can publish them on other ports on the host. See
[Use other ports](install-docker-compose.md#use-other-ports).

ChefBot is meant for a home network. To reach it from outside, put it behind a reverse proxy with a real
certificate. See [Behind a reverse proxy](../admin/reverse-proxy.md).

ChefBot itself only goes out to the internet when it needs to: to download a recipe page you paste a link to, that
recipe's photos, and the webhook, to-do apps and calendar apps you set up.

## Storage that must be kept

Everything ChefBot keeps is in one folder: `/data` in the container (the `chefbot-data` volume), or
`C:\ProgramData\ChefBot` on Windows. Keep it, and keep a copy of the backups somewhere else too.

| In the data folder | Holds | If it's lost |
|---|---|---|
| `database` | Everything: recipes, plans, pantry, accounts and settings, and the database's own password | All data is gone. Restore a backup |
| `keys` | Sign-in keys and the HTTPS certificate | Everyone is signed out, and every device has to accept or install the new certificate |
| `backups` | Daily backups | Only the backups |

## Browsers and phones

Any current browser: Firefox, Chrome, Edge or Safari, on a computer or a phone. To use the shopping list with no
signal, the phone needs ChefBot over HTTPS with a certificate it trusts. See
[Offline on your phone](../using/offline-on-your-phone.md).

## Next

- [Install with Podman](install-podman.md) on Linux with systemd
- [Install on Windows](install-windows.md) with the installer or the portable app
- [Install with Docker](install-docker-compose.md) anywhere else
- [Other platforms](install-other-platforms.md): Synology, Unraid, TrueNAS, Portainer, Docker Desktop, macOS
