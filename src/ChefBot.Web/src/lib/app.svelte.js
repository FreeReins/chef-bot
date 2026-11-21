// Everything the page shows, and the actions that change it. Components read `app` and call these functions;
// Svelte redraws whatever reads a property of `app` when it changes, so nothing here asks for a redraw.
import { api, upload } from './api.js';
// addresses.js imports this module too. That is safe: neither uses the other's exports until a function runs.
import { showAddress, listenForAddresses } from './addresses.js';
import { addDays, upcomingSunday, shortDate } from './format.js';
import { copyStyle, cleanStyles } from './styles.js';

const query = new URLSearchParams(location.search);

export const app = $state({
  screen: 'loading',   // loading | setup-account | setup-https | setup-recipes | setup-styles | setup-stores | setup-samples | sign-in | join | reset | app
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
  styles: null,        // PlanStylesResponse: { styles, known, tags, ingredients }
  styleDraft: null,    // the plan styles chosen in setup but not saved yet
  fileFormats: [],     // RecipeFileFormatResponse[]: the kinds of recipe file that can be imported
  mealieImport: null,  // the latest Mealie import this page started: { state, result?, message? }
  people: { users: [], links: [], newLink: null },
  tab: 'plan',         // the open tab; the address bar says which (see addresses.js)
  recipeId: null,      // the recipe shown on its own page, from /recipes/{id}
  settingsSection: 'login', // which Settings sidebar item is open
  plans: [],           // PlanSummaryResponse[]: the weeks with a plan that haven't ended, earliest first
  plan: null,          // PlanResponse for the week being looked at (one of plans), or null when nothing is planned
  keep: [],            // ids of the draft's meals marked Keep, which Shuffle leaves as they are
  ingredients: null,   // IngredientsResponse for the plan
  pantry: [],          // PantryItemResponse[], loaded by the Pantry tab when there is no plan to read it from
  manualItems: [],     // ManualShoppingItemResponse[]: what was put on the shopping list by hand, plan or not
  recipes: [],
  swap: null,          // { mealId, options } while a meal's alternatives are open
  toasts: [],          // { id, text, kind: 'note' | 'error' } shown in the corner by Toasts.svelte
  confirmation: null,  // { title, message, confirmLabel, danger } while ConfirmDialog.svelte is asking
});

export const isConfirmed = () => app.plan?.status === 'Confirmed';
export const isAdmin = () => app.session?.user?.isAdmin === true;

/** Runs an action, then shows either its result or a readable error. */
export async function run(action) {
  clearErrors();
  try {
    await action();
  } catch (error) {
    if (error.status === 401 && app.screen === 'app') {
      app.screen = 'sign-in';
      toast('Your session has ended. Sign in again.', 'error');
    } else {
      toast(error.message, 'error');
    }
  }
}

// ---------- toasts ----------

let nextToastId = 0;

/**
 * Shows a short message in the corner of the window and returns its id. A note goes away by itself after long
 * enough to read it; an error stays until it is closed or the next action starts.
 */
export function toast(text, kind = 'note') {
  const id = nextToastId++;
  app.toasts.push({ id, text, kind });
  if (kind !== 'error') setTimeout(() => dismissToast(id), Math.max(5000, text.length * 60));
  return id;
}

export function dismissToast(id) {
  const index = app.toasts.findIndex((item) => item.id === id);
  if (index >= 0) app.toasts.splice(index, 1);
}

/** Closes the errors left by an earlier action. Notes stay until they time out. */
export function clearErrors() {
  app.toasts = app.toasts.filter((item) => item.kind !== 'error');
}

// ---------- confirmation ----------

let answerConfirmation = null;

/**
 * Asks before doing something hard to undo, in a dialog on the page. Resolves to true when confirmed, false when
 * cancelled. `danger` colours the confirm button for changes that lose data.
 */
export function ask({ title, message, confirmLabel = 'Confirm', danger = false }) {
  answerConfirmation?.(false);
  app.confirmation = { title, message, confirmLabel, danger };
  return new Promise((resolve) => { answerConfirmation = resolve; });
}

/** Closes the confirmation dialog with the person's answer. */
export function answer(confirmed) {
  const resolve = answerConfirmation;
  answerConfirmation = null;
  app.confirmation = null;
  resolve?.(confirmed);
}

// ---------- plan ----------

const ingredientsFor = (plan) => (plan ? api('GET', `/api/plans/${plan.id}/ingredients`) : null);

/**
 * Loads the weeks that have a plan, then one of them with its meals and ingredients: the one asked for if it is
 * still there, otherwise the earliest (this week's, or else the next one).
 */
export async function loadPlan(planId = app.plan?.id) {
  const plans = await api('GET', '/api/plans');
  const summary = plans.find((plan) => plan.id === planId) ?? plans[0] ?? null;
  const plan = summary ? await api('GET', `/api/plans/${summary.id}`) : null;
  // All at once, so the page never shows a new plan beside the old plan's ingredients.
  const ingredients = await ingredientsFor(plan);
  app.plans = plans;
  app.plan = plan;
  app.ingredients = ingredients;
  app.keep = app.keep.filter((id) => plan?.meals.some((meal) => meal.id === id));
}

