# The web page

`src/ChefBot.Web` is a Svelte 5 page in plain JavaScript, built with Vite. It's a thin client: everything it shows comes
from the [HTTP API](../reference/api.md). Before changing it, read
[`src/ChefBot.Web/AGENTS.md`](https://github.com/FreeReins/chef-bot/blob/main/src/ChefBot.Web/AGENTS.md) and [The web page](https://github.com/FreeReins/chef-bot/blob/main/CONTRIBUTING.md#the-web-page) in
CONTRIBUTING.md.

## Layout

| Folder | Holds |
|---|---|
| `src/App.svelte` | Header, tabs and footer |
| `src/tabs/` | One component per tab: `Plan.svelte`, `Recipes.svelte`, … |
| `src/screens/` | Setup and sign-in |
| `src/settings/` | One panel per Settings section |
| `src/components/` | Pieces used in more than one place |
| `src/lib/` | State (`app`), `run()`, and the actions for each area |
| `public/sw.js` | The service worker |

## State and actions

`lib/state.svelte.js` holds everything the page shows. The actions that load or change it live in one module per area
(`plan`, `shopping`, `recipes`, `setup`, `settings`, `session`), re-exported from `lib/app.svelte.js`. Anything that
calls the API is wrapped in `run()`, so failures show as a message. Settings save by themselves (`autosave()`).

## Addresses

Every view has its own address (`/recipes/{id}`, `/settings/backups`), so it can be bookmarked. The server answers any
path that isn't a file, `/api` or `/help` with `index.html`.

## Offline

- `public/sw.js` keeps the page itself (`index.html` and `/assets`), never `/api`, `/docs`, `/mcp` or `/help`.
- `lib/offline.svelte.js` saves what the Shopping and Pantry tabs show, and shows it when ChefBot can't be reached.
- `lib/outbox.svelte.js` queues shopping changes made offline and sends them in order once ChefBot answers.

## Live updates

The page listens to `/api/live`, a server-sent event stream naming what changed (`plan`, `shopping`, `pantry`…). It
then loads that part again through the API, so a change on one phone shows on the others straight away.

## Messages

`toast()` in `lib/state.svelte.js` is how any part of the page says what happened. While someone is signed in, a note
goes to their inbox (`lib/inbox.svelte.js`, `POST /api/inbox`), shown behind the bell in the header
(`components/Inbox.svelte`), so nothing covers the page. It appears at once as a local entry, replaced by the saved one;
one made offline stays on the page until a reload. Errors, notes with a button such as **Undo**, and anything before
sign-in still show in the corner (`components/Toasts.svelte`).

## Styling

`src/styles.css` holds colour tokens written as `light-dark(light, dark)` and shared building blocks. Each tab has an
area colour, `--area-<tab>`. The page follows the device's theme until someone picks one.

## Help links

The **?** button in the header, the **Help** link in the footer, and the "Guide:" line at the end of each Settings
section (`help` in `SECTIONS`, `src/tabs/Settings.svelte`) open `/help/…`, the documentation built into the image.
`helpUrl(path)` in `src/lib/help.js` builds the address, and `components/HelpLink.svelte` draws a link. The server
side is `UseChefBotWebPage` in `src/ChefBot.Api/WebPage.cs`.
