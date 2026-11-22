# Troubleshooting

Find your problem below and follow the link. Every page says what causes the problem and how to fix it. Most fixes
need only the web page or one command.

## Find your problem

| What you see | Go to |
|---|---|
| ChefBot won't start, or the page doesn't open | [Install and startup](install-and-startup.md) |
| "ChefBot can't write to …" in the log | [Install and startup](install-and-startup.md#chefbot-cant-write-to-a-folder) |
| The database won't accept the password | [Install and startup](install-and-startup.md#the-database-rejects-the-password) |
| "Your connection is not private" or another certificate warning | [HTTPS and certificates](https-and-certificates.md) |
| A phone won't trust ChefBot, or the shopping list doesn't work offline | [HTTPS and certificates](https-and-certificates.md#a-phone-wont-trust-the-certificate) |
| "This account is locked…" | [Signing in](signing-in.md#an-account-is-locked) |
| Forgot a password, or the admin is locked out | [Signing in](signing-in.md) |
| Everyone was signed out | [Signing in](signing-in.md#everyone-was-signed-out) |
| The plan is short, has repeats, or ignores a rule | [Planning](planning.md) |
| Something I have is still on the shopping list | [Planning](planning.md#something-you-have-is-still-on-the-shopping-list) |
| A recipe link won't import | [Recipe imports](recipe-imports.md) |
| Reminders, to-do apps, the calendar or an AI assistant don't work | [Notifications and integrations](notifications-and-integrations.md) |
| A backup failed or won't restore | [Backups](backups.md) |

## First checks

These solve a lot of problems:

1. **Is ChefBot running?** `systemctl --user status chefbot` (Podman) or `docker compose ps` (Docker).
2. **What does the log say?** **Settings → Logs** in ChefBot, or `journalctl --user -u chefbot` /
   `docker compose logs chefbot`. ChefBot's messages say what went wrong and what to do.
3. **Reload the page.** After an upgrade, the browser may still have the old page.

## Still stuck?

[Collect the details](../admin/monitoring-and-logs.md#before-asking-for-help) and
[open an issue](https://github.com/Jamiethenextkevinbaconhamm/chef-bot/issues/new/choose).
