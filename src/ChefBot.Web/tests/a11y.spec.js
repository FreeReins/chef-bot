// Every screen of the page, checked with axe in each project of playwright.config.js (light, dark, phone).
// To cover a new screen or state, add a row to VIEWS: where to go, any answers to change, and what to click first.
import { test, expect, open, reply, library } from './fixtures.js';
import { expectAccessible } from './axe.js';
import * as data from './data.js';

const signedOut = { 'GET /api/session': { setupRequired: false, user: null } };
const noPlan = { 'GET /api/plans': [] };
/** Answers for weeks with these plans: the list of weeks, and each plan by its id. */
const weeks = (...plans) => ({
  'GET /api/plans': plans.map(data.summary),
  'GET /api/plans/*': (request) => plans.find((plan) => request.url().endsWith(plan.id)) ?? plans[0],
});
const staged = weeks(data.plan('Staged'));
/** An AI model is set up for every AI feature. */
const AI = { 'GET /api/ai/features': data.aiFeatures() };
/** Never answers, so the page stays on its loading placeholder. */
const holding = () => new Promise(() => {});
/** The library search, held open except for the one-recipe count the page asks for as it starts. */
const searchHeld = (request) => (new URL(request.url()).searchParams.get('pageSize') === '1'
  ? library(data.recipes())['GET /api/recipes/search'](request) : holding());

/** A library with two cookbooks, the tacos and the curry in Weeknight and the crumble in Grandma's. */
const withCookbooks = library(data.recipesInCookbooks(), data.cookbooks());
/** Plan styles once the household has cookbooks, so a style can have a cookbook rule. */
const stylesWithCookbooks = {
  ...data.planStyles(), cookbooks: data.cookbooks().map(({ id, name }) => ({ id, name })),
};

/** Reloads the page with no connection, once the browser has saved what it shows offline. */
async function goOffline(page, api) {
  await page.waitForFunction(() => localStorage.getItem('chefbot-offline'));
  api.offline = true;
  await page.reload();
  await page.getByText('Offline.', { exact: true }).waitFor();
}

