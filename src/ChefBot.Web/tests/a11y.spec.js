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
  { name: 'shopping, staged plan with hand-added items', path: '/shopping',
    answers: { ...staged, 'GET /api/shopping/items': data.manualItems() } },
  { name: 'shopping, with two weeks planned', path: '/shopping', answers: weeks(data.plan(), data.nextPlan()) },
  { name: 'shopping without a plan', path: '/shopping', answers: { ...noPlan, 'GET /api/shopping/items': data.manualItems() } },
  { name: 'shopping without a plan, empty list', path: '/shopping', answers: { ...noPlan, 'GET /api/pantry': [] } },
  { name: 'pantry with a plan', path: '/pantry' },
  { name: 'pantry without a plan', path: '/pantry', answers: noPlan },

  // Recipes
  { name: 'recipes', path: '/recipes' },
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
  { name: 'a recipe', path: `/recipes/${data.ids.tacos}` },
  {
    name: 'editing a recipe', path: `/recipes/${data.ids.tacos}`,
    act: (page) => page.getByRole('button', { name: 'Edit', exact: true }).click(),
  },
  { name: 'a recipe that is not in the library', path: '/recipes/00000000-0000-4000-8000-00000000ffff' },
  { name: 'sample recipes', path: '/recipes/samples' },

  // People and settings
  {
    name: 'people, after creating an invite link', path: '/people', answers: { 'POST /api/invites': data.issuedLink() },
    act: (page) => page.getByRole('button', { name: 'Create invite link' }).click()
      .then(() => page.getByRole('button', { name: 'Copy' }).waitFor()),
  },
  { name: 'settings: login', path: '/settings/login' },
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
  { name: 'settings: notifications', path: '/settings/notifications' },
  {
    name: 'settings: backups with one opened', path: '/settings/backups',
    act: (page) => page.getByRole('button', { expanded: false }).first().click()
      .then(() => page.getByText('Recipe photos').waitFor()),
  },
  {
    name: 'settings: logs with a long entry opened', path: '/settings/logs',
    act: (page) => page.getByRole('button', { name: 'Show all' }).click(),
  },

  // Someone who isn't an admin sees fewer tabs and sections.
  { name: 'plan for a member', path: '/plan', answers: { 'GET /api/session': data.session(data.member()) } },
  { name: 'plan for a member, no recipes', path: '/plan', answers: { ...noPlan, ...library([]), 'GET /api/session': data.session(data.member()) } },
  { name: 'settings for a member', path: '/settings/https', answers: { 'GET /api/session': data.session(data.member()) } },

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

  await page.getByRole('button', { name: 'Next: stores' }).click();
  await page.getByRole('heading', { name: /Where do you shop/ }).waitFor();
  await expectAccessible(page);
  const saved = api.requests.filter((request) => request === 'PUT /api/plan-styles');
  if (saved.length !== 1) throw new Error(`Expected the plan styles to be saved once, not ${saved.length} times.`);
});
