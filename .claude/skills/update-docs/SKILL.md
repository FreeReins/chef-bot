---
name: update-docs
description: "Write or change ChefBot's documentation in docs/ (user guides, admin, troubleshooting, API reference, developer guide), the README showcase, or docs screenshots, and keep the in-app Help links pointing at the right pages. USE FOR: document a feature, fix or add a docs page, update the API or settings reference, a troubleshooting entry, README changes, retake docs screenshots. DO NOT USE FOR: code changes (add-core-operation, add-stored-entity, add-web-screen), CONTRIBUTING.md conventions alone."
---

# Update the docs

Read `docs/AGENTS.md` first: it has the writing rules, the strict build command, and a table of which page covers
which part of the code. Open only the pages that table names; don't read the whole tree.

## Steps

1. **Find the owner page** in the table in `docs/AGENTS.md`. Change that page; elsewhere, link to it rather than repeat it.
2. **Take facts from the code**, not from other pages: labels from the `.svelte` file, defaults from the settings
   contracts, routes from `src/ChefBot.Api/Endpoints/`, messages from the `throw` sites. Pages can be out of date.
3. **Write for the page's audience** (household, admin, or developer), one task per page, steps numbered, UI labels in
   bold, commands for both Podman and Docker Compose where they differ.
4. **New page:** add it to `nav` in `mkdocs.yml`, and to `docs/index.md` if people would look for it there. If a
   Settings section is about it, set that section's `help` in `src/ChefBot.Web/src/tabs/Settings.svelte`.
5. **Moved or renamed page:** search `src/ChefBot.Web/src` for `help` paths to it, and `docs/` for links.
6. **Check:** run the strict MkDocs build from `docs/AGENTS.md` (both variants). No WARNING lines. If you touched
   `.svelte` files, also `./dev.sh web` and `./dev.sh web-test`.

## Screenshots

Only when the README or a page shows the changed screen. Follow "Screenshots" in `docs/AGENTS.md`, then delete
`src/ChefBot.Web/shots/`.

## Don't

- Don't build the container image or run `./dev.sh` (without `web`) to check docs.
- Don't use MkDocs-only syntax (`!!!` admonitions, tabs, definition lists): GitHub must render every page.
- Don't promise timings, limits or behaviour the code doesn't show.
