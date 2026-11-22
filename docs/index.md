# ChefBot documentation

ChefBot is a self-hosted meal planner. It plans a week of meals around what you already have, builds the shopping
list, tracks your pantry and helps you use food up before it goes off. This is the place to learn how to install it,
use it, keep it running and fix it when something goes wrong.

These pages are plain Markdown in the [`docs/` folder](https://github.com/FreeReins/chef-bot/tree/main/docs)
of the repository. Read them on GitHub, on the [documentation site](https://jamiethenextkevinbaconhamm.github.io/chef-bot/),
in any text editor, or inside ChefBot itself: press the **?** button at the top of any screen.

## I want to…

### Try ChefBot

- [Find out what ChefBot does](getting-started/what-is-chefbot.md)
- [Check what I need to run it](getting-started/requirements.md)
- Install it: [with Podman on Linux](getting-started/install-podman.md),
  [with Docker](getting-started/install-docker-compose.md), [on Windows](getting-started/install-windows.md), or
  [on a NAS, macOS or a Raspberry Pi](getting-started/install-other-platforms.md)
- [Set it up the first time](getting-started/first-run.md)

### Use it day to day

- [Plan, shop and cook through a week](using/weekly-routine.md)
- [Choose what a week holds with plan styles](using/plan-styles.md)
- [Add and import recipes](using/recipes.md), then [cook with large text and timers](using/cooking-mode.md)
- [Use the shopping list](using/shopping-list.md) and [keep track of the pantry](using/pantry.md)
- [Use the shopping list with no signal](using/offline-on-your-phone.md)
- [See the meal plan in your calendar](using/calendar.md)
- [Look back at what you've cooked](using/history-and-achievements.md)

### Run ChefBot for my household

- [Invite people, reset passwords and unlock accounts](admin/people-and-accounts.md)
- [Set up HTTPS](admin/https-and-certificates.md), [use my own certificate](admin/own-certificate.md) or
  [run it behind a reverse proxy](admin/reverse-proxy.md)
- [Reach it away from home over Tailscale](admin/tailscale.md)
- [Look up any setting](admin/settings.md)
- [Send reminders to Home Assistant or anywhere else](admin/notifications.md)
- [Send the shopping list to a to-do app](admin/to-do-apps.md)
- [Connect an AI assistant such as Claude](admin/ai-assistants.md)
- [Back up and restore](admin/backups-and-restore.md)
- [Upgrade to a new version](admin/upgrading.md)
- [Check that it's running and read its logs](admin/monitoring-and-logs.md)
- [Change the ingredient list, stores or starter plan styles](admin/customising-catalogs.md)
- [Uninstall it](admin/uninstall.md)

### Fix a problem

Start at [Troubleshooting](troubleshooting/index.md). It lists common symptoms and links each one to its fix.

### Automate it or build on it

- [The HTTP API](reference/api.md)
- [How ChefBot is built](developers/index.md)

## More

- [Frequently asked questions](reference/faq.md)
- [Limits worth knowing](reference/limits.md)
- [Glossary](reference/glossary.md)
