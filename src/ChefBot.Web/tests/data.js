// What the stubbed API answers with: one realistic household, so every screen has something on it. Each shape is
// the JSON of the C# record named beside it (src/ChefBot.Core/Managers/Contracts/Messages/ unless said otherwise);
// keep them in step when a record changes. Every call returns fresh objects, so a test can change its copy freely.

const ids = {
  admin: '00000000-0000-4000-8000-000000000001',
  sam: '00000000-0000-4000-8000-000000000002',
  plan: '00000000-0000-4000-8000-0000000000a1',
  nextPlan: '00000000-0000-4000-8000-0000000000a2',
  tacos: '00000000-0000-4000-8000-0000000000b1',
  stew: '00000000-0000-4000-8000-0000000000b2',
  oats: '00000000-0000-4000-8000-0000000000b3',
  granola: '00000000-0000-4000-8000-0000000000b4',
  crumble: '00000000-0000-4000-8000-0000000000b5',
  curry: '00000000-0000-4000-8000-0000000000b6',
};
export { ids };

/** UserResponse */
export const admin = () => ({
  id: ids.admin, username: 'alex', displayName: 'Alex Rivera', isAdmin: true,
  createdAt: '2026-01-04T10:00:00Z', lockedUntil: null,
});

/** UserResponse */
export const member = () => ({
  id: ids.sam, username: 'sam', displayName: 'Sam Rivera', isAdmin: false,
  createdAt: '2026-02-11T18:30:00Z', lockedUntil: '2099-01-01T00:00:00Z',
});

/** SessionResponse */
export const session = (user = admin()) => ({ setupRequired: false, user });

/** MealResponse */
const meal = (id, day, date, slot, title, recipeId, extra = {}) => ({
  id: `00000000-0000-4000-8000-0000000001${id}`, date, day, slot, title, recipeId, status: 'Planned',
  isOptional: false, isLeftover: false, isSlowCooker: false, portions: 4,
  ingredients: ['cabbage', 'lime', 'tortillas'], ...extra,
});

/** PlanResponse: a confirmed week with something cooked, something skipped, a leftover and meals for any day. */
export const plan = (status = 'Confirmed') => ({
  id: ids.plan, weekStart: '2026-11-22', weekEnd: '2026-11-28', status,
  meals: [
    meal('01', 'Sunday', '2026-11-22', 'Dinner', 'Slaw Fish Tacos', ids.tacos, { status: status === 'Confirmed' ? 'Cooked' : 'Planned' }),
    meal('02', 'Monday', '2026-11-23', 'Lunch', 'Slaw Fish Tacos', ids.tacos, { isLeftover: true, portions: 2 }),
    meal('03', 'Monday', '2026-11-23', 'Dinner', 'Beef and Barley Stew', ids.stew,
      { isSlowCooker: true, ingredients: ['beef chuck', 'barley', 'carrots', 'onion'] }),
    meal('04', 'Tuesday', '2026-11-24', 'Dinner', 'Chickpea Curry', ids.curry,
      { status: status === 'Confirmed' ? 'Skipped' : 'Planned', ingredients: ['chickpeas', 'spinach', 'coconut milk'] }),
    meal('05', 'Wednesday', '2026-11-25', 'Breakfast', 'Overnight Oats', ids.oats, { ingredients: ['oats', 'milk', 'apples'] }),
    meal('06', null, null, 'Snack', 'Maple Granola', ids.granola, { isOptional: true, ingredients: ['oats', 'maple syrup'] }),
    meal('07', null, null, 'Sweet', 'Pear and Ginger Crumble', ids.crumble, { ingredients: ['pears', 'ginger', 'butter'] }),
    meal('08', 'Thursday', '2026-11-26', 'Dinner', 'Grain bowl: farro, eggs and kale', null, { ingredients: ['farro', 'eggs', 'kale'] }),
    meal('09', null, null, 'Drink', 'Mint Lemonade', null, { portions: 0, ingredients: ['lemons', 'mint'] }),
  ],
  shopping: [],
  styleName: 'Classic week',
  notes: [],
});

