// ChefBot's built-in page: first-run setup, sign-in, then reviewing a staged plan,
// swapping meals, shopping, the pantry and (for admins) inviting people.
// Plain JavaScript on purpose, so the project needs no front-end build step.
'use strict';

const state = {
  screen: 'loading',   // loading | setup-account | setup-https | setup-samples | sign-in | join | reset | app
  session: null,       // SessionResponse: { setupRequired, user, mealieConfigured }
  linkToken: new URLSearchParams(location.search).get('invite') ?? new URLSearchParams(location.search).get('reset'),
  link: null,          // LinkDetails for linkToken: { purpose, username, expiresAt }
  https: null,         // HttpsStatusResponse
  security: null,      // SecurityResponse (admins only)
  notifications: null, // NotificationSettings (admins only)
  backups: null,       // BackupsResponse (admins only)
  samples: [],         // sample recipes offered by the setup wizard
  people: { users: [], links: [], newLink: null },
  tab: 'meals',
  plan: null,          // PlanResponse, or null when nothing is staged yet
  ingredients: null,   // IngredientsResponse for the plan
  recipes: [],
  swap: null,          // { mealId, options } while a meal's alternatives are open
  notes: [],
  error: null,
};

const SLOT_TITLES = { Dinner: 'Dinners', Breakfast: 'Breakfasts', Snack: 'Snacks', Sweet: 'Sweets' };
const DAYS = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

// ---------- helpers ----------

/** Creates an element. Text always goes in as text, never as HTML. */
function h(tag, props = {}, ...children) {
  const element = document.createElement(tag);
  for (const [key, value] of Object.entries(props)) {
    if (key === 'class') element.className = value;
    else if (key.startsWith('on')) element.addEventListener(key.slice(2), value);
    else if (value !== false && value != null) element.setAttribute(key, value === true ? '' : value);
  }
  for (const child of children.flat()) {
    if (child != null && child !== false) element.append(child);
  }
  return element;
}