/**
 * Applies a plan returned by a change, together with the ingredients view that matches it. Its notes (what was left
 * off the shopping list, a meal that moved) are shown unless `quiet`, for refreshes the person didn't ask for.
 */
export async function applyPlan(plan, { quiet = false } = {}) {
  const ingredients = await ingredientsFor(plan);
  app.plan = plan;
  app.ingredients = ingredients;
  const summary = { id: plan.id, weekStart: plan.weekStart, weekEnd: plan.weekEnd, status: plan.status, styleName: plan.styleName };
  app.plans = [...app.plans.filter((other) => other.id !== plan.id && other.weekStart !== plan.weekStart), summary]
    .sort((a, b) => a.weekStart.localeCompare(b.weekStart));
  app.keep = app.keep.filter((id) => plan.meals.some((meal) => meal.id === id));
  if (!quiet) for (const note of plan.notes ?? []) toast(note);
  app.swap = null;
}

export const planAction = (path, body) => run(async () => applyPlan(await api('POST', `/api/plans/${app.plan.id}/${path}`, body)));

/** Shows another week's plan. */
export const selectWeek = (planId) => run(async () => {
  app.swap = null;
  await loadPlan(planId);
});

/** The week to plan next: the one after the latest planned week, or the coming week when nothing is planned. */
export function nextWeekStart() {
  const last = app.plans.at(-1);
  return last ? addDays(last.weekStart, 7) : upcomingSunday();
}

/** True when the week being looked at is confirmed and no later week has a plan, so the page offers to plan one. */
export const canPlanAhead = () => isConfirmed() && app.plans.at(-1)?.id === app.plan?.id;

/** The household's saved style with this name, used to plan a week again in the style it was made with. */
export const styleNamed = (name) => (app.styles?.styles ?? []).find((style) => style.name === name) ?? null;

/**
 * The id to plan with. Until the household saves styles, the choices are the starter styles, which have no id: the
 * chosen one is saved as the household's default first.
 */
async function styleIdFor(style) {
  if (!style || style.id) return style?.id ?? null;
  await saveStyles([{ ...copyStyle(style), isDefault: true }]);
  return styleNamed(style.name)?.id ?? null;
}

/** Plans a week (the next one, unless another is given) in a style, as a draft to review. */
export function planWeek(style, weekStart = nextWeekStart()) {
  return run(async () => {
    const styleId = await styleIdFor(style);
    await applyPlan(await api('POST', '/api/plans/stage', { weekStart, styleId }));
  });
}

/**
 * Plans the draft's week again, keeping the meals marked Keep: in its own style for Shuffle, or in another. The kept
 * meals come back as new meals, so they stay marked.
 */
export function replan(style = styleNamed(app.plan.styleName)) {
  const plan = app.plan;
  const kept = plan.meals.filter((meal) => app.keep.includes(meal.id));
  return run(async () => {
    const styleId = await styleIdFor(style);
    const next = await api('POST', '/api/plans/stage', { weekStart: plan.weekStart, styleId, keep: kept.map((meal) => meal.id) });
    await applyPlan(next);
    app.keep = next.meals
      .filter((meal) => kept.some((old) => old.slot === meal.slot && old.date === meal.date && old.title === meal.title))
      .map((meal) => meal.id);
  });
}

/** Marks a draft's meal to keep when shuffling, or stops keeping it. */
export function toggleKeep(mealId) {
  app.keep = app.keep.includes(mealId) ? app.keep.filter((id) => id !== mealId) : [...app.keep, mealId];
}

/** Makes the draft the working plan. */
export const confirmPlan = () => run(async () => {
  await applyPlan(await api('POST', `/api/plans/${app.plan.id}/confirm`));
  toast(`The plan for ${shortDate(app.plan.weekStart)} to ${shortDate(app.plan.weekEnd)} is confirmed. Tick items off on the Shopping tab as you buy them, and mark meals as cooked here.`);
});

/** Moves a dinner or breakfast to another day of its week; a meal already there swaps with it. */
export const moveMeal = (mealId, date) => run(async () => applyPlan(await api('POST', `/api/plans/${app.plan.id}/meals/${mealId}/move`, { date })));

export async function clearPlan() {
  const draft = app.plan.status === 'Staged';
  if (!(await ask(draft
    ? { title: 'Discard this draft?', message: 'The proposed meals and their shopping list are removed. Your pantry is kept.', confirmLabel: 'Discard draft', danger: true }
    : { title: 'Clear this plan?', message: 'Meals and the shopping list are removed. Your pantry is kept.', confirmLabel: 'Clear plan', danger: true }))) return;
  run(async () => {
    await api('DELETE', `/api/plans/${app.plan.id}`);
    await loadPlan(null);
    toast(draft ? 'The draft was discarded. Your pantry is unchanged.' : 'The plan was cleared. Your pantry is unchanged.');
    app.swap = null;
  });
}

