// Taking recipes and shopping lists out of ChefBot: sharing as text, copying where there is no share sheet, and the
// CSV download. The share sheet and the clipboard are replaced with recorders, so each test sees what was handed over.
import { readFile } from 'node:fs/promises';
import { test, expect, open } from './fixtures.js';
import * as data from './data.js';

/** Records what the page shares or copies in window.handedOver; `sheet` says whether the browser has a share sheet. */
async function recordHandOver(page, { sheet }) {
  await page.addInitScript((withSheet) => {
    window.handedOver = [];
    if (withSheet) {
      navigator.share = async (payload) => { window.handedOver.push({ via: 'share', ...payload }); };
    } else {
      delete Navigator.prototype.share;
      delete navigator.share;
    }
    Object.defineProperty(navigator, 'clipboard', {
      value: { writeText: async (text) => { window.handedOver.push({ via: 'copy', text }); } },
    });
  }, sheet);
}

const handedOver = (page) => page.evaluate(() => window.handedOver);

test('a recipe is shared as text through the share sheet', async ({ page }) => {
  await recordHandOver(page, { sheet: true });
  await open(page, `/recipes/${data.ids.tacos}`);
  await page.getByRole('button', { name: /^More actions/ }).click();
  await page.getByRole('button', { name: 'Share recipe' }).click();

  await expect.poll(() => handedOver(page)).toHaveLength(1);
  const [shared] = await handedOver(page);
  expect(shared.via).toBe('share');
  expect(shared.title).toBe('Slaw Fish Tacos');
  expect(shared.text).toContain('Slaw Fish Tacos\nServes 4 · Prep 20 min · Cook 10 min · Total 30 min');
  expect(shared.text).toContain('Ingredients\n- 600 g fish fillets\n- 1/2 head cabbage');
  expect(shared.text).toContain('Steps\n1. Preheat the oven to 200°C.');
  expect(shared.text).toContain('Source: https://example.com/slaw-fish-tacos');
});

test('without a share sheet the recipe is copied, and the page says so', async ({ page }) => {
  await recordHandOver(page, { sheet: false });
  await open(page, `/recipes/${data.ids.tacos}`);
  await page.getByRole('button', { name: /^More actions/ }).click();
  await page.getByRole('button', { name: 'Copy as text' }).click();

  await expect(page.getByText('Slaw Fish Tacos was copied as text.')).toBeVisible();
  const [copied] = await handedOver(page);
  expect(copied.via).toBe('copy');
  expect(copied.text).toContain('Notes\nAny firm white fish works.');
});

test('the shopping list is copied as a checklist grouped by store', async ({ page }) => {
  await recordHandOver(page, { sheet: false });
  await open(page, '/shopping');
  await page.getByRole('button', { name: 'Copy list' }).click();

  await expect(page.getByText('The shopping list was copied.')).toBeVisible();
  const [{ text }] = await handedOver(page);
  expect(text).toMatch(/^Shopping list, week of .+ \(3 items\)\n/);
  expect(text).toContain('Corner Market\n☐ fish fillets, 600 g');
  expect(text).toContain('Saturday Farmers Market\n☐ limes, 3');
  expect(text).toContain('☐ coconut milk, 1 can (optional)');
  expect(text).not.toContain('tortillas'); // already bought
});

test('the shopping list downloads as CSV', async ({ page }) => {
  await open(page, '/shopping');
  const [file] = await Promise.all([
    page.waitForEvent('download'),
    page.getByRole('button', { name: 'Download CSV' }).click(),
  ]);

  expect(file.suggestedFilename()).toMatch(/^shopping-list-\d{4}-\d\d-\d\d\.csv$/);
  const csv = await readFile(await file.path(), 'utf8');
  const rows = csv.replace(/^\uFEFF/, '').split('\r\n');
  expect(rows[0]).toBe('Name,Quantity,Unit,Store,Food group,Optional,For');
  expect(rows).toContain('fish fillets,600,g,Corner Market,Protein,,Slaw Fish Tacos');
  expect(rows).toContain('coconut milk,1,can,,Other,yes,Chickpea Curry');
});

test('a recipe and the whole library download as schema.org files', async ({ page }) => {
  await open(page, `/recipes/${data.ids.tacos}`);
  await page.getByRole('button', { name: /^More actions/ }).click();
  const one = page.getByRole('link', { name: 'Download recipe file' });
  await expect(one).toHaveAttribute('href', `/api/recipes/${data.ids.tacos}/export`);
  await expect(one).toHaveAttribute('download', '');

  await open(page, '/recipes');
  const all = page.getByRole('link', { name: 'Download all' });
  await expect(all).toHaveAttribute('href', '/api/recipes/export');
  await expect(all).toHaveAttribute('download', '');
});

test('the shopping list is sent to a to-do app, and the page says how many items went', async ({ page, api }) => {
  const [kitchen] = data.shoppingTargets();
  let sent = null;
  api.set({
    'GET /api/shopping/targets': data.shoppingTargets(),
    'POST /api/shopping/send': (request) => {
      sent = request.postDataJSON();
      return { sent: 3, total: 3, problem: null };
    },
  });
  await open(page, '/shopping');
  await page.getByRole('button', { name: 'Send to Kitchen list' }).click();

  await expect(page.getByText('3 items went to Kitchen list.')).toBeVisible();
  expect(sent).toEqual({ targetId: kitchen.id, planId: data.plan().id });
});

test('a to-do app that refuses part of the list shows how far it got', async ({ page, api }) => {
  api.set({
    'GET /api/shopping/targets': data.shoppingTargets(),
    'POST /api/shopping/send': { sent: 1, total: 3, problem: "Kitchen list didn't accept the access token (HTTP 401). Check the token in Settings." },
  });
  await open(page, '/shopping');
  await page.getByRole('button', { name: 'Send to Kitchen list' }).click();

  await expect(page.getByText(/1 of 3 items went to Kitchen list\. Kitchen list didn't accept/)).toBeVisible();
});

test('a new to-do app is saved without echoing a token, and a saved token is kept by sending none', async ({ page, api }) => {
  let saved = null;
  api.set({
    'GET /api/shopping/targets': data.shoppingTargets(),
    'PUT /api/shopping/targets': (request) => {
      saved = request.postDataJSON();
      return data.shoppingTargets();
    },
  });
  await open(page, '/settings/todo-apps');
  await page.getByRole('button', { name: 'Add a to-do app' }).click();
  const fresh = page.getByRole('group', { name: 'New to-do app' });
  await fresh.getByLabel('Kind').selectOption('Todoist');
  await fresh.getByLabel('Access token').fill('abc123');
  await fresh.getByLabel('Name').fill('Todoist'); // last: the group is named after it
  await page.getByRole('button', { name: 'Save' }).click();

  await expect(page.getByText('To-do apps saved.')).toBeVisible();
  expect(saved.targets.map((target) => [target.name, target.kind, target.token])).toEqual([
    ['Kitchen list', 'HomeAssistant', null],
    ['Automation', 'Webhook', null],
    ['Todoist', 'Todoist', 'abc123'],
  ]);
});
