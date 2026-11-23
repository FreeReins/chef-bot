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
  weeknight: '00000000-0000-4000-8000-0000000000d1',
  grandmas: '00000000-0000-4000-8000-0000000000d2',
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

/** The Sunday next week starts on (yyyy-mm-dd), as the page works it out in the tests' time zone, UTC. */
export function nextSunday() {
  const now = new Date();
  return new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate() - now.getUTCDay() + 7)).toISOString().slice(0, 10);
}

/** plan(), moved to the week starting `weekStart`, for screens that only offer days still to come. */
export function planOn(weekStart, status = 'Staged') {
  const moved = plan(status);
  const days = Math.round((Date.parse(weekStart) - Date.parse(moved.weekStart)) / 86400000);
  const shift = (iso) => (iso ? new Date(Date.parse(iso) + days * 86400000).toISOString().slice(0, 10) : iso);
  return { ...moved, weekStart: shift(moved.weekStart), weekEnd: shift(moved.weekEnd), meals: moved.meals.map((meal) => ({ ...meal, date: shift(meal.date) })) };
}

/** PlanSummaryResponse for a plan. */
export const summary = ({ id, weekStart, weekEnd, status, styleName }) => ({ id, weekStart, weekEnd, status, styleName });

/** PlanRuleDetails */
const rule = (limit, count, slot, match, extra = {}) => ({ limit, count, slot, match, values: [], minutes: null, day: null, ...extra });

/** PlanStyleDetails */
const style = (id, name, extra = {}) => ({
  id, name, description: '', isDefault: false, cookingDays: ['Sunday', 'Tuesday', 'Thursday', 'Saturday'], leftoverLunches: true,
  breakfasts: 2, snacks: 3, sweets: 1, drinks: 0, rules: [], favourites: 'ALittle', ...extra,
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
    cookbooks: [],
  };
};

