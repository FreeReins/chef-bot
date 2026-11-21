---
name: add-web-screen
description: "Add a tab or screen to ChefBot's Svelte page: tab list, routing, area colour, test stubs, accessibility rows, screenshots. USE FOR: new tab, new view, new screen state, screenshot the web page. DO NOT USE FOR: backend endpoints (add-core-operation), small edits inside one component."
---

# Add a web screen

All paths are under `src/ChefBot.Web/`.

1. **Component** `src/tabs/<Tab>.svelte`: load in `onMount(() => { run(() => load()); })` with `api()` from
   `lib/api.js`. Keep state local unless another screen needs it (then `lib/app.svelte.js`). An empty state
   uses `<div class="card empty">` with `.empty-icon`. Use `<h2>` for the tab title and `<h3>` below it.
2. **Tab**: in `src/App.svelte` add an import, a `TABS` entry (Lucide icon from `@lucide/svelte/icons/<name>`;
   check `node_modules/@lucide/svelte/dist/icons`), and `TAB_VIEWS`. In `src/lib/addresses.js` add the key to
   `TAB_KEYS` (and `ADMIN_TABS` if admin-only) and to the address comment.
3. **Colour**: `--area-<tab>: light-dark(light, dark)` beside the others in `src/styles.css`.
4. **Stubs**: a shape in `tests/data.js` mirroring the C# record, a default route in `defaults()` in
   `tests/fixtures.js`, and `VIEWS` rows in `tests/a11y.spec.js`. Routes match the path only; a stub that depends
   on query values is a function: `(request) => new URL(request.url()).searchParams...`.
5. **Check**: run `./dev.sh web`, then `./dev.sh web-test --grep "<tab>"`, then the full `./dev.sh web-test`.

## Example

The History tab: `src/tabs/History.svelte`, `src/lib/history.js`, `history()` in `tests/data.js`.

## Screenshot

Run `./dev.sh web-shot /<tab>` and read `src/ChefBot.Web/shots/<tab>-{desktop-light,desktop-dark,phone}.png`.
The page is answered from the test stubs, so add those first. Delete `shots/` afterwards.

## Troubleshooting

- Svelte drops a space before `{#if}`; put the separator inside an expression: ``{` · ${text}`}``.
- A keyed `{#each}` throws on duplicate keys when older pages repeat items.
- Axe waits for animations to finish, so entrance animations are fine. Respect `prefers-reduced-motion`.
