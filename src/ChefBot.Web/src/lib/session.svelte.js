import { listenForAddresses, showAddress } from './addresses.js';
import { api } from './api.js';
import { forgetSnapshot, restoreSnapshot } from './offline.svelte.js';
import { loadOutbox, report, sendOutbox } from './outbox.svelte.js';
import { loadPantry, loadPlan } from './plan.svelte.js';
import { loadRecipes } from './recipes.svelte.js';
import { loadPeople, loadShoppingTargets, loadStores, loadStyles, loadSystem } from './settings.svelte.js';
import { loadManualItems } from './shopping.svelte.js';
import { plural } from './format.js';
import { app, ask, isAdmin, run, toast } from './state.svelte.js';

// ---------- signing in and out ----------

/**
 * Loads everything the signed-in views need, then shows the app. Shopping changes made offline that are still waiting
 * are sent first, so what loads already includes them. The pantry is loaded even with a plan, so the Pantry tab
 * works offline without having been opened.
 */
export async function enterApp() {
  if (app.linkToken) {
    history.replaceState(null, '', location.pathname);
    app.linkToken = null;
  }
  app.offline = false;
  app.offlineSince = null;
  loadOutbox(app.session.user.id);
  const sent = app.outbox.length ? await sendOutbox() : null;
  await Promise.all([loadRecipes(), loadPlan(), loadManualItems(), loadPantry(), loadSystem(), loadStores(), loadShoppingTargets(), loadStyles(), isAdmin() ? loadPeople() : null]);
  await showAddress();
  if (sent) report(sent);
}

export async function signOut() {
  if (app.outbox.length && !(await ask({
    title: 'Sign out without sending your changes?',
    message: `${plural(app.outbox.length, 'shopping change')} made offline haven't reached ChefBot yet. Signing out throws them away.`,
    confirmLabel: 'Sign out',
    danger: true,
  }))) return;
  return run(async () => {
    await api('POST', '/api/account/sign-out');
    forgetSnapshot();
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
  try {
    app.session = await api('GET', '/api/session');
  } catch (error) {
    if (!error.offline) throw error;
    // No connection: show the shopping list and pantry saved last time, if there is one.
    if (restoreSnapshot()) {
      await showAddress();
    } else {
      app.session = { setupRequired: false, user: null };
      app.screen = 'sign-in';
      toast("ChefBot can't be reached, and this device hasn't saved a shopping list yet. Open ChefBot once while connected.", 'error');
    }
    return;
  }
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