/** { name, path, answers?: routes for api.set(), act?: async (page, api) => {} run before checking } */
const VIEWS = [
  // Before signing in
  { name: 'sign in', path: '/', answers: signedOut },
  { name: 'join from an invite', path: '/?invite=invite-token', answers: signedOut },
  { name: 'choose a new password', path: '/?reset=reset-token', answers: signedOut },
  { name: 'setup: create the account', path: '/', answers: { 'GET /api/session': { setupRequired: true, user: null } } },

  // The plan
  { name: 'plan, confirmed week', path: '/plan' },
  {
    name: 'plan, a dinner just marked cooked asks how it was', path: '/plan',
    act: async (page, api) => {
      const cooked = data.plan();
      cooked.meals.find((meal) => meal.title === 'Beef and Barley Stew').status = 'Cooked';
      api.set({ 'POST /api/plans/*/meals/*/status': cooked });
      await page.getByRole('button', { name: 'Cooked: Beef and Barley Stew' }).click();
      await page.getByText('How was it?').waitFor();
    },
  },
  {
    name: 'plan, draft to review with its shopping list open', path: '/plan', answers: staged,
    act: (page) => page.getByText(/items? to buy/).click(),
  },
  {
    name: 'plan, draft with a meal kept and a swap open', path: '/plan', answers: staged,
    act: async (page) => {
      await page.getByRole('button', { name: 'Keep Beef and Barley Stew' }).click();
      await page.getByRole('button', { name: 'Swap Chickpea Curry' }).click();
      await page.getByText('Recipes that use what you have').waitFor();
      await page.getByText("What's needed").first().click();
    },
  },
  {
    name: 'plan, a meal\'s ⋯ menu open', path: '/plan',
    act: async (page) => {
      await page.getByRole('button', { name: 'More for Beef and Barley Stew' }).click();
      await page.getByRole('button', { name: 'Skip Beef and Barley Stew' }).waitFor();
    },
  },
  { name: 'plan, nothing planned', path: '/plan', answers: noPlan },
  { name: 'plan, nothing planned and no recipes', path: '/plan', answers: { ...noPlan, ...library([]) } },
  { name: 'plan, nothing planned and no styles saved', path: '/plan',
    answers: { ...noPlan, 'GET /api/plan-styles': { ...data.planStyles(), styles: [] } } },
  { name: 'plan, this week confirmed and next week a draft', path: '/plan', answers: weeks(data.plan(), data.nextPlan()) },
  {
    name: 'plan, looking at next week\'s draft', path: '/plan', answers: weeks(data.plan(), data.nextPlan()),
    act: (page) => page.getByRole('button', { name: /, draft$/ }).click()
      .then(() => page.getByRole('heading', { name: /draft for/ }).waitFor()),
  },
  { name: 'shopping', path: '/shopping' },
  {
    name: 'shopping, asking before marking everything bought', path: '/shopping',
    act: (page) => page.getByRole('button', { name: 'Mark all as bought' }).click()
      .then(() => page.getByRole('dialog').waitFor()),
  },
  {
    name: 'shopping with the items not on the list opened', path: '/shopping',
    act: async (page) => {
      for (const summary of await page.locator('summary').all()) await summary.click();
    },
  },
  { name: 'shopping, staged plan with hand-added items', path: '/shopping',
    answers: { ...staged, 'GET /api/shopping/items': data.manualItems() } },
  { name: 'shopping, with two weeks planned', path: '/shopping', answers: weeks(data.plan(), data.nextPlan()) },
  { name: 'shopping without a plan', path: '/shopping', answers: { ...noPlan, 'GET /api/shopping/items': data.manualItems() } },
  { name: 'shopping without a plan, empty list', path: '/shopping', answers: { ...noPlan, 'GET /api/pantry': [] } },
  { name: 'shopping, printed', path: '/shopping', act: (page) => page.emulateMedia({ media: 'print' }) },
  { name: 'shopping with to-do apps to send to', path: '/shopping', answers: { 'GET /api/shopping/targets': data.shoppingTargets() } },
  {
    name: 'shopping, offline with a change not sent yet', path: '/shopping', answers: { 'GET /api/shopping/items': data.manualItems() },
    act: async (page, api) => {
      await goOffline(page, api);
      await page.getByLabel('Add to the list').fill('bread');
      await page.getByRole('button', { name: 'Add', exact: true }).click();
      await page.getByText('Not sent yet').waitFor();
    },
  },
  { name: 'pantry, offline', path: '/pantry', act: goOffline },
  { name: 'recipes, offline', path: '/recipes', act: goOffline },
  { name: 'pantry with a plan', path: '/pantry' },
  { name: 'pantry without a plan', path: '/pantry', answers: noPlan },
  { name: 'pantry, many items to use soon', path: '/pantry', answers: { ...noPlan, 'GET /api/pantry': data.pantryToUseSoon() } },
  {
    name: 'pantry, all items to use soon shown', path: '/pantry', answers: { ...noPlan, 'GET /api/pantry': data.pantryToUseSoon() },
    act: (page) => page.getByRole('button', { name: 'Show 2 more' }).click()
      .then(() => page.getByRole('button', { name: 'Show fewer' }).waitFor()),
  },
  {
    name: 'pantry, correcting an item', path: '/pantry',
    act: (page) => page.getByRole('button', { name: 'Change the amount or date of eggs' }).click()
      .then(() => page.getByRole('button', { name: 'Save' }).waitFor()),
  },
  {
    name: 'pantry, an item removed with its undo note', path: '/pantry', answers: {
      'DELETE /api/pantry/*': reply(204),
      'POST /api/plans/*/refresh': data.plan(),
    },
    act: (page) => page.getByRole('button', { name: 'Remove eggs' }).click()
      .then(() => page.getByRole('button', { name: 'Undo' }).waitFor()),
  },

  // Recipes
  { name: 'recipes', path: '/recipes' },
  { name: 'recipes, loading', path: '/recipes', answers: { 'GET /api/recipes/search': searchHeld },
    act: (page) => page.getByRole('status', { name: 'Loading recipes' }).waitFor() },
  {
    name: 'recipes as a list, loading', path: '/recipes', answers: { 'GET /api/recipes/search': searchHeld },
    act: async (page) => {
      await page.evaluate(() => localStorage.setItem('chefbot.recipes.layout', 'list'));
      await page.reload();
      await page.getByRole('status', { name: 'Loading recipes' }).waitFor();
    },
  },
  {
    name: 'recipes, the download all choices open', path: '/recipes',
    act: (page) => page.getByRole('button', { name: 'Download all' }).click()
      .then(() => page.getByRole('link', { name: 'Download as PDF' }).waitFor()),
  },
  {
    name: 'recipes, filters open with a tag and a time chosen', path: '/recipes',
    act: async (page) => {
      await page.getByRole('button', { name: 'Filters' }).click();
      await page.getByRole('textbox', { name: 'Tags' }).fill('veg');
      await page.getByRole('button', { name: /^#vegetarian / }).click();
      await page.getByRole('combobox', { name: 'Ready within' }).selectOption('45');
      await page.getByText('1 recipe matches').waitFor();
    },
  },
  {
    name: 'recipes, filters chosen and the panel closed again', path: '/recipes',
    act: async (page) => {
      await page.getByRole('button', { name: 'Filters' }).click();
      await page.getByRole('combobox', { name: 'Course' }).selectOption('Main');
      await page.getByRole('button', { name: /^#quick / }).click();
      await page.getByRole('button', { name: /^Filters/ }).click();
      await page.getByRole('button', { name: 'Remove filter #quick' }).waitFor();
    },
  },
  {
    name: "recipes, someone else's favourites", path: '/recipes',
    act: async (page) => {
      await page.getByRole('button', { name: 'Filters' }).click();
      await page.getByRole('combobox', { name: 'Favourites' }).selectOption({ label: "James' favourites" });
      await page.getByText('1 recipe matches').waitFor();
    },
  },
  {
    name: 'recipes, filters that match nothing', path: '/recipes',
    act: async (page) => {
      await page.getByRole('button', { name: 'Filters' }).click();
      await page.getByRole('combobox', { name: 'Favourites' }).selectOption('My favourites');
      await page.getByRole('checkbox', { name: 'Slow cooker only' }).check();
      await page.getByText('No recipes match').waitFor();
    },
  },
  {
    name: 'recipes, filters open in a library with very many tags', path: '/recipes', answers: library(data.manyTags(150)),
    act: async (page) => {
      await page.getByRole('button', { name: 'Filters' }).click();
      await page.getByText(/^and \d+ more: type to find them$/).waitFor();
      await page.getByRole('textbox', { name: 'Tags' }).fill('tag 1');
      await page.getByText('Matching').waitFor();
    },
  },
  {
    name: 'recipes, searching with no match', path: '/recipes',
    act: (page) => page.getByRole('searchbox', { name: 'Search' }).fill('zzz')
      .then(() => page.getByText('No recipes match').waitFor()),
  },
  {
    name: 'recipes, the second of several pages', path: '/recipes', answers: library(data.manyRecipes(60)),
    act: (page) => page.getByRole('button', { name: 'Next' }).click()
      .then(() => page.getByText('Page 2 of 3').waitFor()),
  },
  {
    name: 'recipes as a list', path: '/recipes',
    act: (page) => page.getByRole('button', { name: 'List' }).click(),
  },
  { name: 'recipes, an empty library', path: '/recipes', answers: library([]) },
  {
    name: 'recipes, filters open with no cookbooks, so none to choose', path: '/recipes',
    act: async (page) => {
      await page.getByRole('button', { name: 'Filters' }).click();
      await page.getByRole('combobox', { name: 'Course' }).waitFor();
      await expect(page.getByRole('combobox', { name: 'Cookbook' })).toHaveCount(0);
    },
  },
  {
    name: 'recipes, filters open with a cookbook chosen', path: '/recipes', answers: withCookbooks,
    act: async (page) => {
      await page.getByRole('button', { name: 'Filters' }).click();
      await page.getByRole('combobox', { name: 'Cookbook' }).selectOption({ label: 'Weeknight (2)' });
      await page.getByText('2 recipes match').waitFor();
      await page.getByRole('heading', { name: 'Weeknight' }).waitFor();
    },
  },
  {
    name: 'recipes, one cookbook opened from its link', path: `/recipes?cookbook=${data.ids.weeknight}`, answers: withCookbooks,
    act: (page) => page.getByRole('button', { name: 'Remove filter Weeknight' }).waitFor(),
  },
  {
    name: 'recipes, a cookbook being renamed', path: `/recipes?cookbook=${data.ids.weeknight}`, answers: withCookbooks,
    act: async (page) => {
      await page.getByRole('button', { name: 'Rename' }).click();
      await page.getByRole('textbox', { name: 'Name' }).waitFor();
    },
  },
  {
    name: 'recipes, a cookbook\'s downloads open', path: `/recipes?cookbook=${data.ids.weeknight}`, answers: withCookbooks,
    act: async (page) => {
      await page.getByRole('button', { name: 'Download', exact: true }).click();
      await page.locator('#cookbook-downloads').getByRole('link', { name: 'Download as PDF' }).waitFor();
    },
  },
  {
    name: 'recipes, one just made a favourite', path: '/recipes',
    act: (page) => page.getByRole('button', { name: 'Favourite Chickpea Curry' }).click()
      .then(() => page.getByRole('button', { name: 'Favourite Chickpea Curry', pressed: true }).waitFor()),
  },
  { name: 'add a recipe from a link', path: '/recipes/new/link' },
  { name: 'add a recipe by typing it in', path: '/recipes/new/write' },
  { name: 'add recipes from files', path: '/recipes/new/files' },
  {
    name: 'add a recipe from a link, with cookbooks to put it in', path: '/recipes/new/link', answers: withCookbooks,
    act: (page) => page.getByRole('combobox', { name: 'Add to cookbook' }).selectOption({ label: 'Weeknight' }),
  },
  { name: 'add a recipe from a photo, no AI model', path: '/recipes/new/photo' },
  { name: 'add a recipe from a photo', path: '/recipes/new/photo', answers: { 'GET /api/ai/features': data.aiFeatures() },
    act: (page) => page.getByLabel(/Photos of one recipe/).setInputFiles({ name: 'card.png', mimeType: 'image/png', buffer: data.png })
      .then(() => page.getByRole('img', { name: 'Page 1' }).waitFor()) },
  {
    name: 'add a recipe from a photo, checking what was read', path: '/recipes/new/photo',
    answers: { 'GET /api/ai/features': data.aiFeatures(), 'POST /api/recipes/import/photo': data.photoDraft() },
    act: async (page) => {
      await page.getByLabel(/Photos of one recipe/).setInputFiles({ name: 'card.png', mimeType: 'image/png', buffer: data.png });
      await page.getByRole('button', { name: 'Read recipe' }).click();
      await page.getByText('Read with Ollama').waitFor();
    },
  },
  { name: 'a recipe', path: `/recipes/${data.ids.tacos}` },
  { name: 'a recipe missing some details', path: `/recipes/${data.ids.curry}`, answers: AI },
  {
    name: 'a recipe with suggestions to review', path: `/recipes/${data.ids.stew}/improve`,
    answers: { ...AI, ...library(data.recipes().map((recipe) => (recipe.id === data.ids.stew ? { ...recipe, hasSuggestion: true } : recipe))),
      'GET /api/recipes/*/suggestion': data.suggestion(data.ids.stew) },
    act: (page) => page.getByRole('button', { name: /Apply 6 changes/ }).waitFor(),
  },
  {
    name: 'a recipe whose suggestions are stale', path: `/recipes/${data.ids.stew}/improve`,
    answers: { ...AI, 'GET /api/recipes/*/suggestion': data.suggestion(data.ids.stew, true) },
    act: (page) => page.getByText('no longer fit').waitFor(),
  },
  {
    name: 'a recipe with no suggestions yet', path: `/recipes/${data.ids.stew}/improve`, answers: AI,
    act: (page) => page.getByRole('button', { name: 'Ask for suggestions' }).waitFor(),
  },
  { name: 'a recipe, loading', path: `/recipes/${data.ids.tacos}`, answers: { [`GET /api/recipes/${data.ids.tacos}`]: holding },
    act: (page) => page.getByRole('status', { name: 'Loading the recipe' }).waitFor() },
  {
    name: 'a recipe with its menu open', path: `/recipes/${data.ids.tacos}`,
    act: (page) => page.getByRole('button', { name: /^More actions/ }).click()
      .then(() => page.getByRole('button', { name: 'Archive recipe' }).waitFor()),
  },
  { name: 'a recipe, printed', path: `/recipes/${data.ids.tacos}`, act: (page) => page.emulateMedia({ media: 'print' }) },
  { name: 'a recipe without photos', path: `/recipes/${data.ids.stew}` },
  { name: 'a recipe, scaled to 8', path: `/recipes/${data.ids.tacos}?serves=8` },
  {
    name: 'adding a recipe to the plan', path: `/recipes/${data.ids.tacos}/plan`, answers: weeks(data.planOn(data.nextSunday())),
    act: (page) => page.getByRole('button', { name: /^Next week/ }).click(),
  },
  {
    name: 'adding a recipe to a taken slot', path: `/recipes/${data.ids.tacos}/plan`, answers: weeks(data.planOn(data.nextSunday())),
    act: async (page) => {
      await page.getByRole('button', { name: /^Next week/ }).click();
      await page.getByRole('button', { name: /Beef and Barley Stew planned$/ }).click();
      await page.getByRole('radio', { name: 'Replace it' }).waitFor();
    },
  },
  {
    name: 'adding a recipe with no plan that week', path: `/recipes/${data.ids.tacos}/plan`, answers: noPlan,
    act: (page) => page.getByText('No plan for this week yet.', { exact: false }).waitFor(),
  },
  {
    name: 'a recipe, changing your review', path: `/recipes/${data.ids.tacos}`,
    act: (page) => page.getByRole('button', { name: /^Change your review/ }).click(),
  },
  { name: 'editing a recipe', path: `/recipes/${data.ids.tacos}/edit` },
  {
    name: 'a recipe in cookbooks', path: `/recipes/${data.ids.tacos}`, answers: withCookbooks,
    act: (page) => page.getByRole('link', { name: 'Weeknight' }).waitFor(),
  },
  {
    name: 'a recipe\'s cookbooks, before any exist', path: `/recipes/${data.ids.tacos}/cookbooks`,
    act: (page) => page.getByText('Make the first one here').waitFor(),
  },
  {
    name: 'a recipe\'s cookbooks, choosing which', path: `/recipes/${data.ids.tacos}/cookbooks`, answers: withCookbooks,
    act: (page) => page.getByRole('checkbox', { name: 'Weeknight' }).waitFor(),
  },
  { name: 'a recipe\'s photos', path: `/recipes/${data.ids.tacos}/photos` },
  { name: 'cooking mode, gathering the ingredients', path: `/recipes/${data.ids.tacos}/cook` },
  {
    name: 'cooking mode, what to start before cooking', path: `/recipes/${data.ids.tacos}/cook`,
    act: (page) => page.getByRole('button', { name: 'Next' }).click()
      .then(() => page.getByRole('heading', { name: 'Before you start' }).waitFor()),
  },
  {
    name: 'cooking mode, a step with a timer running and larger text', path: `/recipes/${data.ids.tacos}/cook`,
    act: async (page) => {
      await page.getByRole('combobox', { name: 'Go to' }).selectOption('Step 2');
      await page.getByRole('button', { name: 'Start a timer for 8 to 10 minutes' }).click();
      await page.getByRole('button', { name: 'Larger text' }).click();
      await page.getByRole('region', { name: 'Timers' }).waitFor();
    },
  },
  {
    name: 'cooking mode, a timer that has ended', path: `/recipes/${data.ids.tacos}/cook`,
    act: async (page) => {
      await page.clock.install();
      await page.getByRole('combobox', { name: 'Go to' }).selectOption('Step 2');
      await page.getByRole('button', { name: 'Start a timer for 8 to 10 minutes' }).click();
      await page.clock.fastForward('09:00');
      await page.getByText("Time's up", { exact: true }).waitFor();
    },
  },
  {
    name: 'cooking mode, the last page', path: `/recipes/${data.ids.tacos}/cook`,
    act: (page) => page.getByRole('combobox', { name: 'Go to' }).selectOption('Done')
      .then(() => page.getByRole('heading', { name: 'That\'s every step' }).waitFor()),
  },
  {
    name: 'cooking mode, the last page, rated with a note being written', path: `/recipes/${data.ids.tacos}/cook`,
    act: async (page) => {
      const cooking = page.getByRole('dialog', { name: 'Slaw Fish Tacos' });
      await cooking.getByRole('combobox', { name: 'Go to' }).selectOption('Done');
      await cooking.getByRole('button', { name: '4 stars' }).click();
      await cooking.getByText('Saved, 4 stars.').waitFor();
      await cooking.getByRole('button', { name: 'Add a note' }).click();
    },
  },
  { name: 'cooking mode, a recipe with no steps', path: `/recipes/${data.ids.stew}/cook` },
  { name: 'a recipe that is not in the library', path: '/recipes/00000000-0000-4000-8000-00000000ffff' },
  { name: 'sample recipes', path: '/recipes/samples' },

  // The food history
  { name: 'history', path: '/history' },
  { name: 'history, nothing kept yet', path: '/history', answers: { 'GET /api/history': data.emptyHistory() } },
  {
    name: 'history, older months loaded', path: '/history',
    act: async (page, api) => {
      api.set({ 'GET /api/history': (request) => (new URL(request.url()).searchParams.has('before')
        ? data.olderHistory() : data.history()) });
      await page.getByRole('button', { name: 'Dig deeper' }).click();
      await page.getByText('The very first forkful.').waitFor();
    },
  },

  // People and settings
  {
    name: 'people, after creating an invite link', path: '/people', answers: { 'POST /api/invites': data.issuedLink() },
    act: (page) => page.getByRole('button', { name: 'Create invite link' }).click()
      .then(() => page.getByRole('button', { name: 'Copy' }).waitFor()),
  },
  {
    name: 'people, a password reset link shown under the person', path: '/people',
    answers: { 'POST /api/users/*/password-reset': { ...data.issuedLink(), purpose: 'PasswordReset', token: 'new-reset-token' } },
    act: (page) => page.getByRole('button', { name: 'Reset password' }).last().click()
      .then(() => page.getByRole('button', { name: 'Copy' }).waitFor()),
  },
  {
    name: 'people, asking before removing someone', path: '/people',
    act: (page) => page.getByRole('button', { name: 'Remove @sam' }).click()
      .then(() => page.getByRole('dialog').waitFor()),
  },
  { name: 'settings: login', path: '/settings/login' },
  { name: 'settings: appearance', path: '/settings/appearance' },
  {
    // Phones only: the sidebar opened from the menu button. Wide screens show it all the time.
    name: 'settings: the sections menu open', path: '/settings/login',
    act: async (page) => {
      const menu = page.getByRole('button', { name: /^Settings sections/ });
      if (await menu.isVisible()) await menu.click();
    },
  },
  {
    name: 'dark mode chosen with the sun and moon button', path: '/plan',
    act: async (page) => {
      const before = await page.evaluate(() => matchMedia('(prefers-color-scheme: dark)').matches);
      // In the dark project this picks light, so both themes are also checked when chosen rather than followed.
      await page.getByRole('button', { name: before ? 'Switch to light mode' : 'Switch to dark mode' }).click();
    },
  },
  { name: 'sign in, with the theme button', path: '/', answers: signedOut,
    act: (page) => page.getByRole('button', { name: /^Switch to (light|dark) mode$/ }).click() },
  { name: 'settings: https', path: '/settings/https' },
  {
    name: 'settings: recipe health, with a check open', path: '/settings/recipe-health', answers: AI,
    act: async (page) => {
      await page.getByText('No photo', { exact: true }).click();
      await page.getByRole('button', { name: 'Show all 25' }).waitFor();
    },
  },
  { name: 'settings: recipe health, every recipe complete', path: '/settings/recipe-health', answers: { 'GET /api/recipes/health': data.recipeHealth(true) } },
  {
    name: 'settings: recipe health for one cookbook', path: '/settings/recipe-health', answers: withCookbooks,
    act: async (page) => {
      await page.getByRole('combobox', { name: 'Look at' }).selectOption({ label: 'The Weeknight cookbook' });
      await page.getByText('1 of 2 recipes').waitFor();
    },
  },
  { name: 'settings: recipe health, no recipes', path: '/settings/recipe-health',
    answers: { 'GET /api/recipes/health': { recipes: 0, complete: 0, checks: [], duplicates: [], rareTags: [] } } },
  { name: 'settings: plan styles', path: '/settings/styles' },
  {
    name: 'settings: a plan style open for editing, with every kind of rule', path: '/settings/styles',
    act: async (page) => {
      await page.getByRole('button', { name: 'Edit Classic week' }).click();
      for (const match of ['Tag', 'Ingredient', 'ReadyWithin', 'Favourite']) {
        await page.getByRole('button', { name: 'Add a rule' }).click();
        await page.getByRole('combobox', { name: 'what it looks for' }).last().selectOption(match);
      }
    },
  },
  {
    name: 'settings: a plan style with a cookbook rule', path: '/settings/styles',
    answers: { 'GET /api/plan-styles': stylesWithCookbooks, 'PUT /api/plan-styles': stylesWithCookbooks },
    act: async (page) => {
      await page.getByRole('button', { name: 'Edit Classic week' }).click();
      await page.getByRole('button', { name: 'Add a rule' }).click();
      await page.getByRole('combobox', { name: 'what it looks for' }).last().selectOption('Cookbook');
      await page.getByRole('combobox', { name: 'cookbook' }).last().selectOption({ label: "Grandma's" });
    },
  },
  {
    name: 'settings: a plan style leaning on favourites a lot', path: '/settings/styles',
    act: async (page) => {
      await page.getByRole('button', { name: 'Edit Classic week' }).click();
      await page.getByRole('radio', { name: 'A lot' }).check();
    },
  },
  { name: 'settings: stores', path: '/settings/stores' },
  {
    name: 'settings: a store open for editing', path: '/settings/stores',
    act: (page) => page.getByRole('button', { name: 'Edit' }).nth(1).click(),
  },
  {
    name: 'settings: stores, a change saved with Undo offered', path: '/settings/stores',
    act: (page) => page.getByRole('button', { name: /^Remove/ }).first().click()
      .then(() => page.getByRole('button', { name: 'Undo changes' }).waitFor())
      .then(() => page.getByRole('status').filter({ hasText: 'Saved' }).waitFor()),
  },
  { name: 'settings: notifications', path: '/settings/notifications' },
  {
    name: 'settings: notifications, saved by itself', path: '/settings/notifications',
    act: (page) => page.getByRole('checkbox', { name: /^Today's meals/ }).click()
      .then(() => page.getByRole('status').filter({ hasText: 'Saved' }).waitFor()),
  },
  {
    name: 'settings: notifications, a field that stops it saving', path: '/settings/notifications',
    act: async (page) => {
      await page.getByLabel('Send daily reminders at').fill('');
      await page.getByLabel('Send daily reminders at').blur();
      await page.getByText('Not saved yet').waitFor();
    },
  },
  {
    name: 'settings: notifications, a save that failed', path: '/settings/notifications',
    answers: { 'PUT /api/system/notifications': reply(500, { detail: 'The settings could not be saved. Try again in a minute.' }) },
    act: (page) => page.getByRole('checkbox', { name: /^Today's meals/ }).click()
      .then(() => page.getByRole('button', { name: 'Try again' }).waitFor()),
  },
  {
    name: 'settings: backups with one opened', path: '/settings/backups',
    act: (page) => page.locator('#settings-body').getByRole('button', { expanded: false }).first().click()
      .then(() => page.getByText('Recipe photos').waitFor()),
  },
  { name: 'settings: to-do apps', path: '/settings/todo-apps', answers: { 'GET /api/shopping/targets': data.shoppingTargets() } },
  { name: 'settings: no to-do apps yet', path: '/settings/todo-apps' },
  { name: 'settings: calendar feed off', path: '/settings/calendar' },
  {
    name: 'settings: calendar meal times, saved by itself', path: '/settings/calendar',
    act: async (page) => {
      await page.getByLabel('Each meal lasts (minutes)').fill('60');
      await page.getByLabel('Each meal lasts (minutes)').blur();
      await page.getByRole('status').filter({ hasText: 'Saved' }).waitFor();
    },
  },
  { name: 'settings: planning', path: '/settings/planning' },
  {
    name: 'settings: planning, saved by itself', path: '/settings/planning',
    act: async (page) => {
      await page.getByLabel('People you usually cook for').fill('1');
      await page.getByLabel('People you usually cook for').blur();
      await page.getByRole('status').filter({ hasText: 'Saved' }).waitFor();
    },
  },
  {
    name: 'settings: planning with the weekly plan off', path: '/settings/planning',
    answers: { 'GET /api/planning': { ...data.planning(), scheduleEnabled: false, nextStaging: null } },
  },
  {
    name: 'settings: planning, a number that stops it saving', path: '/settings/planning',
    act: async (page) => {
      await page.getByLabel('People you usually cook for').fill('0');
      await page.getByLabel('People you usually cook for').blur();
      await page.getByText('Not saved yet').waitFor();
    },
  },
  { name: 'settings: advanced', path: '/settings/advanced' },
  {
    name: 'settings: advanced, a course word added', path: '/settings/advanced',
    act: async (page) => {
      await page.getByLabel('New word for Sweet words').fill('Pudding');
      await page.getByLabel('New word for Sweet words').press('Enter');
      await page.getByRole('button', { name: 'Remove pudding from Sweet words' }).waitFor();
      await page.getByRole('button', { name: 'Reset to defaults' }).waitFor();
      await page.getByRole('status').filter({ hasText: 'Saved' }).waitFor();
    },
  },
  { name: 'settings: calendar feed on', path: '/settings/calendar', answers: { 'GET /api/calendar/feed': data.calendarFeed() } },
  {
    name: 'settings: asking before giving the calendar a new address', path: '/settings/calendar',
    answers: { 'GET /api/calendar/feed': data.calendarFeed() },
    act: (page) => page.getByRole('button', { name: 'Make a new address' }).click().then(() => page.getByRole('dialog').waitFor()),
  },
  { name: 'settings: AI assistants with no tokens', path: '/settings/assistants' },
  {
    name: 'settings: AI assistants with models set up', path: '/settings/assistants',
    answers: { ...AI, 'GET /api/ai/settings': data.aiSettings(), 'GET /api/ai/sweeps/recipe-details': data.detailsSweep() },
    act: (page) => page.getByRole('button', { name: 'Ask for suggestions' }).waitFor(),
  },
  {
    name: 'settings: editing a saved AI model', path: '/settings/assistants',
    answers: { ...AI, 'GET /api/ai/settings': data.aiSettings(), 'GET /api/ai/sweeps/recipe-details': data.detailsSweep() },
    act: async (page) => {
      await page.locator('summary').filter({ hasText: 'claude-opus-5-5' }).click();
      await page.getByLabel('API key', { exact: true }).waitFor();
    },
  },
  {
    name: 'settings: adding an Ollama model', path: '/settings/assistants',
    act: async (page) => {
      await page.getByLabel('Start from').selectOption('ollama');
      await page.getByRole('button', { name: 'Add a model' }).click();
      await page.getByText('not saved yet').waitFor();
    },
  },
  { name: 'settings: AI assistants with tokens', path: '/settings/assistants', answers: { 'GET /api/tokens': data.apiTokens() } },
  {
    name: 'settings: AI assistants with a new token shown', path: '/settings/assistants',
    act: async (page) => {
      await page.getByLabel(/Name for a new token/).fill('Kitchen tablet');
      await page.getByRole('button', { name: 'Make a token' }).click();
      await page.getByLabel('Token', { exact: true }).waitFor();
    },
  },
  {
    name: 'settings: asking before revoking a token', path: '/settings/assistants', answers: { 'GET /api/tokens': data.apiTokens() },
    act: (page) => page.getByRole('button', { name: 'Revoke Claude Desktop' }).click().then(() => page.getByRole('dialog').waitFor()),
  },
  {
    name: 'settings: adding a Todoist to-do app', path: '/settings/todo-apps',
    act: async (page) => {
      await page.getByRole('button', { name: 'Add a to-do app' }).click();
      await page.getByLabel('Kind').selectOption('Todoist');
      await page.getByText('not saved yet').waitFor();
    },
  },
  {
    name: 'settings: logs with a long entry opened', path: '/settings/logs',
    act: (page) => page.getByRole('button', { name: 'Show all' }).click(),
  },

  // Someone who isn't an admin sees fewer tabs and sections.
  { name: 'plan for a member', path: '/plan', answers: { 'GET /api/session': data.session(data.member()) } },
  // Achievements, from the award in the header: halfway, a brand-new kitchen, every first step done, the optional
  // invite step waved off, and a member (who can't invite, so never sees that step).
  { name: 'achievements', path: '/achievements' },
  { name: 'achievements, a brand-new kitchen', path: '/achievements', answers: { 'GET /api/achievements': data.freshAchievements() } },
  { name: 'achievements, first steps all done', path: '/achievements', answers: { 'GET /api/achievements': data.graduatedAchievements() } },
  {
    name: 'achievements, invite step waved off', path: '/achievements',
    act: async (page) => { await page.getByRole('button', { name: 'Just me, thanks' }).click(); },
  },
  { name: 'achievements for a member', path: '/achievements', answers: { 'GET /api/session': data.session(data.member()) } },
  { name: 'plan for a member, no recipes', path: '/plan', answers: { ...noPlan, ...library([]), 'GET /api/session': data.session(data.member()) } },
  { name: 'settings for a member', path: '/settings/https', answers: { 'GET /api/session': data.session(data.member()) } },
  { name: 'AI assistants for a member', path: '/settings/assistants', answers: { 'GET /api/session': data.session(data.member()) } },
  { name: 'recipe health for a member', path: '/settings/recipe-health', answers: { 'GET /api/session': data.session(data.member()) } },
  { name: 'calendar for a member', path: '/settings/calendar',
    answers: { 'GET /api/session': data.session(data.member()), 'GET /api/calendar/feed': data.calendarFeed() } },
  // The food history is for everyone signed in, not only admins: a member stays on /history and sees it.
  { name: 'history for a member', path: '/history', answers: { 'GET /api/session': data.session(data.member()) },
    act: async (page) => {
      await page.getByRole('heading', { name: 'The Noodle of Time' }).waitFor();
      if (new URL(page.url()).pathname !== '/history') throw new Error(`A member was sent to ${page.url()} instead of the history.`);
    } },

  // Things laid over the page
  {
    name: 'the confirm dialog', path: '/plan',
    act: (page) => page.getByRole('button', { name: 'Clear plan' }).click()
      .then(() => page.getByRole('dialog').waitFor()),
  },
  {
    name: 'a note and an error message', path: '/plan',
    answers: {
      ...staged,
      'POST /api/plans/*/confirm': { ...data.plan(), notes: ['Coconut milk was left off the list: it is already in the pantry.'] },
      'POST /api/plans/stage': reply(500, { detail: 'Next week could not be staged. Try again in a minute.' }),
    },
    act: async (page) => {
      // A note from confirming the plan, then an error from planning the week after.
      await page.getByRole('button', { name: 'Confirm plan' }).click();
      await page.getByText('Coconut milk was left off').waitFor();
      await page.getByRole('button', { name: /^Plan Nov 29/ }).click();
      await page.getByText('Next week could not be staged').waitFor();
    },
  },
];

for (const view of VIEWS) {
  test(view.name, async ({ page, api }) => {
    if (view.answers) api.set(view.answers);
    await open(page, view.path);
    await view.act?.(page, api);
    await expectAccessible(page);
  });
}

test('setup wizard, every step', async ({ page, api }) => {
  api.set({
    'GET /api/session': { setupRequired: true, user: null },
    'POST /api/setup/account': data.admin(),
    ...library([]),
    'GET /api/plan-styles': { ...data.planStyles(), styles: [] },
  });
  await open(page, '/');
  await page.getByLabel('Your name').fill('Alex Rivera');
  await page.getByLabel('Username').fill('alex');
  await page.getByLabel(/^Password/).fill('correct horse');
  await page.getByRole('button', { name: 'Create account' }).click();

  // A welcome before moving on, rather than a jump straight to the next step.
  await page.getByRole('heading', { name: 'Welcome, Alex Rivera.' }).waitFor();
  await expectAccessible(page);
  await page.getByRole('button', { name: 'Continue' }).click();

  await page.getByRole('heading', { name: 'Secure connection' }).waitFor();
  await expect(page.getByRole('heading', { name: 'Secure connection' })).toBeFocused();
  await expectAccessible(page);

  await page.getByRole('button', { name: 'Next' }).click();
  await page.getByRole('heading', { name: 'Add your recipes' }).waitFor();
  await expectAccessible(page);
  await page.getByText('Import my recipes').click();
  await expectAccessible(page);

  // While a file is being imported, setup waits for it and says so.
  const skip = page.getByRole('button', { name: 'Skip for now' });
  let finishImport;
  api.set({ 'POST /api/recipes/import/file': () => new Promise((resolve) => {
    finishImport = () => resolve({ added: 1, alreadyInLibrary: 0, skipped: 0, photosAdded: 0 });
  }) });
  await page.locator('input[type="file"]').setInputFiles({ name: 'recipes.json', mimeType: 'application/json', buffer: Buffer.from('[]') });
  await page.getByText('Importing your recipes…').waitFor();
  await expect(skip).toBeDisabled();
  await expect(page.getByRole('button', { name: 'Back' })).toBeDisabled();
  await expectAccessible(page);
  finishImport();
  await page.getByText('1 added').waitFor();
  await expect(skip).toBeEnabled();

  await page.getByText('Start with sample recipes').click();
  await expectAccessible(page);

  // The same while sample recipes are being added.
  let finishSamples;
  api.set({ 'POST /api/setup/samples': () => new Promise((resolve) => { finishSamples = () => resolve({ added: 2 }); }) });
  await page.getByRole('button', { name: /^Add \d+ sample/ }).click();
  await page.getByText('Adding sample recipes…').waitFor();
  await expect(skip).toBeDisabled();
  await expect(page.getByRole('button', { name: /^Adding \d+ recipes…/ })).toBeVisible();
  finishSamples();
  await expect(skip).toBeEnabled();

  await page.getByRole('button', { name: 'Skip for now' }).click();
  await page.getByRole('heading', { name: 'How do you like to plan a week?' }).waitFor();
  await expectAccessible(page);
  await page.getByLabel('How many people do you usually cook for?').fill('1');
  await page.getByText('planned for 1 person').waitFor();
  await page.getByRole('radio', { name: 'Slow cooker week' }).check();
  await page.getByText('Change these styles, or make your own').click();
  await page.getByRole('button', { name: 'Edit Slow cooker week' }).click();
  await expectAccessible(page);

  await page.getByRole('button', { name: 'Next' }).click();
  await page.getByRole('heading', { name: /Where do you shop/ }).waitFor();
  await expectAccessible(page);
  const saved = api.requests.filter((request) => request === 'PUT /api/plan-styles');
  if (saved.length !== 1) throw new Error(`Expected the plan styles to be saved once, not ${saved.length} times.`);
  const people = api.requests.filter((request) => request === 'PUT /api/planning');
  if (people.length !== 1) throw new Error(`Expected the number of people to be saved once, not ${people.length} times.`);
});
