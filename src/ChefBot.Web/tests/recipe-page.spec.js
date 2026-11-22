// What a recipe's page does that axe can't see: scaling its amounts, adding it to the plan, and its cookbooks.
import { test, expect, open, library } from './fixtures.js';
import * as data from './data.js';

const tacos = `/recipes/${data.ids.tacos}`;

test('scaling a recipe changes its amounts and keeps the choice in the address', async ({ page }) => {
  await open(page, tacos);
  const ingredients = page.getByRole('region', { name: /^Ingredients/ });
  await expect(ingredients).toContainText('1/2 head cabbage');

  await ingredients.getByRole('button', { name: /^2×\W+8 servings$/ }).click();
  await expect(ingredients).toContainText('1200 g fish fillets');
  await expect(ingredients).toContainText('1 head cabbage');
  await expect(ingredients).toContainText('16 tortillas');
  await expect(page).toHaveURL(/\?serves=8$/);

  await ingredients.getByRole('button', { name: 'One serving fewer' }).click();
  await expect(ingredients).toContainText('5¼ limes');
  await page.reload();
  await expect(page.getByRole('spinbutton', { name: 'Servings' })).toHaveValue('7');

  await ingredients.getByRole('button', { name: 'Back to 4' }).click();
  await expect(ingredients).toContainText('3 limes');
  await expect(page).toHaveURL(new RegExp(`${tacos}$`));
});

test('adding a recipe to the plan sends the week, day, meal and servings chosen', async ({ page, api }) => {
  const week = data.planOn(data.nextSunday());
  let sent;
  api.set({
    'GET /api/plans': [data.summary(week)],
    'GET /api/plans/*': week,
    'POST /api/plans/meals': (request) => { sent = request.postDataJSON(); return week; },
  });
  await open(page, `${tacos}?serves=6`);
  await page.getByRole('link', { name: 'Add to plan' }).click();
  const dialog = page.getByRole('dialog', { name: 'Add Slaw Fish Tacos to the plan' });
  await dialog.getByRole('button', { name: /^Next week/ }).click();
  await dialog.getByRole('button', { name: /Beef and Barley Stew planned$/ }).click();
  await dialog.getByRole('radio', { name: 'Add alongside it' }).check();
  await dialog.getByRole('button', { name: /^Add to monday dinner/i }).click();

  await expect(dialog).toBeHidden();
  await expect(page.getByText(/Added Slaw Fish Tacos to Monday dinner for 6/)).toBeVisible();
  expect(sent).toEqual({
    recipeId: data.ids.tacos, weekStart: week.weekStart, date: week.meals[2].date, slot: 'Dinner', servings: 6, replaces: null,
  });
});

test('replacing a meal from a recipe page names the meal it replaces', async ({ page, api }) => {
  const week = data.planOn(data.nextSunday());
  let sent;
  api.set({
    'GET /api/plans': [data.summary(week)],
    'GET /api/plans/*': week,
    'POST /api/plans/meals': (request) => { sent = request.postDataJSON(); return week; },
  });
  await open(page, `${tacos}/plan`);
  const dialog = page.getByRole('dialog');
  await dialog.getByRole('button', { name: /^Next week/ }).click();
  await dialog.getByRole('button', { name: /Beef and Barley Stew planned$/ }).click();
  await dialog.getByRole('radio', { name: 'For the household (2)' }).check();
  await dialog.getByRole('button', { name: 'Replace Beef and Barley Stew' }).click();

  await expect(dialog).toBeHidden();
  expect(sent).toMatchObject({ servings: null, replaces: week.meals[2].id });
});

test('the first cookbook is made from a recipe, and ticking cookbooks puts it in or takes it out', async ({ page, api }) => {
  const made = { id: data.ids.weeknight, name: 'Weeknight', description: null, recipes: 0 };
  const sent = [];
  api.set({
    'POST /api/cookbooks': (request) => { sent.push(['made', request.postDataJSON().name]); return made; },
    'POST /api/cookbooks/*/recipes': (request) => { sent.push(['added', request.postDataJSON().recipeIds]); return { ...made, recipes: 1 }; },
    'DELETE /api/cookbooks/*/recipes/*': () => { sent.push(['removed']); return made; },
  });
  await open(page, tacos);
  await page.getByRole('button', { name: 'More actions for Slaw Fish Tacos' }).click();
  await page.getByRole('link', { name: 'Add to cookbook' }).click();
  const dialog = page.getByRole('dialog', { name: 'Cookbooks for Slaw Fish Tacos' });
  await expect(dialog).toContainText('Make the first one here');

  // Once it exists, the cookbook list answers with it.
  api.set({ ...library(data.recipes(), [made]) });
  await dialog.getByRole('textbox', { name: 'New cookbook' }).fill('Weeknight');
  await dialog.getByRole('button', { name: 'Make and add' }).click();
  const box = dialog.getByRole('checkbox', { name: 'Weeknight' });
  await expect(box).toBeChecked();
  await box.uncheck();
  await expect(box).not.toBeChecked();

  expect(sent).toEqual([['made', 'Weeknight'], ['added', [data.ids.tacos]], ['removed']]);
});