export async function regenerate() {
  if (!(await ask({ title: 'Replan from your pantry?', message: 'This week is planned again around what is in your pantry, so you buy less. Meals already cooked are not kept.',
    confirmLabel: 'Replan' }))) return;
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
    await applyPlan(await api('POST', `/api/plans/${app.plan.id}/refresh`), { quiet: true });
  } else {
    await loadPantry();
  }
}

/** Loads the pantry on its own. With a plan, the pantry comes with the plan's ingredients view instead. */
export const loadPantry = async () => { app.pantry = await api('GET', '/api/pantry'); };

// ---------- hand-added shopping items ----------

export const loadManualItems = async () => { app.manualItems = await api('GET', '/api/shopping/items'); };

/** Puts something on the shopping list by hand: { name, quantity?, unit? }. Resolves to true once it is there. */
export function addToShoppingList(item) {
  let added = false;
  return run(async () => {
    app.manualItems = await api('POST', '/api/shopping/items', item);
    added = true;
  }).then(() => added);
}

/** Takes a hand-added item off the list without buying it. */
export const removeManualItem = (id) => run(async () => {
  app.manualItems = await api('DELETE', `/api/shopping/items/${id}`);
});

/**
 * Buys hand-added items: `path` is `{id}/purchase` or `purchase-all`. What was bought goes into the pantry, so the
 * plan's shopping list and the pantry are reloaded too.
 */
export const buyManual = (path) => run(async () => {
  app.manualItems = await api('POST', `/api/shopping/items/${path}`, {});
  await refreshAfterPantryChange();
});

/**
 * Marks everything on the shopping list as bought. With a confirmed plan one request covers the plan's items and the
 * hand-added ones; otherwise only the hand-added items can be bought.
 */
export const buyEverything = () => run(async () => {
  if (isConfirmed()) {
    await applyPlan(await api('POST', `/api/plans/${app.plan.id}/shopping/purchase-all`, {}));
    await loadManualItems();
  } else {
    app.manualItems = await api('POST', '/api/shopping/items/purchase-all', {});
    await refreshAfterPantryChange();
  }
});

/** Buys a hand-added item's ingredient at another store from now on. Open plans' lists follow, so they reload too. */
export const setManualStore = (id, store) => run(async () => {
  app.manualItems = await api('POST', `/api/shopping/items/${id}/store`, { store });
  await Promise.all([loadStores(), loadPlan()]);
});

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
    if (app.screen === 'app') importingToast = toast('Importing from Mealie in the background. This page will update when it finishes.');
    await pollImport();
  });
}

// The "importing" note, closed when the import finishes so it never sits beside the result.
let importingToast = null;

async function pollImport() {
  for (let attempt = 0; attempt < 60; attempt++) {
    await new Promise((resolve) => setTimeout(resolve, 2000));
    const status = await api('GET', '/api/recipes/import/status').catch(() => null);
    if (!status || status.state === 'Running') continue;

    app.mealieImport = status;
    const inApp = app.screen === 'app';
    dismissToast(importingToast);
    if (status.state === 'Succeeded') {
      if (inApp) toast(describeMealieImport(status.result));
      await loadRecipes().catch(() => {});
    } else {
      app.mealieImport.message = status.message || 'The Mealie import failed. Check the address and token, then try again.';
      if (inApp) toast(app.mealieImport.message, 'error');
    }
    return;
  }
  app.mealieImport = { state: 'Failed', message: 'The Mealie import is taking longer than expected. Check the Recipes tab later to see what arrived.' };
}

/**
 * Imports one recipe file. A .zip, such as a Mealie backup, is sent as it is so its photos come too; other files are
 * sent as text. Returns what happened: { added, alreadyInLibrary, skipped, photosAdded }.
 */
export async function importFile(file) {
  const result = file.name.toLowerCase().endsWith('.zip')
    ? await upload(`/api/recipes/import/archive?fileName=${encodeURIComponent(file.name)}`, file, 'application/zip')
    : await api('POST', '/api/recipes/import/file', { fileName: file.name, content: await file.text() });
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
  { screen: 'setup-styles', label: 'Plan style' },
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
    else if (screen === 'setup-styles') await openSetupStyles();
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

/**
 * Setup: choose how weeks are planned. Until the household has styles, every starter style is offered, the first as
 * the default; choices not saved yet are kept while moving between steps.
 */
async function openSetupStyles() {
  await loadStyles();
  app.styleDraft ??= app.styles.styles.length
    ? $state.snapshot(app.styles.styles)
    : app.styles.known.map((style, index) => ({ ...copyStyle(style), isDefault: index === 0 }));
  app.screen = 'setup-styles';
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
  await Promise.all([loadPlan(), loadManualItems()]);
}

/** Buys a shopping item's ingredient at another store from now on. An empty name means no particular store. */
export function setShoppingStore(itemId, store) {
  return run(async () => {
    await applyPlan(await api('POST', `/api/plans/${app.plan.id}/shopping/${itemId}/store`, { store }));
    await Promise.all([loadStores(), loadManualItems()]);
  });
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
  await Promise.all([loadRecipes(), loadPlan(), loadManualItems(), loadSystem(), loadStores(), loadStyles(), isAdmin() ? loadPeople() : null]);
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
