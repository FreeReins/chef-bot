// Keyboard and focus behaviour that axe can't see: where focus goes, and which keys move it.
import { test, expect, open } from './fixtures.js';
import * as data from './data.js';

const staged = { 'GET /api/plans': [data.summary(data.plan('Staged'))], 'GET /api/plans/*': data.plan('Staged') };
const stew = data.plan().meals.find((meal) => meal.title === 'Beef and Barley Stew');

test('the confirm dialog starts on Cancel, and Escape cancels', async ({ page, api }) => {
  await open(page, '/plan');
  const opener = page.getByRole('button', { name: 'Clear plan' });
  await opener.click();
  const dialog = page.getByRole('dialog', { name: 'Clear this plan?' });
  await expect(dialog).toBeVisible();
  await expect(dialog).toHaveAccessibleDescription(/pantry is kept/);
  await expect(dialog.getByRole('button', { name: 'Cancel' })).toBeFocused();

  await page.keyboard.press('Escape');
  await expect(dialog).toBeHidden();
  await expect(opener).toBeFocused();
  expect(api.requests.filter((request) => request.startsWith('DELETE'))).toEqual([]);
});

test('the sample course tabs move with the arrow keys', async ({ page }) => {
  await open(page, '/recipes/samples');
  const tabs = page.getByRole('tab');
  const names = ['Dinners', 'Breakfasts', 'Snacks', 'Sweets'];
  const tab = (name) => page.getByRole('tab', { name: new RegExp(`^${name}`) });
  const panel = page.getByRole('tabpanel');

  await expect(tabs).toHaveCount(names.length);
  await expect(tab('Dinners')).toHaveAttribute('aria-selected', 'true');
  // Only the selected tab is in the Tab order; the arrow keys reach the rest.
  await expect(page.locator('[role="tab"][tabindex="0"]')).toHaveCount(1);
  await expect(panel).toHaveAccessibleName(/^Dinners/);

  await tab('Dinners').focus();
  await page.keyboard.press('ArrowRight');
  await expect(tab('Breakfasts')).toBeFocused();
  await expect(tab('Breakfasts')).toHaveAttribute('aria-selected', 'true');
  await expect(tab('Breakfasts')).toHaveAttribute('tabindex', '0');
  await expect(tab('Dinners')).toHaveAttribute('tabindex', '-1');
  await expect(panel).toHaveAccessibleName(/^Breakfasts/);
  await expect(panel).toContainText('Overnight Oats');

  await page.keyboard.press('End');
  await expect(tab('Sweets')).toBeFocused();
  await page.keyboard.press('ArrowRight');
  await expect(tab('Dinners')).toBeFocused();
  await page.keyboard.press('ArrowLeft');
  await expect(tab('Sweets')).toBeFocused();
  await page.keyboard.press('Home');
  await expect(tab('Dinners')).toBeFocused();
  await expect(tab('Dinners')).toHaveAttribute('aria-selected', 'true');
});

test('a Settings section link moves focus to the section', async ({ page }) => {
  await open(page, '/settings/login');
  await page.getByRole('navigation', { name: 'Settings' }).getByRole('link', { name: 'Backups' }).click();
  await expect(page.getByRole('heading', { name: 'Backups' })).toBeVisible();
  await expect(page.locator('#settings-body')).toBeFocused();
});

test('every control in the header and the tabs shows where focus is', async ({ page }) => {
  await open(page, '/plan');
  const controls = page.locator('header.top :is(a, button), nav.tabs a');
  const count = await controls.count();
  expect(count).toBeGreaterThan(5);
  // Tab from the top of the page through all of them, checking each one's outline as it takes focus.
  await page.locator('body').focus();
  for (let index = 0; index < count; index++) {
    await page.keyboard.press('Tab');
    const focused = await page.evaluate(() => {
      const element = document.activeElement;
      const style = getComputedStyle(element);
      return { text: element.textContent.trim() || element.getAttribute('aria-label'), outline: style.outlineStyle, width: style.outlineWidth };
    });
    expect(focused.outline, `the focus outline on "${focused.text}"`).not.toBe('none');
    expect(focused.width, `the focus outline on "${focused.text}"`).not.toBe('0px');
  }
});

