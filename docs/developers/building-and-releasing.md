# Building and releasing

## The container image

The [`Dockerfile`](https://github.com/FreeReins/chef-bot/blob/main/Dockerfile) builds everything, so
only Podman or Docker is needed:

| Stage | Does |
|---|---|
| `web` | Builds the Svelte page with Node |
| `docs` | Builds this documentation with MkDocs, for `/help` |
| `build` | Restores and builds the .NET solution, and writes Wolverine's generated code |
| `test` | Runs the tests |
| `publish` | Publishes the API |
| `postgres` | Installs PostgreSQL 17 from PostgreSQL's apt repository, so the server can be copied out without the package's dependencies (LLVM, Perl, systemd) |
| `final` | The runtime image: the app, the page in `wwwroot/`, the docs in `wwwroot/help/`, the PostgreSQL server in `/usr/lib/postgresql/17`, and `tini` as process 1, running as user 1654 with everything it keeps in `/data` |

`deploy/podman/install.sh --build` builds the image as `localhost/chefbot:latest`. While developing, run the tests
with `dotnet run --project tests/ChefBot.Core.Tests` instead: it's much faster.

## The bundled database

ChefBot runs its own PostgreSQL, so an install is one container or one Windows program.
`src/ChefBot.Api/Database/BundledDatabase.cs` does it, unless `Database:Mode` is `External`:

1. Before the services are registered, it points `ConnectionStrings:ChefBot` at `127.0.0.1:<Database:Port>`, with the
   password kept in `<Data:Root>/database/password`.
2. As the host starts (`BundledDatabaseLifetime`, before `ChefBotStartup` creates the tables and before the message
   bus), it runs `initdb` the first time, writes `chefbot.conf` (listen on 127.0.0.1 only, no socket files, no JIT),
   and starts the server with `pg_ctl`. A server left running by a ChefBot that was killed is used as it is.
3. Once the host has stopped, the message bus included, it stops the server with `pg_ctl stop -m fast`.

It refuses a data folder made by another major version of PostgreSQL. `./dev.sh` uses `Database:Mode=External`
with its own throwaway database container instead.

## The Windows app

[`deploy/windows/build.ps1`](https://github.com/FreeReins/chef-bot/blob/main/deploy/windows/build.ps1)
builds both Windows downloads, after the web page and docs are built:

1. Downloads EDB's PostgreSQL binaries zip pinned in `deploy/windows/postgres.json`, checks its SHA-256, and keeps
   only `bin`, `lib` and `share` (plus the Visual C++ runtime from the zip).
2. Publishes `src/ChefBot.Api` for `win-x64`, self-contained, with `ChefBotPayload` pointing at the page, the help,
   PostgreSQL and `appsettings.Production.json` (HTTPS on, as in the image). Once as a folder, renamed to
   `ChefBot.exe`, for the installer; once as a single self-extracting exe with the `ChefBot.Portable` switch, which
   keeps its data in `ChefBot-data` next to the exe.
3. On Windows, builds `ChefBot.msi` from `deploy/windows/Package.wxs` with WiX 5. It installs the **ChefBot** service
   (account `NT SERVICE\ChefBot`, started with `--Data:Root=C:\ProgramData\ChefBot`), firewall rules for 8080 and 8443
   on private networks, the event log source and Start menu shortcuts.

On Linux, `pwsh deploy/windows/build.ps1 -SkipInstaller` (in the .NET SDK container, which has PowerShell) builds the
portable exe for checking. Updating PostgreSQL: change the version, URL and checksum in `postgres.json`, and
`POSTGRES_MAJOR` in the Dockerfile only for a new major version, which needs a backup and restore (see
[Upgrading](../admin/upgrading.md#when-a-version-changes-the-database)).

## Code made at build time

ChefBot doesn't use reflection-based JSON or compile code while running:

- Every JSON body, message and file goes through a source-generated `JsonSerializerContext`.
- Release builds compile in Wolverine's handler code, written beforehand to `src/ChefBot.Api/Internal/Generated/`.
  The image build writes it from the source. Debug builds generate it at startup.

[Code made at build time](https://github.com/FreeReins/chef-bot/blob/main/CONTRIBUTING.md#code-made-at-build-time)
in CONTRIBUTING.md has the details, including how to run a Release build locally.

## Releases

Every merge to `main` runs
[`.github/workflows/release.yml`](https://github.com/FreeReins/chef-bot/blob/main/.github/workflows/release.yml).
It reads the Conventional Commit messages since the last tag to choose the version (`fix:` a patch, `feat:` a minor,
`!` a major; [Releases](https://github.com/FreeReins/chef-bot/blob/main/CONTRIBUTING.md#releases) in CONTRIBUTING.md
has the full table and the one-time GitHub setup). When the commits call for a release, it:

1. Builds the image natively for `linux/amd64` and `linux/arm64`, each running the tests.
2. Pushes both to `ghcr.io/freereins/chef-bot` and joins them into one multi-arch image tagged
   `1.2.3`, `1.2` and `latest`.
3. Builds the Windows installer and portable app on a Windows runner, after running the tests, and checks both:
   the portable app must answer `/health`, then again after being killed; the MSI must install a service that
   answers `/health`, and uninstall cleanly.
4. Creates a GitHub release with generated notes, the two Windows downloads, and the documentation as a zip, to read
   with no service running.

Nothing is tagged or published until every build and check has passed.

A release that changes the database tables carries a migration, which is applied when it starts, so people upgrade
as usual. Its notes should still say the tables changed, since going back to an older version then needs a restore (see
[Upgrading](../admin/upgrading.md#go-back-to-an-older-version)).

## The documentation

These pages are Markdown in `docs/`, readable on GitHub as they are. The same files are published three ways:

| Where | How |
|---|---|
| GitHub | Read as they are |
| The documentation site | [`.github/workflows/docs.yml`](https://github.com/FreeReins/chef-bot/blob/main/.github/workflows/docs.yml) builds them with MkDocs Material and publishes them to GitHub Pages on every push to `main` |
| Inside ChefBot, at `/help` | The `docs` stage of the image builds them, and the app serves them without sign-in |

To build them locally:

```sh
python3 -m venv .venv && .venv/bin/pip install -r docs/requirements.txt
.venv/bin/mkdocs serve        # http://127.0.0.1:8000, reloads as you save
.venv/bin/mkdocs build --strict
```

The site's menu is `nav` in [`mkdocs.yml`](https://github.com/FreeReins/chef-bot/blob/main/mkdocs.yml):
a new page needs a line there. [`docs/AGENTS.md`](https://github.com/FreeReins/chef-bot/blob/main/docs/AGENTS.md)
has the writing rules.

[`.github/workflows/links.yml`](https://github.com/FreeReins/chef-bot/blob/main/.github/workflows/links.yml)
checks the links in every Markdown file on pull requests, and outside links once a week.
