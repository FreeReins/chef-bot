import { api } from './api.js';
import { addDays, shortDate, upcomingSunday } from './format.js';
import { copyStyle } from './styles.js';
import { saveStyles } from './settings.svelte.js';
import { app, ask, isConfirmed, run, toast } from './state.svelte.js';
import { live, onLive } from './live.svelte.js';

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
    app.dealt = Date.now();
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
    app.dealt = Date.now();
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
  return run(async () => {
    await api('DELETE', `/api/plans/${app.plan.id}`);
    await loadPlan(null);
    toast(draft ? 'The draft was discarded. Your pantry is unchanged.' : 'The plan was cleared. Your pantry is unchanged.');
    app.swap = null;
  });
}

export async function regenerate() {
  if (!(await ask({ title: 'Replan from your pantry?', message: 'This week is planned again around what is in your pantry, so you buy less. Meals already cooked are not kept.',
    confirmLabel: 'Replan' }))) return;
  return planAction('regenerate');
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

/**
 * Adds a recipe to a week's plan from its page: { recipeId, weekStart, date, slot, servings, replaces }. The plan
 * comes back at once with the meal on it; its shopping list catches up in the background, and the live stream reloads
 * the plan when it has. Without the live stream, the plan is brought in line here instead. `added(plan)` runs once
 * it is saved.
 */
export const addToPlan = (body, added) => run(async () => {
  let plan = await api('POST', '/api/plans/meals', body);
  if (!live.connected) plan = await api('POST', `/api/plans/${plan.id}/refresh`);
  await applyPlan(plan, { quiet: true });
  added(plan);
});

/**
 * After the pantry changed: ChefBot brings the open plans' shopping lists in line in the background and says so on
 * the live stream, which reloads the plan. Here only what the pantry itself shows is loaded. Without the live stream,
 * the plan is brought in line and loaded straight away instead.
 */
export async function refreshAfterPantryChange() {
  if (app.plan && !live.connected) {
    await applyPlan(await api('POST', `/api/plans/${app.plan.id}/refresh`), { quiet: true });
  } else if (app.plan) {
    app.ingredients = await ingredientsFor(app.plan);
  } else {
    await loadPantry();
  }
}

/** Takes an item out of the pantry, with an Undo that puts it back as it was. */
export const removePantryItem = (item) => run(async () => {
  await api('DELETE', `/api/pantry/${item.id}`);
  await refreshAfterPantryChange();
  toast(`${item.name} was taken out of the pantry.`, 'note', {
    label: 'Undo',
    run: () => run(async () => {
      await api('POST', '/api/pantry', { name: item.name, quantity: item.quantity, unit: item.unit || null, expiresOn: item.expiresOn });
      await refreshAfterPantryChange();
    }),
  });
});

/** Corrects how much of a pantry item is left, or when it expires: { quantity?, expiresOn? }. */
export const updatePantryItem = (id, changes) => run(async () => {
  await api('PATCH', `/api/pantry/${id}`, changes);
  await refreshAfterPantryChange();
});

/** Loads the pantry on its own. With a plan, the pantry comes with the plan's ingredients view instead. */
export const loadPantry = async () => { app.pantry = await api('GET', '/api/pantry'); };

// Someone changed the plan or the pantry, here or on another device.
onLive('plan', () => loadPlan());
onLive('pantry', () => loadPantry());