/** PlanResponse: a draft for the week after plan(), with a dinner and a snack. */
export const nextPlan = () => ({
  id: ids.nextPlan, weekStart: '2026-11-29', weekEnd: '2026-12-05', status: 'Staged',
  meals: [
    meal('21', 'Sunday', '2026-11-29', 'Dinner', 'Chickpea Curry', ids.curry, { ingredients: ['chickpeas', 'spinach', 'coconut milk'] }),
    meal('22', null, null, 'Snack', 'Maple Granola', ids.granola, { ingredients: ['oats', 'maple syrup'] }),
  ],
  shopping: [],
  styleName: 'Slow cooker week',
  notes: [],
});

/** PlanSummaryResponse for a plan. */
export const summary = ({ id, weekStart, weekEnd, status, styleName }) => ({ id, weekStart, weekEnd, status, styleName });

/** PlanRuleDetails */
const rule = (limit, count, slot, match, extra = {}) => ({ limit, count, slot, match, values: [], minutes: null, day: null, ...extra });

/** PlanStyleDetails */
const style = (id, name, extra = {}) => ({
  id, name, description: '', isDefault: false, cookingDays: ['Sunday', 'Tuesday', 'Thursday', 'Saturday'], leftoverLunches: true,
  breakfasts: 2, snacks: 3, sweets: 1, drinks: 0, rules: [], ...extra,
});

/** PlanStylesResponse: two saved styles, the starter styles, and what rules can name. */
export const planStyles = () => {
  const classic = {
    description: 'Four dinners with leftovers for weekday lunches, one slow cooker meal on Tuesday, breakfasts, snacks and a sweet.',
    rules: [rule('AtLeast', 1, 'Dinner', 'SlowCooker', { day: 'Tuesday' })],
  };
  const slowCooker = {
    description: 'Three set-and-forget slow cooker dinners with leftovers, plus snacks and drinks.',
    cookingDays: ['Sunday', 'Tuesday', 'Thursday'], breakfasts: 0, sweets: 0, drinks: 2,
    rules: [rule('AtLeast', 3, 'Dinner', 'SlowCooker')],
  };
  const meatFree = {
    description: 'Four vegetarian dinners with leftovers: no meat or fish in any meal.',
    rules: [rule('None', 0, null, 'Ingredient', { values: ['chicken', 'beef', 'pork'] })],
  };
  return {
    styles: [
      style('00000000-0000-4000-8000-000000000601', 'Classic week', { ...classic, isDefault: true }),
      style('00000000-0000-4000-8000-000000000602', 'Slow cooker week', slowCooker),
    ],
    known: [
      style(null, 'Classic week', { ...classic, isDefault: true }),
      style(null, 'Slow cooker week', slowCooker),
      style(null, 'Meat-free', meatFree),
    ],
    tags: ['fish', 'quick'],
    ingredients: ['beef', 'cabbage', 'chicken', 'eggs', 'pork'],
  };
};

/** IngredientLine */
const line = (name, status, extra = {}) => ({
  name, status, quantity: 1, unit: '', store: null, group: 'Other', expiresOn: null, isOptional: false,
  usedIn: ['Slaw Fish Tacos'], shoppingItemId: null, ...extra,
});

/** PantryItemResponse */
const pantryItem = (id, name, extra = {}) => ({
  id: `00000000-0000-4000-8000-0000000002${id}`, name, quantity: 1, unit: '', purchasedOn: '2026-11-20',
  expiresOn: null, daysLeft: null, freshness: 'Unknown', group: 'Other', ...extra,
});

/** PantryItemResponse[]: what GET /api/pantry answers when there is no plan. */
export const pantry = () => [
  pantryItem('01', 'cabbage', { unit: 'head', group: 'Vegetable', expiresOn: '2026-11-25', daysLeft: 3, freshness: 'ExpiringSoon' }),
  pantryItem('02', 'eggs', { quantity: 12, group: 'Protein', expiresOn: '2026-12-10', daysLeft: 18, freshness: 'Fresh' }),
  pantryItem('03', 'barley', { quantity: 500, unit: 'g', group: 'WholeGrain' }),
  pantryItem('04', 'cream', { unit: 'cup', group: 'Dairy', expiresOn: '2026-11-19', daysLeft: -1, freshness: 'Expired' }),
];

