# ChefBot

ChefBot plans a week of meals, snacks and sweets around three things: ingredients the meals can share,
what is in season, and what is already in your kitchen and about to expire. It then builds the shopping
list, tracks what you bought with expiration dates, and helps you change the plan mid-week to use food up.

It is a self-hosted web app: one container for the app, one for PostgreSQL.

## What it does

- **Stages a plan.** Three to five dinners (at least one slow cooker meal), weekday lunches from leftovers,
  optional weekday breakfasts, plus snacks and sweets. A staged plan is a proposal: shuffle it, swap meals,
  then confirm it.
- **Shows it three ways.** *Meals* lists what was chosen. *Schedule* lays it out day by day.
  *Shopping* shows everything the plan needs, where each item comes from (to buy, pantry, staple)
  and which meals use it.
- **Builds the shopping list.** Quantities are totalled and scaled to portions, staples are left off,
  pantry stock is subtracted, and the list is split between the stores you shop at, or grouped by food group
  if you haven't chosen any.
- **Tracks the pantry.** Marking an item as bought puts it in the pantry with an expiration date, estimated
  from the ingredient's shelf life unless you enter the date on the package. The *Pantry* tab lists what you
  have, which planned meals will use each item, and what no meal uses yet.
- **Swaps meals after shopping.** For any meal, ChefBot suggests recipes ranked by how much of the pantry they
  use, favoring food that expires soonest, and invents simple plates from what you have:
  a whole grain, a protein and a vegetable or fruit. Choosing one updates the plan, the leftover lunches
  and the shopping list together.
