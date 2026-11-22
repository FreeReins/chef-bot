import { listenForAddresses, showAddress } from './addresses.js';
import { api } from './api.js';
import { loadPlan } from './plan.svelte.js';
import { loadRecipes } from './recipes.svelte.js';
import { loadPeople, loadShoppingTargets, loadStores, loadStyles, loadSystem } from './settings.svelte.js';
import { loadManualItems } from './shopping.svelte.js';
import { app, isAdmin, run, toast } from './state.svelte.js';

// ---------- signing in and out ----------

/** Loads everything the signed-in views need, then shows the app. */
export async function enterApp() {
  if (app.linkToken) {
    history.replaceState(null, '', location.pathname);
    app.linkToken = null;
  }
  await Promise.all([loadRecipes(), loadPlan(), loadManualItems(), loadSystem(), loadStores(), loadShoppingTargets(), loadStyles(), isAdmin() ? loadPeople() : null]);
  await showAddress();
}

export function signOut() {
  return run(async () => {
    await api('POST', '/api/account/sign-out');
    app.session.user = null;
    app.plan = null;
    app.plans = [];
    app.keep = [];
    app.toasts = [];
    app.screen = 'sign-in';
  });
}

/** Works out which screen to show first: setup, the app, an invite or reset link, or sign-in. */
export async function start() {
  listenForAddresses();
  app.session = await api('GET', '/api/session');
  if (app.session.setupRequired) {
    app.screen = 'setup-account';
  } else if (app.session.user) {
    await enterApp();
  } else if (app.linkToken) {
    await api('GET', `/api/account/link?token=${encodeURIComponent(app.linkToken)}`)
      .then((link) => { app.link = link; app.screen = link.purpose === 'Invite' ? 'join' : 'reset'; })
      .catch((error) => { app.screen = 'sign-in'; toast(error.message, 'error'); });
  } else {
    app.screen = 'sign-in';
  }
}
