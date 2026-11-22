// The `test` every spec uses: a page whose /api requests are answered from data.js, plus `api` to change answers.
// A request with no answer gets a 500 and fails the test once it ends, so the stubs can't fall behind the page.
import { test as base, expect } from '@playwright/test';
import * as data from './data.js';

const REPLY = Symbol('reply');

/** An answer other than 200 with JSON, for example `reply(500, { detail: 'The server is down.' })`. */
export const reply = (status, body = null, contentType = 'application/json') => ({ [REPLY]: true, status, body, contentType });

const COURSES = ['Main', 'Breakfast', 'Snack', 'Sweet', 'Drink', 'Other'];

/** RecipeSummaryResponse, from a RecipeResponse. */
const summary = ({ id, name, course, isSlowCooker, ingredients, tags, images, favouredBy }) =>
  ({ id, name, course, isSlowCooker, ingredients, tags, coverUrl: images[0]?.url ?? null, favouredBy });

/**
 * Answers for a recipe library, as the server gives them: the whole list, one recipe by id (404 when it isn't there),
 * and a search a page at a time.
 */
export const library = (recipes) => ({
  'GET /api/recipes': recipes,
  'GET /api/recipes/*': (request) => recipes.find((recipe) => new URL(request.url()).pathname.endsWith(`/${recipe.id}`))
    ?? reply(404, { detail: 'There is no recipe with that id.' }),
  // Favouriting answers as the signed-in admin would get it: the recipe with them added or taken off.
  'PUT /api/recipes/*/favourite': (request) => favourite(recipes, request, true),
  'DELETE /api/recipes/*/favourite': (request) => favourite(recipes, request, false),
  'GET /api/recipes/search': (request) => {
    const params = new URL(request.url()).searchParams;
    const words = (params.get('query') ?? '').toLowerCase().split(/\s+/).filter(Boolean);
    const course = params.get('course');
    const page = Math.max(1, Number(params.get('page') ?? 1));
    const pageSize = Math.min(100, Math.max(1, Number(params.get('pageSize') ?? 25)));
    const matches = recipes
      .filter((recipe) => !course || recipe.course === course)
      .filter((recipe) => words.every((word) => [recipe.name, ...recipe.ingredients, ...recipe.tags]
        .some((text) => text.toLowerCase().includes(word))))
      .sort((a, b) => COURSES.indexOf(a.course) - COURSES.indexOf(b.course) || a.name.localeCompare(b.name));
    return {
      recipes: matches.slice((page - 1) * pageSize, page * pageSize)
        .map((recipe) => summary(recipe)),
      total: matches.length, page, pageSize, librarySize: recipes.length,
    };
  },
});

function favourite(recipes, request, add) {
  const id = new URL(request.url()).pathname.split('/')[3];
  const recipe = recipes.find((candidate) => candidate.id === id);
  if (!recipe) return reply(404, { detail: 'There is no recipe with that id.' });
  const others = recipe.favouredBy.filter((favourite) => favourite.userId !== data.ids.admin);
  return summary({ ...recipe, favouredBy: add ? [...others, { userId: data.ids.admin, name: 'Alex Rivera' }] : others });
}

