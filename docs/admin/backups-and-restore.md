# Backups and restore

ChefBot backs itself up every night. This page explains what's in a backup, how to keep copies safe, and how to
restore one.

## What ChefBot does by itself

- Writes a backup every day at **03:00** and keeps the newest **14**.
- An admin changes the time, how many to keep, and a maximum age under **Settings → Backups**.
- Backups are compressed JSON files with all of ChefBot's own data: recipes and their photos, plans, the pantry,
  accounts, settings and to-do app tokens. API tokens for AI assistants aren't included.

## Make, download and delete backups

Under **Settings → Backups**:

- **Back up now** makes one straight away. Do this before upgrading.
- Each backup in the list can be opened to see what it holds, **downloaded**, **restored** or **deleted**.

## Keep a copy somewhere else

A backup on the same disk doesn't help if that disk fails. Either:

- download a backup now and then, and keep it somewhere safe; or
- keep the backups in a folder that is itself backed up, as below.

Backups contain password hashes and to-do app tokens, so keep downloaded copies private.

### Keep backups in a folder

**Podman:** create the folder and give it to ChefBot's user, then change the backups line in
`~/.config/containers/systemd/chefbot/chefbot.container` (and in `deploy/podman/chefbot.container`, so `install.sh`
keeps it):

```sh
mkdir ~/chefbot-backups
podman unshare chown 1654 ~/chefbot-backups
```

```ini
Volume=%h/chefbot-backups:/app/backups:Z
```

Then run `systemctl --user daemon-reload && systemctl --user restart chefbot`.

**Docker Compose:** see [Use host folders instead of volumes](../getting-started/install-docker-compose.md#use-host-folders-instead-of-volumes).

## Restore a backup

> [!WARNING]
> Restoring replaces **everything** in ChefBot with what the backup holds. Make a fresh backup first if you might
> want today's data back.

1. Open **Settings → Backups**.
2. To restore a backup you downloaded, put it back in the backups folder first, keeping its name
   (`chefbot-backup-….json.gz`). For a volume, copy it in: `podman cp chefbot-backup-….json.gz chefbot:/app/backups/`
   or `docker compose cp chefbot-backup-….json.gz chefbot:/app/backups/`. Then reload the page.
3. Press restore next to the backup and confirm.
4. Sign in again.

After a restore:

- **Everyone signs in again,** the admin who restored included.
- **Passwords go back** to what they were when the backup was made. Sessions from before the restore stop working.
- **AI assistants need new tokens.**

## Move ChefBot to another machine

1. On the old machine, **Back up now** and download the backup.
2. Install ChefBot on the new machine and create a temporary account in the setup wizard.
3. Copy the backup into the new backups folder and restore it.
4. Sign in with your old account.

Devices that trusted the old certificate need the new one. Or copy the keys volume across too, to keep it.

## If something goes wrong

See [Backup problems](../troubleshooting/backups.md).
