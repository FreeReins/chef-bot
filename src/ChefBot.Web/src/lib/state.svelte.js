// Everything the page shows (`app`), plus what every area's actions use: run(), toasts and the confirmation
// dialog. Components read `app` and import from app.svelte.js; Svelte redraws whatever reads a property of `app`
// when it changes, so nothing asks for a redraw.

const query = new URLSearchParams(location.search);

export const app = $state({
  screen: 'loading',   // loading | setup-account | setup-https | setup-recipes | setup-styles | setup-stores | setup-samples | sign-in | join | reset | app
  session: null,       // SessionResponse: { setupRequired, user }
  linkToken: query.get('invite') ?? query.get('reset'),
  link: null,          // LinkDetails for linkToken: { purpose, username, expiresAt }
  https: null,         // HttpsStatusResponse
  security: null,      // SecurityResponse (admins only)
  notifications: null, // NotificationSettings (admins only)
  backups: null,       // BackupsResponse (admins only)
  samples: [],         // sample recipes offered by the setup wizard
  stores: null,        // GroceryStoresResponse: { stores, known, ingredients }
  storeDraft: null,    // the stores chosen in setup but not saved yet
  styles: null,        // PlanStylesResponse: { styles, known, tags, ingredients }
  styleDraft: null,    // the plan styles chosen in setup but not saved yet
  fileFormats: [],     // RecipeFileFormatResponse[]: the kinds of recipe file that can be imported
  people: { users: [], links: [], newLink: null },
  tab: 'plan',         // the open tab; the address bar says which (see addresses.js)
  recipeId: null,      // the recipe shown on its own page, from /recipes/{id}
  recipeDialog: null,  // the Recipes dialog open: new-link | new-write | new-files (/recipes/new/...) or edit | photos | cook (/recipes/{id}/...)
  settingsSection: 'login', // which Settings sidebar item is open
  plans: [],           // PlanSummaryResponse[]: the weeks with a plan that haven't ended, earliest first
  plan: null,          // PlanResponse for the week being looked at (one of plans), or null when nothing is planned
  keep: [],            // ids of the draft's meals marked Keep, which Shuffle leaves as they are
  ingredients: null,   // IngredientsResponse for the plan
  pantry: [],          // PantryItemResponse[], loaded by the Pantry tab when there is no plan to read it from
  manualItems: [],     // ManualShoppingItemResponse[]: what was put on the shopping list by hand, plan or not
  shoppingTargets: [], // ShoppingTargetResponse[]: the to-do apps the shopping list can be sent to
  recipeCount: 0,      // how many recipes the library holds; the Recipes tab fetches them a page at a time
  recipesChanged: 0,   // goes up each time the library may have changed, so the open page of recipes reloads
  recipeName: null,    // the name of the recipe open on its own page, once loaded, for the browser tab's title
  swap: null,          // { mealId, options } while a meal's alternatives are open
  toasts: [],          // { id, text, kind: 'note' | 'error', action?: { label, run } } shown in the corner by Toasts.svelte
  confirmation: null,  // { title, message, confirmLabel, danger } while ConfirmDialog.svelte is asking
  busy: 0,             // actions run() has started and not finished; App.svelte shows a progress bar while above 0
  importing: 0,        // recipe files being sent by FileImport.svelte; setup can't move on until they are in
  setupDirection: 1,   // which way the last setup move went: 1 forward, -1 back, so the steps slide the right way
  dealt: 0,            // when a week was last planned or shuffled (Date.now()), so the board deals its new meals in
  offline: false,      // true while ChefBot can't be reached: Shopping and Pantry show what was saved (offline.svelte.js)
  offlineSince: null,  // when what is shown offline was last loaded from ChefBot (an ISO time)
  achievements: null,  // AchievementsResponse: first steps, badges and chef rank (achievements.svelte.js)
  outbox: [],          // shopping changes made offline, oldest first, waiting to be sent (outbox.svelte.js)
});

export const isConfirmed = () => app.plan?.status === 'Confirmed';
export const isAdmin = () => app.session?.user?.isAdmin === true;

/** Runs an action, then shows either its result or a readable error. */
export async function run(action) {
  clearErrors();
  app.busy++;
  try {
    await action();
  } catch (error) {
    if (error.offline) {
      wentOffline();
    } else if (error.status === 401 && app.screen === 'app') {
      app.screen = 'sign-in';
      toast('Your session has ended. Sign in again.', 'error');
    } else {
      toast(error.message, 'error');
    }
  } finally {
    app.busy--;
  }
}

/**
 * Notes that ChefBot can't be reached, once per spell offline. Only signed-in views have a list to fall back on;
 * before that, the error is shown like any other.
 */
export function wentOffline() {
  if (app.screen !== 'app') {
    toast("ChefBot can't be reached. Check your connection, then reload the page.", 'error');
    return;
  }
  if (app.offline) return;
  app.offline = true;
  toast("ChefBot can't be reached. The shopping list still works, and your changes are sent when you're back online.");
}

// ---------- toasts ----------

let nextToastId = 0;

/**
 * Shows a short message in the corner of the window and returns its id. A note goes away by itself after long
 * enough to read it; an error stays until it is closed or the next action starts. `action` ({ label, run }) adds a
 * button to the message, such as Undo; a note with one stays a little longer, so there is time to press it.
 */
export function toast(text, kind = 'note', action = null) {
  const id = nextToastId++;
  app.toasts.push({ id, text, kind, action });
  if (kind !== 'error') setTimeout(() => dismissToast(id), Math.max(action ? 8000 : 5000, text.length * 60));
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
