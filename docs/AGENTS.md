# AGENTS.md for the documentation

`docs/` is ChefBot's user and developer documentation, in plain Markdown. The same files are read on GitHub, published
to GitHub Pages (`.github/workflows/docs.yml`), zipped into each release, and built into the image at `/help`
(Dockerfile `docs` stage), where the app's **?** button and "Guide:" links open them. This file isn't published
(`exclude_docs` in `mkdocs.yml`).

## Check your work

```sh
python3 -m venv /tmp/chefbot-docs && /tmp/chefbot-docs/bin/pip install -q -r docs/requirements.txt   # once
/tmp/chefbot-docs/bin/mkdocs build --strict -d /tmp/chefbot-docs/site       # must print no WARNING
SITE_URL= /tmp/chefbot-docs/bin/mkdocs build --strict -d /tmp/chefbot-docs/help   # the /help variant
```

Use the scratchpad instead of `/tmp` when one is given. A docs change is done when the strict build is clean. Don't
build the container image to check docs.

## Writing rules

- **Audience first.** `getting-started/` and `using/` are for anyone in the household; `admin/` for whoever runs
  ChefBot; `troubleshooting/` is symptom first; `developers/` explains design and links to CONTRIBUTING.md for rules.
- **One task per page.** Open with one or two sentences on who it's for and what they'll have done. Then numbered
  steps, then "If something goes wrong" linking to `troubleshooting/`.
- **Plain words, short sentences.** UI labels in **bold**, exactly as the page shows them (read the `.svelte` file).
  Commands ready to copy, with Podman and Docker Compose versions where they differ.
- **Facts from the code, never from memory.** Defaults come from `ResourceAccess/Contracts/Messages/*Settings.cs`,
  routes from `src/ChefBot.Api/Endpoints/`, labels from `src/ChefBot.Web/src/`, error wording from the `throw` sites.
  Don't promise timings or behaviour the code doesn't show.
- **Links:** relative between `.md` files inside `docs/`, always to a file (`../admin/settings.md#advanced`), never to a
  directory. Anything outside `docs/` (CONTRIBUTING.md, `deploy/`, source files) gets an absolute
  `https://github.com/FreeReins/chef-bot/blob/main/…` link, or the strict build and `/help` break.
- **Only syntax both GitHub and MkDocs render:** tables, fenced code, Mermaid, and GitHub alerts (`> [!NOTE]`,
  `> [!TIP]`, `> [!WARNING]`). No definition lists, no MkDocs-only admonitions (`!!!`), no tabs.
- **Say it once.** Link to the page that owns a fact instead of repeating it. The table below says who owns what.
- **Solo households are first class:** never imply ChefBot needs more than one person.

## Adding a page

1. Write it in the right folder, named in the reader's words (`backups-and-restore.md`).
2. Add it to `nav` in `mkdocs.yml`, and to `docs/index.md` if people would look for it there.
3. If a Settings section or screen is about it, point the section's `help` in
   `src/ChefBot.Web/src/tabs/Settings.svelte` at it (or use `<HelpLink page="…">` from `components/HelpLink.svelte`).

Renaming or moving a page breaks `/help` links in the app: search `src/ChefBot.Web/src` for the old path.

## Which page covers what

| When this changes in the code | Update |
|---|---|
| An endpoint (`src/ChefBot.Api/Endpoints/`) | `reference/api.md` |
| A setting in `appsettings.json`, `chefbot.env.example` or an `Https`/`Planner` option | `admin/settings.md` (environment table), plus the page for that feature |
| A Settings panel (`src/ChefBot.Web/src/settings/<X>Panel.svelte`) | `admin/settings.md` (that section), plus its guide (the `help` path in `Settings.svelte`) |
| Setup wizard (`screens/Setup*.svelte`) | `getting-started/first-run.md` |
| Plan tab, planning, plan styles | `using/weekly-routine.md`, `using/plan-styles.md`, `developers/planner.md` |
| Shopping, stores, to-do apps | `using/shopping-list.md`, `admin/to-do-apps.md` |
| Pantry, swaps | `using/pantry.md` |
| Recipes, imports, exports | `using/recipes.md`, `troubleshooting/recipe-imports.md` |
| Cooking mode | `using/cooking-mode.md` |
| Calendar feed | `using/calendar.md` |
| History, achievements | `using/history-and-achievements.md` |
| Notifications and their payloads | `admin/notifications.md` |
| Accounts, lockout, invites | `admin/people-and-accounts.md`, `troubleshooting/signing-in.md` |
| HTTPS, certificates, proxies | `admin/https-and-certificates.md`, `admin/own-certificate.md`, `admin/reverse-proxy.md` |
| Backups | `admin/backups-and-restore.md`, `troubleshooting/backups.md` |
| A new table or schema change | `admin/upgrading.md` ("Versions that needed this"), `developers/data-model.md` |
| Ports, volumes, `deploy/` units, `deploy/compose/compose.yaml`, Dockerfile | `getting-started/install-*.md`, `getting-started/requirements.md`, `developers/building-and-releasing.md` |
| Messages, handlers, queues, scheduled jobs | `developers/messaging-and-scheduling.md`, `admin/monitoring-and-logs.md` |
| Layers, architecture rules | `developers/architecture.md` (rules themselves stay in CONTRIBUTING.md) |
| A new user-facing error message | the matching `troubleshooting/` page |
| Something ChefBot can't do | `reference/limits.md` |

The root `README.md` is the showcase: a feature list with one link per feature. Change it only when a headline feature
is added or removed.

## Screenshots

`docs/images/` holds screenshots cropped to the first screen: `<view>-desktop-light.png`, `-desktop-dark.png` and
`-phone.png` (541 px wide). Retake them after a visible change:

```sh
./dev.sh web && ./dev.sh web-shot /plan     # full pages into src/ChefBot.Web/shots/
```

then crop with Pillow: desktop to `(0, 0, 1280, 720)`, phone to `(0, 0, 1082, 2202)` resized to `541×1101`, saved with
`optimize=True`.
