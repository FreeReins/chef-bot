// Shopping changes made while ChefBot can't be reached, kept in this browser until they can be sent. Each entry is one
// request the page would have made online, in the order the person made them:
//   { kind: 'buy-plan', planId, itemId, name, purchasedOn }   ticked off a plan's list
//   { kind: 'add-manual', tempId, name, quantity, unit }      put on the list by hand (tempId: the id shown until sent)
//   { kind: 'remove-manual', id, name }                       taken off the list without buying it
//   { kind: 'buy-manual', id, name, purchasedOn }             a hand-added item ticked off
// sync() sends them once ChefBot answers again, then reloads the list.
import { api } from './api.js';
import { loadPantry, loadPlan } from './plan.svelte.js';
import { loadStores } from './settings.svelte.js';
import { loadManualItems } from './shopping.svelte.js';
import { app, toast } from './state.svelte.js';
import { plural, todayIso } from './format.js';

const KEY = 'chefbot-outbox';

/** The id a hand-added item has until ChefBot gives it a real one. */
export const isTempId = (id) => typeof id === 'string' && id.startsWith('offline-');
const newTempId = () => `offline-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`;

/** Loads the outbox saved for this person. Someone else's unsent changes stay saved but aren't shown or sent. */
export function loadOutbox(userId) {
  try {
    const saved = JSON.parse(localStorage.getItem(KEY) ?? 'null');
    app.outbox = saved?.userId === userId ? saved.entries : [];
  } catch {
    app.outbox = [];
  }
}

function saveOutbox() {
  try {
    const userId = app.session?.user?.id;
    if (app.outbox.length && userId) localStorage.setItem(KEY, JSON.stringify({ userId, entries: app.outbox }));
    else localStorage.removeItem(KEY);
  } catch { /* storage refused: the changes still go once online, if the page stays open */ }
}

/** Throws away this person's unsent changes, for signing out. */
export function clearOutbox() {
  app.outbox = [];
  saveOutbox();
}

function queue(entry) {
  app.outbox.push(entry);
  saveOutbox();
}

// ---------- changes made offline ----------
// Each changes what the page shows straight away, as the server would, and queues the request for later.

/** Ticks a plan's item off: it moves to "Already bought", as it would once the purchase is saved. */
export function buyPlanItemOffline(itemId) {
  const line = app.ingredients?.ingredients.find((other) => other.shoppingItemId === itemId);
  if (!line || !app.plan) return;
  line.status = 'Purchased';
  line.pending = true;
  queue({ kind: 'buy-plan', planId: app.plan.id, itemId, name: line.name, purchasedOn: todayIso() });
}

/**
 * Puts something on the list by hand. The same thing in the same unit adds to the amount already listed, as on the
 * server; anything new shows with a temporary id until it is sent.
 */
export function addManualOffline({ name, quantity = null, unit = null }) {
  const same = app.manualItems.find((item) => item.name.toLowerCase() === name.toLowerCase() && (item.unit || '') === (unit || ''));
  if (same) {
    if (quantity != null) same.quantity = (same.quantity ?? 0) + quantity;
    same.pending = true;
    queue({ kind: 'add-manual', tempId: null, name, quantity, unit });
    return;
  }
  const tempId = newTempId();
  app.manualItems.push({ id: tempId, name, quantity, unit: unit ?? '', store: '', group: 'Other', area: 'Other', pending: true });
  queue({ kind: 'add-manual', tempId, name, quantity, unit });
}

/**
 * Takes a hand-added item off the list. One added offline just leaves the outbox. Returns what puts it back, for
 * the Undo button.
 */
export function removeManualOffline(id) {
  const index = app.manualItems.findIndex((item) => item.id === id);
  if (index < 0) return null;
  const [item] = app.manualItems.splice(index, 1);
  const before = app.outbox.slice();
  if (isTempId(id)) app.outbox = app.outbox.filter((entry) => entry.tempId !== id && entry.id !== id);
  else app.outbox.push({ kind: 'remove-manual', id, name: item.name });
  saveOutbox();
  return () => {
    app.manualItems.splice(Math.min(index, app.manualItems.length), 0, item);
    app.outbox = before;
    saveOutbox();
  };
}

/** Ticks a hand-added item off: it leaves the list. */
export function buyManualOffline(id) {
  const index = app.manualItems.findIndex((item) => item.id === id);
  if (index < 0) return;
  const [item] = app.manualItems.splice(index, 1);
  queue({ kind: 'buy-manual', id, name: item.name, purchasedOn: todayIso() });
}

