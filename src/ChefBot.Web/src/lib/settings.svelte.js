import { api } from './api.js';
import { cleanStyles } from './styles.js';
import { applyPlan, loadPlan } from './plan.svelte.js';
import { loadManualItems } from './shopping.svelte.js';
import { app, isAdmin, run } from './state.svelte.js';

// ---------- stores ----------

/** Loads the household's stores, the known stores that can be added, and the ingredient names a store can list. */
export async function loadStores() {
  app.stores = await api('GET', '/api/stores');
}

/** Saves the household's stores. Open shopping lists are rebuilt to match, so the plan is reloaded too. */
export async function saveStores(stores) {
  app.stores = await api('PUT', '/api/stores', { stores });
  await Promise.all([loadPlan(), loadManualItems()]);
}

/** Buys a shopping item's ingredient at another store from now on. An empty name means no particular store. */
export function setShoppingStore(itemId, store) {
  return run(async () => {
    await applyPlan(await api('POST', `/api/plans/${app.plan.id}/shopping/${itemId}/store`, { store }));
    await Promise.all([loadStores(), loadManualItems()]);
  });
}

// ---------- to-do apps ----------

/** Loads the to-do apps the shopping list can be sent to. Their tokens never come back, only hasToken. */
export async function loadShoppingTargets() {
  app.shoppingTargets = await api('GET', '/api/shopping/targets');
}

/** Saves the to-do apps (admins only). A target sent with a null token keeps the one saved for it. */
export async function saveShoppingTargets(targets) {
  app.shoppingTargets = await api('PUT', '/api/shopping/targets', { targets });
}

// ---------- plan styles ----------

/** Loads the household's plan styles, the starter styles, and the tags and ingredients rules can name. */
export async function loadStyles() {
  app.styles = await api('GET', '/api/plan-styles');
}

/** Saves the household's plan styles. Plans already made keep the style they were made with. */
export async function saveStyles(styles) {
  app.styles = await api('PUT', '/api/plan-styles', { styles: cleanStyles(styles) });
}

// ---------- planning ----------

/** Loads how many people plans are for, the weekly schedule and how long leftovers keep. */
export async function loadPlanning() {
  app.planning = await api('GET', '/api/planning');
}

/**
 * Saves some of the planning settings, keeping the rest as they are. Weeks not yet confirmed are resized for a new
 * number of people, so the plan is reloaded when that changed.
 */
export async function savePlanning(changes) {
  const before = app.planning;
  const { nextStaging, isSaved, ...current } = before;
  app.planning = await api('PUT', '/api/planning', { ...current, ...changes });
  if (app.planning.people !== before.people || app.planning.maxLeftoverDays !== before.maxLeftoverDays) await loadPlan();
}

// ---------- settings ----------

export const httpsUrl = () => `https://${location.hostname}${app.https.port === 443 ? '' : `:${app.https.port}`}/`;

/** Saves the sign-in rules. Turning "always use HTTPS" on from plain HTTP moves the browser to HTTPS. */
export async function saveSecurity(changes) {
  app.security = await api('PUT', '/api/system/security', { ...app.security, ...changes });
  app.https.requireHttps = app.security.requireHttps;
  if (app.security.requireHttps && location.protocol === 'http:') {
    location.href = httpsUrl();
  }
}

export async function loadSystem() {
  const admin = (path) => (isAdmin() ? api('GET', path) : null);
  const [https, security, notifications, backups] = await Promise.all([
    api('GET', '/api/system/https'),
    admin('/api/system/security'),
    admin('/api/system/notifications'),
    admin('/api/system/backups'),
  ]);
  Object.assign(app, { https, security, notifications, backups });
}

// ---------- AI models ----------

/** Which AI features are ready, so the page offers only those. */
export async function loadAiFeatures() {
  app.aiFeatures = await api('GET', '/api/ai/features');
}

// ---------- people ----------

export async function loadPeople() {
  const [users, links] = await Promise.all([api('GET', '/api/users'), api('GET', '/api/links')]);
  app.people.users = users;
  app.people.links = links;
}
