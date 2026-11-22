# Frequently asked questions

## Is ChefBot free?

Yes. It's open source under the GNU GPL v3.0. If it helps your household, you can
[support its development](https://buymeacoffee.com/strifer.btw).

## Does ChefBot send my data anywhere?

No. Everything stays in your own database. ChefBot only goes out to the internet to download a recipe page you give
it, that recipe's photos, and the webhook, to-do apps and calendar apps you set up yourself.

## Do I need to be technical to run it?

You need to be comfortable copying a few commands into a terminal, or pasting a compose file into a NAS's container
app. After that, everything is done in the web page. See [Install](../getting-started/requirements.md).

## Does it work for one person?

Yes. Nothing depends on household size. Set **People you usually cook for** to 1 under **Settings → Planning**.
Inviting others is optional.

## Can I use it on my phone?

Yes. It's a web page that works on any phone, and the shopping list works with no signal once the phone trusts
ChefBot's certificate. Add it to your home screen to open it like an app. See
[Offline on your phone](../using/offline-on-your-phone.md).

## Can I bring my recipes from Mealie, Tandoor or a website?

Yes: a Mealie backup `.zip` with its photos, schema.org recipe files (Tandoor and many other apps export these), or a
link to almost any recipe site. See [Recipes](../using/recipes.md). Recipes download as schema.org JSON too, so you can
take them elsewhere.

## Can I reach ChefBot away from home?

Use a VPN such as WireGuard or Tailscale, or put it behind a reverse proxy with a real certificate. See
[Reach ChefBot over Tailscale](../admin/tailscale.md) and [Behind a reverse proxy](../admin/reverse-proxy.md).

## Does it work with Home Assistant?

Yes, three ways: reminders go to a Home Assistant webhook, the shopping list can go to any Home Assistant to-do list,
and the meal plan can be a calendar. See [Notifications](../admin/notifications.md), [To-do apps](../admin/to-do-apps.md)
and [Calendar](../using/calendar.md).

## Can an AI assistant use it?

Yes. ChefBot is an MCP server. Claude and other MCP clients can plan weeks, swap meals and manage the pantry and
shopping list. See [AI assistants](../admin/ai-assistants.md).

## How do I get help?

See [Troubleshooting](../troubleshooting/index.md) first. If that doesn't help,
[open an issue](https://github.com/Jamiethenextkevinbaconhamm/chef-bot/issues/new/choose).
