// Everything the page shows, and the actions that change it. Components read `app` and call these functions;
// Svelte redraws whatever reads a property of `app` when it changes, so nothing here asks for a redraw.
import { api } from './api.js';
// addresses.js imports this module too. That is safe: neither uses the other's exports until a function runs.
import { showAddress, listenForAddresses } from './addresses.js';

const query = new URLSearchParams(location.search);

export const app = $state({
  screen: 'loading',   // loading | setup-account | setup-https | setup-recipes | setup-stores | setup-samples | sign-in | join | reset | app
  session: null,       // SessionResponse: { setupRequired, user, mealieConfigured }
  linkToken: query.get('invite') ?? query.get('reset'),
  link: null,          // LinkDetails for linkToken: { purpose, username, expiresAt }
  https: null,         // HttpsStatusResponse
  security: null,      // SecurityResponse (admins only)
  mealie: null,        // MealieSettingsResponse: { url, hasApiToken, isConfigured } (admins only)
  notifications: null, // NotificationSettings (admins only)
  backups: null,       // BackupsResponse (admins only)
  samples: [],         // sample recipes offered by the setup wizard
  stores: null,        // GroceryStoresResponse: { stores, known, ingredients }
  storeDraft: null,    // the stores chosen in setup but not saved yet
  fileFormats: [],     // RecipeFileFormatResponse[]: the kinds of recipe file that can be imported
  mealieImport: null,  // the latest Mealie import this page started: { state, result?, message? }
  people: { users: [], links: [], newLink: null },
  tab: 'meals',        // the open tab; the address bar says which (see addresses.js)
  recipeId: null,      // the recipe shown on its own page, from /recipes/{id}
  settingsSection: 'login', // which Settings sidebar item is open
  plan: null,          // PlanResponse, or null when nothing is staged yet
  ingredients: null,   // IngredientsResponse for the plan
  recipes: [],
  swap: null,          // { mealId, options } while a meal's alternatives are open
  notes: [],
  error: null,
});

export const isConfirmed = () => app.plan?.status === 'Confirmed';
export const isAdmin = () => app.session?.user?.isAdmin === true;

/** Runs an action, then shows either its result or a readable error. */
export async function run(action) {
  app.error = null;
  try {
    await action();
  } catch (error) {
    app.error = error.message;
    if (error.status === 401 && app.screen === 'app') {
      app.screen = 'sign-in';
      app.error = 'Your session has ended. Sign in again.';
    }
  }
}

/** Clears the messages left by the previous view. */
export function clearMessages() {
  app.notes = [];
  app.error = null;
}

// ---------- plan ----------

const ingredientsFor = (plan) => (plan ? api('GET', `/api/plans/${plan.id}/ingredients`) : null);

export async function loadPlan() {
  let plan = null;
  try {
    plan = await api('GET', '/api/plans/current');
  } catch (error) {
    if (error.status !== 404) throw error;
  }
  // Both at once, so the page never shows a new plan beside the old plan's ingredients.
  const ingredients = await ingredientsFor(plan);
  app.plan = plan;
  app.ingredients = ingredients;
}

/** Applies a plan returned by a change, together with the ingredients view that matches it. */
export async function applyPlan(plan) {
  const ingredients = await ingredientsFor(plan);
  app.plan = plan;
  app.ingredients = ingredients;
  app.notes = plan.notes ?? [];
  app.swap = null;
}

export const stage = (body) => run(async () => applyPlan(await api('POST', '/api/plans/stage', body)));
export const planAction = (path, body) => run(async () => applyPlan(await api('POST', `/api/plans/${app.plan.id}/${path}`, body)));

export function clearPlan() {
  if (!confirm('Clear this plan? Meals and the shopping list are removed. Your pantry is kept.')) return;
  run(async () => {
    await api('DELETE', `/api/plans/${app.plan.id}`);
    await loadPlan();
    app.notes = ['The plan was cleared. Your pantry is unchanged.'];
    app.swap = null;
  });
}

export function regenerate() {
  if (!confirm('Replace this plan with a new one built around what is in your pantry?')) return;
  planAction('regenerate');
}

export function openSwap(meal) {
  return run(async () => {
    const options = await api('GET', `/api/plans/${app.plan.id}/meals/${meal.id}/alternatives`);
    app.swap = { mealId: meal.id, options };
  });
}

export function closeSwap() {
  app.swap = null;
}

export function replaceMeal(mealId, body) {
  return run(async () => applyPlan(await api('PUT', `/api/plans/${app.plan.id}/meals/${mealId}`, body)));
}

export async function refreshAfterPantryChange() {
  if (app.plan) {
    await applyPlan(await api('POST', `/api/plans/${app.plan.id}/refresh`));
  }
}

// ---------- recipes ----------

export const loadRecipes = async () => { app.recipes = await api('GET', '/api/recipes'); };

/** Describes a finished Mealie import in one sentence. */
export function describeMealieImport({ added, updated, keptLocal, archived, skipped }) {
  return `Mealie import finished: ${added} added, ${updated} updated, ${keptLocal} kept as edited here, ${archived} archived, ${skipped} skipped.`;
}

/**
 * Starts a Mealie import and follows it to the end. Progress is kept in `app.mealieImport`, which the setup
 * wizard shows in place; inside the app it is also reported in the page's messages.
 */
export function importFromMealie() {
  return run(async () => {
    await api('POST', '/api/recipes/import/mealie');
    app.mealieImport = { state: 'Running' };
    if (app.screen === 'app') app.notes = ['Importing from Mealie in the background. This page will update when it finishes.'];
    await pollImport();
  });
}