/** ManualShoppingItemResponse[]: things put on the shopping list by hand, one at a store and one without. */
export const manualItems = () => [
  { id: '00000000-0000-4000-8000-000000000401', name: 'milk', quantity: 2, unit: 'l', store: 'Corner Market', group: 'Dairy' },
  { id: '00000000-0000-4000-8000-000000000402', name: 'dish soap', quantity: null, unit: '', store: '', group: 'Other' },
];

/** IngredientsResponse: the shopping list and the pantry for the plan. */
export const ingredients = () => {
  const [cabbage, eggs, barley, cream] = pantry();
  return {
    ingredients: [
      line('fish fillets', 'ToBuy', { quantity: 600, unit: 'g', store: 'Corner Market', group: 'Protein', shoppingItemId: '00000000-0000-4000-8000-000000000301' }),
      line('limes', 'ToBuy', { quantity: 3, store: 'Saturday Farmers Market', group: 'Fruit', shoppingItemId: '00000000-0000-4000-8000-000000000302' }),
      line('coconut milk', 'ToBuy', { unit: 'can', group: 'Other', isOptional: true, usedIn: ['Chickpea Curry'], shoppingItemId: '00000000-0000-4000-8000-000000000303' }),
      line('tortillas', 'Purchased', { quantity: 8, group: 'WholeGrain' }),
      line('cabbage', 'InPantry', { unit: 'head', group: 'Vegetable', expiresOn: '2026-11-25' }),
      line('salt', 'Staple', { quantity: null, group: 'Staple', usedIn: ['Slaw Fish Tacos', 'Beef and Barley Stew'] }),
    ],
    pantry: [
      { item: cabbage, usedByPlan: true, usedFor: [{ mealId: plan().meals[0].id, title: 'Slaw Fish Tacos', date: '2026-11-22', slot: 'Dinner' }] },
      { item: barley, usedByPlan: true, usedFor: [{ mealId: plan().meals[2].id, title: 'Beef and Barley Stew', date: '2026-11-23', slot: 'Dinner' }] },
      { item: eggs, usedByPlan: false, usedFor: [] },
      { item: cream, usedByPlan: false, usedFor: [] },
    ],
  };
};

/** RecipeResponse */
const recipe = (id, name, course, extra = {}) => ({
  id, name, course, isSlowCooker: false, servings: 4, ingredients: ['oats', 'milk'],
  description: null, instructions: [], prepMinutes: null, cookMinutes: null, totalMinutes: null, sourceUrl: null,
  author: null, yieldText: null, notes: null, tags: [], images: [], ingredientDetails: [], stepDetails: [], ...extra,
});

/** RecipeResponse[]: the first has everything filled in, for the recipe page. */
export const recipes = () => [
  recipe(ids.tacos, 'Slaw Fish Tacos', 'Main', {
    ingredients: ['600 g fish fillets', '1/2 head cabbage', '3 limes', '8 tortillas'],
    description: 'Crisp fish and a sharp lime slaw. The slaw keeps for lunch the next day.',
    instructions: [
      'Preheat the oven to 200°C. Shred the cabbage and dress it with lime and salt.',
      'Season the fish and roast it for 8 to 10 minutes.',
      'Warm the tortillas and fill them.',
    ],
    prepMinutes: 20, cookMinutes: 10, totalMinutes: 30, sourceUrl: 'https://example.com/slaw-fish-tacos',
    author: 'Jo Park', yieldText: '8 tacos', notes: 'Any firm white fish works.', tags: ['quick', 'fish'],
    images: [
      { id: '00000000-0000-4000-8000-000000000401', url: '/api/test-images/1.png' },
      { id: '00000000-0000-4000-8000-000000000402', url: '/api/test-images/2.png' },
    ],
    ingredientDetails: [{ text: '600 g fish fillets', quantity: 600, unit: 'g', name: 'fish fillets', note: null }],
    stepDetails: [
      { text: 'Preheat the oven to 200°C. Shred the cabbage and dress it with lime and salt.', timers: [],
        headStart: { kind: 'Preheat', text: 'Preheat the oven to 200°C' }, ingredients: [1, 2] },
      { text: 'Season the fish and roast it for 8 to 10 minutes.', timers: [{ text: '8 to 10 minutes', seconds: 480 }],
        headStart: null, ingredients: [0] },
      { text: 'Warm the tortillas and fill them.', timers: [], headStart: null, ingredients: [3] },
    ],
  }),
  recipe(ids.stew, 'Beef and Barley Stew', 'Main', { isSlowCooker: true, ingredients: ['beef chuck', 'barley', 'carrots'] }),
  recipe(ids.curry, 'Chickpea Curry', 'Main', { ingredients: ['chickpeas', 'spinach', 'coconut milk'] }),
  recipe(ids.oats, 'Overnight Oats', 'Breakfast'),
  recipe(ids.granola, 'Maple Granola', 'Snack', { ingredients: ['oats', 'maple syrup'] }),
  recipe(ids.crumble, 'Pear and Ginger Crumble', 'Sweet', { ingredients: ['pears', 'ginger', 'butter'] }),
];