/** IngredientLine */
const line = (name, status, extra = {}) => ({
  name, status, quantity: 1, unit: '', store: null, group: 'Other', area: 'Other', expiresOn: null, isOptional: false,
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

/** PantryItemResponse[]: the pantry with five items to use soon, more than the Pantry tab shows until asked. */
export const pantryToUseSoon = () => [
  ...pantry(),
  pantryItem('05', 'spinach', { unit: 'bag', group: 'Vegetable', expiresOn: '2026-11-23', daysLeft: 1, freshness: 'ExpiringSoon' }),
  pantryItem('06', 'yogurt', { unit: 'tub', group: 'Dairy', expiresOn: '2026-11-24', daysLeft: 2, freshness: 'ExpiringSoon' }),
  pantryItem('07', 'chicken thighs', { quantity: 600, unit: 'g', group: 'Protein', expiresOn: '2026-11-22', daysLeft: 0, freshness: 'ExpiringSoon' }),
];

/** ManualShoppingItemResponse[]: things put on the shopping list by hand, one at a store and one without. */
export const manualItems = () => [
  { id: '00000000-0000-4000-8000-000000000401', name: 'milk', quantity: 2, unit: 'l', store: 'Corner Market', group: 'Dairy', area: 'DairyAndChilled' },
  { id: '00000000-0000-4000-8000-000000000402', name: 'dish soap', quantity: null, unit: '', store: '', group: 'Other', area: 'Other' },
];

/** IngredientsResponse: the shopping list and the pantry for the plan. */
export const ingredients = () => {
  const [cabbage, eggs, barley, cream] = pantry();
  return {
    ingredients: [
      line('fish fillets', 'ToBuy', { quantity: 600, unit: 'g', store: 'Corner Market', group: 'Protein', area: 'MeatAndSeafood', shoppingItemId: '00000000-0000-4000-8000-000000000301' }),
      line('limes', 'ToBuy', { quantity: 3, store: 'Saturday Farmers Market', group: 'Fruit', area: 'Produce', shoppingItemId: '00000000-0000-4000-8000-000000000302' }),
      line('coconut milk', 'ToBuy', { unit: 'can', group: 'Other', area: 'CannedAndDry', isOptional: true, usedIn: ['Chickpea Curry'], shoppingItemId: '00000000-0000-4000-8000-000000000303' }),
      line('tortillas', 'Purchased', { quantity: 8, group: 'WholeGrain', area: 'Bakery' }),
      line('cabbage', 'InPantry', { unit: 'head', group: 'Vegetable', area: 'Produce', expiresOn: '2026-11-25' }),
      line('salt', 'Staple', { quantity: null, group: 'Staple', area: 'SpicesAndBaking', usedIn: ['Slaw Fish Tacos', 'Beef and Barley Stew'] }),
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
  author: null, yieldText: null, notes: null, tags: [], images: [], ingredientDetails: [], stepDetails: [], favouredBy: [], rating: null, issues: [],
  cookbooks: [], ...extra,
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
    favouredBy: [{ userId: ids.admin, name: 'Alex Rivera' }, { userId: ids.sam, name: 'Sam Rivera' }],
    rating: { average: 4.5, count: 2 },
    images: [
      { id: '00000000-0000-4000-8000-000000000401', url: '/api/test-images/1.png' },
      { id: '00000000-0000-4000-8000-000000000402', url: '/api/test-images/2.png' },
    ],
    ingredientDetails: [
      { text: '600 g fish fillets', quantity: 600, unit: 'g', name: 'fish fillets', note: null },
      { text: '1/2 head cabbage', quantity: 0.5, unit: 'head', name: 'cabbage', note: null },
      { text: '3 limes', quantity: 3, unit: '', name: 'lime', note: null },
      { text: '8 tortillas', quantity: 8, unit: '', name: 'tortilla', note: null },
    ],
    stepDetails: [
      { text: 'Preheat the oven to 200°C. Shred the cabbage and dress it with lime and salt.', timers: [],
        headStart: { kind: 'Preheat', text: 'Preheat the oven to 200°C' }, ingredients: [1, 2] },
      { text: 'Season the fish and roast it for 8 to 10 minutes.', timers: [{ text: '8 to 10 minutes', seconds: 480 }],
        headStart: null, ingredients: [0] },
      { text: 'Warm the tortillas and fill them.', timers: [], headStart: null, ingredients: [3] },
    ],
  }),
  recipe(ids.stew, 'Beef and Barley Stew', 'Main', {
    isSlowCooker: true, ingredients: ['beef chuck', 'barley', 'carrots'], favouredBy: [{ userId: '00000000-0000-4000-8000-000000000a01', name: 'James' }],
  }),
  recipe(ids.curry, 'Chickpea Curry', 'Main', {
    ingredients: ['chickpeas', 'spinach', 'coconut milk'], tags: ['quick', 'vegetarian'], totalMinutes: 35,
    issues: ['NoPhoto', 'NoDescription', 'ThinSteps', 'MissingAmounts'],
  }),
  recipe(ids.oats, 'Overnight Oats', 'Breakfast'),
  recipe(ids.granola, 'Maple Granola', 'Snack', { ingredients: ['oats', 'maple syrup'] }),
  recipe(ids.crumble, 'Pear and Ginger Crumble', 'Sweet', { ingredients: ['pears', 'ginger', 'butter'] }),
];

/** CookbookResponse[] without their counts, which library() works out: the household's two cookbooks. */
export const cookbooks = () => [
  { id: ids.weeknight, name: 'Weeknight', description: 'Quick dinners for busy evenings.' },
  { id: ids.grandmas, name: "Grandma's", description: null },
];

/** RecipeResponse[]: the usual recipes, with the tacos and the curry in the Weeknight cookbook and the crumble in Grandma's. */
export const recipesInCookbooks = () => {
  const [weeknight, grandmas] = cookbooks().map(({ id, name }) => ({ id, name }));
  const into = { [ids.tacos]: [weeknight], [ids.curry]: [weeknight], [ids.crumble]: [grandmas] };
  return recipes().map((recipe) => ({ ...recipe, cookbooks: into[recipe.id] ?? [] }));
};

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

/** RecipeResponse[]: the usual recipes, sharing `count` tags between them unevenly, as a big imported library might. */
export const manyTags = (count) => recipes().map((recipe, index) => ({
  ...recipe,
  tags: [...recipe.tags, ...Array.from({ length: count }, (_, tag) => `tag ${tag + 1}`).filter((_, tag) => tag % (index + 1) === 0)],
}));

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
/** An inbox with a plan the weekly schedule staged, unread unless `read`, and an older note from the page. */
export const inbox = ({ read = false } = {}) => ({
  entries: [
    { id: '00000000-0000-4000-8000-000000000901', text: 'The weekly schedule made a new plan. Review it on the Plan tab, then confirm it.',
      source: 'Plan', createdAt: new Date(Date.now() - 5 * 60000).toISOString(), read },
    { id: '00000000-0000-4000-8000-000000000902', text: 'Recipe saved.', source: 'Page', createdAt: '2026-11-20T18:00:00Z', read: true },
  ],
  unread: read ? 0 : 1,
});

export const security = () => ({ maxFailedSignIns: 5, lockoutMinutes: 15, requireHttps: false });

/** NotificationSettingsResponse */
export const notifications = () => ({
  webhookUrl: 'https://ntfy.example.com/chefbot', planReady: true, slowCooker: true, mealsToday: false,
  expiringFood: true, shopping: false, achievements: true, dailyTime: '07:30', lastDailyRunOn: '2026-11-20',
});

/** PlannerSettingsResponse */
export const planning = () => ({
  people: 2, scheduleEnabled: true, scheduleDay: 'Saturday', scheduleTime: '09:00', maxLeftoverDays: 3,
  nextStaging: '2026-11-21T15:00:00Z', isSaved: true,
});

/** CalendarTimesResponse */
export const calendarTimes = () => ({
  breakfastTime: '08:00', lunchTime: '12:30', dinnerTime: '18:30', mealMinutes: 45, slowCookerHours: 8, isSaved: false,
});

/** AdvancedSettingsValues */
const advancedValues = () => ({
  avoidRepeatDays: 28, expiringSoonDays: 3, perishableWithinDays: 21,
  words: {
    breakfast: ['breakfast', 'brunch'], snack: ['snack', 'snacks', 'appetizer', 'appetizers'],
    sweet: ['dessert', 'desserts', 'sweet', 'sweets', 'treat', 'treats', 'cookies', 'cake'],
    drink: ['drink', 'drinks', 'beverage', 'beverages', 'cocktail', 'cocktails', 'smoothie', 'lemonade'],
    slowCooker: ['slow cooker', 'crockpot', 'crock pot'],
    notAMeal: ['side', 'sides', 'side dish', 'sauce', 'condiment', 'bread', 'baking'],
    main: [],
  },
});

/** AdvancedSettingsResponse */
export const advanced = () => ({ current: advancedValues(), defaults: advancedValues(), isSaved: false });

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
  { name: 'Slaw Fish Tacos', course: 'Main', isSlowCooker: false, servings: 4, ingredients: ['fish', 'cabbage', 'limes'], alreadyAdded: true,
    description: 'Spiced, pan-seared fish in charred tortillas with a bright lime slaw.', totalMinutes: 25 },
  { name: 'Lentil Shepherd\'s Pie', course: 'Main', isSlowCooker: false, servings: 6, ingredients: ['lentils', 'potatoes'], alreadyAdded: false,
    description: 'Thyme-scented lentils and vegetables under a golden mashed potato crust, a hearty meat-free dinner that feeds a crowd and reheats well.', totalMinutes: 75 },
  { name: 'Pulled Pork', course: 'Main', isSlowCooker: true, servings: 8, ingredients: ['pork shoulder', 'onion'], alreadyAdded: false,
    description: 'Spice-rubbed pork shoulder cooked until it falls apart.', totalMinutes: 510 },
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

/** ShoppingTargetResponse[]: a Home Assistant list with its token saved, and a webhook without one. */
export const shoppingTargets = () => [
  { id: '00000000-0000-4000-8000-000000000501', name: 'Kitchen list', kind: 'HomeAssistant', url: 'http://homeassistant.local:8123',
    list: 'todo.shopping_list', hasToken: true },
  { id: '00000000-0000-4000-8000-000000000502', name: 'Automation', kind: 'Webhook', url: 'https://hooks.example/shopping',
    list: '', hasToken: false },
];

/** CalendarFeedResponse with the feed turned on. */
export const calendarFeed = () => ({
  url: 'https://chefbot.local:8443/api/calendar/0f3a9c2e7b5d41e8a6c0f3a9c2e7b5d41e8a6c0f3a9c2e7b5d41e8a6c0f3a9c2e/meals.ics',
  createdAt: '2026-11-20T09:00:00Z',
});

/** The signed-in person's API tokens, newest first. */
export const apiTokens = () => [
  { id: 'a7f0c2d1-0000-4000-8000-000000000002', name: 'Claude Code on the laptop', createdAt: '2026-11-02T18:30:00Z' },
  { id: 'a7f0c2d1-0000-4000-8000-000000000001', name: 'Claude Desktop', createdAt: '2026-10-14T09:00:00Z' },
];

/** A token just made, with its secret. */
export const issuedApiToken = (name = 'Kitchen tablet') => ({
  id: 'a7f0c2d1-0000-4000-8000-000000000003', name,
  token: 'chefbot_9e359d02660d4d63bdc9fba67942c7e46a4231d31489474d517c20b37ac3631b', createdAt: '2026-11-08T12:00:00Z',
});

/** HistoryMoment */
const moment = (date, kind, extra = {}) => ({
  at: `${date}T00:00:00+00:00`, kind, title: null, recipeId: null, count: null, dishes: [], favouredBy: [], review: null, ...extra,
});

/** HistoryResponse: two months of a household's food story, with older months still to load. */
export const history = () => ({
  chapters: [
    {
      month: '2026-11-01', dinners: 6, topDish: 'Slaw Fish Tacos', topDishCooks: 3, topTag: 'Weeknight',
      moments: [
        moment('2026-11-07', 'WeekOfPlates', { count: 3, dishes: [
          { title: 'Slaw Fish Tacos', recipeId: ids.tacos, date: '2026-11-03' },
          { title: 'Chickpea Curry', recipeId: ids.curry, date: '2026-11-05' },
          { title: 'Pizza night', recipeId: null, date: '2026-11-07' },
        ] }),
        moment('2026-11-06', 'Reviewed', { title: 'Slaw Fish Tacos', recipeId: ids.tacos,
          review: { stars: 5, notes: 'Crispier than last time. Double the slaw.', reviewedBy: 'Sam Rivera' } }),
        moment('2026-11-06', 'FanFavourite', { title: 'Slaw Fish Tacos', recipeId: ids.tacos, count: 5 }),
        moment('2026-11-05', 'FavouriteCooked', { title: 'Beef and Barley Stew', recipeId: ids.stew, favouredBy: ['Sam', 'James'] }),
        moment('2026-11-04', 'Haul', { count: 23 }),
        moment('2026-11-02', 'Comeback', { title: 'Beef and Barley Stew', recipeId: ids.stew, count: 132, favouredBy: ['Jamie'] }),
        moment('2026-11-01', 'DinnerMilestone', { title: 'Chickpea Curry', recipeId: ids.curry, count: 100 }),
      ],
    },
    {
      month: '2026-10-01', dinners: 2, topDish: null, topDishCooks: 0, topTag: null,
      moments: [
        moment('2026-10-20', 'Debut', { title: 'Chickpea Curry', recipeId: ids.curry }),
        moment('2026-10-18', 'RecipeRetired', { title: 'Beige Casserole' }),
        moment('2026-10-12', 'RecipeStampede', { count: 42, dishes: [
          { title: 'Overnight Oats', recipeId: ids.oats, date: '2026-10-12' },
          { title: 'Maple Granola', recipeId: ids.granola, date: '2026-10-12' },
        ] }),
        moment('2026-10-10', 'RecipeAdded', { title: 'Apple Crumble', recipeId: ids.crumble }),
        moment('2026-10-03', 'PersonJoined', { title: 'Sam' }),
        moment('2026-10-01', 'KitchenOpened', { title: 'Jamie' }),
      ],
    },
  ],
  olderBefore: '2026-10-01', since: '2026-03-01', totalDinners: 128,
});

/** HistoryResponse with nothing kept yet. */
export const emptyHistory = () => ({ chapters: [], olderBefore: null, since: null, totalDinners: 0 });

/** HistoryResponse: the month before history(), the very first one kept. */
export const olderHistory = () => ({
  chapters: [{
    month: '2026-09-01', dinners: 0, topDish: null, topDishCooks: 0, topTag: null,
    moments: [moment('2026-09-14', 'RecipeAdded', { title: 'Slaw Fish Tacos', recipeId: ids.tacos })],
  }],
  olderBefore: null, since: '2026-09-14', totalDinners: 128,
});

/** RecipeReviewsResponse for a recipe: the tacos have two, one the signed-in admin's own; others have none. */
export const reviews = (recipeId) => (recipeId === ids.tacos ? {
  rating: { average: 4.5, count: 2 },
  reviews: [
    { id: '00000000-0000-4000-8000-000000000601', userId: ids.sam, reviewedBy: 'Sam Rivera', stars: 5,
      notes: 'Crispier than last time. Double the slaw.', at: '2026-11-06T19:30:00Z', edited: false },
    { id: '00000000-0000-4000-8000-000000000602', userId: ids.admin, reviewedBy: 'Alex Rivera', stars: 4,
      notes: null, at: '2026-10-20T19:10:00Z', edited: true },
  ],
} : { rating: null, reviews: [] });

/** Achievement, for AchievementsResponse below: `tiers` 1 for a first step, 3 for a badge. */
const achievement = (kind, tier, progress, goal, { tiers = 3, optional = false } = {}) => ({ kind, tier, tiers, progress, goal, optional });
const step = (kind, done, optional = false) => achievement(kind, done ? 1 : 0, done ? 1 : 0, done ? null : 1, { tiers: 1, optional });
const STEP_KINDS = ['FirstRecipe', 'FirstPlan', 'PlanConfirmed', 'FirstShop', 'FirstCook', 'FirstFavourite'];

/** AchievementsResponse: halfway through the first steps, a few badges in, nobody invited yet. */
export const achievements = () => ({
  points: 640, rank: 2, rankPoints: 500, nextRankPoints: 1200,
  starter: [...STEP_KINDS.map((kind, index) => step(kind, index < 3)), step('FirstGuest', false, true)],
  badges: [
    achievement('Cookbook', 1, 12, 50), achievement('Dinners', 1, 14, 50), achievement('Repertoire', 2, 21, 50),
    achievement('WeeksPlanned', 1, 5, 12), achievement('Hauls', 0, 2, 3), achievement('BestStreak', 3, 27, null),
    achievement('Comebacks', 0, 0, 1), achievement('Critic', 1, 3, 10), achievement('Favourites', 0, 4, 5),
  ],
});

/** AchievementsResponse: every first step done, so the card offers to be put away. */
export const graduatedAchievements = () => ({
  ...achievements(), starter: [...STEP_KINDS.map((kind) => step(kind, true)), step('FirstGuest', false, true)],
});

/** Each badge's bronze goal, as AchievementEngine sets them. */
const FIRST_GOALS = { Cookbook: 10, Dinners: 10, Repertoire: 5, WeeksPlanned: 4, Hauls: 3, BestStreak: 3, Comebacks: 1, Critic: 1, Favourites: 5 };

/** AchievementsResponse for a kitchen that has just opened. */
export const freshAchievements = () => ({
  points: 0, rank: 0, rankPoints: 0, nextRankPoints: 150,
  starter: [...STEP_KINDS.map((kind) => step(kind, false)), step('FirstGuest', false, true)],
  badges: Object.entries(FIRST_GOALS).map(([kind, goal]) => achievement(kind, 0, 0, goal)),
});

/** AiFeaturesResponse: which AI features have a model. */
export const aiFeatures = (ready = true) => ({ recipeFromPhoto: ready, recipeDetails: ready });

const AI_TASKS = [
  { task: 'RecipeFromPhoto', label: 'Reading recipe photos', description: 'Reads a recipe from photos of a cookbook page, a card or a screenshot. Needs a model that can see images.' },
  { task: 'RecipeDetails', label: 'Completing recipes', description: 'Suggests what a recipe is missing: details, clearer steps, ingredient amounts and tips.' },
];

/** AiSettingsResponse: with models, Claude as the default and a local model reading photos; empty without. */
export const aiSettings = (withModels = true) => (withModels ? {
  models: [
    { id: '00000000-0000-4000-8000-000000000b01', name: 'Claude', provider: 'Anthropic', model: 'claude-opus-5-5', endpoint: '', hasApiKey: true },
    { id: '00000000-0000-4000-8000-000000000b02', name: 'Ollama', provider: 'OpenAiCompatible', model: 'qwen2.5vl', endpoint: 'http://ollama.local:11434/v1', hasApiKey: false },
  ],
  defaultModel: 'Claude',
  tasks: AI_TASKS.map((task) => ({ ...task, model: task.task === 'RecipeFromPhoto' ? 'Ollama' : null })),
} : { models: [], defaultModel: null, tasks: AI_TASKS.map((task) => ({ ...task, model: null })) });

/** DetailsSweepStatusResponse. */
export const detailsSweep = (sparse = 12, waiting = 3) => ({ sparse, waiting, modelName: 'Claude' });

/** RecipePhotoResponse: a recipe card read from a photo. */
export const photoDraft = () => ({
  readWith: 'Ollama',
  draft: {
    name: "Grandma's Oat Cookies", course: 'Sweet', isSlowCooker: false, servings: null,
    ingredients: ['2 cups rolled oats', '1 cup butter, softened', '1 cup brown sugar'],
    description: null, instructions: ['Cream the butter and sugar.', 'Stir in the oats.', 'Bake 12 minutes at 350°F.'],
    prepMinutes: 15, cookMinutes: 12, totalMinutes: null, sourceUrl: null, author: 'Grandma June', yieldText: '24 cookies',
    notes: null, tags: ['cookies'],
  },
});

/** RecipeSuggestionResponse for a recipe: one change of each kind the dialog shows. */
export const suggestion = (recipeId, stale = false) => ({
  recipeId, modelName: 'Claude', createdAt: '2026-11-22T18:00:00Z', status: 'Waiting', stale,
  changes: [
    { id: 1, field: 'TotalMinutes', current: [], suggested: ['40'], line: null },
    { id: 2, field: 'Tags', current: [], suggested: ['stew', 'slow cooker', 'beef'], line: null },
    { id: 3, field: 'IngredientAmount', current: ['beef chuck'], suggested: ['2 lb beef chuck, cubed'], line: 0 },
    { id: 4, field: 'Instructions', current: [], suggested: ['Brown the beef in batches.', 'Add the barley and carrots, cover with stock.', 'Cook on low for 8 hours.'], line: null },
    { id: 5, field: 'NewIngredient', current: [], suggested: ['4 cups beef stock'], line: null },
    { id: 6, field: 'Notes', current: [], suggested: ['Keeps four days in the fridge and freezes well.'], line: null },
  ],
});

const ref = (id, name) => ({ id, name });
const HEALTH_ISSUES = ['NoPhoto', 'NoDescription', 'NoTimes', 'NoTags', 'ThinSteps', 'MissingAmounts'];

/** RecipeHealthResponse: a library with gaps, a pair of recipes that share a name and a tag only one recipe uses; or all complete. */
export const recipeHealth = (complete = false) => (complete ? {
  recipes: 6, complete: 6, checks: HEALTH_ISSUES.map((issue) => ({ issue, recipes: [] })), duplicates: [], rareTags: [], cookbook: null,
} : {
  cookbook: null,
  recipes: 26, complete: 1,
  checks: HEALTH_ISSUES.map((issue) => ({
    issue,
    recipes: {
      NoPhoto: [ref(ids.stew, 'Beef and Barley Stew'), ref(ids.curry, 'Chickpea Curry'), ref(ids.oats, 'Overnight Oats'),
        ...Array.from({ length: 22 }, (_, index) => ref(`00000000-0000-4000-8000-${String(900000000 + index).padStart(12, '0')}`, `Test Recipe ${String(index + 1).padStart(2, '0')}`))],
      NoDescription: [ref(ids.curry, 'Chickpea Curry'), ref(ids.granola, 'Maple Granola')],
      ThinSteps: [ref(ids.curry, 'Chickpea Curry')],
      MissingAmounts: [ref(ids.crumble, 'Pear and Ginger Crumble')],
    }[issue] ?? [],
  })),
  duplicates: [{ recipes: [ref(ids.oats, 'Overnight Oats'), ref('00000000-0000-4000-8000-0000000000c1', 'overnight oats!')] }],
  rareTags: [{ tag: 'veggie', recipe: ref(ids.curry, 'Chickpea Curry') }],
});

/** RecipeHealthResponse for the Weeknight cookbook's two recipes: the curry is missing things, the tacos are complete. */
export const weeknightHealth = () => ({
  recipes: 2, complete: 1, cookbook: ref(ids.weeknight, 'Weeknight'),
  checks: HEALTH_ISSUES.map((issue) => ({
    issue, recipes: ['NoPhoto', 'NoDescription', 'ThinSteps', 'MissingAmounts'].includes(issue) ? [ref(ids.curry, 'Chickpea Curry')] : [],
  })),
  duplicates: [], rareTags: [],
});
