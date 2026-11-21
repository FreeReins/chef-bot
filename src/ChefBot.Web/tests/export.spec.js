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
  const rows = csv.replace(/^﻿/, '').split('\r\n');
  expect(rows[0]).toBe('Name,Quantity,Unit,Store,Food group,Optional,For');
  expect(rows).toContain('fish fillets,600,g,Corner Market,Protein,,Slaw Fish Tacos');
  expect(rows).toContain('coconut milk,1,can,,Other,yes,Chickpea Curry');
});
