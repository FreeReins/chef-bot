// Every screen of the page, checked with axe in each project of playwright.config.js (light, dark, phone).
// To cover a new screen or state, add a row to VIEWS: where to go, any answers to change, and what to click first.
import { test, open, reply } from './fixtures.js';
import { expectAccessible } from './axe.js';
import * as data from './data.js';

const signedOut = { 'GET /api/session': { setupRequired: false, user: null, mealieConfigured: false } };
const noPlan = { 'GET /api/plans/current': reply(404, { detail: 'There is no plan yet.' }) };

/** { name, path, answers?: routes for api.set(), act?: async (page, api) => {} run before checking } */
const VIEWS = [
  // Before signing in
  { name: 'sign in', path: '/', answers: signedOut },
  { name: 'join from an invite', path: '/?invite=invite-token', answers: signedOut },
  { name: 'choose a new password', path: '/?reset=reset-token', answers: signedOut },
  { name: 'setup: create the account', path: '/', answers: { 'GET /api/session': { setupRequired: true, user: null, mealieConfigured: false } } },

  // The plan
  { name: 'meals, confirmed plan', path: '/meals' },
  { name: 'meals, staged plan', path: '/meals', answers: { 'GET /api/plans/current': data.plan('Staged') } },
  { name: 'meals, nothing staged', path: '/meals', answers: noPlan },
  {
    name: 'meals, swap open', path: '/meals', answers: { 'GET /api/plans/current': data.plan('Staged') },
    act: (page) => page.getByRole('button', { name: 'Swap Beef and Barley Stew' }).click()
      .then(() => page.getByText('Recipes that use what you have').waitFor()),
  },
  { name: 'schedule, confirmed plan', path: '/schedule' },
  { name: 'schedule, staged plan', path: '/schedule', answers: { 'GET /api/plans/current': data.plan('Staged') } },
  { name: 'shopping', path: '/shopping' },
  { name: 'shopping, staged plan with hand-added items', path: '/shopping',
    answers: { 'GET /api/plans/current': data.plan('Staged'), 'GET /api/shopping/items': data.manualItems() } },
  { name: 'shopping without a plan', path: '/shopping', answers: { ...noPlan, 'GET /api/shopping/items': data.manualItems() } },
  { name: 'shopping without a plan, empty list', path: '/shopping', answers: { ...noPlan, 'GET /api/pantry': [] } },
  { name: 'pantry with a plan', path: '/pantry' },
  { name: 'pantry without a plan', path: '/pantry', answers: noPlan },

  // Recipes
  { name: 'recipes', path: '/recipes' },
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
  { name: 'settings: stores', path: '/settings/stores' },
  {
    name: 'settings: a store open for editing', path: '/settings/stores',
    act: (page) => page.getByRole('button', { name: 'Edit' }).nth(1).click(),
  },
  { name: 'settings: mealie', path: '/settings/mealie' },
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
  { name: 'meals for a member', path: '/meals', answers: { 'GET /api/session': data.session(data.member()) } },
  { name: 'settings for a member', path: '/settings/https', answers: { 'GET /api/session': data.session(data.member()) } },

  // Things laid over the page
  {
    name: 'the confirm dialog', path: '/meals',
    act: (page) => page.getByRole('button', { name: 'Clear plan' }).click()
      .then(() => page.getByRole('dialog').waitFor()),
  },
  {
    name: 'a note and an error message', path: '/meals',
    answers: {
      'GET /api/plans/current': data.plan('Staged'),
      'POST /api/plans/*/confirm': { ...data.plan(), notes: ['Coconut milk was left off the list: it is already in the pantry.'] },
      'POST /api/plans/stage': reply(500, { detail: 'Next week could not be staged. Try again in a minute.' }),
    },
    act: async (page) => {
      // A note from confirming the plan, then an error from staging the next week.
      await page.getByRole('button', { name: 'Confirm plan' }).click();
      await page.getByText('Coconut milk was left off').waitFor();
      await page.getByRole('button', { name: 'Stage next week' }).click();
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
    'GET /api/session': { setupRequired: true, user: null, mealieConfigured: false },
    'POST /api/setup/account': data.admin(),
    'GET /api/recipes': [],
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
  await page.getByRole('heading', { name: /Where do you shop/ }).waitFor();
  await expectAccessible(page);
});
