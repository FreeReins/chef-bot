import { api } from './api.js';
import { copyStyle } from './styles.js';
import { loadFileFormats, loadRecipes } from './recipes.svelte.js';
import { loadPlanning, loadStores, loadStyles } from './settings.svelte.js';
import { app, run } from './state.svelte.js';

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
    const steps = setupSteps();
    const from = steps.findIndex((step) => step.screen === app.screen);
    const to = steps.findIndex((step) => step.screen === screen);
    app.setupDirection = to < from ? -1 : 1;
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

/** Setup: choose how to fill the recipe library. */
async function openSetupRecipes() {
  const [samples] = await Promise.all([api('GET', '/api/setup/samples'), loadFileFormats(), loadRecipes()]);
  app.samples = samples;
  app.screen = 'setup-recipes';
}

/**
 * Setup: how many people to plan for, and how weeks are planned. Until the household has styles, every starter style
 * is offered, the first as the default; choices not saved yet are kept while moving between steps.
 */
async function openSetupStyles() {
  await Promise.all([loadStyles(), loadPlanning()]);
  app.peopleDraft ??= app.planning.people;
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
