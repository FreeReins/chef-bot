# Developer guide

This section explains how ChefBot is built and why, for anyone who wants to change it, extend it, or just understand
it. For the day-to-day rules (where a class goes, style, checks to run), read
[CONTRIBUTING.md](https://github.com/FreeReins/chef-bot/blob/main/CONTRIBUTING.md). These pages explain the design behind those rules.

## The short version

- **.NET 10** backend: an HTTP API on [Wolverine](https://wolverinefx.net), EF Core on **PostgreSQL**.
- **Svelte 5** web page, built with Vite and served by the API.
- **One container** for the app, one for the database. Rootless Podman Quadlet units, or Docker Compose.
- **Layered core** with calls that only go down, checked by tests.
- **Event-driven** between areas: a change is saved with the events it caused, and handlers react afterwards.
- **No reflection at runtime:** JSON and Wolverine's code are generated at build time.

## Pages

| Page | Covers |
|---|---|
| [Architecture](architecture.md) | The projects and layers, and why they're split that way |
| [Request lifecycle](request-lifecycle.md) | What happens between an HTTP request and the database |
| [Messaging and scheduling](messaging-and-scheduling.md) | Events, handlers, queues, retries and timed jobs |
| [The planner](planner.md) | How a week is chosen and kept in step with the pantry |
| [Data model](data-model.md) | Tables, schema creation, backups |
| [Security](security.md) | Sign-in, roles, tokens, HTTPS and outbound requests |
| [The web page](web-client.md) | The Svelte client, offline mode and live updates |
| [Testing](testing.md) | Unit, architecture and accessibility tests |
| [Building and releasing](building-and-releasing.md) | The image, generated code, releases and these docs |
| [Adding a recipe site](adding-a-recipe-site.md) | Teaching ChefBot to read a new website |

## Get started

```sh
git clone https://github.com/FreeReins/chef-bot.git && cd chef-bot
./dev.sh          # dev database in a container, app with hot reload on http://localhost:8080
```

You need the .NET 10 SDK and Podman. Node isn't needed on the host. See
[Build and test](https://github.com/FreeReins/chef-bot/blob/main/CONTRIBUTING.md#build-and-test).
