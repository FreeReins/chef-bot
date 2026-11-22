# Building and releasing

## The container image

The [`Dockerfile`](https://github.com/Jamiethenextkevinbaconhamm/chef-bot/blob/main/Dockerfile) builds everything, so
only Podman or Docker is needed:

| Stage | Does |
|---|---|
| `web` | Builds the Svelte page with Node |
| `docs` | Builds this documentation with MkDocs, for `/help` |
| `build` | Restores and builds the .NET solution, and writes Wolverine's generated code |
| `test` | Runs the tests |
| `publish` | Publishes the API |
| `final` | The runtime image: the app, the page in `wwwroot/`, the docs in `wwwroot/help/`, running as user 1654 |

`deploy/podman/install.sh` runs the `test` stage, then builds the image as `localhost/chefbot:latest`. While
developing, run the tests with `dotnet run --project tests/ChefBot.Core.Tests` instead: it's much faster.

## Code made at build time

ChefBot doesn't use reflection-based JSON or compile code while running:

- Every JSON body, message and file goes through a source-generated `JsonSerializerContext`.
- Release builds compile in Wolverine's handler code, written beforehand to `src/ChefBot.Api/Internal/Generated/`.
  The image build writes it from the source. Debug builds generate it at startup.

[Code made at build time](https://github.com/Jamiethenextkevinbaconhamm/chef-bot/blob/main/CONTRIBUTING.md#code-made-at-build-time)
in CONTRIBUTING.md has the details, including how to run a Release build locally.

## Releases

Pushing a tag such as `v1.2.3` runs
[`.github/workflows/release.yml`](https://github.com/Jamiethenextkevinbaconhamm/chef-bot/blob/main/.github/workflows/release.yml):

1. Builds the image natively for `linux/amd64` and `linux/arm64`, each running the tests.
2. Pushes both to `ghcr.io/jamiethenextkevinbaconhamm/chef-bot` and joins them into one multi-arch image tagged
   `1.2.3`, `1.2` and `latest`.
3. Creates a GitHub release with generated notes and the documentation as a zip, to read with no service running.

```sh
git tag v1.2.3
git push origin v1.2.3
```

A release that changes the database tables must say so in its notes, with a link to
[Upgrading](../admin/upgrading.md#when-a-version-changes-the-database).

## The documentation

These pages are Markdown in `docs/`, readable on GitHub as they are. The same files are published three ways:

| Where | How |
|---|---|
| GitHub | Read as they are |
| The documentation site | [`.github/workflows/docs.yml`](https://github.com/Jamiethenextkevinbaconhamm/chef-bot/blob/main/.github/workflows/docs.yml) builds them with MkDocs Material and publishes them to GitHub Pages on every push to `main` |
| Inside ChefBot, at `/help` | The `docs` stage of the image builds them, and the app serves them without sign-in |

To build them locally:

```sh
python3 -m venv .venv && .venv/bin/pip install -r docs/requirements.txt
.venv/bin/mkdocs serve        # http://127.0.0.1:8000, reloads as you save
.venv/bin/mkdocs build --strict
```

The site's menu is `nav` in [`mkdocs.yml`](https://github.com/Jamiethenextkevinbaconhamm/chef-bot/blob/main/mkdocs.yml):
a new page needs a line there. [`docs/AGENTS.md`](https://github.com/Jamiethenextkevinbaconhamm/chef-bot/blob/main/docs/AGENTS.md)
has the writing rules.

[`.github/workflows/links.yml`](https://github.com/Jamiethenextkevinbaconhamm/chef-bot/blob/main/.github/workflows/links.yml)
checks the links in every Markdown file on pull requests, and outside links once a week.