/** RecipeResponse[]: the usual recipes, then numbered ones up to `count` in all, so the library runs to several pages. */
export const manyRecipes = (count) => {
  const courses = ['Main', 'Breakfast', 'Snack', 'Sweet'];
  const extra = Array.from({ length: Math.max(0, count - 6) }, (_, index) => recipe(
    `00000000-0000-4000-8000-${String(900000000 + index).padStart(12, '0')}`,
    `Test Recipe ${String(index + 1).padStart(2, '0')}`,
    courses[index % courses.length],
  ));
  return [...recipes(), ...extra];
};

/** GroceryStoresResponse */
export const stores = () => ({
  stores: [
    { name: 'Corner Market', isMain: true, items: [], seasonalProduce: false, months: [] },
    { name: 'Saturday Farmers Market', isMain: false, items: ['eggs', 'honey'], seasonalProduce: true, months: [5, 6, 7, 8, 9, 10] },
  ],
  known: [
    { name: 'Costco', kind: 'WarehouseClub', description: 'Bulk staples and meat.', items: ['rice'], seasonalProduce: false, months: [] },
    { name: 'Trader Joe\'s', kind: 'Specialty', description: 'Snacks and frozen meals.', items: [], seasonalProduce: false, months: [] },
  ],
  ingredients: ['eggs', 'honey', 'rice', 'fish fillets', 'limes', 'cabbage'],
});

/** HttpsStatusResponse (src/ChefBot.Api/Https/) */
export const https = () => ({
  mode: 'SelfSigned', enabled: true, port: 8443, hostnames: ['chefbot.local'], expiresOn: '2027-11-01T00:00:00Z',
  canRegenerate: true, isSecureNow: false, requireHttps: false,
});

/** SecurityResponse */
export const security = () => ({ maxFailedSignIns: 5, lockoutMinutes: 15, requireHttps: false });

/** NotificationSettingsResponse */
export const notifications = () => ({
  webhookUrl: 'https://ntfy.example.com/chefbot', planReady: true, slowCooker: true, mealsToday: false,
  expiringFood: true, shopping: false, dailyTime: '07:30', lastDailyRunOn: '2026-11-20',
});

/** BackupsResponse */
export const backups = () => ({
  enabled: true, time: '03:00', keepCount: 14, keepDays: 0, lastResult: 'The last backup finished at 3:00 today.',
  files: [
    { name: 'chefbot-2026-11-20.zip', createdAt: '2026-11-20T03:00:00Z', sizeBytes: 2_400_000 },
    { name: 'chefbot-2026-11-19.zip', createdAt: '2026-11-19T03:00:00Z', sizeBytes: 800_000 },
  ],
});

/** BackupSummary */
export const backupSummary = () => ({
  recipes: 6, recipePhotos: 2, mealPlans: 3, plannedMeals: 24, shoppingItems: 40, pantryItems: 4, manualShoppingItems: 2, users: 2, groceryStores: 2,
  planStyles: 2,
});

