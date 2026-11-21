// Each view has its own address, so it can be bookmarked, reloaded or opened in a new tab:
//   /plan  /shopping  /pantry  /recipes  /recipes/{id}  /recipes/samples  /history  /people  /settings/{section}
// and the Recipes dialogs: /recipes/new/{link|write|files}  /recipes/{id}/{edit|photos|cook}
// The address is the source of truth: links and buttons call go(path), and showAddress() shows what it names.
import { api } from './api.js';
import { app, run, isAdmin, clearErrors } from './app.svelte.js';

const TAB_KEYS = ['plan', 'shopping', 'pantry', 'recipes', 'history', 'people', 'settings'];
/** Tabs that were renamed, so old bookmarks still land somewhere sensible. */
const RENAMED_TABS = { ingredients: 'shopping', meals: 'plan', schedule: 'plan' };
const ADMIN_TABS = ['people'];
/** The tabs of the Add a recipe dialog, and the dialogs a recipe's page opens. */
const ADD_TABS = ['link', 'write', 'files'];
const RECIPE_DIALOGS = ['edit', 'photos', 'cook'];

/** Moves to an address inside the page and shows it. */
export async function go(path, { replace = false } = {}) {
  const here = location.pathname + location.search;
  if (path !== here) {
    // Each new entry remembers the address it was opened from, so closeDialog() can step back to it.
    if (replace) history.replaceState(history.state, '', path);
    else history.pushState({ from: here }, '', path);
  }
  clearErrors();
  await run(showAddress);
}

/**
 * Closes a dialog that has its own address by moving to `base`, the address behind it. When the dialog was opened from
 * there, this goes back in history instead, so Back afterwards doesn't reopen it.
 */
export async function closeDialog(base) {
  if (history.state?.from === base) history.back();
  else await go(base, { replace: true });
}

/** Shows the view the address bar names, once signed in. Unknown addresses go to the plan. */
export async function showAddress() {
  const [first = 'plan', second = null, third = null] = location.pathname.split('/').filter(Boolean).map(decodeURIComponent);
  if (RENAMED_TABS[first]) {
    history.replaceState(null, '', `/${RENAMED_TABS[first]}`);
    return showAddress();
  }
  if (!TAB_KEYS.includes(first) || (ADMIN_TABS.includes(first) && !isAdmin())) {
    history.replaceState(null, '', '/plan');
    return showAddress();
  }

  if (first === 'recipes' && second === 'new' && !ADD_TABS.includes(third)) {
    history.replaceState(null, '', '/recipes/new/link');
    return showAddress();
  }

  app.tab = first;
  app.recipeId = first === 'recipes' && second !== 'samples' && second !== 'new' ? second : null;
  app.recipeDialog = first !== 'recipes' ? null
    : second === 'new' ? `new-${third}`
      : app.recipeId && RECIPE_DIALOGS.includes(third) ? third : null;
  if (first === 'settings' && second) app.settingsSection = second;
  app.swap = null;

  if (first === 'recipes' && second === 'samples' && isAdmin()) {
    app.samples = await api('GET', '/api/setup/samples');
    app.screen = 'setup-samples';
  } else {
    app.screen = 'app';
  }
}

/**
 * Follows a click on a link to another view inside the page without reloading it. Returns true when it did;
 * ctrl-, shift- and middle-clicks, downloads, API links and the /docs page are left to the browser so they open a new tab.
 */
export function followLink(event) {
  const link = event.target.closest?.('a[href]');
  if (!link || event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return false;
  const url = new URL(link.href, location.href);
  if (url.origin !== location.origin || link.target || link.hasAttribute('download')
      || /^\/(api|openapi|docs|health)(\/|$)/.test(url.pathname)) return false;
  event.preventDefault();
  go(url.pathname + url.search);
  return true;
}

/** Follows in-page links and the back and forward buttons, once someone is signed in. Called once at start. */
export function listenForAddresses() {
  addEventListener('popstate', () => { if (app.session?.user) go(location.pathname + location.search, { replace: true }); });
  document.addEventListener('click', (event) => { if (app.session?.user) followLink(event); });
}