async function pollImport() {
  for (let attempt = 0; attempt < 60; attempt++) {
    await new Promise((resolve) => setTimeout(resolve, 2000));
    const status = await api('GET', '/api/recipes/import/status').catch(() => null);
    if (!status || status.state === 'Running') continue;

    app.mealieImport = status;
    const inApp = app.screen === 'app';
    if (status.state === 'Succeeded') {
      if (inApp) app.notes = [describeMealieImport(status.result)];
      await loadRecipes().catch(() => {});
    } else {
      app.mealieImport.message = status.message || 'The Mealie import failed. Check the address and token, then try again.';
      if (inApp) { app.error = app.mealieImport.message; app.notes = []; }
    }
    return;
  }
  app.mealieImport = { state: 'Failed', message: 'The Mealie import is taking longer than expected. Check the Recipes tab later to see what arrived.' };
}

/** Imports one recipe file. Returns what happened: { added, alreadyInLibrary, skipped }. */
export async function importFile(file) {
  const result = await api('POST', '/api/recipes/import/file', { fileName: file.name, content: await file.text() });
  await loadRecipes();
  return result;
}

/** Loads the kinds of recipe file that can be imported, once. */
export async function loadFileFormats() {
  if (app.fileFormats.length === 0) app.fileFormats = await api('GET', '/api/recipes/import/formats');
}

// ---------- setup ----------

/** The setup wizard's steps in order, with their labels. */
const SETUP_STEPS = [
  { screen: 'setup-account', label: 'Your account' },
  { screen: 'setup-https', label: 'Secure connection' },
  { screen: 'setup-recipes', label: 'Recipes' },
  { screen: 'setup-stores', label: 'Stores (optional)' },
];

/**
 * The steps this server shows. The secure connection step is left out once HTTPS is known to be off; that is only
 * known after the account exists, so the first step counts it.
 */
export const setupSteps = () => SETUP_STEPS.filter((step) => step.screen !== 'setup-https' || app.https?.enabled !== false);

/**
 * Shows a setup step, loading what it needs first. Every step after the first needs the account, so until it
 * exists only the first can be shown.
 */
export function openSetupStep(screen) {
  return run(async () => {
    if (screen !== 'setup-account' && !app.session?.user) return;
    if (screen === 'setup-recipes') await openSetupRecipes();
    else if (screen === 'setup-stores') await openSetupStores();
    else app.screen = screen;
  });
}

/** Moves to the next setup step (1) or the previous one (-1). */
export function moveSetup(offset) {
  const steps = setupSteps();
  const next = steps[steps.findIndex((step) => step.screen === app.screen) + offset];
  return next ? openSetupStep(next.screen) : Promise.resolve();
}

/** Setup: choose how to fill the recipe library. A Mealie import already running keeps its progress. */
async function openSetupRecipes() {
  const [samples] = await Promise.all([api('GET', '/api/setup/samples'), loadFileFormats(), loadRecipes()]);
  app.samples = samples;
  app.screen = 'setup-recipes';
}

/** Setup: choose the stores the shopping list is split between. Choices not saved yet are kept while moving between steps. */
async function openSetupStores() {
  await loadStores();
  app.storeDraft ??= $state.snapshot(app.stores.stores);
  app.screen = 'setup-stores';
}

// ---------- stores ----------

/** Loads the household's stores, the known stores that can be added, and the ingredient names a store can list. */
export async function loadStores() {
  app.stores = await api('GET', '/api/stores');
}

/** Saves the household's stores. Open shopping lists are rebuilt to match, so the plan is reloaded too. */
export async function saveStores(stores) {
  app.stores = await api('PUT', '/api/stores', { stores });
  await loadPlan();
}

/** Buys a shopping item's ingredient at another store from now on. An empty name means no particular store. */
export function setShoppingStore(itemId, store) {
  return run(async () => {
    await applyPlan(await api('POST', `/api/plans/${app.plan.id}/shopping/${itemId}/store`, { store }));
    await loadStores();
  });
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

/** Keeps the saved Mealie settings, and whether importing is possible, in step everywhere on the page. */
export function applyMealie(settings) {
  app.mealie = settings;
  app.session.mealieConfigured = settings.isConfigured;
}

/** Saves Mealie's address and token. An empty token keeps the saved one; an empty address stops using Mealie. */
export async function saveMealie(url, apiToken) {
  applyMealie(await api('PUT', '/api/system/mealie', { url, apiToken: apiToken || null }));
}

export async function loadSystem() {
  const admin = (path) => (isAdmin() ? api('GET', path) : null);
  const [https, security, mealie, notifications, backups] = await Promise.all([
    api('GET', '/api/system/https'),
    admin('/api/system/security'),
    admin('/api/system/mealie'),
    admin('/api/system/notifications'),
    admin('/api/system/backups'),
  ]);
  Object.assign(app, { https, security, mealie, notifications, backups });
}

// ---------- people ----------

export async function loadPeople() {
  const [users, links] = await Promise.all([api('GET', '/api/users'), api('GET', '/api/links')]);
  app.people.users = users;
  app.people.links = links;
}

// ---------- signing in and out ----------

/** Loads everything the signed-in views need, then shows the app. */
export async function enterApp() {
  if (app.linkToken) {
    history.replaceState(null, '', location.pathname);
    app.linkToken = null;
  }
  await Promise.all([loadRecipes(), loadPlan(), loadSystem(), loadStores(), isAdmin() ? loadPeople() : null]);
  await showAddress();
}

export function signOut() {
  return run(async () => {
    await api('POST', '/api/account/sign-out');
    app.session.user = null;
    app.plan = null;
    app.notes = [];
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
      .catch((error) => { app.screen = 'sign-in'; app.error = error.message; });
  } else {
    app.screen = 'sign-in';
  }
}
