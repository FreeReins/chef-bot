// Keyboard and focus behaviour that axe can't see: where focus goes, and which keys move it.
import { test, expect, open } from './fixtures.js';

test('the confirm dialog starts on Cancel, and Escape cancels', async ({ page, api }) => {
  await open(page, '/meals');
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
  await open(page, '/meals');
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