- **Keeps its own recipes.** Recipes live in ChefBot's database: their ingredients, steps, prep and cook times,
  description, tags, notes, source link and photos. Add them in the web page, or import them from
  [Mealie](https://mealie.io). Mealie is optional and only ever read from. Imported photos are downloaded once,
  in the background, so they still show when Mealie or the recipe's website is down.
- **Has accounts.** A setup wizard creates the first (admin) account, who can invite the rest of the
  household with single-use links. Everyone shares the same recipes, plan and pantry.
- **Runs on a schedule.** Next week's plan is staged automatically (Saturday 09:00 by default).
- **Sends reminders.** A webhook, for example into Home Assistant, gets a message when a plan is ready, on the
  morning of a slow cooker meal, when food is about to expire, and when shopping is still to do.
- **Backs itself up.** A daily backup with retention you choose, and one-click restore.

## Quick start

You need Linux with Podman 5 or later and systemd, as on Fedora, Bazzite and other Fedora Atomic desktops.
Nothing else. ChefBot runs as rootless [Quadlet](https://docs.podman.io/en/latest/markdown/podman-systemd.unit.5.html)
services: ordinary systemd user services that start the containers, restart them if they stop, and log to
the journal.

```sh
deploy/podman/install.sh
loginctl enable-linger $USER    # optional: keep ChefBot running after logout and start it at boot
```

The script:

1. Builds the image from this repository, after running the tests.
2. Creates a random database password as the podman secret `chefbot-db-password`.
3. Copies [chefbot.env.example](deploy/podman/chefbot.env.example) to `~/.config/chefbot/chefbot.env`, the
   settings file, unless it already exists.
4. Installs the units from [deploy/podman](deploy/podman) into `~/.config/containers/systemd/chefbot/`
   and starts them.

Run it again after pulling changes to rebuild and restart. Your settings, data and password are kept.

Day to day:

```sh
systemctl --user status chefbot          # is it running?
systemctl --user restart chefbot         # after editing chefbot.env
journalctl --user -u chefbot -f          # app logs (chefbot-db for the database)
systemctl --user stop chefbot-pod        # stop the app and the database
```

To remove ChefBot:

```sh
deploy/podman/uninstall.sh                # stop it and remove its services; keep its data
deploy/podman/uninstall.sh --delete-data  # also delete its data, password, settings and image (asks first)
```

The first form tears down the pod and its containers, including one started by hand, and removes the units
from `~/.config/containers/systemd/chefbot/`. It keeps the `chefbot-db`, `chefbot-keys` and `chefbot-backups`
volumes, the `chefbot-db-password` secret, `chefbot.env` and the image, so `install.sh` brings ChefBot back as
it was. `--delete-data` removes those too; download a backup from **Settings → Backups** first if you want
one. It deletes the secret and the database together because PostgreSQL only takes the password when the
database is first created, so a new secret wouldn't match an old database.

Open <http://localhost:8080>. The first visit starts the setup wizard:

1. **Create your account.** This first account is the admin.
2. **Secure connection.** Shows the HTTPS address, lets you name the certificate for the address you use,
   download it, and choose to always use HTTPS.
3. **Add your recipes.** Pick one or both:
   - **Import my recipes** from a recipe file (JSON in ChefBot's own shape, the `database.json` from a Mealie
     backup .zip, a Mealie recipe export or schema.org recipes from a website), as many files as you like, or connect Mealie once and import from it.
   - **Start with sample recipes**: tick the ones you want from the 22 samples. You can come back to this
     list later from the Recipes tab.

   Or skip the step and add recipes later.

Then press **Stage next week**.

To use your own recipes, an admin enters Mealie's address and an API token under **Settings → Mealie**, then
presses **Import from Mealie** on the Recipes tab. `Mealie__Url` and `Mealie__ApiToken` in `chefbot.env` can
set the starting values instead; anything saved on the Settings tab takes their place.

### Adding people

On the **People** tab an admin presses **Create invite link** and sends the link to someone. The link lets
one person create an account, works once and expires after 7 days. A link is shown only once: if it
wasn't copied, press **Regenerate** next to it, which stops the old link working and shows a new one.
Unused links can also be revoked, and accounts removed, from the same tab.

### Passwords and lockout

- **Change your own password** on the Settings tab. Your other devices are signed out.
- **Forgot it?** An admin presses **Reset password** next to your name on the People tab and sends you the
  link. It works once and expires after 24 hours.
- **The admin forgot theirs?** Set `Accounts__RecoverUsername` in `chefbot.env`, restart, and read the reset
  link in `journalctl --user -u chefbot`. Clear the setting afterwards.
- **Lockout.** After 5 wrong passwords in a row an account is locked for 15 minutes. An admin can change both
  numbers on the Settings tab (0 attempts turns lockout off) and can unlock someone early on the People tab.

### HTTPS

With the provided Podman setup, ChefBot serves HTTPS on port 8443 with a certificate it creates itself,
next to HTTP on 8080. Nothing needs to be installed or issued.

- The first visit to `https://your-host:8443` shows a browser warning, because nobody but ChefBot vouches for
  the certificate. Choose to continue. To remove the warning for good, download the certificate (setup wizard
  or Settings tab) and add it to your device's trusted certificates.
- For that to work the certificate must name the address you use. Enter it in the wizard or on the Settings
  tab, or set `Https__Hostnames=chefbot.local,192.168.1.20` in `chefbot.env`. A new certificate is used
  immediately, no restart.
- **Always use HTTPS** (wizard or Settings) sends plain HTTP visitors to the HTTPS address.
- The certificate is kept in the `chefbot-keys` volume, lasts about two years and is renewed in its last
  30 days. A renewed certificate has to be accepted or installed again.
- To use your own certificate, or to run behind a reverse proxy, see
  [Your own certificate](#your-own-certificate) and [Behind a reverse proxy](#behind-a-reverse-proxy).

## Running the container

Every release is published as a container image, so ChefBot runs anywhere Docker or Podman does. It needs a
PostgreSQL database next to it. The image serves HTTP on port 8080 and HTTPS on 8443, with a certificate it
makes itself.

```yaml
# compose.yaml: then run `docker compose up -d` and open https://your-host:8443
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

- **Keep the two volumes.** Without `/app/keys` on a named volume, replacing the container makes a new
  certificate that every browser has to accept again, and signs everyone out.
- **Host folders instead of volumes** must be writable by the container's user, id 1654:
  `sudo chown 1654 ./keys ./backups` with Docker, `podman unshare chown 1654 ./keys ./backups` with rootless
  Podman (add `:Z` to the mount on Fedora and other SELinux systems). ChefBot stops at startup with a message
  naming the folder if it can't write there.
- **Other ports on the host:** when HTTPS is published on a different port, for example `"443:8443"`, set
  `Https__PublicPort` to that port (`443`). ChefBot uses it in links and when sending plain HTTP to HTTPS.
- Every other setting in [Configuration](#configuration) can be given the same way, with `__` between levels.

### Your own certificate

```yaml
    environment:
      Https__Mode: Certificate
      Https__CertificatePath: /certs/fullchain.pem
      Https__KeyPath: /certs/privkey.pem
    volumes:
      - /srv/chefbot/certs:/certs:ro
```

- **Use the full chain.** A PEM file holding the certificate followed by its intermediates (Let's Encrypt's
  `fullchain.pem`) is served whole, which phones, `curl` and Home Assistant need. A `.pfx` file works too,
  with `Https__CertificatePassword` if it has one.
- **Renewals are picked up by themselves.** ChefBot checks the files every minute and switches to a renewed
  certificate without a restart. A half-written file is ignored until it is complete.
- **The key must be readable by user 1654.** Let's Encrypt keys are readable by root only, so copy both
  files to the mounted folder from a certbot deploy hook, giving ChefBot its own copy of the key:
  ```sh
  install -m 644 "$RENEWED_LINEAGE/fullchain.pem" /srv/chefbot/certs/fullchain.pem
  install -m 600 -o 1654 "$RENEWED_LINEAGE/privkey.pem" /srv/chefbot/certs/privkey.pem
  ```
  With rootless Podman, user 1654 inside the container is a different id on the host; run
  `podman unshare chown 1654 /srv/chefbot/certs/privkey.pem` after copying instead of `-o 1654`.

### Behind a reverse proxy

When Caddy, Traefik, nginx or similar handles HTTPS, let it talk to ChefBot over plain HTTP:

```yaml
    environment:
      Https__Mode: "Off"
      Https__TrustedProxies: 172.16.0.0/12   # the proxy's address or network
```

- **`Https__TrustedProxies`** lists the proxies whose `X-Forwarded-For` and `X-Forwarded-Proto` headers ChefBot
  believes: addresses or networks, separated by commas. With it, ChefBot knows visitors arrived over HTTPS, so
  sign-in cookies are marked secure and the Settings page says HTTPS is handled by the proxy. Leave it empty
  when nothing sits in front of ChefBot, or anyone could claim to be on HTTPS.
- **Leave "Always use HTTPS" off.** Redirecting is the proxy's job.
- Point the proxy at port 8080. Port 8443 isn't used in this mode.

### Upgrading

The database isn't upgraded automatically yet, so a version that changes its tables needs a fresh database.
The version that added the layered core and stored messages is one: it adds a `ScheduleState` table and the
`wolverine` schema. Your data comes across in a backup:

1. On the old version, open Settings, Backups and press *Back up now*. Download the backup too, to be safe.
2. Stop ChefBot and delete its database volume, keeping the keys and backups volumes. With the Podman setup:
   `systemctl --user stop chefbot chefbot-db && podman volume rm chefbot-db`. With compose:
   `docker compose down && docker volume rm <project>_chefbot-db`.
3. Start the new version and create a temporary first account in the setup wizard.
4. Open Settings, Backups and restore the backup. Everything, including the accounts, is replaced by what the
   backup holds, so sign in again with your old account.

The version that added choosing stores is another: it adds a `GroceryStores` table. Backups from before it
have no stores, so after restoring one, choose yours under Settings, Stores. Until you do, shopping lists are
grouped by food group. The `Stores` setting is no longer read.

## A week with ChefBot

1. **Saturday morning** the schedule stages next week and posts a summary to your webhook.
2. **Review** the Meals tab. Press *Shuffle* for a different week, or *Swap* on a single meal.
3. **Confirm** the plan. The Shopping tab is now your shopping list.
4. **Shop**, then press *Bought* per item or *Mark all as bought*. Purchases move to the pantry with
   expiration dates.
5. **During the week**, mark meals *Cooked* on the Schedule tab; their ingredients come out of the pantry.
   Plans changed? *Swap* a meal: suggestions that use up expiring food come first.

Two buttons at the top change the whole plan:

- **Regenerate from pantry** replaces the plan with a new one built around what is in the pantry, which is
  the one to use after shopping: recipes that use food you already bought are strongly preferred, and the new
  shopping list only holds what is still missing. The whole week is replaced, including days already past.
- **Clear plan** removes the plan and its shopping list. The pantry is kept.

## API

The web page is a thin client over a JSON API, so anything it does can be automated.
The full description is served at `/openapi/v1.json`, and `/docs` shows it as a Swagger page where you can
try each call. Both need you to be signed in.

| Method and path | What it does |
|---|---|
| `POST /api/plans/stage` | Stage a week. Body `{}` for the upcoming week, or `{"weekStart": "2026-11-08", "replace": true}` |
| `GET /api/plans/current` | The plan for this week, or the next upcoming one |
| `POST /api/plans/{planId}/confirm` | Make a staged plan the working plan |
| `GET /api/plans/{planId}/ingredients` | Everything the plan needs, plus the pantry with the meals that use each item (`usedFor`) |
| `GET /api/plans/{planId}/meals/{mealId}/alternatives` | Recipe and creative suggestions for one meal |
| `PUT /api/plans/{planId}/meals/{mealId}` | Replace a meal: `{"recipeId": "..."}` or `{"custom": {"title": "...", "ingredients": ["..."]}}` |
| `POST /api/plans/{planId}/meals/{mealId}/status` | `{"status": "Cooked"}`, `"Skipped"` or `"Planned"` |
| `POST /api/plans/{planId}/shopping/{itemId}/purchase` | Mark bought. Body `{}` or `{"expiresOn": "2026-11-20", "quantity": 2}` |
| `POST /api/plans/{planId}/shopping/purchase-all` | Mark everything bought. Body `{}` |
| `POST /api/plans/{planId}/shopping/{itemId}/store` | Buy this ingredient at another store from now on: `{"store": "Costco"}`, or `{"store": ""}` for no particular store |
| `GET /api/stores`, `PUT /api/stores` | The household's stores, the known stores and the ingredient names they can list. `PUT` replaces the stores (admin) |
| `GET` `POST /api/pantry`, `PATCH` `DELETE /api/pantry/{id}` | Read and correct the pantry |
| `GET` `POST /api/recipes`, `PUT` `DELETE /api/recipes/{id}` | Manage local recipes. A recipe has `name`, `course`, `servings`, `isSlowCooker` and `ingredients` (lines such as `"2 cups kale"`), and optionally `description`, `instructions` (a list of steps), `prepMinutes`, `cookMinutes`, `totalMinutes`, `sourceUrl`, `author`, `yieldText`, `notes` and `tags`. Responses add `images` and `ingredientDetails` (each line's quantity, unit and food) |
| `POST /api/recipes/{id}/images` | Add a photo: `{"data": "<base64>"}`, a JPEG, PNG, WebP or GIF of up to 5 MB, at most 10 per recipe. Makes an imported recipe a local one |
| `GET` `DELETE /api/recipes/{id}/images/{imageId}` | Get a photo as an image file, or remove it |
| `POST /api/recipes/import/file` | Import a recipe file: `{"fileName": "recipes.json", "content": "..."}`. Names already in the library are skipped |
| `GET /api/recipes/import/formats` | The kinds of recipe file that can be imported, with their file name endings |
| `POST /api/recipes/import/mealie` | Start a background import. Poll `GET /api/recipes/import/status` |
| `GET /api/session` | Whether setup is needed and who is signed in |
| `POST /api/setup/account`, `POST /api/account/sign-in`, `POST /api/account/sign-out` | First account, signing in and out |
| `GET` `POST /api/setup/samples` | Review the sample recipes and add chosen ones by name (admin) |
| `POST /api/invites`, `POST /api/users/{id}/password-reset` | Create an invite or a password reset link (admin) |
| `GET /api/links`, `POST /api/links/{id}/regenerate`, `DELETE /api/links/{id}` | List, replace or revoke unused links (admin) |
| `POST /api/account/accept-invite`, `POST /api/account/reset-password` | Use an invite or reset link |
| `POST /api/account/password` | Change your own password |
| `GET /api/users`, `DELETE /api/users/{id}`, `POST /api/users/{id}/unlock` | See, remove and unlock accounts (admin) |
| `GET` `PUT /api/system/security` | Lockout rules and "always use HTTPS" (admin) |
| `GET /api/system/https`, `GET` `POST /api/system/https/certificate` | HTTPS status, download or recreate the certificate |
| `POST /api/plans/{planId}/regenerate`, `DELETE /api/plans/{planId}` | Rebuild a plan around the pantry, or clear it |
| `GET` `PUT /api/system/mealie` | Mealie's address and API token. The token is never returned (admin) |
| `GET` `PUT /api/system/notifications`, `POST .../test`, `POST .../send-today` | Reminder settings, and sending now (admin) |
| `GET` `PUT` `POST /api/system/backups` | Backup settings and list, or back up now (admin) |
| `GET` `DELETE /api/system/backups/{name}`, `POST .../{name}/restore` | Download, delete or restore a backup (admin) |

Everything except the session, setup-account, sign-in, invite and reset endpoints needs a signed-in session cookie.
Endpoints marked *admin* answer `403` with the detail "Only an admin can do that." for other accounts.
Changing the pantry (`POST`, `PATCH` or `DELETE /api/pantry`) also updates the shopping lists of plans whose week
hasn't ended, a moment later, as `POST /api/plans/{planId}/refresh` does.
For scripts, sign in first and keep the cookie:

```sh
curl -c cookies.txt -X POST localhost:8080/api/account/sign-in -H 'Content-Type: application/json' \
  -d '{"username": "jamie", "password": "..."}'
```

Errors come back as [problem details](https://www.rfc-editor.org/rfc/rfc9457) with a readable `detail`.

Example:

```sh
curl -b cookies.txt -X POST localhost:8080/api/plans/stage -H 'Content-Type: application/json' -d '{}'
```

## Configuration

Settings live in `src/ChefBot.Api/appsettings.json`. Override any of them with environment variables,
using `__` between levels (for example `Planner__Dinners=5`). With the Podman setup, put them in
`~/.config/chefbot/chefbot.env` and run `systemctl --user restart chefbot`.

| Setting | Default | Meaning |
|---|---|---|
| `Planner:Dinners` | `4` | Dinners per week: 3, 4 or 5 |
| `Planner:SlowCookerMeals` / `SlowCookerDay` | `1` / `Tuesday` | Slow cooker dinners and the preferred day |
| `Planner:BreakfastRecipes`, `Snacks`, `Sweets` | `2`, `3`, `1` | How many of each to plan |
| `Planner:DinnerPortions`, `LunchPortions`, `BreakfastPortions` | `2` | People eating each meal |
| `Planner:ExpiringSoonDays` | `3` | Pantry food expiring this soon is planned first |
| `Planner:AvoidRepeatDays` | `28` | Don't repeat a recipe planned this recently |
| `Planner:ImportFromMealieBeforePlanning` | `false` | Refresh from Mealie before the weekly schedule stages a plan. Staging by hand uses the saved recipes |
| `Planner:Weights` | see file | How strongly sharing, season, pantry use and variety count |
| `Schedule:Enabled`, `Day`, `Time` | `true`, `Saturday`, `09:00` | When next week is staged automatically |
| `Classifier` | see file | Tag words that sort imported recipes into courses |
| `Notifications:WebhookUrl` | empty | The starting webhook address, until one is saved on the Settings tab |
| `Mealie:Url`, `Mealie:ApiToken` | empty | The starting Mealie connection, until one is saved on the Settings tab |
| `Planner:Weights:RegeneratePantryBoost` | `4` | How much more pantry food counts in "Regenerate from pantry" |
| `Data:Backups` | `backups` | The folder backups are written to |
| `Https:Mode` | `Off` (`SelfSigned` in the container image) | `Off`, `SelfSigned` or `Certificate` |
| `Https:HttpPort`, `Https:Port`, `Https:PublicPort` | `8080`, `8443`, `8443` | Listening ports, and the HTTPS port people type |
| `Https:Hostnames` | empty | Names the first self-signed certificate covers |
| `Https:CertificatePath`, `KeyPath`, `CertificatePassword` | empty | Your own certificate, in `Certificate` mode |
| `Https:TrustedProxies` | empty | Reverse proxies whose forwarded headers are believed |
| `Accounts:RecoverUsername` | empty | Logs a password reset link for this account at startup |

### The ingredient catalog

`config/ingredients.json` is what makes planning work. Each entry gives an ingredient a shared name,
aliases, a group (whole grain, protein, vegetable, ...), a shelf life and its local season:

```json
{"name": "cabbage", "group": "Vegetable", "shelfLifeDays": 14, "seasonMonths": [6, 7, 9, 10, 11, 12, 1, 2]}
```

The seasons shipped here are a starting point for the Kansas City area and the shelf lives are rough
estimates. Edit both to match what you see, then mount your copy over the built-in one
(see the comments in [chefbot.container](deploy/podman/chefbot.container)).

### Stores

Stores are chosen in the last, optional step of setup, or later under Settings, Stores. Each item on the
shopping list goes to the first store that lists its ingredient, then in-season produce goes to a seasonal
market while it is open, and everything else goes to the main store. Items that match none of these, which is
every item when no stores are chosen, are grouped by food group instead. On the Ingredients tab, the menu
beside an item moves that ingredient to another store for this and later lists.

`config/stores.json` holds the known stores offered as starting points: national and regional supermarkets,
warehouse clubs with bulk meat and dairy, specialty markets with the ingredients they're good for, and a
farmers market open May to October. Adding one copies it, so you can change its items or months afterwards.
To offer different stores, edit the file and mount your copy over the built-in one
(see the comments in [chefbot.container](deploy/podman/chefbot.container)).

```json
{"name": "Costco", "kind": "WarehouseClub", "description": "Bulk meat, eggs and dairy", "items": ["chicken", "egg"]}
```

### Notifications and Home Assistant

On the Settings tab an admin enters a webhook address, picks which reminders to send and when the daily ones
go out, and can press **Send a test** or **Send today's reminders now** to check the other end.
(`Notifications__WebhookUrl` in `chefbot.env` sets the starting address.)

Every notification is a JSON POST:

```json
{
  "type": "slow_cooker",
  "title": "Start the slow cooker",
  "message": "Tonight is Slow Cooker Beef Stew. Get the slow cooker going this morning.",
  "sentAt": "2026-11-10T13:30:00+00:00",
  "data": { "date": "2026-11-10", "meals": ["Slow Cooker Beef Stew"], "ingredients": ["stew meat", "carrot"] }
}
```

| `type` | Sent | `data` holds |
|---|---|---|
| `plan_ready` | When a week is staged or regenerated | `planId`, `weekStart`, `trigger`, `dinners` (day, title, slowCooker), `snacks`, `sweets`, `toBuy` |
| `slow_cooker` | Daily time, on the day of a slow cooker dinner | `meals`, `ingredients` |
| `expiring_food` | Daily time, when pantry food expires within `Planner:ExpiringSoonDays` | `items` (name, expiresOn, daysLeft, usedByPlan) |
| `shopping` | Daily time, the day before a week starts and on its first day | `status`, `toBuy` |
| `meals_today` | Daily time (off by default) | `meals` (slot, title, optional, leftover) |
| `test` | From the Settings tab | |

`title` and `message` are ready to show as they are; `type` and `data` let an automation word its own
reminder or do something else entirely. A Home Assistant example that forwards everything to phones and
also announces the slow cooker reminder on a speaker:

```yaml
automation:
  - alias: "ChefBot reminders"
    triggers:
      - trigger: webhook
        webhook_id: chefbot
        allowed_methods: [POST]
        local_only: true
    actions:
      - action: notify.notify
        data:
          title: "{{ trigger.json.title }}"
          message: "{{ trigger.json.message }}"
      - if: "{{ trigger.json.type == 'slow_cooker' }}"
        then:
          - action: tts.speak
            target:
              entity_id: tts.home_assistant_cloud
            data:
              media_player_entity_id: media_player.kitchen
              message: "Remember to start {{ trigger.json.data.meals | join(' and ') }} in the slow cooker."
```

The webhook address for this example is `http://your-home-assistant:8123/api/webhook/chefbot`.

### Backups

ChefBot writes a backup every day at 03:00 and keeps the newest 14. On the Settings tab an admin can change
the time, how many to keep, a maximum age in days, make a backup immediately, download one, delete one, or
**restore** one, which replaces everything in ChefBot with that backup.

- Backups are compressed JSON files holding ChefBot's own data: recipes and their photos, plans, the pantry,
  accounts and settings. Photos make backups larger, by up to 5 MB each. They are stored in the `chefbot-backups` Podman volume. To keep them in a host folder instead, see the
  comment in [chefbot.container](deploy/podman/chefbot.container).
- They contain password hashes, so keep downloaded copies private.
- A backup on the same disk doesn't protect against losing the disk: download one now and then, or point
  the volume at a folder that is itself backed up.

## How it is built

```
src/ChefBot.Core             The core, in layers: managers, engines, resource accessors and utilities, behind
                             public contracts. PostgreSQL via EF Core, backups, Mealie, the webhook.
src/ChefBot.Api              Wolverine HTTP endpoints and message handlers, sign-in, HTTPS. Serves the web page.
src/ChefBot.Web              The web page: Svelte 5, built with Vite.
tests/ChefBot.Core.Tests     TUnit tests for the logic and the layering.
config/                     The ingredient catalog and sample recipes.
```

- **The core is layered by what changes for different reasons.** Five managers (plans, pantry, recipes,
  accounts, maintenance) run each use case; engines do the planning, shopping, suggestion, validation, security
  and notification work; resource accessors are the only code that reads or writes the database, Mealie, the
  webhook or the backup folder. Calls only go down, and the API sees only the managers' interfaces.
- **[Wolverine](https://wolverinefx.net)** serves the HTTP endpoints (`Endpoints/`) and carries messages between
  managers (`Messaging/`): `PlanStaged` sends the "plan ready" notification, `PantryChanged` refreshes the open
  plans' shopping lists, and the Mealie import runs in the background. Messages are stored in PostgreSQL, in the
  `wolverine` schema, and are only sent once the change that caused them is saved, so a restart doesn't lose them.
- **Every 10 minutes** a timer asks for the scheduled jobs: next week's plan, the daily reminders and the daily
  backup. Each one runs only when its time has come and it hasn't run yet.
- **The planner** scores whole weeks, not single recipes. It starts from several random weeks and improves
  each one swap at a time (`WeekSelector`), scoring with `WeekScorer`.
- **Every change to a plan** goes through the plan manager, which ends each change by placing the leftover
  lunches and rebuilding the shopping list, so they always match the meals and the pantry.

See [CONTRIBUTING.md](CONTRIBUTING.md) for conventions.

## Limits worth knowing

- **Built for a home network.** Passwords are hashed, accounts lock after repeated wrong passwords and HTTPS
  is built in, but the certificate is self-signed and there is no two-step sign-in. Don't expose it to the
  internet without a reverse proxy and a real certificate.
- **Reset and invite links are passed along by you.** ChefBot sends no email.
- **Accounts share everything.** There is one household per installation: no per-person plans or pantries.
- **Amounts are only compared within the same unit.** ChefBot does not convert cups to grams. When it can't
  compare, having some of an ingredient counts as having enough.
- **Scheduled jobs catch up, within limits.** A weekly plan due while ChefBot was stopped is staged at the next
  check (every 10 minutes) as long as its week hasn't started; each week is staged once, so a plan you clear
  stays cleared. Daily reminders and backups run at the next check if their time has passed that day.
- **One ChefBot per database.** Changes to a plan are made one at a time inside the app, so don't run two copies
  against the same database.
- **Notifications go one way.** ChefBot posts to a webhook; it has no API keys yet, so Home Assistant can't
  call ChefBot's API on its own.
- **The database schema is created on first start** and is not upgraded automatically yet
  (see [Upgrading](#upgrading) and CONTRIBUTING.md).
- **Seasons are guesses** from the catalog, and stores only know what you tell them. Neither is live store data.

## License

GNU General Public License v3.0. See [LICENSE](LICENSE).
