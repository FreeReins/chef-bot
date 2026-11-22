# Monitoring and logs

This page shows how to check that ChefBot is running, where its logs are, and what to collect before asking for help.

## Is it running?

| Setup | Command |
|---|---|
| Podman | `systemctl --user status chefbot chefbot-db` |
| Docker Compose | `docker compose ps` |

Both containers should be running, and the database should be healthy.

## Health check

`GET /health` answers `{"status":"ok"}` when ChefBot is up. It needs no sign-in and is never redirected to HTTPS, so
uptime monitors such as Uptime Kuma or Home Assistant can use it:

```sh
curl http://localhost:8080/health
```

## Logs

**In ChefBot:** admins open **Settings → Logs** for the last 1000 entries since ChefBot started. Filter to warnings and
errors, or search.

**Full logs:**

| Setup | App | Database |
|---|---|---|
| Podman | `journalctl --user -u chefbot -f` | `journalctl --user -u chefbot-db -f` |
| Docker Compose | `docker compose logs -f chefbot` | `docker compose logs -f db` |

Leave out `-f` to read the log so far instead of following it. With Podman, `--since "1 hour ago"` narrows it down.

For more detail while tracking down a problem, set `Logging__LogLevel__Default=Debug`, restart, reproduce the
problem, then set it back.

## Scheduled jobs

Next week's plan, the daily reminders and the daily backup are scheduled jobs, kept in the database. Each run
schedules the next one, so they survive restarts. If ChefBot was stopped when a job was due, it runs as soon as
ChefBot starts again:

- the weekly plan is made, as long as its week hasn't started. Each week is planned once, so a plan you clear stays
  cleared;
- daily reminders and the backup run if their time has passed that day.

A webhook or photo site that's down is tried again over the next half hour. Anything that still fails is logged.

## Before asking for help

Collect:

1. The ChefBot version (the release tag, or `git log -1 --oneline` with Podman).
2. How you installed it (Podman, Docker Compose, NAS…).
3. The log around the time of the problem, with passwords and tokens removed.
4. What you did, what you expected, and what happened.

Then [open an issue](https://github.com/Jamiethenextkevinbaconhamm/chef-bot/issues/new/choose).
