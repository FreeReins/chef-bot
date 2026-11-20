// Everything the page shows, and the actions that change it. Components read `app` and call these functions;
// Svelte redraws whatever reads a property of `app` when it changes, so nothing here asks for a redraw.
import { api } from './api.js';

const query = new URLSearchParams(location.search);

export const app = $state({
  screen: 'loading',   // loading | setup-account | setup-https | setup-samples | sign-in | join | reset | app
  session: null,       // SessionResponse: { setupRequired, user, mealieConfigured }
  linkToken: query.get('invite') ?? query.get('reset'),
  link: null,          // LinkDetails for linkToken: { purpose, username, expiresAt }
  https: null,         // HttpsStatusResponse
  security: null,      // SecurityResponse (admins only)
  mealie: null,        // MealieSettingsResponse: { url, hasApiToken, isConfigured } (admins only)
  notifications: null, // NotificationSettings (admins only)
  backups: null,       // BackupsResponse (admins only)
  samples: [],         // sample recipes offered by the setup wizard
  people: { users: [], links: [], newLink: null },
  tab: 'meals',
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

export function showTab(tab) {
  app.tab = tab;
  clearMessages();
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

export function importFromMealie() {
  return run(async () => {
    await api('POST', '/api/recipes/import/mealie');
    app.notes = ['Importing from Mealie in the background. This page will update when it finishes.'];
    pollImport();
  });
}

async function pollImport() {
  for (let attempt = 0; attempt < 60; attempt++) {
    await new Promise((resolve) => setTimeout(resolve, 2000));
    const status = await api('GET', '/api/recipes/import/status').catch(() => null);
    if (!status || status.state === 'Running') continue;

    if (status.state === 'Succeeded') {
      const { added, updated, keptLocal, archived, skipped } = status.result;
      app.notes = [`Mealie import finished: ${added} added, ${updated} updated, ${keptLocal} kept as edited here, ${archived} archived, ${skipped} skipped.`];
      await loadRecipes().catch(() => {});
    } else {
      app.error = status.message || 'The Mealie import failed.';
      app.notes = [];
    }
    return;
  }
}

/** Opens the sample recipe picker: step three of setup, and reachable later from the Recipes tab. */
export async function openSamples() {
  app.samples = await api('GET', '/api/setup/samples');
  app.screen = 'setup-samples';
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
  await Promise.all([loadRecipes(), loadPlan(), loadSystem(), isAdmin() ? loadPeople() : null]);
  app.screen = 'app';
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
