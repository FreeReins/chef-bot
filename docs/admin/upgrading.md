# Upgrading

This page explains how to move to a new version of ChefBot, and what to do when a version changes the database.

## Before every upgrade

Open **Settings → Backups**, press **Back up now**, and download the backup. It takes a minute and makes any upgrade
safe to undo.

## Upgrade

**Podman:**

```sh
cd chef-bot
git pull
deploy/podman/install.sh
```

The script rebuilds the image, runs the tests and restarts ChefBot. Your settings, data and password are kept.

**Docker Compose:**

```sh
docker compose pull
docker compose up -d
```

To stay on one version instead of `latest`, use a version tag such as
`ghcr.io/jamiethenextkevinbaconhamm/chef-bot:1.2` (that minor version's fixes) or `:1.2.3` (exactly that one).

Read the [release notes](https://github.com/Jamiethenextkevinbaconhamm/chef-bot/releases) first. They say when a
version needs the steps below.

## When a version changes the database

ChefBot creates its database tables on the first start, but doesn't upgrade them yet. A version that changes the
tables needs a fresh database, with your data brought across in a backup:

1. **On the old version:** **Settings → Backups → Back up now**, and download the backup.
2. **Stop ChefBot and delete only its database,** keeping the keys and backups:
   - **Podman:** `systemctl --user stop chefbot chefbot-db && podman volume rm chefbot-db`
   - **Docker Compose:** `docker compose down && docker volume rm <folder name>_chefbot-db`
     (`docker volume ls` shows the exact name)
3. **Start the new version** (`install.sh`, or `docker compose up -d`) and create a temporary first account in the
   setup wizard.
4. **Restore the backup** under **Settings → Backups**. Everything, including the accounts, is replaced by what the
   backup holds, so sign in again with your old account.

### Versions that needed this

- **Layered core and stored messages:** added the `ScheduleState` table and the `wolverine` schema.
- **Choosing stores:** added the `GroceryStores` table. Backups from before it have no stores, so choose yours again
  under **Settings → Stores**. Until you do, shopping lists are grouped by food group.
- **Planning settings in the web page:** people, the weekly schedule, leftovers, calendar times and the advanced
  planning settings moved from `chefbot.env` into the database. Set them again under **Settings → Planning**,
  **Calendar** and **Advanced**, and remove the old lines from `chefbot.env`. See the
  [settings reference](settings.md#settings-in-the-environment).
- **Scaling recipes and adding them to the plan:** added a column to the planned meals, for meals made for a chosen
  number of servings. Backups from before it restore as they are.
- **AI models, recipes from photos and recipe suggestions:** added the `AiModels`, `AiRoutes` and
  `RecipeSuggestions` tables. Backups from before it restore as they are, with no AI models set up.

## Go back to an older version

Install the older version the same way (`git checkout <tag>` then `install.sh`, or the older image tag), with a fresh
database if the tables changed, and restore a backup made on that version.

## If something goes wrong

See [Install and startup problems](../troubleshooting/install-and-startup.md).