/** Ticks off everything on the list one by one, so nothing added on another device meanwhile is bought by mistake. */
export function buyAllOffline(withPlan, store = null) {
  const atStore = (item) => !store || item.store === store;
  if (withPlan) {
    for (const line of app.ingredients?.ingredients ?? []) {
      if (line.status === 'ToBuy' && line.shoppingItemId && atStore(line)) buyPlanItemOffline(line.shoppingItemId);
    }
  }
  for (const item of app.manualItems.filter(atStore)) buyManualOffline(item.id);
}

// ---------- sending ----------

let syncing = null;

/**
 * Sends the outbox once ChefBot answers, then reloads the shopping list and the pantry. While ChefBot still can't be
 * reached nothing changes; `quiet` leaves that unsaid, for the checks the page makes by itself. One at a time.
 */
export function sync({ quiet = false } = {}) {
  syncing ??= trySync(quiet).finally(() => { syncing = null; });
  return syncing;
}

async function trySync(quiet) {
  let session;
  try {
    session = await api('GET', '/api/session');
  } catch (error) {
    if (!error.offline) throw error;
    if (!quiet) toast("ChefBot still can't be reached. Your changes are kept on this phone until it can.");
    return;
  }
  if (!session.user || session.user.id !== app.session?.user?.id) {
    // Signed out meanwhile: the outbox waits for this person to sign in again, and enterApp() sends it.
    const count = app.outbox.length;
    app.offline = false;
    app.session = session;
    app.screen = 'sign-in';
    if (count) toast(`Sign in to send your ${plural(count, 'shopping change')} to ChefBot.`, 'error');
    return;
  }
  const result = await sendOutbox();
  await Promise.all([loadPlan(), loadManualItems(), loadPantry(), loadStores()]);
  app.offline = false;
  app.offlineSince = null;
  report(result);
}

/**
 * Sends each change in turn, taking it out of the outbox as soon as ChefBot has it, so a connection lost halfway
 * carries on where it stopped. A change that no longer applies (the plan was replanned, the item bought or removed
 * on another device) is skipped and named. Anything else, such as ChefBot going away again, stops the sending and
 * keeps the rest.
 */
export async function sendOutbox() {
  const result = { sent: 0, skipped: [] };
  while (app.outbox.length) {
    const entry = app.outbox[0];
    try {
      await send(entry);
      result.sent++;
    } catch (error) {
      if (error.offline || error.status === 401 || !(error.status >= 400 && error.status < 500)) throw error;
      result.skipped.push(entry.name);
    }
    app.outbox.shift();
    saveOutbox();
  }
  return result;
}

async function send(entry) {
  const unsent = () => Object.assign(new Error(`${entry.name} was never sent.`), { status: 404 });
  switch (entry.kind) {
    case 'buy-plan':
      return api('POST', `/api/plans/${entry.planId}/shopping/${entry.itemId}/purchase`, { purchasedOn: entry.purchasedOn });
    case 'add-manual': {
      const before = new Set((await api('GET', '/api/shopping/items')).map((item) => item.id));
      const after = await api('POST', '/api/shopping/items', { name: entry.name, quantity: entry.quantity, unit: entry.unit });
      if (!entry.tempId) return;
      // The new item, or the one it was added to when the same thing was listed already.
      const id = after.find((item) => !before.has(item.id))?.id
        ?? after.find((item) => item.name.toLowerCase() === entry.name.toLowerCase())?.id;
      for (const later of app.outbox) if (later.id === entry.tempId) later.id = id ?? later.id;
      return;
    }
    case 'remove-manual':
      if (isTempId(entry.id)) throw unsent();
      return api('DELETE', `/api/shopping/items/${entry.id}`);
    case 'buy-manual':
      if (isTempId(entry.id)) throw unsent();
      return api('POST', `/api/shopping/items/${entry.id}/purchase`, { purchasedOn: entry.purchasedOn });
    default:
      throw Object.assign(new Error('Unknown change.'), { status: 400 });
  }
}

/** Says what reached ChefBot, and names anything that couldn't be applied. */
export function report({ sent, skipped }) {
  if (skipped.length) {
    toast(`${plural(sent, 'shopping change')} reached ChefBot. ${plural(skipped.length, 'change')} couldn't be applied `
      + `because the list changed meanwhile: ${skipped.join(', ')}. Check the Shopping tab.`, 'error');
  } else if (sent) {
    toast(`Your ${plural(sent, 'shopping change')} reached ChefBot.`);
  }
}