/** What every test starts with: an admin signed in, a confirmed plan for one week, and everything the Settings tab loads. */
function defaults() {
  return {
    'GET /api/session': data.session(),
    ...library(data.recipes()),
    'GET /api/plans': [data.summary(data.plan())],
    'GET /api/plans/*': data.plan(),
    'GET /api/plan-styles': data.planStyles(),
    'PUT /api/plan-styles': data.planStyles(),
    'POST /api/plans/*/meals/*/move': (request) => {
      const mealId = new URL(request.url()).pathname.split('/')[5];
      const plan = data.plan('Staged');
      const { date } = request.postDataJSON();
      Object.assign(plan.meals.find((meal) => meal.id === mealId), {
        date, day: new Date(`${date}T00:00:00Z`).toLocaleDateString('en-US', { weekday: 'long', timeZone: 'UTC' }),
      });
      return plan;
    },
    'GET /api/plans/*/ingredients': data.ingredients(),
    'GET /api/history': data.history(),
    'GET /api/plans/*/meals/*/alternatives': (request) => {
      const mealId = new URL(request.url()).pathname.split('/')[5];
      return data.alternatives(data.plan().meals.find((meal) => meal.id === mealId));
    },
    'GET /api/pantry': data.pantry(),
    'GET /api/shopping/items': [],
    'GET /api/shopping/targets': [],
    'GET /api/calendar/feed': { url: null, createdAt: null },
    'GET /api/tokens': [],
    'POST /api/tokens': (request) => data.issuedApiToken(request.postDataJSON().name),
    'GET /api/stores': data.stores(),
    'PUT /api/stores': (request) => ({ ...data.stores(), stores: request.postDataJSON().stores }),
    'GET /api/system/https': data.https(),
    'GET /api/system/security': data.security(),
    'PUT /api/system/security': (request) => request.postDataJSON(),
    'GET /api/system/notifications': data.notifications(),
    'PUT /api/system/notifications': (request) => ({ ...data.notifications(), ...request.postDataJSON() }),
    'GET /api/system/backups': data.backups(),
    'PUT /api/system/backups': (request) => ({ ...data.backups(), ...request.postDataJSON() }),
    'GET /api/system/backups/*/summary': data.backupSummary(),
    'GET /api/system/logs': data.logs(),
    'GET /api/users': data.users(),
    'GET /api/links': data.links(),
    'GET /api/setup/samples': data.samples(),
    'GET /api/recipes/import/formats': data.fileFormats(),
    'GET /api/account/link': (request) =>
      data.linkDetails(new URL(request.url()).searchParams.get('token') === 'reset-token' ? 'PasswordReset' : 'Invite'),
    'GET /api/test-images/*': reply(200, data.png, 'image/png'),
  };
}

/** 'GET /api/plans/*' matches any one path segment where the * is. */
const pattern = (key) => new RegExp(`^${key.replace(/[.+?^${}()|[\]\\]/g, '\\$&').replaceAll('*', '[^/]+')}$`);

class ApiStub {
  /** Requests that had no answer, as 'METHOD /path?query'. */
  unknown = [];
  /** Every request made, as 'METHOD /path?query', in order. */
  requests = [];
  #routes = new Map();

  constructor(page) {
    this.page = page;
    this.set(defaults());
  }

  /**
   * Answers requests matching each 'METHOD /path' key from now on. A value is sent as JSON; a function gets the
   * Playwright request and returns the value (or a promise of it); `reply()` sends another status or content type.
   */
  set(routes) {
    for (const [key, value] of Object.entries(routes)) this.#routes.set(key, { match: pattern(key), value });
    return this;
  }

  async install() {
    await this.page.route('**/api/**', async (route) => {
      const request = route.request();
      const url = new URL(request.url());
      const key = `${request.method()} ${url.pathname}`;
      this.requests.push(key + url.search);
      // The newest matching route wins, so set() can narrow a default.
      const found = [...this.#routes.values()].reverse().find((entry) => entry.match.test(key));
      if (!found) {
        this.unknown.push(key + url.search);
        return route.fulfill({ status: 500, json: { detail: `The test has no answer for ${key}.` } });
      }
      const value = typeof found.value === 'function' ? await found.value(request) : found.value;
      if (value?.[REPLY]) {
        const body = Buffer.isBuffer(value.body) ? value.body : value.body == null ? '' : JSON.stringify(value.body);
        return route.fulfill({ status: value.status, contentType: value.contentType, body });
      }
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(value ?? null) });
    });
  }
}

export const test = base.extend({
  // Automatic, so a test that never names `api` still gets the stubbed answers.
  api: [async ({ page }, use) => {
    const stub = new ApiStub(page);
    await stub.install();
    await use(stub);
    expect(stub.unknown, 'requests the test had no answer for; add them to tests/fixtures.js').toEqual([]);
  }, { auto: true }],
});

export { expect };

/** Opens an address and waits until the page has left its loading screen. */
export async function open(page, path) {
  await page.goto(path);
  await expect(page.getByRole('status', { name: 'Loading' })).toHaveCount(0);
}