/** LogsResponse (src/ChefBot.Api/Logging/): a long message, a warning and an error with details. */
export const logs = () => ({
  capacity: 1000,
  entries: [
    { sequence: 1, time: '2026-11-20T03:00:00Z', level: 'Information', category: 'ChefBot.Backups', message: 'Backup made.', exception: null },
    { sequence: 2, time: '2026-11-20T03:05:00Z', level: 'Warning', category: 'ChefBot.Backups', message: 'The backup folder took 9 seconds to answer.', exception: null },
    { sequence: 3, time: '2026-11-20T03:10:00Z', level: 'Error', category: 'ChefBot.Notifications', message: 'The webhook failed.', exception: 'System.Net.Http.HttpRequestException: Connection refused' },
    { sequence: 4, time: '2026-11-20T03:15:00Z', level: 'Information', category: 'ChefBot.Planning', message: 'Line one\nLine two\nLine three\nLine four\nLine five', exception: null },
  ],
});

/** UserResponse[] */
export const users = () => [admin(), member()];

/** PendingLink[] */
export const links = () => [
  { id: '00000000-0000-4000-8000-000000000501', purpose: 'Invite', username: null, createdAt: '2026-11-18T12:00:00Z', expiresAt: '2026-11-25T12:00:00Z' },
  { id: '00000000-0000-4000-8000-000000000502', purpose: 'PasswordReset', username: 'sam', createdAt: '2026-11-19T12:00:00Z', expiresAt: '2026-11-20T12:00:00Z' },
];

/** IssuedLink */
export const issuedLink = () => ({
  id: '00000000-0000-4000-8000-000000000503', purpose: 'Invite', token: 'new-invite-token', expiresAt: '2026-11-27T12:00:00Z',
});

/** LinkDetails */
export const linkDetails = (purpose = 'Invite') => ({ purpose, username: purpose === 'Invite' ? null : 'sam', expiresAt: '2026-11-27T12:00:00Z' });

/** SampleRecipeResponse[]: every course, one already in the library. */
export const samples = () => [
  { name: 'Slaw Fish Tacos', course: 'Main', isSlowCooker: false, servings: 4, ingredients: ['fish', 'cabbage', 'limes'], alreadyAdded: true },
  { name: 'Lentil Shepherd\'s Pie', course: 'Main', isSlowCooker: false, servings: 6, ingredients: ['lentils', 'potatoes'], alreadyAdded: false },
  { name: 'Pulled Pork', course: 'Main', isSlowCooker: true, servings: 8, ingredients: ['pork shoulder', 'onion'], alreadyAdded: false },
  { name: 'Overnight Oats', course: 'Breakfast', isSlowCooker: false, servings: 2, ingredients: ['oats', 'milk'], alreadyAdded: false },
  { name: 'Maple Granola', course: 'Snack', isSlowCooker: false, servings: null, ingredients: ['oats', 'maple syrup'], alreadyAdded: false },
  { name: 'Pear Crumble', course: 'Sweet', isSlowCooker: false, servings: 6, ingredients: ['pears', 'butter'], alreadyAdded: false },
];

/** RecipeFileFormatResponse[] */
export const fileFormats = () => [
  { name: 'Recipe JSON-LD', extensions: ['.json', '.html'] },
  { name: 'Mealie backup', extensions: ['.zip'] },
];

/** AlternativesResponse for a meal. */
export const alternatives = (forMeal) => ({
  meal: forMeal,
  recipes: [
    { recipeId: ids.curry, name: 'Chickpea Curry', isSlowCooker: false, score: 0.8, onHand: ['spinach'], expiringSoon: ['cabbage'], toBuy: ['coconut milk'] },
    { recipeId: ids.stew, name: 'Beef and Barley Stew', isSlowCooker: true, score: 0.6, onHand: ['barley'], expiringSoon: [], toBuy: [] },
  ],
  creative: [
    { title: 'Grain bowl: barley, eggs and cabbage', components: [
      { name: 'barley', group: 'WholeGrain', onHand: true, expiresOn: null },
      { name: 'eggs', group: 'Protein', onHand: true, expiresOn: '2026-12-10' },
      { name: 'kale', group: 'Vegetable', onHand: false, expiresOn: null },
    ], ingredients: ['barley', 'eggs', 'kale'] },
  ],
});

// A 1×1 PNG for recipe photos.
export const png = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==', 'base64');