async function api(method, path, body) {
  const response = await fetch(path, {
    method,
    headers: body === undefined ? {} : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await response.text();
  let data = null;
  try { data = text ? JSON.parse(text) : null; } catch { /* not JSON */ }
  if (!response.ok) {
    const error = new Error(data?.detail || data?.title || `The request failed (${response.status}).`);
    error.status = response.status;
    throw error;
  }
  return data;
}

/** Runs an action, then shows either its result or a readable error. */
async function run(action) {
  state.error = null;
  try {
    await action();
  } catch (error) {
    state.error = error.message;
    if (error.status === 401 && state.screen === 'app') {
      state.screen = 'sign-in';
      state.error = 'Your session has ended. Sign in again.';
    }
  }
  render();
}

function parseDate(iso) {
  const [year, month, day] = iso.split('-').map(Number);
  return new Date(year, month - 1, day);
}

function shortDate(iso) {
  return iso ? parseDate(iso).toLocaleDateString(undefined, { month: 'short', day: 'numeric' }) : '';
}

function amount(quantity, unit) {
  if (quantity == null) return '';
  const rounded = Math.round(quantity * 100) / 100;
  return unit ? `${rounded} ${unit}` : `${rounded}`;
}

const chip = (text, tone = '') => h('span', { class: `chip ${tone}` }, text);
const isConfirmed = () => state.plan?.status === 'Confirmed';
const isAdmin = () => state.session?.user?.isAdmin === true;

// ---------- loading and actions ----------

async function loadPlan() {
  try {
    state.plan = await api('GET', '/api/plans/current');
  } catch (error) {
    if (error.status !== 404) throw error;
    state.plan = null;
  }
  state.ingredients = state.plan ? await api('GET', `/api/plans/${state.plan.id}/ingredients`) : null;
}

/** Applies a plan returned by a change, then refreshes the ingredients view to match. */
async function applyPlan(plan) {
  state.plan = plan;
  state.notes = plan.notes ?? [];
  state.swap = null;
  state.ingredients = await api('GET', `/api/plans/${plan.id}/ingredients`);
}

const stage = (body) => run(async () => applyPlan(await api('POST', '/api/plans/stage', body)));
const planAction = (path, body) => run(async () => applyPlan(await api('POST', `/api/plans/${state.plan.id}/${path}`, body)));

function openSwap(meal) {
  return run(async () => {
    const options = await api('GET', `/api/plans/${state.plan.id}/meals/${meal.id}/alternatives`);
    state.swap = { mealId: meal.id, options };
  });
}

function replaceMeal(mealId, body) {
  return run(async () => applyPlan(await api('PUT', `/api/plans/${state.plan.id}/meals/${mealId}`, body)));
}

async function refreshAfterPantryChange() {
  if (state.plan) {
    await applyPlan(await api('POST', `/api/plans/${state.plan.id}/refresh`));
  }
}

// ---------- header ----------

function renderHeader() {
  const week = document.getElementById('week');
  const actions = document.getElementById('actions');
  actions.replaceChildren();

  if (state.screen !== 'app') {
    week.textContent = 'Meal planning around what you have';
    return;
  }

  const account = [
    h('span', { class: 'muted', style: 'align-self:center' }, state.session.user.displayName),
    h('button', { onclick: signOut }, 'Sign out'),
  ];

  if (!state.plan) {
    week.textContent = 'No plan yet';
    actions.append(h('button', { class: 'primary', onclick: () => stage({}) }, 'Stage next week'), ...account);
    return;
  }

  const { weekStart, weekEnd, status } = state.plan;
  week.replaceChildren(
    `Week of ${shortDate(weekStart)} to ${shortDate(weekEnd)} `,
    h('span', { class: `pill ${status.toLowerCase()}` }, status));

  const clearPlan = () => {
    if (!confirm('Clear this plan? Meals and the shopping list are removed. Your pantry is kept.')) return;
    run(async () => {
      await api('DELETE', `/api/plans/${state.plan.id}`);
      state.notes = ['The plan was cleared. Your pantry is unchanged.'];
      state.swap = null;
      await loadPlan();
    });
  };

  const regenerate = () => {
    if (!confirm('Replace this plan with a new one built around what is in your pantry?')) return;
    planAction('regenerate');
  };

  actions.append(
    h('button', { title: 'Remove this plan and keep the pantry', onclick: clearPlan }, 'Clear plan'),
    h('button', { title: 'Build a new plan that uses what you have already bought', onclick: regenerate }, 'Regenerate from pantry'));

  if (isConfirmed()) {
    actions.append(h('button', { onclick: () => stage({}) }, 'Stage next week'));
  } else {
    actions.append(
      h('button', { onclick: () => stage({ weekStart }) }, 'Shuffle'),
      h('button', { class: 'primary', onclick: () => planAction('confirm') }, 'Confirm plan'));
  }
  actions.append(...account);
}

function renderMessages() {
  const box = document.getElementById('messages');
  const error = state.error ? [h('p', { class: 'error', role: 'alert' }, state.error)] : [];
  box.replaceChildren(...error, ...state.notes.map((note) => h('p', { class: 'note' }, note)));
}

// ---------- Meals tab ----------

function renderMeals() {
  const cooked = state.plan.meals.filter((meal) => !meal.isLeftover);
  return Object.entries(SLOT_TITLES).flatMap(([slot, title]) => {
    const meals = cooked.filter((meal) => meal.slot === slot);
    return meals.length === 0 ? [] : [h('h2', {}, title), ...meals.map(mealCard)];
  });
}

function mealCard(meal) {
  const swapOpen = state.swap?.mealId === meal.id;
  return h('article', { class: `card ${meal.status === 'Planned' ? '' : 'done'}` },
    h('div', { class: 'row' },
      h('div', {},
        h('h3', {}, meal.title),
        h('div', { class: 'chips' },
          meal.day ? chip(`${meal.day} ${shortDate(meal.date)}`) : chip('Any day'),
          meal.isSlowCooker ? chip('Slow cooker', 'good') : null,
          meal.isOptional ? chip('Optional') : null,
          meal.status !== 'Planned' ? chip(meal.status) : null),
        h('p', { class: 'muted' }, meal.ingredients.join(', '))),
      h('div', { class: 'row-actions' },
        meal.status === 'Planned'
          ? h('button', { class: 'small', onclick: () => (swapOpen ? closeSwap() : openSwap(meal)) }, swapOpen ? 'Close' : 'Swap')
          : null)),
    swapOpen ? swapPanel(meal) : null);
}

function closeSwap() {
  state.swap = null;
  render();
}

function swapPanel(meal) {
  const { recipes, creative } = state.swap.options;
  return h('div', { class: 'swap' },
    h('h3', {}, 'Recipes that use what you have'),
    recipes.length === 0 ? h('p', { class: 'muted' }, 'No other recipes fit this slot.') : null,
    ...recipes.map((option) => h('div', { class: 'option row' },
      h('div', {},
        h('strong', {}, option.name),
        h('div', { class: 'chips' },
          option.isSlowCooker ? chip('Slow cooker', 'good') : null,
          ...option.expiringSoon.map((name) => chip(`Uses up ${name}`, 'warn')),
          option.onHand.length ? chip(`Have: ${option.onHand.join(', ')}`, 'good') : null,
          option.toBuy.length ? chip(`Buy: ${option.toBuy.join(', ')}`) : null)),
      h('button', { class: 'small', onclick: () => replaceMeal(meal.id, { recipeId: option.recipeId }) }, 'Use this'))),
    creative.length ? h('h3', { style: 'margin-top:.75rem' }, 'Creative ideas: grain, protein and produce') : null,
    ...creative.map((idea) => h('div', { class: 'option row' },
      h('div', {},
        h('strong', {}, idea.title),
        h('div', { class: 'chips' }, ...idea.components.map((part) => part.onHand
          ? chip(part.expiresOn ? `${part.name}, use by ${shortDate(part.expiresOn)}` : `${part.name}, on hand`, 'good')
          : chip(`${part.name}, to buy`)))),
      h('button', { class: 'small', onclick: () => replaceMeal(meal.id, { custom: { title: idea.title, ingredients: idea.ingredients } }) }, 'Use this'))));
}

// ---------- Schedule tab ----------

function renderSchedule() {
  const start = parseDate(state.plan.weekStart);
  const days = DAYS.map((name, offset) => {
    const date = new Date(start.getFullYear(), start.getMonth(), start.getDate() + offset);
    const meals = state.plan.meals.filter((meal) => meal.day === name);
    return h('section', { class: 'card' },
      h('h3', {}, `${name} `, h('span', { class: 'muted' }, date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' }))),
      meals.length === 0 ? h('p', { class: 'muted' }, 'Nothing planned') : meals.map(scheduleLine));
  });

  const anyDay = state.plan.meals.filter((meal) => !meal.day);
  return [
    h('div', { class: 'grid' }, days),
    anyDay.length ? h('section', { class: 'card' }, h('h3', {}, 'Any day: snacks and sweets'), anyDay.map(scheduleLine)) : null,
    isConfirmed() ? null : h('p', { class: 'muted' }, 'Confirm the plan to start marking meals as cooked.'),
  ];
}

function scheduleLine(meal) {
  const setStatus = (status) => planAction(`meals/${meal.id}/status`, { status });
  const buttons = !isConfirmed() ? null
    : meal.status === 'Planned'
      ? [h('button', { class: 'small', onclick: () => setStatus('Cooked') }, meal.isLeftover ? 'Eaten' : 'Cooked'),
         h('button', { class: 'small', onclick: () => setStatus('Skipped') }, 'Skip')]
      : [chip(meal.status), h('button', { class: 'small', onclick: () => setStatus('Planned') }, 'Undo')];

  return h('div', { class: 'slot' },
    h('span', { class: 'label' }, meal.slot),
    h('span', {}, meal.title, meal.isOptional ? ' (optional)' : ''),
    h('span', { class: 'row-actions' }, buttons));
}

// ---------- Ingredients tab ----------

function renderIngredients() {
  const lines = state.ingredients?.ingredients ?? [];
  const byStatus = (status) => lines.filter((line) => line.status === status);
  const toBuy = byStatus('ToBuy');
  const stores = [...new Set(toBuy.map((line) => line.store))];

  const needed = h('section', {},
    h('div', { class: 'row' },
      h('h2', {}, `To buy (${toBuy.length})`),
      isConfirmed() && toBuy.length
        ? h('button', { class: 'small', onclick: () => planAction('shopping/purchase-all', {}) }, 'Mark all as bought')
        : null),
    toBuy.length === 0 ? h('p', { class: 'muted' }, 'Nothing left to buy for this plan.') : null,
    isConfirmed() || toBuy.length === 0 ? null : h('p', { class: 'muted' }, 'Confirm the plan to start ticking items off.'),
    ...stores.map((store) => h('div', { class: 'card' },
      h('h3', {}, store),
      h('ul', { class: 'plain' }, toBuy.filter((line) => line.store === store).map(ingredientLine)))),
    ingredientGroup('From your pantry', byStatus('InPantry')),
    ingredientGroup('Already bought', byStatus('Purchased')),
    ingredientGroup('Staples assumed on hand', byStatus('Staple')));

  return h('div', { class: 'columns' }, needed, renderPantry());
}

function ingredientGroup(title, lines) {
  return lines.length === 0 ? null
    : h('div', {}, h('h2', {}, `${title} (${lines.length})`), h('div', { class: 'card' }, h('ul', { class: 'plain' }, lines.map(ingredientLine))));
}

function ingredientLine(line) {
  const canBuy = line.status === 'ToBuy' && isConfirmed() && line.shoppingItemId;
  return h('li', { class: 'row' },
    h('div', {},
      h('strong', {}, line.name), ' ', amount(line.quantity, line.unit),
      h('div', { class: 'chips' },
        line.isOptional ? chip('Optional') : null,
        line.expiresOn ? chip(`Use by ${shortDate(line.expiresOn)}`, 'warn') : null,
        ...line.usedIn.map((meal) => chip(meal)))),
    canBuy ? h('button', { class: 'small', onclick: () => planAction(`shopping/${line.shoppingItemId}/purchase`, {}) }, 'Bought') : null);
}

function renderPantry() {
  const pantry = state.ingredients?.pantry ?? [];
  const tones = { Expired: 'bad', ExpiringSoon: 'warn', Fresh: 'good', Unknown: '' };
  const labels = { Expired: 'Expired', ExpiringSoon: 'Use soon', Fresh: 'Fresh', Unknown: 'No date' };

  const remove = (id) => run(async () => {
    await api('DELETE', `/api/pantry/${id}`);
    await refreshAfterPantryChange();
  });

  const add = (event) => {
    event.preventDefault();
    const form = new FormData(event.target);
    return run(async () => {
      await api('POST', '/api/pantry', {
        name: form.get('name'),
        quantity: form.get('quantity') ? Number(form.get('quantity')) : null,
        unit: form.get('unit') || null,
        expiresOn: form.get('expiresOn') || null,
      });
      await refreshAfterPantryChange();
    });
  };

  return h('section', {},
    h('h2', {}, `Pantry (${pantry.length})`),
    h('div', { class: 'card' },
      pantry.length === 0 ? h('p', { class: 'muted' }, 'Empty. Items appear here when you mark shopping as bought.') : null,
      h('ul', { class: 'plain' }, pantry.map(({ item, usedByPlan }) => h('li', { class: 'row' },
        h('div', {},
          h('strong', {}, item.name), ' ', amount(item.quantity, item.unit),
          h('div', { class: 'chips' },
            chip(item.expiresOn ? `${labels[item.freshness]}, ${shortDate(item.expiresOn)}` : labels[item.freshness], tones[item.freshness]),
            usedByPlan ? null : chip('Not used by this plan'))),
        h('button', { class: 'small', 'aria-label': `Remove ${item.name}`, onclick: () => remove(item.id) }, 'Remove'))))),
    h('form', { class: 'inline card', onsubmit: add },
      h('label', {}, 'Add an item', h('input', { id: 'pantry-name', name: 'name', required: true, placeholder: 'e.g. cabbage' })),
      h('label', {}, 'Amount', h('input', { id: 'pantry-quantity', name: 'quantity', type: 'number', min: '0', step: 'any' })),
      h('label', {}, 'Unit', h('input', { id: 'pantry-unit', name: 'unit', size: '6', placeholder: 'head' })),
      h('label', {}, 'Expires', h('input', { id: 'pantry-expires', name: 'expiresOn', type: 'date' })),
      h('button', { type: 'submit' }, 'Add')));
}

// ---------- Recipes tab ----------

function renderRecipes() {
  const importFromMealie = () => run(async () => {
    await api('POST', '/api/recipes/import/mealie');
    state.notes = ['Importing from Mealie in the background. This page will update when it finishes.'];
    pollImport();
  });

  const add = (event) => {
    event.preventDefault();
    const form = new FormData(event.target);
    return run(async () => {
      await api('POST', '/api/recipes', {
        name: form.get('name'),
        course: form.get('course'),
        isSlowCooker: form.get('isSlowCooker') === 'on',
        servings: form.get('servings') ? Number(form.get('servings')) : null,
        ingredients: String(form.get('ingredients')).split('\n').map((line) => line.trim()).filter(Boolean),
      });
      event.target.reset();
      state.recipes = await api('GET', '/api/recipes');
    });
  };

  const courses = ['Main', 'Breakfast', 'Snack', 'Sweet', 'Other'];
  return h('div', { class: 'columns' },
    h('section', {},
      h('div', { class: 'row' },
        h('h2', {}, `Recipes (${state.recipes.length})`),
        h('div', { class: 'row-actions' },
          isAdmin() ? h('button', { class: 'small', onclick: () => run(openSamples) }, 'Browse sample recipes') : null,
          state.session.mealieConfigured ? h('button', { class: 'small', onclick: importFromMealie }, 'Import from Mealie') : null)),
      ...courses.map((course) => {
        const recipes = state.recipes.filter((recipe) => recipe.course === course);
        return recipes.length === 0 ? null : h('div', { class: 'card' },
          h('h3', {}, `${course} (${recipes.length})`),
          h('ul', { class: 'plain' }, recipes.map((recipe) => h('li', {},
            h('strong', {}, recipe.name), ' ',
            recipe.isSlowCooker ? chip('Slow cooker', 'good') : null,
            recipe.origin === 'Mealie' ? chip('From Mealie') : null,
            h('p', { class: 'muted' }, recipe.ingredients.join(', '))))));
      })),
    h('section', {},
      h('h2', {}, 'Add a recipe'),
      h('form', { class: 'stack card', onsubmit: add },
        h('label', {}, 'Name', h('input', { id: 'recipe-name', name: 'name', required: true })),
        h('label', {}, 'Course', h('select', { id: 'recipe-course', name: 'course' }, courses.map((course) => h('option', {}, course)))),
        h('label', {}, 'Servings', h('input', { id: 'recipe-servings', name: 'servings', type: 'number', min: '1', step: 'any', placeholder: '4' })),
        h('label', { style: 'display:flex;gap:.4rem;align-items:center' },
          h('input', { id: 'recipe-slow', name: 'isSlowCooker', type: 'checkbox' }), 'Slow cooker'),
        h('label', {}, 'Ingredients, one per line',
          h('textarea', { id: 'recipe-ingredients', name: 'ingredients', required: true, placeholder: '1 lb chicken thighs\n0.5 head cabbage\n2 cups brown rice' })),
        h('button', { type: 'submit', class: 'primary' }, 'Save recipe'))));
}

async function pollImport() {
  for (let attempt = 0; attempt < 60; attempt++) {
    await new Promise((resolve) => setTimeout(resolve, 2000));
    const status = await api('GET', '/api/recipes/import/status').catch(() => null);
    if (!status || status.state === 'Running') continue;

    if (status.state === 'Succeeded') {
      const { added, updated, keptLocal, archived, skipped } = status.result;
      state.notes = [`Mealie import finished: ${added} added, ${updated} updated, ${keptLocal} kept as edited here, ${archived} archived, ${skipped} skipped.`];
      state.recipes = await api('GET', '/api/recipes').catch(() => state.recipes);
    } else {
      state.error = status.message || 'The Mealie import failed.';
      state.notes = [];
    }
    render();
    return;
  }
}

// ---------- setup wizard, sign-in and joining ----------

/** Shared by the setup wizard and the invite page: both create an account. */
function accountForm({ step, title, intro, button, submit }) {
  const onsubmit = (event) => {
    event.preventDefault();
    const form = new FormData(event.target);
    return run(() => submit({
      displayName: form.get('displayName'),
      username: form.get('username'),
      password: form.get('password'),
    }));
  };

  return h('section', { class: 'gate' },
    step ? h('p', { class: 'steps' }, step) : null,
    h('h2', {}, title),
    h('p', { class: 'muted' }, intro),
    h('form', { class: 'stack card', onsubmit },
      h('label', {}, 'Your name', h('input', { id: 'account-name', name: 'displayName', required: true, autocomplete: 'name' })),
      h('label', {}, 'Username', h('input', { id: 'account-username', name: 'username', required: true, minlength: '3', maxlength: '32',
        pattern: '[A-Za-z0-9._\\-]+', autocomplete: 'username', autocapitalize: 'none' })),
      h('label', {}, 'Password (at least 8 characters)', h('input', { id: 'account-password', name: 'password', type: 'password',
        required: true, minlength: '8', autocomplete: 'new-password' })),
      h('button', { type: 'submit', class: 'primary' }, button)));
}

function renderSetupAccount() {
  return accountForm({
    step: 'Setup, step 1 of 3',
    title: 'Create your account',
    intro: 'Welcome to ChefBot. This first account is the admin: it can invite the rest of the household later.',
    button: 'Create account',
    submit: async (account) => {
      state.session.user = await api('POST', '/api/setup/account', account);
      state.session.setupRequired = false;
      await loadSystem();
      if (state.https.enabled) {
        state.screen = 'setup-https';
      } else {
        await openSamples();
      }
    },
  });
}

function renderJoin() {
  return accountForm({
    title: 'Join this household',
    intro: 'You have been invited to ChefBot. Create an account to see the meal plan, shopping list and pantry.',
    button: 'Join',
    submit: async (account) => {
      state.session.user = await api('POST', '/api/account/accept-invite', { token: state.linkToken, ...account });
      await enterApp();
    },
  });
}

function renderSignIn() {
  const onsubmit = (event) => {
    event.preventDefault();
    const form = new FormData(event.target);
    return run(async () => {
      state.session.user = await api('POST', '/api/account/sign-in', { username: form.get('username'), password: form.get('password') });
      await enterApp();
    });
  };

  return h('section', { class: 'gate' },
    h('h2', {}, 'Sign in'),
    h('form', { class: 'stack card', onsubmit },
      h('label', {}, 'Username', h('input', { id: 'signin-username', name: 'username', required: true, autocomplete: 'username', autocapitalize: 'none' })),
      h('label', {}, 'Password', h('input', { id: 'signin-password', name: 'password', type: 'password', required: true, autocomplete: 'current-password' })),
      h('button', { type: 'submit', class: 'primary' }, 'Sign in')),
    h('p', { class: 'muted' }, 'No account, or forgot your password? Ask an admin for an invite or a password reset link.'));
}

function renderReset() {
  const onsubmit = (event) => {
    event.preventDefault();
    const password = new FormData(event.target).get('password');
    return run(async () => {
      state.session.user = await api('POST', '/api/account/reset-password', { token: state.linkToken, password });
      await enterApp();
      state.notes = ['Your password has been changed.'];
    });
  };

  return h('section', { class: 'gate' },
    h('h2', {}, 'Choose a new password'),
    h('p', { class: 'muted' }, state.link?.username ? `For the account @${state.link.username}.` : ''),
    h('form', { class: 'stack card', onsubmit },
      h('label', {}, 'New password (at least 8 characters)', h('input', { id: 'reset-password', name: 'password', type: 'password',
        required: true, minlength: '8', autocomplete: 'new-password' })),
      h('button', { type: 'submit', class: 'primary' }, 'Save password')));
}

// ---------- HTTPS: setup step and Settings section ----------

const httpsUrl = () => `https://${location.hostname}${state.https.port === 443 ? '' : `:${state.https.port}`}/`;

/** Saves the sign-in rules. Turning "always use HTTPS" on from plain HTTP moves the browser to HTTPS. */
async function saveSecurity(changes) {
  state.security = await api('PUT', '/api/system/security', { ...state.security, ...changes });
  state.https.requireHttps = state.security.requireHttps;
  if (state.security.requireHttps && location.protocol === 'http:') {
    location.href = httpsUrl();
  }
}

function httpsPanel() {
  const https = state.https;
  if (!https?.enabled) {
    return h('div', { class: 'card' },
      h('p', {}, 'HTTPS is switched off.'),
      h('p', { class: 'muted' }, 'Set Https__Mode to SelfSigned (or Certificate) and restart to turn it on. Leave it off behind a reverse proxy that adds HTTPS itself.'));
  }

  const covered = [...new Set([...https.hostnames, location.hostname])].join(', ');
  const regenerate = (event) => {
    event.preventDefault();
    const hostnames = String(new FormData(event.target).get('hostnames')).split(',').map((name) => name.trim()).filter(Boolean);
    return run(async () => {
      state.https = await api('POST', '/api/system/https/certificate', { hostnames });
      state.notes = ['A new certificate is in use. Your browser may ask you to accept it again.'];
    });
  };

  return h('div', { class: 'card' },
    https.isSecureNow
      ? h('p', {}, chip('Secure', 'good'), ' You are using HTTPS.')
      : h('p', {}, chip('Not secure', 'warn'), ' You are using plain HTTP. ', h('a', { href: httpsUrl() }, 'Open ChefBot over HTTPS')),
    https.mode === 'SelfSigned'
      ? h('p', { class: 'muted' }, 'ChefBot made its own certificate, so the first visit over HTTPS shows a browser warning. '
          + 'Choose to continue, or install the certificate on your devices to make the warning go away.')
      : null,
    h('p', { class: 'muted' }, https.expiresOn ? `The certificate is valid until ${new Date(https.expiresOn).toLocaleDateString()}.` : ''),
    h('div', { class: 'row-actions', style: 'margin:.5rem 0' },
      h('a', { href: '/api/system/https/certificate', download: 'chefbot.crt' }, 'Download the certificate')),
    isAdmin() && https.canRegenerate ? h('form', { class: 'stack', onsubmit: regenerate },
      h('label', {}, 'Names and addresses people use to reach ChefBot, separated by commas',
        h('input', { id: 'https-hostnames', name: 'hostnames', value: covered, placeholder: 'chefbot.local, 192.168.1.20' })),
      h('div', { class: 'row-actions' }, h('button', { type: 'submit' }, 'Create a certificate for these names'))) : null,
    isAdmin() && state.security ? h('label', { class: 'check', style: 'margin-top:.5rem' },
      h('input', { id: 'require-https', type: 'checkbox', checked: state.security.requireHttps,
        onchange: (event) => run(() => saveSecurity({ requireHttps: event.target.checked })) }),
      h('span', {}, 'Always use HTTPS',
        h('span', { class: 'muted', style: 'display:block' }, 'Sends anyone arriving over plain HTTP to the HTTPS address. Turn it on once HTTPS works in your browser.'))) : null);
}

function renderSetupHttps() {
  return h('section', { class: 'gate' },
    h('p', { class: 'steps' }, 'Setup, step 2 of 3'),
    h('h2', {}, 'Secure connection'),
    h('p', { class: 'muted' }, 'HTTPS keeps passwords private on your network. You can change any of this later on the Settings tab.'),
    httpsPanel(),
    h('div', { class: 'row-actions', style: 'margin-top:1rem' },
      h('button', { class: 'primary', onclick: () => run(openSamples) }, 'Continue')));
}

// ---------- Settings tab ----------

function renderSettings() {
  const changePassword = (event) => {
    event.preventDefault();
    const form = new FormData(event.target);
    return run(async () => {
      await api('POST', '/api/account/password', { currentPassword: form.get('current'), newPassword: form.get('next') });
      event.target.reset();
      state.notes = ['Your password has been changed. Other devices have been signed out.'];
    });
  };

  const saveRules = (event) => {
    event.preventDefault();
    const form = new FormData(event.target);
    return run(async () => {
      await saveSecurity({ maxFailedSignIns: Number(form.get('attempts')), lockoutMinutes: Number(form.get('minutes')) });
      state.notes = ['Sign-in rules saved.'];
    });
  };

  return h('div', { class: 'columns' },
    h('section', {},
      h('h2', {}, 'Your password'),
      h('form', { class: 'stack card', onsubmit: changePassword },
        h('label', {}, 'Current password', h('input', { id: 'password-current', name: 'current', type: 'password', required: true, autocomplete: 'current-password' })),
        h('label', {}, 'New password (at least 8 characters)', h('input', { id: 'password-next', name: 'next', type: 'password', required: true, minlength: '8', autocomplete: 'new-password' })),
        h('button', { type: 'submit' }, 'Change password')),
      isAdmin() && state.security ? [
        h('h2', {}, 'Sign-in lockout'),
        h('form', { class: 'stack card', onsubmit: saveRules },
          h('label', {}, 'Wrong passwords in a row before an account is locked (0 turns lockout off)',
            h('input', { id: 'lockout-attempts', name: 'attempts', type: 'number', min: '0', max: '100', required: true, value: state.security.maxFailedSignIns })),
          h('label', {}, 'Minutes an account stays locked',
            h('input', { id: 'lockout-minutes', name: 'minutes', type: 'number', min: '1', max: '1440', required: true, value: state.security.lockoutMinutes })),
          h('button', { type: 'submit' }, 'Save'))] : null),
    h('section', {},
      h('h2', {}, 'Secure connection (HTTPS)'), httpsPanel(),
      isAdmin() && state.notifications ? notificationsPanel() : null,
      isAdmin() && state.backups ? backupsPanel() : null));
}

const REMINDERS = [
  ['planReady', 'A new plan is ready', 'Sent whenever a week is staged or regenerated.'],
  ['slowCooker', 'Start the slow cooker', 'On the morning of a slow cooker dinner.'],
  ['expiringFood', 'Food to use soon', 'When pantry food is about to expire, noting what no meal uses.'],
  ['shopping', 'Confirm the plan or finish shopping', 'The day before a week starts and on its first day.'],
  ['mealsToday', "Today's meals", 'A daily summary of what is planned.'],
];

function notificationsPanel() {
  const settings = state.notifications;

  const save = (event) => {
    event.preventDefault();
    const form = new FormData(event.target);
    const changes = { webhookUrl: form.get('webhookUrl'), dailyTime: form.get('dailyTime') };
    for (const [key] of REMINDERS) changes[key] = form.get(key) === 'on';
    return run(async () => {
      state.notifications = await api('PUT', '/api/system/notifications', { ...settings, ...changes });
      state.notes = ['Notification settings saved.'];
    });
  };

  const send = (path, describe) => run(async () => {
    state.notes = [describe(await api('POST', path))];
  });

  const describeTest = (result) => (result.delivered ? 'The test notification was delivered.' : `The test wasn't delivered. ${result.problem}`);
  const describeRun = (result) => [
    result.sent.length ? `Sent: ${result.sent.join(', ')}.` : 'Nothing needed sending today.',
    result.problems.length ? `Not delivered: ${result.problems.join('; ')}` : '',
  ].join(' ').trim();

  return [
    h('h2', {}, 'Notifications'),
    h('form', { class: 'stack card', onsubmit: save },
      h('p', { class: 'muted' }, 'ChefBot posts reminders as JSON to a webhook, such as a Home Assistant webhook, which can turn them into phone notifications or announcements.'),
      h('label', {}, 'Webhook address (leave empty to turn notifications off)',
        h('input', { id: 'notify-url', name: 'webhookUrl', type: 'url', value: settings.webhookUrl, placeholder: 'http://homeassistant.local:8123/api/webhook/chefbot' })),
      h('label', {}, 'Send daily reminders at', h('input', { id: 'notify-time', name: 'dailyTime', type: 'time', required: true, value: settings.dailyTime })),
      REMINDERS.map(([key, title, detail]) => h('label', { class: 'check' },
        h('input', { id: `notify-${key}`, name: key, type: 'checkbox', checked: settings[key] }),
        h('span', {}, title, h('span', { class: 'muted', style: 'display:block' }, detail)))),
      h('div', { class: 'row-actions' },
        h('button', { type: 'submit', class: 'primary' }, 'Save'),
        h('button', { type: 'button', onclick: () => send('/api/system/notifications/test', describeTest) }, 'Send a test'),
        h('button', { type: 'button', onclick: () => send('/api/system/notifications/send-today', describeRun) }, "Send today's reminders now"))),
  ];
}

function backupsPanel() {
  const backups = state.backups;
  const size = (bytes) => (bytes < 1048576 ? `${Math.max(1, Math.round(bytes / 1024))} KB` : `${(bytes / 1048576).toFixed(1)} MB`);

  const save = (event) => {
    event.preventDefault();
    const form = new FormData(event.target);
    return run(async () => {
      state.backups = await api('PUT', '/api/system/backups', {
        enabled: form.get('enabled') === 'on',
        time: form.get('time'),
        keepCount: Number(form.get('keepCount')),
        keepDays: Number(form.get('keepDays')),
      });
      state.notes = ['Backup settings saved.'];
    });
  };

  const runNow = () => run(async () => {
    state.backups = await api('POST', '/api/system/backups');
    state.notes = ['Backup made.'];
  });

  const remove = (file) => run(async () => {
    state.backups = await api('DELETE', `/api/system/backups/${file.name}`);
  });

  const restore = (file) => {
    if (!confirm(`Replace everything in ChefBot with the backup from ${new Date(file.createdAt).toLocaleString()}? Anything saved since then is lost.`)) return;
    run(async () => {
      await api('POST', `/api/system/backups/${file.name}/restore`);
      location.reload();
    });
  };

  return [
    h('h2', {}, 'Backups'),
    h('form', { class: 'stack card', onsubmit: save },
      h('label', { class: 'check' },
        h('input', { id: 'backup-enabled', name: 'enabled', type: 'checkbox', checked: backups.enabled }),
        h('span', {}, 'Back up automatically every day')),
      h('label', {}, 'At', h('input', { id: 'backup-time', name: 'time', type: 'time', required: true, value: backups.time })),
      h('label', {}, 'Keep this many backups (0 keeps them all)',
        h('input', { id: 'backup-count', name: 'keepCount', type: 'number', min: '0', max: '1000', required: true, value: backups.keepCount })),
      h('label', {}, 'Delete backups older than this many days (0 keeps them whatever their age)',
        h('input', { id: 'backup-days', name: 'keepDays', type: 'number', min: '0', max: '3650', required: true, value: backups.keepDays })),
      backups.lastResult ? h('p', { class: 'muted' }, backups.lastResult) : null,
      h('div', { class: 'row-actions' },
        h('button', { type: 'submit', class: 'primary' }, 'Save'),
        h('button', { type: 'button', onclick: runNow }, 'Back up now'))),
    h('div', { class: 'card' },
      backups.files.length === 0 ? h('p', { class: 'muted' }, 'No backups yet.') : null,
      h('ul', { class: 'plain' }, backups.files.map((file) => h('li', { class: 'row' },
        h('div', {}, h('strong', {}, new Date(file.createdAt).toLocaleString()), ' ', h('span', { class: 'muted' }, size(file.sizeBytes))),
        h('div', { class: 'row-actions' },
          h('a', { href: `/api/system/backups/${file.name}`, download: file.name }, 'Download'),
          h('button', { class: 'small', onclick: () => restore(file) }, 'Restore'),
          h('button', { class: 'small', onclick: () => remove(file) }, 'Delete')))))),
  ];
}

async function loadSystem() {
  const admin = (path) => (isAdmin() ? api('GET', path) : null);
  [state.https, state.security, state.notifications, state.backups] = await Promise.all([
    api('GET', '/api/system/https'),
    admin('/api/system/security'),
    admin('/api/system/notifications'),
    admin('/api/system/backups'),
  ]);
}

function signOut() {
  return run(async () => {
    await api('POST', '/api/account/sign-out');
    state.session.user = null;
    state.plan = null;
    state.notes = [];
    state.screen = 'sign-in';
  });
}

/** Opens the sample recipe picker: step two of setup, and reachable later from the Recipes tab. */
async function openSamples() {
  state.samples = await api('GET', '/api/setup/samples');
  state.screen = 'setup-samples';
}

function renderSetupSamples() {
  const firstRun = state.recipes.length === 0 && !state.plan;
  const available = state.samples.filter((sample) => !sample.alreadyAdded);
  const courses = ['Main', 'Breakfast', 'Snack', 'Sweet'];

  const setAll = (checked) => {
    for (const box of document.querySelectorAll('#samples input[type=checkbox]:not(:disabled)')) box.checked = checked;
  };

  const onsubmit = (event) => {
    event.preventDefault();
    const names = new FormData(event.target).getAll('sample');
    return run(async () => {
      const result = names.length ? await api('POST', '/api/setup/samples', { names }) : { added: 0 };
      await enterApp();
      state.tab = 'recipes';
      state.notes = [result.added ? `Added ${result.added} recipes. Stage a week when you are ready.` : 'No sample recipes were added.'];
    });
  };

  return h('section', { class: 'gate wide' },
    firstRun ? h('p', { class: 'steps' }, 'Setup, step 3 of 3') : null,
    h('h2', {}, 'Choose starter recipes'),
    h('p', { class: 'muted' },
      'These sample recipes let you try planning straight away. Tick the ones you want, or skip them and add your own. ',
      state.session.mealieConfigured ? 'You can also import your recipes from Mealie on the Recipes tab.' : ''),
    h('form', { id: 'samples', onsubmit },
      h('div', { class: 'row-actions', style: 'margin:.75rem 0' },
        h('button', { type: 'button', class: 'small', onclick: () => setAll(true) }, 'Select all'),
        h('button', { type: 'button', class: 'small', onclick: () => setAll(false) }, 'Select none')),
      ...courses.map((course) => {
        const samples = state.samples.filter((sample) => sample.course === course);
        return samples.length === 0 ? null : h('div', { class: 'card' },
          h('h3', {}, `${SLOT_TITLES[course] ?? 'Dinners'} (${samples.length})`),
          samples.map((sample, index) => h('label', { class: 'check' },
            h('input', { id: `sample-${course}-${index}`, type: 'checkbox', name: 'sample', value: sample.name,
              checked: !sample.alreadyAdded, disabled: sample.alreadyAdded }),
            h('span', {},
              h('strong', {}, sample.name), ' ',
              sample.isSlowCooker ? chip('Slow cooker', 'good') : null,
              sample.alreadyAdded ? chip('Already in your recipes') : null,
              h('span', { class: 'muted', style: 'display:block' }, sample.ingredients.join(', '))))));
      }),
      h('div', { class: 'row-actions', style: 'margin-top:1rem' },
        h('button', { type: 'submit', class: 'primary', disabled: available.length === 0 }, 'Add selected recipes'),
        h('button', { type: 'button', onclick: () => run(enterApp) }, firstRun ? 'Skip for now' : 'Back'))));
}

// ---------- People tab (admins) ----------

async function loadPeople() {
  const [users, links] = await Promise.all([api('GET', '/api/users'), api('GET', '/api/links')]);
  state.people = { ...state.people, users, links };
}

function renderPeople() {
  const { users, links, newLink } = state.people;
  const day = (iso) => new Date(iso).toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
  const isLocked = (user) => user.lockedUntil && new Date(user.lockedUntil) > new Date();

  /** Runs a request that returns a new link, then shows it once so it can be copied. */
  const issue = (label, request) => run(async () => {
    const link = await request();
    const parameter = link.purpose === 'Invite' ? 'invite' : 'reset';
    state.people.newLink = { label, url: `${location.origin}/?${parameter}=${link.token}`, expiresAt: link.expiresAt };
    await loadPeople();
  });

  const change = (request) => run(async () => {
    await request();
    state.people.newLink = null;
    await loadPeople();
  });

  const copyLink = async (event) => {
    const input = document.getElementById('new-link');
    try {
      await navigator.clipboard.writeText(input.value);
      event.target.textContent = 'Copied';
    } catch {
      input.select();
    }
  };

  const describe = (link) => (link.purpose === 'Invite' ? 'Invite' : `Password reset for @${link.username ?? 'a removed account'}`);

  return h('div', { class: 'columns' },
    h('section', {},
      h('h2', {}, `People (${users.length})`),
      h('div', { class: 'card' }, h('ul', { class: 'plain' }, users.map((user) => h('li', { class: 'row' },
        h('div', {},
          h('strong', {}, user.displayName), ' ', h('span', { class: 'muted' }, `@${user.username}`),
          h('div', { class: 'chips' },
            user.isAdmin ? chip('Admin', 'good') : null,
            isLocked(user) ? chip('Locked', 'bad') : null,
            user.id === state.session.user.id ? chip('You') : null,
            chip(`Joined ${day(user.createdAt)}`))),
        h('div', { class: 'row-actions' },
          isLocked(user) ? h('button', { class: 'small', onclick: () => change(() => api('POST', `/api/users/${user.id}/unlock`)) }, 'Unlock') : null,
          h('button', { class: 'small', onclick: () => issue(`Password reset link for @${user.username}`,
            () => api('POST', `/api/users/${user.id}/password-reset`)) }, 'Reset password'),
          user.id === state.session.user.id ? null
            : h('button', { class: 'small', onclick: () => change(() => api('DELETE', `/api/users/${user.id}`)) }, 'Remove'))))))),
    h('section', {},
      h('h2', {}, 'Invite someone'),
      h('div', { class: 'card' },
        h('p', { class: 'muted' }, 'An invite link lets one person create an account. It works once and expires after 7 days.'),
        h('div', { class: 'row-actions', style: 'margin-top:.5rem' },
          h('button', { class: 'primary', onclick: () => issue('Invite link', () => api('POST', '/api/invites')) }, 'Create invite link'))),
      newLink ? h('div', { class: 'card' },
        h('h3', {}, newLink.label),
        h('div', { class: 'link-box' },
          h('input', { id: 'new-link', readonly: true, value: newLink.url, 'aria-label': newLink.label }),
          h('button', { onclick: copyLink }, 'Copy')),
        h('p', { class: 'muted' }, `Copy this link now: it is only shown once. It expires ${day(newLink.expiresAt)}. `
          + 'If you lose it, use Regenerate below to replace it.')) : null,
      links.length ? h('h2', {}, `Unused links (${links.length})`) : null,
      links.length ? h('div', { class: 'card' }, h('ul', { class: 'plain' }, links.map((link) => h('li', { class: 'row' },
        h('div', {}, h('strong', {}, describe(link)),
          h('span', { class: 'muted', style: 'display:block' }, `Created ${day(link.createdAt)}, expires ${day(link.expiresAt)}`)),
        h('div', { class: 'row-actions' },
          h('button', { class: 'small', title: 'Stop the old link working and show a new one',
            onclick: () => issue(describe(link), () => api('POST', `/api/links/${link.id}/regenerate`)) }, 'Regenerate'),
          h('button', { class: 'small', onclick: () => change(() => api('DELETE', `/api/links/${link.id}`)) }, 'Revoke')))))) : null));
}

// ---------- page ----------

/** Loads everything the signed-in views need and shows the app. */
async function enterApp() {
  if (state.linkToken) {
    history.replaceState(null, '', location.pathname);
    state.linkToken = null;
  }
  state.screen = 'app';
  [state.recipes] = await Promise.all([api('GET', '/api/recipes'), loadPlan(), loadSystem(), isAdmin() ? loadPeople() : null]);
}

async function start() {
  state.session = await api('GET', '/api/session');
  if (state.session.setupRequired) {
    state.screen = 'setup-account';
  } else if (state.session.user) {
    await enterApp();
  } else if (state.linkToken) {
    await api('GET', `/api/account/link?token=${encodeURIComponent(state.linkToken)}`)
      .then((link) => { state.link = link; state.screen = link.purpose === 'Invite' ? 'join' : 'reset'; })
      .catch((error) => { state.screen = 'sign-in'; state.error = error.message; });
  } else {
    state.screen = 'sign-in';
  }
}

function render() {
  renderHeader();
  renderMessages();

  const inApp = state.screen === 'app';
  document.getElementById('tabs').hidden = !inApp;
  document.getElementById('footer').hidden = !inApp;
  document.getElementById('people-tab').hidden = !isAdmin();
  for (const tab of document.querySelectorAll('.tabs button')) {
    tab.setAttribute('aria-selected', String(tab.dataset.tab === state.tab));
  }

  const view = document.getElementById('view');
  const gates = { 'setup-account': renderSetupAccount, 'setup-https': renderSetupHttps, 'setup-samples': renderSetupSamples,
    'sign-in': renderSignIn, join: renderJoin, reset: renderReset };
  if (state.screen === 'loading') {
    view.replaceChildren(h('p', { class: 'muted' }, 'Loading…'));
  } else if (!inApp) {
    view.replaceChildren(gates[state.screen]());
  } else if (state.tab === 'recipes') {
    view.replaceChildren(renderRecipes());
  } else if (state.tab === 'people') {
    view.replaceChildren(renderPeople());
  } else if (state.tab === 'settings') {
    view.replaceChildren(renderSettings());
  } else if (!state.plan) {
    view.replaceChildren(h('div', { class: 'card empty' },
      h('h2', {}, 'Nothing is staged yet'),
      h('p', { class: 'muted' }, state.recipes.length === 0
        ? 'Add some recipes on the Recipes tab first, then stage a week.'
        : 'Stage a week to see proposed meals, a schedule and the ingredients it needs.')));
  } else {
    const content = { meals: renderMeals, schedule: renderSchedule, ingredients: renderIngredients }[state.tab]();
    view.replaceChildren(...[content].flat().filter(Boolean));
  }
}

for (const tab of document.querySelectorAll('.tabs button')) {
  tab.addEventListener('click', () => {
    state.tab = tab.dataset.tab;
    state.notes = [];
    state.error = null;
    render();
  });
}

run(start);
