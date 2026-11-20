// Each view has its own address, so it can be bookmarked, reloaded or opened in a new tab:
//   /meals  /schedule  /ingredients  /recipes  /recipes/{id}  /recipes/samples  /people  /settings/{section}
// The address is the source of truth: links and buttons call go(path), and showAddress() shows what it names.
import { api } from './api.js';
import { app, run, isAdmin, clearMessages } from './app.svelte.js';

const TAB_KEYS = ['meals', 'schedule', 'ingredients', 'recipes', 'people', 'settings'];
const ADMIN_TABS = ['people'];

/** Moves to an address inside the page and shows it. */
export async function go(path, { replace = false } = {}) {
  if (path !== location.pathname + location.search) {
    history[replace ? 'replaceState' : 'pushState'](null, '', path);
  }
  clearMessages();
  await run(showAddress);
}

/** Shows the view the address bar names, once signed in. Unknown addresses go to the meals. */
export async function showAddress() {
  const [first = 'meals', second = null] = location.pathname.split('/').filter(Boolean).map(decodeURIComponent);
  if (!TAB_KEYS.includes(first) || (ADMIN_TABS.includes(first) && !isAdmin())) {
    history.replaceState(null, '', '/meals');
    return showAddress();
  }

  app.tab = first;
  app.recipeId = first === 'recipes' && second !== 'samples' ? second : null;
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
 * ctrl-, shift- and middle-clicks, downloads and API links are left to the browser so they open a new tab.
 */
export function followLink(event) {
  const link = event.target.closest?.('a[href]');
  if (!link || event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return false;
  const url = new URL(link.href, location.href);
  if (url.origin !== location.origin || link.target || link.hasAttribute('download')
      || /^\/(api|openapi|health)(\/|$)/.test(url.pathname)) return false;
  event.preventDefault();
  go(url.pathname + url.search);
  return true;
}

/** Follows in-page links and the back and forward buttons, once someone is signed in. Called once at start. */
export function listenForAddresses() {
  addEventListener('popstate', () => { if (app.session?.user) go(location.pathname + location.search, { replace: true }); });
  document.addEventListener('click', (event) => { if (app.session?.user) followLink(event); });
}