test('a meal moves to another day from its Move menu, and focus stays on that menu', async ({ page, api }) => {
  api.set(staged);
  await open(page, '/plan');
  const menu = page.getByRole('combobox', { name: 'Move Beef and Barley Stew to another day' });
  await menu.focus();
  await menu.selectOption('2026-11-25');

  await expect(page.locator('section', { has: page.getByRole('heading', { name: /^Wednesday/ }) })).toContainText('Beef and Barley Stew');
  await expect(menu).toBeFocused();
  await expect(menu).toHaveValue('2026-11-25');
  expect(api.requests).toContain(`POST /api/plans/${data.ids.plan}/meals/${stew.id}/move`);
});

test('dragging a dinner onto another day moves it', async ({ page, api, isMobile }) => {
  test.skip(isMobile, 'Touch screens move meals with the Move menu instead.');
  api.set(staged);
  await open(page, '/plan');
  const friday = page.locator('section', { has: page.getByRole('heading', { name: /^Friday/ }) });

  const stewCard = page.locator('article', { hasText: 'Beef and Barley Stew' });
  await expect(stewCard).toHaveAttribute('draggable', 'true');

  // The browser's own drag events, carrying one shared DataTransfer, as a mouse drag sends them.
  const dataTransfer = await page.evaluateHandle(() => new DataTransfer());
  await stewCard.dispatchEvent('dragstart', { dataTransfer });
  await friday.dispatchEvent('dragover', { dataTransfer });
  await expect(friday).toContainText('Drop to move to Friday');
  await friday.dispatchEvent('drop', { dataTransfer });

  await expect(friday).toContainText('Beef and Barley Stew');
  expect(api.requests).toContain(`POST /api/plans/${data.ids.plan}/meals/${stew.id}/move`);
});

test('Keep marks a meal, and Shuffle sends the kept meals', async ({ page, api }) => {
  api.set({ ...staged, 'POST /api/plans/stage': data.plan('Staged') });
  await open(page, '/plan');
  const keep = page.getByRole('button', { name: 'Keep Beef and Barley Stew' });
  await keep.click();
  await expect(keep).toHaveAttribute('aria-pressed', 'true');

  const shuffled = page.waitForRequest((request) => request.url().endsWith('/api/plans/stage'));
  await page.getByRole('button', { name: 'Shuffle the rest (keeping 1)' }).click();

  expect((await shuffled).postDataJSON()).toMatchObject({ weekStart: '2026-11-22', keep: [stew.id] });
  await expect(page.getByRole('button', { name: 'Keep Beef and Barley Stew' })).toHaveAttribute('aria-pressed', 'true');
});

test('the plan and the plan style editor fit the window without scrolling sideways', async ({ page, api }) => {
  const fits = () => page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth);
  api.set(staged);
  await open(page, '/plan');
  await page.getByText(/items? to buy/).click();
  expect(await fits()).toBe(true);

  await open(page, '/settings/styles');
  await page.getByRole('button', { name: 'Edit Classic week' }).click();
  await page.getByRole('button', { name: 'Add a rule' }).click();
  await page.getByRole('combobox', { name: 'what it looks for' }).last().selectOption('Ingredient');
  expect(await fits()).toBe(true);
});

test('Swap opens a dialog of replacements; Escape closes it and choosing one replaces the meal', async ({ page, api }) => {
  api.set({ ...staged, 'PUT /api/plans/*/meals/*': data.plan('Staged') });
  await open(page, '/plan');
  const swap = page.getByRole('button', { name: 'Swap Beef and Barley Stew' });
  await swap.click();
  const dialog = page.getByRole('dialog', { name: 'Swap Beef and Barley Stew' });
  await expect(dialog).toBeVisible();
  await expect(dialog).toContainText('Recipes that use what you have');

  await page.keyboard.press('Escape');
  await expect(dialog).toBeHidden();
  await expect(swap).toBeFocused();

  await swap.click();
  await dialog.getByRole('button', { name: 'Use this' }).first().click();
  await expect(dialog).toBeHidden();
  expect(api.requests).toContain(`PUT /api/plans/${data.ids.plan}/meals/${stew.id}`);
});

