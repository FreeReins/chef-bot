// Offline mode: what the Shopping and Pantry tabs need is kept in this browser as it changes, so they still open with
// no connection, such as on a phone in a shop. So are the recipes of the saved plan's meals, draft or confirmed, to
// read and cook from (text only: photos aren't kept). While ChefBot can't be reached (app.offline) the page checks
// every half minute, and as soon as it answers, sync() sends what changed meanwhile (outbox.svelte.js).
// The page itself is kept by the service worker in public/sw.js.
import { untrack } from 'svelte';
import { api } from './api.js';
import { onLive } from './live.svelte.js';
import { clearOutbox, loadOutbox, sync } from './outbox.svelte.js';
import { app } from './state.svelte.js';

const KEY = 'chefbot-offline';
/** The saved plan's recipes, apart from the snapshot so running out of room for them never costs the list. */
const RECIPES_KEY = 'chefbot-offline-recipes';
/** The tabs that work offline; the others say they need ChefBot. */
export const OFFLINE_TABS = ['shopping', 'pantry'];
const CHECK_EVERY = 30_000;

/** When the shown data last came from ChefBot. */
let savedAt = null;

/** The recipes kept for offline reading, by id: { userId, recipes }. Replaced whole, never changed in place. */
let saved = $state.raw({ userId: null, recipes: {} });
/** Saves run one after another, so a slow one never writes over a newer one. */
let saving = Promise.resolve();

/** The ids of the recipes the plan's meals follow, once each, whether the plan is a draft or confirmed. */
export const planRecipeIds = (plan = app.plan) => [...new Set((plan?.meals ?? []).map((meal) => meal.recipeId).filter(Boolean))];

/** A recipe kept on this device for reading offline, or null. */
export const savedRecipe = (id) => (id && saved.userId === app.session?.user?.id ? saved.recipes[id] ?? null : null);
export const hasSavedRecipe = (id) => savedRecipe(id) !== null;

/**
 * Keeps the recipes in `ids` on this device and drops the rest. Only those not kept yet are loaded, or all of them
 * with `refresh`, such as after a recipe changed. Quiet: a recipe that can't be loaded now is tried on the next change.
 */
function saveRecipes(userId, ids, refresh = false) {
  saving = saving.then(async () => {
    const kept = saved.userId === userId ? saved.recipes : {};
    const wanted = refresh ? ids : ids.filter((id) => !kept[id]);
    const loaded = await Promise.allSettled(wanted.map((id) => api('GET', `/api/recipes/${id}`)));
    if (app.session?.user?.id !== userId) return; // signed out meanwhile
    const recipes = {};
    for (const id of ids) if (kept[id]) recipes[id] = kept[id];
    loaded.forEach((result, index) => {
      // Photos aren't kept, so the page shows none offline rather than broken images.
      if (result.status === 'fulfilled' && result.value) recipes[wanted[index]] = { ...result.value, images: [] };
      else if (result.reason?.status === 404) delete recipes[wanted[index]];
    });
    saved = { userId, recipes };
    try { localStorage.setItem(RECIPES_KEY, JSON.stringify(saved)); } catch { /* storage refused: no offline recipes */ }
  }).catch(() => { /* the next change tries again */ });
}

// A recipe changed here or on another device: load the kept ones again, so the phone reads the latest.
onLive('recipes', () => {
  if (!app.offline && app.session?.user) saveRecipes(app.session.user.id, planRecipeIds(), true);
});

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

  // Keeps the recipes of the plan being looked at, as it is planned, shuffled or swapped and as the week changes.
  $effect(() => {
    if (app.screen !== 'app' || !app.session?.user || app.offline) return;
    const userId = app.session.user.id;
    const ids = planRecipeIds();
    untrack(() => saveRecipes(userId, ids));
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
  try {
    const recipes = JSON.parse(localStorage.getItem(RECIPES_KEY) ?? 'null');
    if (recipes?.userId === snapshot.userId) saved = { userId: recipes.userId, recipes: recipes.recipes ?? {} };
  } catch { /* no recipes kept */ }
  return true;
}

/** Forgets the saved list, its recipes and any unsent changes, for signing out on a shared device. */
export function forgetSnapshot() {
  saved = { userId: null, recipes: {} };
  try {
    localStorage.removeItem(KEY);
    localStorage.removeItem(RECIPES_KEY);
  } catch { /* nothing saved */ }
  clearOutbox();
}

/** True when the open tab can't be used offline. A kept recipe can be read and cooked from; nothing else on Recipes. */
export const needsConnection = () => app.offline && !OFFLINE_TABS.includes(app.tab) && !readableOffline();
const readableOffline = () => app.tab === 'recipes' && hasSavedRecipe(app.recipeId) && [null, 'cook'].includes(app.recipeDialog);
