# ChefBot

ChefBot plans a week of meals, snacks, sweets and drinks around three things: ingredients the meals can share,
what is in season, and what is already in your kitchen and about to expire. It then builds the shopping
list, tracks what you bought with expiration dates, and helps you change the plan mid-week to use food up.

It is a self-hosted web app: one container for the app, one for PostgreSQL.

## What it does

- **Plans a week in your style.** A plan style sets the days you cook dinner, whether weekday lunches are
  leftovers, how many breakfasts, snacks, sweets and drinks to plan, and rules for the recipes: "at least 3
  slow cooker dinners", "no meals made with beef or pork", "at least 2 dinners made with chicken", "at least 4
  dinners ready within 30 minutes", "exactly 1 dinner tagged fish, on Friday". Starter styles cover a classic
  week, a slow cooker week, no slow cooker, quick weeknights, meat-free and dinners only; keep the ones you
  like, change them, or make your own, and pick a style each time you plan.
- **Gives you a draft to review.** The *Plan* tab shows the week day by day. Drag a meal to another day (or
  use its Move menu), swap one, mark the ones you like with *Keep* and shuffle the rest, and see what the
  shopping list comes to, then confirm. *Shopping* shows everything the plan needs, where each item comes
  from (to buy, pantry, staple) and which meals use it.
- **Builds the shopping list.** Quantities are totalled and scaled to portions, staples are left off,
  pantry stock is subtracted, and the list is split between the stores you shop at, or grouped by food group
  if you haven't chosen any. Anything else can be added to the list by hand, with or without a plan, and
  pantry items that are expiring can be put back on it in one click.
- **Tracks the pantry.** Marking an item as bought puts it in the pantry with an expiration date, estimated
  from the ingredient's shelf life unless you enter the date on the package. The *Pantry* tab lists what you
  have, which planned meals will use each item, and what no meal uses yet.
- **Swaps meals after shopping.** For any meal, ChefBot suggests recipes ranked by how much of the pantry they
  use, favoring food that expires soonest, and invents simple plates from what you have:
  a whole grain, a protein and a vegetable or fruit. Choosing one updates the plan, the leftover lunches
  and the shopping list together.
