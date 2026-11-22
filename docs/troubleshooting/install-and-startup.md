# Install and startup problems

## The page doesn't open

1. **Check it's running:** `systemctl --user status chefbot chefbot-db` or `docker compose ps`. If it isn't, read the
   log (`journalctl --user -u chefbot -n 50` or `docker compose logs chefbot`): the last lines say why it stopped.
2. **Check the address.** From the machine itself, try <http://localhost:8080>. From another device, use the
   machine's network address, such as `http://192.168.1.20:8080`.
3. **Check the firewall.** On Fedora: `sudo firewall-cmd --add-port=8080/tcp --add-port=8443/tcp --permanent && sudo
   firewall-cmd --reload`. On Windows or macOS, allow Docker through when asked.
4. **It stops when you log out (Podman):** run `loginctl enable-linger $USER`.

## A port is already in use

The log or `podman`/`docker` says the address is already in use. Something else has 8080 or 8443. Publish ChefBot on
other ports: [Podman](../getting-started/install-podman.md#change-the-ports) or
[Docker Compose](../getting-started/install-docker-compose.md#use-other-ports).

## ChefBot can't write to a folder

> ChefBot can't write to '/app/backups' (Data:Backups): … It runs as user 1654 …

A host folder mounted into the container belongs to someone else. Give it to ChefBot's user:

```sh
sudo chown 1654 <folder>              # Docker
podman unshare chown 1654 <folder>    # rootless Podman
```

On Fedora and other SELinux systems, also add `:Z` to the mount. Named volumes need none of this.

## The database rejects the password

The log says `password authentication failed for user "chefbot"`.

PostgreSQL only takes the password when the database is first created. If the password changed since (a new Podman
secret, or a changed compose file), it no longer matches.

- **Put the old password back,** if you know it.
- **Or start again with a fresh database,** keeping your data in a backup: see
  [When a version changes the database](../admin/upgrading.md#when-a-version-changes-the-database). With Podman,
  `uninstall.sh --delete-data` removes the secret and the database together.

## The database isn't ready

ChefBot waits for PostgreSQL to accept connections. If the database container keeps restarting, read its log
(`journalctl --user -u chefbot-db` or `docker compose logs db`). Common causes: the disk is full, or the database
volume belongs to a different PostgreSQL version.

## "Set the ConnectionStrings__ChefBot setting"

ChefBot was started without a database connection string. Add `ConnectionStrings__ChefBot` (see the
[compose file](../getting-started/install-docker-compose.md#install)).

## A setting is wrong

ChefBot stops at startup with a message that names the setting, such as:

- `"…" in Https:TrustedProxies isn't an IP address (10.0.0.5) or a network (172.16.0.0/12).`
- `Https:Mode is Certificate, but the file in Https:CertificatePath wasn't found: … In a container, check that it is
  mounted.`

Fix the setting in `chefbot.env` or `compose.yaml` and start it again.

## The build fails (Podman install)

`install.sh` builds the image and runs the tests first. If a step fails, the output above the error says which. Make
sure you have the latest code (`git pull`), enough disk space (`podman system df`), and run it again.

## The web page looks broken after an upgrade

Reload the page. If it still looks wrong, clear the site's data in the browser, or sign out and in again.

## "The web page hasn't been built yet"

You're running ChefBot from source without building the page. Build it once with `./dev.sh web`. The container image
always includes it.
