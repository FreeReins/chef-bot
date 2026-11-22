// Offline mode: the shopping list and pantry saved in the browser when ChefBot can't be reached, and the changes made
// meanwhile sent once it answers again (src/lib/offline.svelte.js, src/lib/outbox.svelte.js). `api.offline` makes
// every API request fail as with no signal. The service worker isn't part of these tests: Vite's dev server serves
// the page, and the page registers it only in a build.
import { test, expect, open, reply } from './fixtures.js';
import * as data from './data.js';

const [milk, soap] = data.manualItems();
const fish = data.ingredients().ingredients[0];
const limes = data.ingredients().ingredients[1];
const today = new Date().toISOString().slice(0, 10); // the tests run in UTC (playwright.config.js)

const banner = (page) => page.getByRole('status').filter({ hasText: 'Offline.' });
const changes = (api, from) => api.requests.slice(from).filter((request) => !request.startsWith('GET '));

/** Opens the Shopping tab while connected, so the browser saves it, then reloads it with no connection. */
async function openOffline(page, api, path = '/shopping') {
  await open(page, path);
  await page.waitForFunction(() => localStorage.getItem('chefbot-offline'));
  api.offline = true;
  await page.reload();
  await expect(banner(page)).toBeVisible();
}

test('the shopping list opens offline and its changes are sent once ChefBot answers', async ({ page, api }) => {
  api.set({ 'GET /api/shopping/items': data.manualItems() });
  await openOffline(page, api);
  await expect(page.getByLabel('Bought: fish fillets')).toBeVisible();

  await page.getByLabel('Bought: fish fillets').click();
  await page.getByLabel('Add to the list').fill('bread');
  await page.getByRole('button', { name: 'Add', exact: true }).click();
  await expect(page.locator('li', { hasText: 'bread' }).getByText('Not sent yet')).toBeVisible();
  await page.getByRole('button', { name: 'Remove dish soap from the list' }).click();
  await page.getByLabel('Bought: milk').click();
  await expect(page.getByLabel('Bought: fish fillets')).toHaveCount(0);
  await expect(banner(page)).toContainText('4 changes will be sent');

  // Still there after a reload with no connection.
  await page.reload();
  await expect(banner(page)).toContainText('4 changes will be sent');
  await expect(page.getByText('bread')).toBeVisible();

  const bodies = [];
  const record = (answer) => (request) => { bodies.push(request.postDataJSON?.() ?? null); return answer; };
  const bread = { id: '00000000-0000-4000-8000-000000000499', name: 'bread', quantity: null, unit: '', store: '', group: 'WholeGrain' };
  api.set({
    'POST /api/plans/*/shopping/*/purchase': record(data.plan()),
    'POST /api/shopping/items': record([milk, soap, bread]),
    'DELETE /api/shopping/items/*': record([milk, bread]),
    'POST /api/shopping/items/*/purchase': record([bread]),
  });
  api.offline = false;
  const from = api.requests.length;
  await page.getByRole('button', { name: 'Try again' }).click();

  await expect(page.getByText('Your 4 shopping changes reached ChefBot.')).toBeVisible();
  await expect(banner(page)).toHaveCount(0);
  expect(changes(api, from)).toEqual([
    `POST /api/plans/${data.ids.plan}/shopping/${fish.shoppingItemId}/purchase`,
    'POST /api/shopping/items',
    `DELETE /api/shopping/items/${soap.id}`,
    `POST /api/shopping/items/${milk.id}/purchase`,
  ]);
  // Bought on the day they were ticked, not the day they reached ChefBot.
  expect(bodies).toEqual([{ purchasedOn: today }, { name: 'bread', quantity: null, unit: null }, null, { purchasedOn: today }]);
});

