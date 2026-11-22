# Backup problems

## "The backup couldn't be written"

The message after it says why:

- **Permission denied:** the backups folder isn't writable by user 1654. See
  [ChefBot can't write to a folder](install-and-startup.md#chefbot-cant-write-to-a-folder).
- **No space left on device:** free some space, or keep fewer backups under **Settings → Backups**. Photos make
  backups bigger.

## No backups are made

Check **Back up automatically every day** and the time under **Settings → Backups**. The last result is shown there. If
ChefBot was stopped at that time, the backup runs when ChefBot starts again that day.

## "This backup couldn't be read"

The file is damaged or isn't a ChefBot backup. Download it to check, or delete it and use another.

## A downloaded backup doesn't show in the list

ChefBot lists files named `chefbot-backup-….json.gz` in its backups folder. Copy it back in with that name. See
[Restore a backup](../admin/backups-and-restore.md#restore-a-backup).

## Signed out after restoring

Expected: everyone signs in again, with the passwords from the time of the backup.

## AI assistants stopped working after restoring

Tokens aren't kept in backups. Make new ones under **Settings → AI assistants**.

## Stores are missing after restoring an old backup

Backups from before stores could be chosen have none. Choose them again under **Settings → Stores**.
