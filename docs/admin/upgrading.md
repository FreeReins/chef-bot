# Upgrading

This page explains how to move to a new version of ChefBot, and what to do when a version changes the database.

## Before every upgrade

Open **Settings → Backups**, press **Back up now**, and download the backup. It takes a minute and makes any upgrade
safe to undo.

## Upgrade

**Podman:** nothing to do. `podman-auto-update.timer`, which `install.sh` turns on, checks once a day and restarts
ChefBot with a new image. To update now:

```sh
podman auto-update
```

If you installed with `install.sh --build`, `git pull` in the clone and run `deploy/podman/install.sh --build` again.

**Docker Compose:**

```sh
docker compose pull
docker compose up -d
```

**Windows:** download the new `ChefBot-….msi` from the
[releases](https://github.com/FreeReins/chef-bot/releases) and run it. It replaces the old version
and keeps `C:\ProgramData\ChefBot`. For the portable app, replace the `.exe` and keep the `ChefBot-data` folder
next to it.

To stay on one version of the image instead of `latest`, use a version tag such as
`ghcr.io/freereins/chef-bot:1.2` (that minor version's fixes) or `:1.2.3` (exactly that one).

Read the [release notes](https://github.com/FreeReins/chef-bot/releases) first. They say when a
version needs the steps below.

## When a version changes the database

ChefBot upgrades its database by itself when a new version starts: there's nothing to do. A database made before
ChefBot did this is recognised and upgraded too. Make a backup first all the same (**Settings → Backups → Back up
now**), in case you want to go back.

The versions listed below came before that, and needed a fresh database with your data brought across in a backup.
These steps still move a database to a new major version of PostgreSQL:

1. **On the old version:** **Settings → Backups → Back up now**, and download the backup.
2. **Stop ChefBot and delete only its database,** the `database` folder in the data folder, keeping the keys and
   backups:
   - **Podman:** `systemctl --user stop chefbot`, then
     `podman run --rm -v chefbot-data:/data --entrypoint rm ghcr.io/freereins/chef-bot -rf /data/database`
   - **Docker Compose:** `docker compose down`, then
     `docker compose run --rm --entrypoint rm chefbot -rf /data/database`
   - **Windows:** stop the **ChefBot** service, then delete `C:\ProgramData\ChefBot\database`
3. **Start the new version** (`systemctl --user start chefbot`, `docker compose up -d`, or start the service) and
   create a temporary first account in the setup wizard.
4. **Restore the backup** under **Settings → Backups**. Everything, including the accounts, is replaced by what the
   backup holds, so sign in again with your old account.

The same steps move a database to a new major version of PostgreSQL. ChefBot comes with PostgreSQL 17, and only
moves to a newer one in a release that says so; if it ever finds a database made by another version, it stops and
says so rather than touching it.

### Versions that needed this

From the version that added the bell's messages (the `InboxEntries` table) on, upgrades apply database changes by
themselves.

- **API token limits:** added the `ExpiresAt`, `LastUsedAt` and `ReadOnly` columns to the `ApiTokens` table. Existing
  tokens last until revoked and can make changes, as before. AI models' API keys are encrypted the first time
  ChefBot reads them.

- **One line per ingredient:** added the `ExtraAmounts` column to the `ShoppingItems` and `ManualShoppingItems`
  tables. At startup, saved recipes' ingredients are filed under the catalog's current names (shallots no longer as
  onion), and the open plans' shopping lists follow. Pantry items and hand-added items saved before keep their names.

- **One container:** ChefBot runs its own PostgreSQL, keeping everything in one `/data` volume (or
  `C:\ProgramData\ChefBot`). Installs from before it used a separate database container and three volumes: back up,
  install afresh, and restore the backup.

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
- **Cookbooks:** added the `Cookbooks` table, and a column on recipes for the cookbooks each one is in. Backups from
  before it restore as they are, with no cookbooks.

## Go back to an older version

Install the older version the same way (the older image tag, or the older MSI after uninstalling the newer one),
with a fresh database if the tables changed, and restore a backup made on that version. An older version doesn't
undo a newer one's database changes, so going back always needs the fresh database and backup.

## If something goes wrong

See [Install and startup problems](../troubleshooting/install-and-startup.md).