test('an item added offline and bought before sending is bought by its new id', async ({ page, api }) => {
  await openOffline(page, api);
  await page.getByLabel('Add to the list').fill('bread');
  await page.getByRole('button', { name: 'Add', exact: true }).click();
  await page.getByLabel('Bought: bread').click();

  const bread = { id: '00000000-0000-4000-8000-000000000499', name: 'bread', quantity: null, unit: '', store: '', group: 'WholeGrain' };
  api.set({ 'POST /api/shopping/items': [bread], 'POST /api/shopping/items/*/purchase': [] });
  api.offline = false;
  const from = api.requests.length;
  await page.getByRole('button', { name: 'Try again' }).click();

  await expect(page.getByText('Your 2 shopping changes reached ChefBot.')).toBeVisible();
  expect(changes(api, from)).toEqual(['POST /api/shopping/items', `POST /api/shopping/items/${bread.id}/purchase`]);
});

test('a change that no longer applies is skipped and named, and the rest still go', async ({ page, api }) => {
  await openOffline(page, api);
  await page.getByLabel('Bought: fish fillets').click();
  await page.getByLabel('Bought: limes').click();

  api.set({
    [`POST /api/plans/*/shopping/${fish.shoppingItemId}/purchase`]: reply(404, { detail: 'That item is not on the list.' }),
    [`POST /api/plans/*/shopping/${limes.shoppingItemId}/purchase`]: data.plan(),
  });
  api.offline = false;
  const from = api.requests.length;
  await page.getByRole('button', { name: 'Try again' }).click();

  await expect(page.getByText(/1 shopping change reached ChefBot\. 1 change couldn't be applied .*: fish fillets\./)).toBeVisible();
  expect(changes(api, from)).toHaveLength(2);
});

test('losing the connection with the page open keeps the change for later', async ({ page, api }) => {
  await open(page, '/shopping');
  api.offline = true;
  await page.getByLabel('Bought: fish fillets').click();

  await expect(page.getByText(/ChefBot can't be reached\. The shopping list still works/)).toBeVisible();
  await expect(banner(page)).toContainText('1 change will be sent');
  await expect(page.getByLabel('Bought: fish fillets')).toHaveCount(0);
});

test('an ended session asks to sign in, then sends the changes', async ({ page, api }) => {
  await openOffline(page, api);
  await page.getByLabel('Add to the list').fill('bread');
  await page.getByRole('button', { name: 'Add', exact: true }).click();

  api.set({ 'GET /api/session': { setupRequired: false, user: null } });
  api.offline = false;
  await page.getByRole('button', { name: 'Try again' }).click();
  await expect(page.getByRole('heading', { name: 'Sign in' })).toBeVisible();
  await expect(page.getByText('Sign in to send your 1 shopping change to ChefBot.')).toBeVisible();

  api.set({ 'GET /api/session': data.session(), 'POST /api/account/sign-in': data.admin(), 'POST /api/shopping/items': [] });
  await page.getByLabel('Username').fill('alex');
  await page.getByLabel('Password').fill('correct horse');
  await page.getByRole('button', { name: 'Sign in' }).click();
  await expect(page.getByText('Your 1 shopping change reached ChefBot.')).toBeVisible();
  expect(api.requests).toContain('POST /api/shopping/items');
});

test('the pantry is shown offline but not changed', async ({ page, api }) => {
  await openOffline(page, api, '/pantry');
  await expect(page.getByText('Offline: the pantry can be changed once ChefBot can be reached.')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Change the amount or date of eggs' })).toBeDisabled();
  await expect(page.getByLabel('Add an item')).toHaveCount(0);
});

test('with nothing saved, opening offline says how to get a list', async ({ page, api }) => {
  api.offline = true;
  await open(page, '/shopping');
  await expect(page.getByText(/hasn't saved a shopping list yet/)).toBeVisible();
});

test('signing out forgets the saved list', async ({ page, api }) => {
  api.set({ 'POST /api/account/sign-out': null });
  await open(page, '/shopping');
  await page.waitForFunction(() => localStorage.getItem('chefbot-offline'));
  await page.getByRole('button', { name: 'Sign out' }).click();
  await expect(page.getByRole('heading', { name: 'Sign in' })).toBeVisible();
  expect(await page.evaluate(() => localStorage.getItem('chefbot-offline'))).toBeNull();
});
