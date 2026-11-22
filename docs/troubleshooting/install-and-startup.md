# Install and startup problems

## The page doesn't open

1. **Check it's running:** `systemctl --user status chefbot`, `docker compose ps`, or the **ChefBot** service in the
   Windows **Services** app. If it isn't, read the log (`journalctl --user -u chefbot -n 50`,
   `docker compose logs chefbot`, or **Event Viewer** on Windows): the last lines say why it stopped.
2. **Check the address.** From the machine itself, try <http://localhost:8080>. From another device, use the
   machine's network address, such as `http://192.168.1.20:8080`.
3. **Check the firewall.** On Fedora: `sudo firewall-cmd --add-port=8080/tcp --add-port=8443/tcp --permanent && sudo
   firewall-cmd --reload`. On macOS, or Windows with Docker, allow Docker through when asked. The ChefBot installer
   for Windows opens 8080 and 8443 on private networks only: if Windows calls your home network **Public**, change it
   to **Private** in **Settings → Network & internet**.
4. **It stops when you log out (Podman):** run `loginctl enable-linger $USER`.

## A port is already in use

The log or `podman`/`docker` says the address is already in use. Something else has 8080 or 8443. Publish ChefBot on
other ports: [Podman](../getting-started/install-podman.md#change-the-ports) or
[Docker Compose](../getting-started/install-docker-compose.md#use-other-ports).

If the bundled database's port is the one in use, ChefBot says its PostgreSQL didn't start, and the end of the
database log mentions `could not bind` or `Address already in use`: set `Database__Port` to another port. See the
[settings reference](../admin/settings.md#settings-in-the-environment).

## ChefBot can't write to a folder

> ChefBot can't write to '/data/backups' (Data:Backups): … It runs as user 1654 …

A host folder mounted into the container belongs to someone else. Give it to ChefBot's user:

```sh
sudo chown 1654 <folder>              # Docker
podman unshare chown 1654 <folder>    # rootless Podman
```

On Fedora and other SELinux systems, also add `:Z` to the mount. Named volumes need none of this.

## "ChefBot's bundled PostgreSQL didn't start"

ChefBot runs its own database and stops if it can't start it. The message ends with the last lines of the database's
log, which say why. Common causes: the disk is full, the data folder can't be written (see above), or the database
port is in use (see [A port is already in use](#a-port-is-already-in-use)).

## "The database … was made by PostgreSQL N"

The data folder holds a database from another major version of PostgreSQL than the one this ChefBot comes with.
ChefBot doesn't touch it. Go back to the ChefBot version you had, or move to the new one with a backup: see
[When a version changes the database](../admin/upgrading.md#when-a-version-changes-the-database).

## "ChefBot's database password file … is missing"

The `database/password` file in the data folder was deleted or not copied with the rest. Put it back from wherever
the data folder came from. Without it, start with a fresh database and restore a backup, as in
[When a version changes the database](../admin/upgrading.md#when-a-version-changes-the-database).

## "… has files in it but isn't a PostgreSQL database"

The first start was interrupted while it was creating the database. If ChefBot has never run properly with this data
folder, delete its `database` folder and start ChefBot again.

## A setting is wrong

ChefBot stops at startup with a message that names the setting, such as:

- `"…" in Https:TrustedProxies isn't an IP address (10.0.0.5) or a network (172.16.0.0/12).`
- `Https:Mode is Certificate, but the file in Https:CertificatePath wasn't found: … In a container, check that it is
  mounted.`

Fix the setting in `chefbot.env` or `compose.yaml` and start it again.

## The build fails (`install.sh --build`)

If a step fails, the output above the error says which. Make sure you have the latest code (`git pull`), enough disk
space (`podman system df`), and run it again. Without `--build`, `install.sh` downloads the published image instead.

## Windows says it protected your PC

Windows SmartScreen warns about programs it hasn't seen often. Press **More info**, check the publisher and file name,
then **Run anyway**. See [Install on Windows](../getting-started/install-windows.md).

## The web page looks broken after an upgrade

Reload the page. If it still looks wrong, clear the site's data in the browser, or sign out and in again.

## "The web page hasn't been built yet"

You're running ChefBot from source without building the page. Build it once with `./dev.sh web`. The container image
always includes it.
