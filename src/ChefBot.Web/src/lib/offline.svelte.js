// Offline mode: what the Shopping and Pantry tabs need is kept in this browser as it changes, so they still open with
// no connection, such as on a phone in a shop. While ChefBot can't be reached (app.offline) the page checks every
// half minute, and as soon as it answers, sync() sends what changed meanwhile (outbox.svelte.js).
// The page itself is kept by the service worker in public/sw.js.
import { untrack } from 'svelte';
import { clearOutbox, loadOutbox, sync } from './outbox.svelte.js';
import { app } from './state.svelte.js';

const KEY = 'chefbot-offline';
/** The tabs that work offline; the others say they need ChefBot. */
export const OFFLINE_TABS = ['shopping', 'pantry'];
const CHECK_EVERY = 30_000;

/** When the shown data last came from ChefBot. */
let savedAt = null;

/** Checks for ChefBot again, without a message when it still can't be reached. */
const check = () => sync({ quiet: true }).catch(() => { /* still offline, or the next check tries again */ });

$effect.root(() => {
  // Saves the snapshot whenever what it holds changes, offline too, so a reload shows the changes made in the shop.
  $effect(() => {
    if (app.screen !== 'app' || !app.session?.user) return;
    if (!app.offline) savedAt = new Date().toISOString();
    const snapshot = {
      userId: app.session.user.id,
      savedAt: app.offline ? (app.offlineSince ?? savedAt) : savedAt,
      session: app.session,
      plans: app.plans,
      plan: app.plan,
      ingredients: app.ingredients,
      manualItems: app.manualItems,
      pantry: app.pantry,
      stores: { stores: app.stores?.stores ?? [], known: [], ingredients: [] },
    };
    try { localStorage.setItem(KEY, JSON.stringify(snapshot)); } catch { /* storage refused: no offline list */ }
  });

  // While offline, look for ChefBot every so often, and the moment the device says it is back online.
  $effect(() => {
    if (!app.offline) return;
    untrack(() => { app.offlineSince ??= savedAt; });
    const timer = setInterval(check, CHECK_EVERY);
    addEventListener('online', check);
    return () => {
      clearInterval(timer);
      removeEventListener('online', check);
    };
  });
});

/**
 * Shows what was saved last time, when ChefBot can't be reached as the page opens. Returns false when nothing was
 * saved, such as on a device that never opened ChefBot while connected.
 */
export function restoreSnapshot() {
  let snapshot;
  try { snapshot = JSON.parse(localStorage.getItem(KEY) ?? 'null'); } catch { snapshot = null; }
  if (!snapshot?.session?.user) return false;
  Object.assign(app, {
    session: snapshot.session,
    plans: snapshot.plans ?? [],
    plan: snapshot.plan ?? null,
    ingredients: snapshot.ingredients ?? null,
    manualItems: snapshot.manualItems ?? [],
    pantry: snapshot.pantry ?? [],
    stores: snapshot.stores ?? null,
    offlineSince: snapshot.savedAt,
    offline: true,
  });
  loadOutbox(snapshot.userId);
  return true;
}

/** Forgets the saved list and any unsent changes, for signing out on a shared device. */
export function forgetSnapshot() {
  try { localStorage.removeItem(KEY); } catch { /* nothing saved */ }
  clearOutbox();
}

/** True when the open tab can't be used offline. */
export const needsConnection = () => app.offline && !OFFLINE_TABS.includes(app.tab);
