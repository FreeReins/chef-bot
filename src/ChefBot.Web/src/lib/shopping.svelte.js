import { api } from './api.js';
import { applyPlan, loadPlan, refreshAfterPantryChange } from './plan.svelte.js';
import { loadStores } from './settings.svelte.js';
import { app, ask, isConfirmed, run, toast, wentOffline } from './state.svelte.js';
import { refreshAchievements } from './achievements.svelte.js';
import {
  addManualOffline, buyAllOffline, buyManualOffline, buyPlanItemOffline, removeManualOffline,
} from './outbox.svelte.js';

// ---------- hand-added shopping items ----------

/**
 * Adding, removing and buying work offline too: `online` sends the change, and when ChefBot can't be reached (already,
 * or as it turns out now) `offline` makes it on the page and keeps it in the outbox until ChefBot answers again.
 */
async function online(send, offline) {
  if (!app.offline) {
    try {
      return await send();
    } catch (error) {
      if (!error.offline) throw error;
      wentOffline();
    }
  }
  return offline();
}

export const loadManualItems = async () => { app.manualItems = await api('GET', '/api/shopping/items'); };

/** Puts something on the shopping list by hand: { name, quantity?, unit? }. Resolves to true once it is there. */
export function addToShoppingList(item) {
  let added = false;
  return run(async () => {
    await online(
      async () => { app.manualItems = await api('POST', '/api/shopping/items', item); },
      () => addManualOffline(item));
    added = true;
  }).then(() => added);
}

/** Takes a hand-added item off the list without buying it, with an Undo that puts it back. */
export const removeManualItem = (id) => run(async () => {
  const item = app.manualItems.find((other) => other.id === id);
  // Online, Undo adds it again; offline, it takes the removal back out of the outbox.
  const undo = await online(async () => {
    app.manualItems = await api('DELETE', `/api/shopping/items/${id}`);
    return () => addToShoppingList({ name: item.name, quantity: item.quantity, unit: item.unit || null });
  }, () => removeManualOffline(id));
  if (item && undo) toast(`${item.name} was taken off the list.`, 'note', { label: 'Undo', run: undo });
});

/**
 * Buys a hand-added item. What was bought goes into the pantry, so the plan's shopping list and the pantry are
 * reloaded too.
 */
export const buyManual = (id) => run(() => online(async () => {
  app.manualItems = await api('POST', `/api/shopping/items/${id}/purchase`, {});
  await refreshAfterPantryChange();
}, () => buyManualOffline(id)));

/** Ticks one of the plan's shopping items off, once the plan is confirmed. */
export const buyPlanItem = (itemId) => run(() => online(
  async () => applyPlan(await api('POST', `/api/plans/${app.plan.id}/shopping/${itemId}/purchase`, {})),
  () => buyPlanItemOffline(itemId)));

/**
 * Marks everything on the shopping list as bought, once the person confirms: `count` is how many items that is. With a
 * confirmed plan one request covers the plan's items and the hand-added ones; otherwise only the hand-added items
 * can be bought.
 */
export async function buyEverything(count) {
  if (!(await ask({
    title: `Mark ${count === 1 ? 'this item' : `all ${count} items`} as bought?`,
    message: 'They come off the shopping list and go into your pantry.',
    confirmLabel: 'Mark as bought',
  }))) return;
  await buyAll();
}

const buyAll = () => run(() => online(async () => {
  if (isConfirmed()) {
    await applyPlan(await api('POST', `/api/plans/${app.plan.id}/shopping/purchase-all`, {}));
    await loadManualItems();
  } else {
    app.manualItems = await api('POST', '/api/shopping/items/purchase-all', {});
    await refreshAfterPantryChange();
    refreshAchievements();
  }
}, () => buyAllOffline(isConfirmed())));

/** Buys a hand-added item's ingredient at another store from now on. Open plans' lists follow, so they reload too. */
export const setManualStore = (id, store) => run(async () => {
  app.manualItems = await api('POST', `/api/shopping/items/${id}/store`, { store });
  await Promise.all([loadStores(), loadPlan()]);
});