test('the sun and moon button switches the theme, and the choice survives a reload', async ({ page }) => {
  await open(page, '/plan');
  // What is painted: the page background is #141816 in dark mode and #f6f7f4 in light.
  const dark = () => page.evaluate(() => (getComputedStyle(document.body).backgroundColor === 'rgb(20, 24, 22)' ? 'dark' : 'light'));
  const startedDark = (await dark()) === 'dark';
  await page.getByRole('button', { name: startedDark ? 'Switch to light mode' : 'Switch to dark mode' }).click();
  const chosen = startedDark ? 'light' : 'dark';
  await expect(page.locator('html')).toHaveAttribute('data-theme', chosen);

  await open(page, '/plan');
  await expect(page.locator('html')).toHaveAttribute('data-theme', chosen);
  expect(await dark()).toBe(chosen);

  // Settings, Appearance goes back to following the device.
  await open(page, '/settings/appearance');
  await page.getByRole('button', { name: 'Match this device' }).click();
  await expect(page.locator('html')).not.toHaveAttribute('data-theme', /./);
  expect(await dark()).toBe(startedDark ? 'dark' : 'light');
});

test('a settings change saves by itself, once, without a Save button', async ({ page, api }) => {
  await open(page, '/settings/notifications');
  await expect(page.getByRole('button', { name: 'Save' })).toHaveCount(0);
  const saved = page.waitForRequest((request) => request.method() === 'PUT' && request.url().endsWith('/api/system/notifications'));
  await page.getByRole('checkbox', { name: /^Today's meals/ }).click();
  expect((await saved).postDataJSON()).toMatchObject({ mealsToday: true, dailyTime: '07:30' });
  await expect(page.getByRole('status').filter({ hasText: 'Saved' })).toBeVisible();
  expect(api.requests.filter((request) => request === 'PUT /api/system/notifications')).toHaveLength(1);
});

test('a change waiting to be saved is saved when another section opens', async ({ page, api }) => {
  await open(page, '/settings/backups');
  await page.getByLabel(/^Keep this many backups/).fill('7');
  await page.getByLabel(/^Keep this many backups/).press('Tab');
  // Straight to another section, before the pause before saving is over.
  await page.getByRole('navigation', { name: 'Settings' }).getByRole('link', { name: 'Login' }).click();
  await expect.poll(() => api.requests.filter((request) => request === 'PUT /api/system/backups').length).toBe(1);
});

test('Escape closes a pantry correction and puts focus back on its button', async ({ page, api }) => {
  await open(page, '/pantry');
  const button = page.getByRole('button', { name: 'Change the amount or date of eggs' });
  await button.click();
  await expect(page.getByRole('button', { name: 'Save' })).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(page.getByRole('button', { name: 'Save' })).toHaveCount(0);
  await expect(button).toBeFocused();
  expect(api.requests.filter((request) => request.startsWith('PATCH') || request.startsWith('PUT'))).toEqual([]);
});

test('Back to recipes is a link to the list, and returns to it', async ({ page }) => {
  await open(page, '/recipes');
  await page.getByRole('link', { name: /Slaw Fish Tacos/ }).first().click();
  const back = page.getByRole('link', { name: 'Back to recipes' });
  await expect(back).toHaveAttribute('href', '/recipes');
  await back.click();
  await expect(page).toHaveURL(/\/recipes$/);
  await expect(page.getByRole('heading', { name: /^Recipes/ })).toBeVisible();
});

test('a form sent twice quickly is only sent once', async ({ page, api }) => {
  let answer;
  api.set({ 'POST /api/shopping/items': () => new Promise((resolve) => { answer = resolve; }) });
  await open(page, '/shopping');
  await page.getByLabel('Add to the list').fill('milk');
  const add = page.getByRole('button', { name: 'Add', exact: true });
  await add.click();
  await expect(add).toHaveAttribute('aria-busy', 'true');
  await add.click();
  answer(data.manualItems());
  await expect(add).not.toHaveAttribute('aria-busy', 'true');
  expect(api.requests.filter((request) => request === 'POST /api/shopping/items')).toHaveLength(1);
});