- **Keeps its own recipes.** Recipes live in ChefBot's database: their ingredients, steps, prep and cook times,
  description, tags, notes, source link and photos. Add them in the web page, paste a link to a recipe on a
  website, or import a recipe file such as a [Mealie](https://mealie.io) backup .zip. Photos from a website are
  downloaded once, in the background, so they still show when the recipe's website is down.
- **Helps while you cook.** *Start cooking* on a recipe fills the screen with large text: first the ingredients to
  set out, then what to start early (preheating the oven, a pot of water, an overnight marinade), then one step at
  a time with the ingredients it uses. Times in a step start a timer that rings when it ends, and the screen stays
  on where the browser allows it.
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
journalctl --user -u chefbot -f          # app logs (chefbot-db for the database); recent ones are also under Settings, Logs
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

Open <http://localhost:8080>. It sends you on to <https://localhost:8443>, where the browser warns about
the certificate the first time (see [HTTPS](#https)). The first visit starts the setup wizard:

1. **Create your account.** This first account is the admin.
2. **Secure connection.** Shows the HTTPS address, lets you name the certificate for the address you use,
   download it, and choose whether to always use HTTPS. It starts on.
3. **Add your recipes.** Pick one or both:
   - **Import my recipes** from a recipe file (JSON in ChefBot's own shape, a Mealie backup .zip with its
     photos or the `database.json` from one, a Mealie recipe export or schema.org recipes from a website), as many files as you like.
   - **Start with sample recipes**: tick the ones you want from the 25 samples. You can come back to this
     list later from the Recipes tab.

   Or skip the step and add recipes later.
4. **Choose a plan style.** Pick the starter style for most weeks; the others are kept so you can choose one
   for any week. Change them, or make your own, here or later under **Settings → Plan styles**.
5. **Stores (optional).** Choose where you shop, so the shopping list is split between them.

Then press **Plan** on the Plan tab.

To bring recipes over from Mealie, make a backup in Mealie (**Settings → Backups**) and import the .zip from
the Recipes tab. Importing a newer backup later adds only the recipes that aren't in ChefBot yet.

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
  Sign-ins sent all at once count too: only the first 5 have their passwords checked.

### HTTPS

With the provided Podman setup, ChefBot serves HTTPS on port 8443 with a certificate it creates itself,
next to HTTP on 8080. Nothing needs to be installed or issued.

- The first visit to `https://your-host:8443` shows a browser warning, because nobody but ChefBot vouches for
  the certificate. Choose to continue. To remove the warning for good, download the certificate (setup wizard
  or Settings tab) and add it to your device's trusted certificates.
- For that to work the certificate must name the address you use. Enter it in the wizard or on the Settings
  tab, or set `Https__Hostnames=chefbot.local,192.168.1.20` in `chefbot.env`. A new certificate is used
  immediately, no restart.
- **Always use HTTPS** (wizard or Settings) sends plain HTTP visitors to the HTTPS address, so passwords never
  cross the network unencrypted. It is on from the start whenever ChefBot serves HTTPS, until an admin saves
  the sign-in settings. After that, the saved choice stays.
- The certificate is kept in the `chefbot-keys` volume, lasts about two years and is renewed in its last
  30 days. A renewed certificate has to be accepted or installed again.
- To use your own certificate, or to run behind a reverse proxy, see
  [Your own certificate](#your-own-certificate) and [Behind a reverse proxy](#behind-a-reverse-proxy).

### Offline on your phone

The Shopping and Pantry tabs keep working with no connection, for example in a shop with no signal. You can tick
items off, add items, remove the ones you added and mark everything as bought. The changes are sent to ChefBot as
soon as the phone can reach it again. Items count as bought on the day you ticked them. The pantry can only be read
while offline, and the other tabs need ChefBot.

Phones only keep a web page for offline use over HTTPS with a certificate they trust. With ChefBot's own
certificate:

1. Download the certificate from the setup wizard or the Settings tab, on the phone itself.
2. Install and trust it:
   - **iPhone:** open the downloaded profile in Settings and install it. Then turn it on under
     Settings → General → About → Certificate Trust Settings.
   - **Android:** Settings → Security → Encryption & credentials → Install a certificate → CA certificate.
3. Open `https://your-host:8443` while connected and sign in. You can also add it to the home screen.

After that, open ChefBot as usual. When it can't be reached, a banner says so and shows when the list was last
loaded. With your own trusted certificate or a reverse proxy, only step 3 is needed. Over plain HTTP the page
still works offline while it stays open, but a reload needs the connection. Signing out removes the saved list
from the phone.

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
- **Browsers remember to use HTTPS.** With your own certificate and "Always use HTTPS" on, ChefBot sends
  `Strict-Transport-Security` for a year, so browsers go straight to HTTPS without trying HTTP first. Keep a
  valid certificate for that address for that long. A browser that has seen the header won't let anyone click
  past a certificate warning. A self-signed certificate never gets the header, because it warns until each
  device trusts it.

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
- **"Always use HTTPS" stays off.** Redirecting to HTTPS, and sending `Strict-Transport-Security`, is the
  proxy's job.
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

1. **Saturday morning** the schedule plans next week in your default style as a draft, and posts a summary to
   your webhook. Or press **Plan** on the Plan tab yourself, choosing a style.
2. **Review** the draft on the Plan tab. Drag meals between days, *Swap* any you don't want, press *Keep* on
   the ones you like and *Shuffle* the rest, or plan the week again in another style. Open the shopping
   summary to see what you'd buy.
3. **Confirm** the plan. The Shopping tab is now your shopping list.
4. **Shop**, then press *Bought* per item or *Mark all as bought*. Purchases move to the pantry with
   expiration dates.
5. **During the week**, mark meals *Cooked* on the Plan tab; their ingredients come out of the pantry.
   Plans changed? Move or *Swap* a meal: suggestions that use up expiring food come first. When this week is
   confirmed, the Plan tab offers to plan the week after, and a switch at the top moves between the two.

Two buttons on a confirmed week change the whole plan:

- **Replan from pantry** replaces the plan with a new one in the same style, built around what is in the
  pantry, which is the one to use after shopping: recipes that use food you already bought are strongly
  preferred, and the new shopping list only holds what is still missing. The whole week is replaced,
  including days already past.
- **Clear plan** removes the plan and its shopping list. The pantry is kept. A draft has **Discard draft**
  instead.

## API

The web page is a thin client over a JSON API, so anything it does can be automated.
The full description is served at `/openapi/v1.json`, and `/docs` shows it as a Swagger page where you can
try each call. Both need you to be signed in.

| Method and path | What it does |
|---|---|
| `POST /api/plans/stage` | Plan a week as a draft. Body `{}` for the upcoming week in the default style, or `{"weekStart": "2026-11-08", "styleId": "...", "keep": ["<mealId>"], "replace": true}`: `keep` carries meals over from the week's current plan, as Shuffle does |
| `GET /api/plans` | The weeks with a plan that haven't ended, earliest first: `id`, `weekStart`, `weekEnd`, `status`, `styleName` |
| `GET /api/plans/current`, `GET /api/plans/{planId}` | The plan for this week (or else the next upcoming one), or a plan by id |
| `POST /api/plans/{planId}/confirm` | Make a staged plan the working plan |
| `GET /api/plans/{planId}/ingredients` | Everything the plan needs, plus the pantry with the meals that use each item (`usedFor`) |
| `GET /api/plans/{planId}/meals/{mealId}/alternatives` | Recipe and creative suggestions for one meal |
| `PUT /api/plans/{planId}/meals/{mealId}` | Replace a meal: `{"recipeId": "..."}` or `{"custom": {"title": "...", "ingredients": ["..."]}}` |
| `POST /api/plans/{planId}/meals/{mealId}/move` | Move a dinner or breakfast to another day of its week: `{"date": "2026-11-11"}`. A meal of the same kind already there swaps with it; leftover lunches and the shopping list follow |
| `POST /api/plans/{planId}/meals/{mealId}/status` | `{"status": "Cooked"}`, `"Skipped"` or `"Planned"` |
| `POST /api/plans/{planId}/shopping/{itemId}/purchase` | Mark bought. Body `{}` or `{"expiresOn": "2026-11-20", "quantity": 2}` |
| `POST /api/plans/{planId}/shopping/purchase-all` | Mark everything bought, including the items added by hand. Body `{}` |
| `POST /api/plans/{planId}/shopping/{itemId}/store` | Buy this ingredient at another store from now on: `{"store": "Costco"}`, or `{"store": ""}` for no particular store |
| `GET` `POST /api/shopping/items` | The items added to the shopping list by hand, which need no plan, or add one: `{"name": "milk", "quantity": 2, "unit": "l"}` (amount and unit optional; adding the same thing again adds to its amount) |
| `POST /api/shopping/items/{itemId}/purchase`, `POST /api/shopping/items/purchase-all`, `DELETE /api/shopping/items/{itemId}` | Mark a hand-added item (or all of them) bought, which puts it in the pantry, or take it off the list. Body `{}` or, for one item, `{"expiresOn": "2026-11-20", "quantity": 2}` |
| `POST /api/shopping/items/{itemId}/store` | Buy a hand-added item's ingredient at another store from now on, as for a plan's item |
| `GET /api/shopping/targets`, `PUT /api/shopping/targets` | The to-do apps the shopping list can be sent to: `{"id", "name", "kind", "url", "list", "hasToken"}`, with `kind` one of `HomeAssistant`, `Todoist` or `Webhook`. Tokens are never sent back. `PUT` (admin) replaces them all: `{"targets": [{"id": null, "name": "Kitchen list", "kind": "HomeAssistant", "url": "http://homeassistant.local:8123", "list": "todo.shopping_list", "token": "..."}]}`; a target sent with `"token": null` keeps its saved token |
| `POST /api/shopping/targets/{targetId}/test` | Add one test item to a to-do app (admin). Answers `{"sent", "total", "problem"}` |
| `GET /api/calendar/feed` | The calendar feed's address, `{"url", "createdAt"}`, or `{"url": null}` when it is off |
| `POST` `DELETE /api/calendar/feed` | Turn the calendar feed on with a new address (the old one stops working), or off (admin) |
| `GET /api/calendar/{token}/meals.ics` | The calendar feed as iCalendar text: each breakfast, lunch and dinner of the confirmed weeks from last week on, at the times under `Calendar`. Needs no sign-in, only the secret in the address. 404 when the feed is off or the secret is wrong |
| `POST /api/shopping/send` | Add what is left to buy to a to-do app: `{"targetId": "...", "planId": "..."}`, the plan's items not yet bought plus the hand-added ones (only those without `planId`). Nothing is ticked off in ChefBot, and sending again adds the items again. Answers `{"sent", "total", "problem"}`; items go one at a time, so a problem stops the rest |
| `GET /api/stores`, `PUT /api/stores` | The household's stores, the known stores and the ingredient names they can list. `PUT` replaces the stores (admin) |
| `GET /api/plan-styles`, `PUT /api/plan-styles` | The household's plan styles, the starter styles, and the recipe tags and ingredient names rules can use. `PUT` replaces the styles: `{"styles": [{"name": "Crockpot week", "isDefault": true, "cookingDays": ["Sunday", "Wednesday"], "leftoverLunches": true, "breakfasts": 0, "snacks": 2, "sweets": 0, "drinks": 1, "rules": [{"limit": "AtLeast", "count": 2, "slot": "Dinner", "match": "SlowCooker"}]}]}`. A rule's `limit` is `AtLeast`, `AtMost`, `Exactly` or `None`; `match` is `SlowCooker`, `Tag` or `Ingredient` (with `values`) or `ReadyWithin` (with `minutes`), or `Favourite` (someone's favourite; recipes planned recently don't count towards it); `day` puts matching dinners on a day first. A style's `favourites` is `Off`, `ALittle` (the default) or `ALot`: how much favourite recipes are preferred, more so the more people have them as a favourite, but never ahead of avoiding recent meals |
| `GET` `POST /api/pantry`, `PATCH` `DELETE /api/pantry/{id}` | Read and correct the pantry |
| `GET` `POST /api/recipes`, `PUT` `DELETE /api/recipes/{id}` | Manage local recipes. A recipe has `name`, `course`, `servings`, `isSlowCooker` and `ingredients` (lines such as `"2 cups kale"`), and optionally `description`, `instructions` (a list of steps), `prepMinutes`, `cookMinutes`, `totalMinutes`, `sourceUrl`, `author`, `yieldText`, `notes` and `tags`. Responses add `images`, `ingredientDetails` (each line's quantity, unit and food) and `stepDetails` (for each step: the `timers` it gives in seconds, a `headStart` such as preheating the oven, boiling water or a long wait, and the positions of the `ingredients` it names) and `favouredBy`: the people who have it as a favourite, each `{"userId", "name"}`, oldest first, and `rating`: `{"average", "count"}`, the average stars of its reviews to one decimal place, or null when it has none |
| `GET /api/recipes/search?query=chicken&course=Main&page=1&pageSize=25` | One page of the library, in course order and then by name. Every parameter is optional: `query` keeps recipes whose name, ingredients or tags contain each of its words (any case), `pageSize` defaults to 25 (at most 100). `tag` (repeat it for several) keeps recipes with every tag given, `favourites=true` anyone's favourites, `favouredBy=<user id>` one person's, `slowCooker=true` slow cooker recipes, and `readyWithin=30` recipes that say they are ready within that many minutes. Answers `{"recipes": [...], "total", "page", "pageSize", "librarySize"}`, where each recipe has only `id`, `name`, `course`, `isSlowCooker`, `ingredients`, `tags`, `coverUrl` (its first photo, or null), `favouredBy` and `rating` |
| `GET /api/history?before=2026-05-01&months=6` | The household's food history, newest month first: recipes added and retired, cooking (with debuts, fan favourites, comebacks, milestone dinners and someone's favourite being cooked picked out), reviews, big shopping hauls and people joining. Both parameters are optional; `months` defaults to 6 (at most 24). Answers `{"chapters": [...], "olderBefore", "since", "totalDinners"}`, where each moment has an `at` timestamp in the household's time zone (midnight when only the day was recorded), and a cook has `favouredBy`: whose favourite the recipe was when it was cooked, and a `Reviewed` moment has `review`: `{"stars", "notes", "reviewedBy"}`; pass `olderBefore` as `before` for older months |
| `GET /api/recipes/filters` | What the library can be filtered by: `{"tags": [{"name", "recipes"}], "people": [{"userId", "name"}]}`: every tag once with how many recipes have it, most used first, and the people who have favourites |
| `GET /api/recipes/{id}` | One recipe in full. 404 when it doesn't exist or has been archived |
| `GET /api/recipes/{id}/export?format=json` | One recipe as a file to download, named after it. `format` is optional: `json` (the default) gives a schema.org JSON-LD file (`.json`, `application/ld+json`), which Mealie, Tandoor and other recipe apps import, as does ChefBot's own import. It adds `course`, `isSlowCooker` and `notes`, which schema.org has no word for, and lists photos only when they came from the web, by their original address. `pdf` gives a PDF to print or keep (`.pdf`): text only, without photos, on US Letter pages; letters outside western European alphabets print as `?`. `markdown` gives Markdown (`.md`) and `text` plain text (`.txt`), both UTF-8. A format it doesn't know gives JSON-LD. 404 when the recipe doesn't exist or has been archived |
| `GET /api/recipes/export?format=json` | Every recipe that can be planned, in name order, in one file named `chefbot-recipes-<date>`. `format` works as above: JSON-LD lists the recipes as a `@graph`, the PDF starts each recipe on a new page, and Markdown and text put a rule between them |
| `PUT` `DELETE /api/recipes/{id}/favourite` | Add the recipe to the signed-in person's favourites, or take it off. Doing either twice changes nothing. Answers the recipe as in a search, with its `favouredBy`. 404 when the recipe doesn't exist or has been archived |
| `GET` `POST /api/recipes/{id}/reviews`, `PUT` `DELETE /api/recipes/{id}/reviews/{reviewId}` | A recipe's reviews. `POST` adds the signed-in person's review, `{"stars": 4, "notes": "Less salt next time."}`, with stars from 1 to 5 and optional notes of up to 2000 characters. Anyone can review a recipe as often as they like. `PUT` changes your own review, and `DELETE` removes it; an admin can delete anyone's. Each answers `{"rating": {"average", "count"}, "reviews": [{"id", "userId", "reviewedBy", "stars", "notes", "at", "edited"}]}`, newest first. 403 for someone else's review, 404 when the recipe doesn't exist or has been archived |
| `POST /api/recipes/{id}/images` | Add a photo: `{"data": "<base64>"}`, a JPEG, PNG, WebP or GIF of up to 5 MB, at most 10 per recipe |
| `GET` `DELETE /api/recipes/{id}/images/{imageId}` | Get a photo as an image file, or remove it |
| `POST /api/recipes/import/file` | Import a recipe file: `{"fileName": "recipes.json", "content": "..."}`. Names already in the library are skipped |
| `POST /api/recipes/import/archive?fileName=backup.zip` | Import a Mealie backup .zip, sent as the raw body (`Content-Type: application/zip`, up to 200 MB), with its photos. Names already in the library are skipped, but one with no photos gets the backup's. The answer adds `photosAdded` |
| `GET /api/recipes/import/formats` | The kinds of recipe file that can be imported, with their file name endings |
| `POST /api/recipes/import/web` | Add the recipe on a web page: `{"url": "https://example.com/recipes/soup"}`. Answers `{"recipe": {...}, "readWith": "Any site (schema.org)"}` naming the reader used. Photos download in the background. 409 when that page or name is already in the library, 502 when the page can't be downloaded |
| `GET /api/session` | Whether setup is needed and who is signed in |
| `POST /api/setup/account`, `POST /api/account/sign-in`, `POST /api/account/sign-out` | First account, signing in and out |
| `GET` `POST /api/setup/samples` | Review the sample recipes and add chosen ones by name (admin) |
| `POST /api/invites`, `POST /api/users/{id}/password-reset` | Create an invite or a password reset link (admin) |
| `GET /api/links`, `POST /api/links/{id}/regenerate`, `DELETE /api/links/{id}` | List, replace or revoke unused links (admin) |
| `POST /api/account/accept-invite`, `POST /api/account/reset-password` | Use an invite or reset link |
| `POST /api/account/password` | Change your own password |
| `GET` `POST /api/tokens`, `DELETE /api/tokens/{id}` | List, make (`{"name": "Claude Code"}`, answers with the `token` once) or revoke your own API tokens for AI assistants |
| `GET /api/users`, `DELETE /api/users/{id}`, `POST /api/users/{id}/unlock` | See, remove and unlock accounts (admin) |
| `GET` `PUT /api/system/security` | Lockout rules and "always use HTTPS" (admin) |
| `GET /api/system/https`, `GET` `POST /api/system/https/certificate` | HTTPS status, download or recreate the certificate |
| `POST /api/plans/{planId}/regenerate`, `DELETE /api/plans/{planId}` | Rebuild a plan around the pantry in the style it was made in, or clear it |
| `GET` `PUT /api/system/notifications`, `POST .../test`, `POST .../send-today` | Reminder settings, and sending now (admin) |
| `GET` `PUT` `POST /api/system/backups` | Backup settings and list, or back up now (admin) |
| `GET` `DELETE /api/system/backups/{name}`, `GET .../{name}/summary`, `POST .../{name}/restore` | Download, delete, see what it holds or restore a backup (admin) |
| `GET /api/system/logs` | The last 1000 log entries since startup; `?after={sequence}` returns only newer ones (admin) |

Everything except the session, setup-account, sign-in, invite and reset endpoints needs a signed-in session cookie.
Endpoints marked *admin* answer `403` with the detail "Only an admin can do that." for other accounts.
Changing the pantry (`POST`, `PATCH` or `DELETE /api/pantry`) also updates the shopping lists of plans whose week
hasn't ended, a moment later, as `POST /api/plans/{planId}/refresh` does.
For scripts, sign in first and keep the cookie:

```sh
curl -c cookies.txt -X POST localhost:8080/api/account/sign-in -H 'Content-Type: application/json' \
  -d '{"username": "jamie", "password": "..."}'
```

API tokens only work on the MCP endpoint (see [AI assistants (MCP)](#ai-assistants-mcp)), not on `/api`.

Errors come back as [problem details](https://www.rfc-editor.org/rfc/rfc9457) with a readable `detail`.

Example:

```sh
curl -b cookies.txt -X POST localhost:8080/api/plans/stage -H 'Content-Type: application/json' -d '{}'
```

## Configuration

Settings live in `src/ChefBot.Api/appsettings.json`. Override any of them with environment variables,
using `__` between levels (for example `Planner__DinnerPortions=4`). With the Podman setup, put them in
`~/.config/chefbot/chefbot.env` and run `systemctl --user restart chefbot`.

| Setting | Default | Meaning |
|---|---|---|
| `Planner:DinnerPortions`, `LunchPortions`, `BreakfastPortions` | `2` | People eating each meal |
| `Planner:ExpiringSoonDays` | `3` | Pantry food expiring this soon is planned first |
| `Planner:AvoidRepeatDays` | `28` | Don't repeat a recipe planned this recently |
| `Planner:Weights` | see file | How strongly sharing, season, pantry use and variety count |
| `Schedule:Enabled`, `Day`, `Time` | `true`, `Saturday`, `09:00` | When next week is staged automatically |
| `Calendar:BreakfastTime`, `LunchTime`, `DinnerTime` | `08:00`, `12:30`, `18:30` | When meals appear in the calendar feed |
| `Calendar:MealMinutes`, `SlowCookerHours` | `45`, `8` | How long each meal's event lasts, and how many hours before a slow cooker dinner its reminder goes off (`0` for none) |
| `Classifier` | see file | Tag words that sort imported recipes into courses |
| `Notifications:WebhookUrl` | empty | The starting webhook address, until one is saved on the Settings tab |
| `Planner:Weights:RegeneratePantryBoost` | `4` | How much more pantry food counts in "Replan from pantry" |
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

### Plan styles

What a week holds (dinner days, leftover lunches, how many breakfasts, snacks, sweets and drinks, and rules for
the recipes) is set by plan styles, chosen in setup and changed under **Settings → Plan styles** by anyone
signed in. The `Planner:Dinners`, `SlowCookerMeals`, `SlowCookerDay`, `BreakfastRecipes`, `Snacks` and `Sweets`
settings of earlier versions are no longer read. Until a household saves any styles, weeks are planned in the
first starter style.

A rule your recipes can't meet, say three slow cooker dinners with only one slow cooker recipe, is relaxed for
that week and the plan's notes say so. Ingredient rules match whole words in ingredient names, so "chicken"
also matches chicken thighs and chicken broth.

Each style also says how much to lean on favourites: not at all, a little (the default) or a lot. A recipe more
people have hearted counts for more. Recipes planned in the last `Planner:AvoidRepeatDays` days are still left
out first, so a favourite never comes round sooner than any other recipe would. A rule can also ask for
favourites, such as "at least 1 dinner someone's favourite"; when the favourites were all planned recently, the
rule gives way for that week rather than repeat one.

`config/plan-styles.json` holds the starter styles; the first is the default for a new household. Adding one
copies it, so you can change it afterwards. To offer different starters, edit the file and mount your copy
over the built-in one (see the comments in [chefbot.container](deploy/podman/chefbot.container)).

Drinks are a course like snacks and sweets: imported recipes labelled drink, beverage, cocktail, smoothie or
lemonade are filed as drinks (`Classifier:DrinkWords`), and three drinks are among the sample recipes.

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
| `plan_ready` | When a week is staged or regenerated | `planId`, `weekStart`, `trigger`, `style`, `dinners` (day, title, slowCooker), `snacks`, `sweets`, `drinks`, `toBuy` |
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

### Sending the shopping list to a to-do app

The Shopping tab can send what is left to buy to a to-do app, one item per thing to buy, such as "fish fillets,
600 g", with its store and meals where the app keeps a description. An admin sets the apps up under
**Settings → To-do apps**, and **Send a test item** there checks each one.

- **Home Assistant** reaches any to-do list Home Assistant has: its own Shopping List, Local To-do, or one it syncs
  with Bring!, Todoist, Google Tasks and others. Give Home Assistant's address, the list's entity (such as
  `todo.shopping_list`) and a long-lived access token from your Home Assistant profile page, under **Security**.
  ChefBot calls `todo.add_item` for each item.
- **Todoist** needs an API token from Todoist's **Settings → Integrations → Developer**, and optionally a project id
  (the Inbox otherwise).
- **Webhook** posts the whole list at once as `{"type": "shopping_list", "sentAt": "...", "items": [{"title",
  "description"}]}`, for an automation to handle. A token, if given, is sent as a bearer token.

Tokens are kept in the database, and in backups, and never shown again once saved. Sending adds items each time,
so send once you've finished changing the plan.

### The meal plan in your calendar

Under **Settings → Calendar**, an admin turns on a calendar feed, and everyone in the household can subscribe to
it. Calendar apps then show each confirmed week's breakfasts, lunches and dinners at mealtimes, each with a link
to its recipe, and remind you to start the slow cooker. The address carries a secret instead of a sign-in, and
**Make a new address** stops the old one working.

[docs/calendar.md](docs/calendar.md) says what the calendar shows and which calendar apps can reach a ChefBot on a
home network. It has step-by-step instructions for iPhone and Mac, Android (with ICSx⁵), Google Calendar, Outlook,
Thunderbird and GNOME Calendar.

### AI assistants (MCP)

ChefBot is also an [MCP](https://modelcontextprotocol.io) server at `/mcp`, so assistants such as Claude can work
with your kitchen: search and import recipes, read the week's plan, plan a week, swap, move or mark meals cooked,
and look at or change the pantry and the shopping list. Admin settings (people, stores, plan styles, backups and
so on) aren't offered.

Under **Settings → AI assistants**, make a token for each assistant and copy it: it is shown only once. The
assistant sends it as `Authorization: Bearer <token>`. For Claude Code:

```sh
claude mcp add --transport http chefbot https://your-host:8443/mcp --header "Authorization: Bearer chefbot_..."
```

Other clients that support Streamable HTTP with a custom header take the same address and header. A token acts as
the person who made it, keeps working after a password change, and stops working when it is revoked or its account
is removed. Tokens aren't kept in backups, so after a restore assistants need new ones. The assistant has to reach
ChefBot at that address, so an assistant that runs in the cloud can't use a ChefBot that only your home network can
reach. With ChefBot's own certificate, the machine running the assistant must trust it (see [HTTPS](#https)).

### Backups

ChefBot writes a backup every day at 03:00 and keeps the newest 14. On the Settings tab an admin can change
the time, how many to keep, a maximum age in days, make a backup immediately, download one, delete one, or
**restore** one, which replaces everything in ChefBot with that backup.

- **After a restore, everyone signs in again**, the admin who restored included. Passwords go back to what
  they were when the backup was made. Sessions from before the restore stop working, including any started
  before a later password change, so restoring an old backup can't bring back a session that change ended.

- Backups are compressed JSON files holding ChefBot's own data: recipes and their photos, plans, the pantry,
  accounts and settings. Photos make backups larger, by up to 5 MB each. They are stored in the `chefbot-backups` Podman volume. To keep them in a host folder instead, see the
  comment in [chefbot.container](deploy/podman/chefbot.container).
- They contain password hashes, so keep downloaded copies private.
- A backup on the same disk doesn't protect against losing the disk: download one now and then, or point
  the volume at a folder that is itself backed up.

## How it is built

```
src/ChefBot.Core             The core, in layers: managers, engines, resource accessors and utilities, behind
                             public contracts. PostgreSQL via EF Core, backups, the webhook.
src/ChefBot.Api              Wolverine HTTP endpoints and message handlers, sign-in, HTTPS. Serves the web page.
src/ChefBot.Web              The web page: Svelte 5, built with Vite.
tests/ChefBot.Core.Tests     TUnit tests for the logic and the layering.
config/                     The ingredient catalog and sample recipes.
```

- **The core is layered by what changes for different reasons.** Five managers (plans, pantry, recipes,
  accounts, maintenance) run each use case; engines do the planning, shopping, suggestion, validation, security
  and notification work; resource accessors are the only code that reads or writes the database, the web,
  the webhook or the backup folder. Calls only go down, and the API sees only the managers' interfaces.
- **[Wolverine](https://wolverinefx.net)** serves the HTTP endpoints (`Endpoints/`) and carries messages between
  managers (`Messaging/`): `PlanStaged` sends the "plan ready" notification, `PantryChanged` refreshes the open
  plans' shopping lists, and `FetchRecipeImages` downloads imported photos in the background. Messages are stored in PostgreSQL, in the
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
- **Recipe links need a site that describes its recipes.** Most recipe sites include schema.org data for search
  engines, and that is what ChefBot reads. Sites without it, behind a sign-in, or that turn away programs get a
  message saying so; type those recipes in, or add a reader for the site (CONTRIBUTING.md). ChefBot only
  downloads pages and photos from the public internet, never from addresses on your own network.
- **Seasons are guesses** from the catalog, and stores only know what you tell them. Neither is live store data.

## License

GNU General Public License v3.0. See [LICENSE](LICENSE).
