// Every screen of the page, checked with axe in each project of playwright.config.js (light, dark, phone).
// To cover a new screen or state, add a row to VIEWS: where to go, any answers to change, and what to click first.
import { test, open, reply, library } from './fixtures.js';
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
    name: 'plan, draft to review with its shopping list open', path: '/plan', answers: staged,
    act: (page) => page.getByText(/items? to buy/).click(),
  },
  {
    name: 'plan, draft with a meal kept and a swap open', path: '/plan', answers: staged,
    act: async (page) => {
      await page.getByRole('button', { name: 'Keep Beef and Barley Stew' }).click();
      await page.getByRole('button', { name: 'Swap Chickpea Curry' }).click();
      await page.getByText('Recipes that use what you have').waitFor();
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
  { name: 'pantry with a plan', path: '/pantry' },
  { name: 'pantry without a plan', path: '/pantry', answers: noPlan },
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
  {
    name: 'recipes, the download all choices open', path: '/recipes',
    act: (page) => page.getByRole('button', { name: 'Download all' }).click()
      .then(() => page.getByRole('link', { name: 'Download as PDF' }).waitFor()),
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
  { name: 'add a recipe from a link', path: '/recipes/new/link' },
  { name: 'add a recipe by typing it in', path: '/recipes/new/write' },
  { name: 'add recipes from files', path: '/recipes/new/files' },
  { name: 'a recipe', path: `/recipes/${data.ids.tacos}` },
  {
    name: 'a recipe with its menu open', path: `/recipes/${data.ids.tacos}`,
    act: (page) => page.getByRole('button', { name: /^More actions/ }).click()
      .then(() => page.getByRole('button', { name: 'Archive recipe' }).waitFor()),
  },
  { name: 'a recipe, printed', path: `/recipes/${data.ids.tacos}`, act: (page) => page.emulateMedia({ media: 'print' }) },
  { name: 'a recipe without photos', path: `/recipes/${data.ids.stew}` },
  { name: 'editing a recipe', path: `/recipes/${data.ids.tacos}/edit` },
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
  { name: 'cooking mode, a recipe with no steps', path: `/recipes/${data.ids.stew}/cook` },
  { name: 'a recipe that is not in the library', path: '/recipes/00000000-0000-4000-8000-00000000ffff' },
  { name: 'sample recipes', path: '/recipes/samples' },

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
  { name: 'settings: plan styles', path: '/settings/styles' },
  {
    name: 'settings: a plan style open for editing, with every kind of rule', path: '/settings/styles',
    act: async (page) => {
      await page.getByRole('button', { name: 'Edit Classic week' }).click();
      for (const match of ['Tag', 'Ingredient', 'ReadyWithin']) {
        await page.getByRole('button', { name: 'Add a rule' }).click();
        await page.getByRole('combobox', { name: 'what it looks for' }).last().selectOption(match);
      }
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
    act: (page) => page.getByRole('button', { expanded: false }).first().click()
      .then(() => page.getByText('Recipe photos').waitFor()),
  },
  { name: 'settings: to-do apps', path: '/settings/todo-apps', answers: { 'GET /api/shopping/targets': data.shoppingTargets() } },
  { name: 'settings: no to-do apps yet', path: '/settings/todo-apps' },
  { name: 'settings: calendar feed off', path: '/settings/calendar' },
  { name: 'settings: calendar feed on', path: '/settings/calendar', answers: { 'GET /api/calendar/feed': data.calendarFeed() } },
  {
    name: 'settings: asking before giving the calendar a new address', path: '/settings/calendar',
    answers: { 'GET /api/calendar/feed': data.calendarFeed() },
    act: (page) => page.getByRole('button', { name: 'Make a new address' }).click().then(() => page.getByRole('dialog').waitFor()),
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
  { name: 'plan for a member, no recipes', path: '/plan', answers: { ...noPlan, ...library([]), 'GET /api/session': data.session(data.member()) } },
  { name: 'settings for a member', path: '/settings/https', answers: { 'GET /api/session': data.session(data.member()) } },
  { name: 'calendar for a member', path: '/settings/calendar',
    answers: { 'GET /api/session': data.session(data.member()), 'GET /api/calendar/feed': data.calendarFeed() } },

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

  await page.getByRole('heading', { name: 'Secure connection' }).waitFor();
  await expectAccessible(page);

  await page.getByRole('button', { name: 'Next' }).click();
  await page.getByRole('heading', { name: 'Add your recipes' }).waitFor();
  await expectAccessible(page);
  await page.getByText('Import my recipes').click();
  await expectAccessible(page);
  await page.getByText('Start with sample recipes').click();
  await expectAccessible(page);

  await page.getByRole('button', { name: 'Skip for now' }).click();
  await page.getByRole('heading', { name: 'How do you like to plan a week?' }).waitFor();
  await expectAccessible(page);
  await page.getByRole('radio', { name: 'Slow cooker week' }).check();
  await page.getByText('Change these styles, or make your own').click();
  await page.getByRole('button', { name: 'Edit Slow cooker week' }).click();
  await expectAccessible(page);

  await page.getByRole('button', { name: 'Next' }).click();
  await page.getByRole('heading', { name: /Where do you shop/ }).waitFor();
  await expectAccessible(page);
  const saved = api.requests.filter((request) => request === 'PUT /api/plan-styles');
  if (saved.length !== 1) throw new Error(`Expected the plan styles to be saved once, not ${saved.length} times.`);
});
