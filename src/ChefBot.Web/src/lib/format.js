export const SLOT_TITLES = { Dinner: 'Dinners', Breakfast: 'Breakfasts', Snack: 'Snacks', Sweet: 'Sweets', Drink: 'Drinks' };
export const DAYS = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

/** Food groups and their titles, in the order they are walked through in a shop. */
export const FOOD_GROUPS = [
  ['Vegetable', 'Vegetables'], ['Fruit', 'Fruit'], ['Herb', 'Herbs and aromatics'], ['Protein', 'Meat, fish, eggs and beans'],
  ['Dairy', 'Dairy'], ['WholeGrain', 'Grains'], ['Other', 'Everything else'], ['Staple', 'Staples'],
];

/** The parts of a store (the API's StoreArea), in the order most shoppers walk a supermarket. The shopping list groups by them. */
export const STORE_AREAS = [
  ['Produce', 'Fruit and vegetables'], ['Bakery', 'Bakery'], ['MeatAndSeafood', 'Meat and seafood'],
  ['DairyAndChilled', 'Dairy, eggs and chilled'], ['GrainsAndPasta', 'Rice, grains and pasta'], ['CannedAndDry', 'Canned and dry goods'],
  ['SaucesAndOils', 'Oils, sauces and condiments'], ['International', 'International'], ['SpicesAndBaking', 'Spices and baking'],
  ['Frozen', 'Frozen'], ['Drinks', 'Drinks'], ['Other', 'Everything else'],
];

/** What a store area is called on the page, for example "Canned and dry goods". */
export const areaName = (area) => (STORE_AREAS.find(([key]) => key === area) ?? STORE_AREAS.at(-1))[1];

/** Reads a yyyy-mm-dd date as a local date, so it doesn't shift a day in time zones west of UTC. */
export function parseDate(iso) {
  const [year, month, day] = iso.split('-').map(Number);
  return new Date(year, month - 1, day);
}

/** A local date as yyyy-mm-dd, the way the API writes dates. */
export const isoDate = (date) =>
  `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;

export const todayIso = () => isoDate(new Date());

/** The yyyy-mm-dd date some days after (or before) another. */
export function addDays(iso, days) {
  const date = parseDate(iso);
  date.setDate(date.getDate() + days);
  return isoDate(date);
}

/** The Sunday starting the next week to plan: today if it is Sunday, otherwise the coming one. */
export function upcomingSunday() {
  const today = new Date();
  return addDays(isoDate(today), (7 - today.getDay()) % 7);
}

/** Which week a plan covers, in words: "This week", "Next week", or "Week of Nov 29" further out. */
export function weekName(weekStart, weekEnd) {
  const today = todayIso();
  if (weekStart <= today && today <= weekEnd) return 'This week';
  if (weekStart > today && weekStart <= addDays(today, 7)) return 'Next week';
  return `Week of ${shortDate(weekStart)}`;
}

export const dayMonth = (date) => date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });

export const shortDate = (iso) => (iso ? dayMonth(parseDate(iso)) : '');

/** How long ago a moment (an ISO time) was, briefly: "Just now", "5 min ago", "3 h ago", "Yesterday", or its date. */
export function ago(iso, now = new Date()) {
  const then = new Date(iso);
  const minutes = Math.floor((now - then) / 60000);
  if (minutes < 1) return 'Just now';
  if (minutes < 60) return `${minutes} min ago`;
  if (minutes < 24 * 60 && then.getDate() === now.getDate()) return `${Math.floor(minutes / 60)} h ago`;
  const yesterday = new Date(now);
  yesterday.setDate(now.getDate() - 1);
  if (then.toDateString() === yesterday.toDateString()) return 'Yesterday';
  return dayMonth(then);
}

/**
 * A date in full, with the time when it is known: "Saturday, November 21, 2026 at 6:42 PM", or
 * "Saturday, November 21, 2026" when there is only a yyyy-mm-dd date.
 */
export function fullDate(iso, at) {
  if (at) return new Date(at).toLocaleString(undefined, { dateStyle: 'full', timeStyle: 'short' });
  return iso ? parseDate(iso).toLocaleDateString(undefined, { dateStyle: 'full' }) : '';
}

/**
 * "600 g", or with the amounts that couldn't be added to it (`more`, a line's `moreAmounts`) "2 tbsp + 100 g".
 * Empty when no amount is known.
 */
export function amount(quantity, unit, more = []) {
  const one = (q, u) => {
    const rounded = Math.round(q * 100) / 100;
    return u ? `${rounded} ${u}` : `${rounded}`;
  };
  return [quantity == null ? null : one(quantity, unit), ...(more ?? []).map((extra) => one(extra.quantity, extra.unit))]
    .filter(Boolean)
    .join(' + ');
}

/**
 * Several lines' amounts as one line's: amounts in the same unit are added, the rest kept apart. Each of `lines` has
 * { quantity, unit, moreAmounts }; the answer is { quantity, unit, moreAmounts } for the first amount and the rest.
 */
export function addAmounts(lines) {
  const totals = [];
  for (const line of lines) {
    for (const part of [{ quantity: line.quantity, unit: line.unit }, ...(line.moreAmounts ?? [])]) {
      if (part.quantity == null) continue;
      const same = totals.find((total) => total.unit === (part.unit || ''));
      if (same) same.quantity += part.quantity;
      else totals.push({ quantity: part.quantity, unit: part.unit || '' });
    }
  }
  const [first, ...rest] = totals;
  return { quantity: first?.quantity ?? null, unit: first?.unit ?? lines[0]?.unit ?? '', moreAmounts: rest };
}

/** "45 min", "1 h", "1 h 10 min". */
export function duration(minutes) {
  const hours = Math.floor(minutes / 60);
  const rest = minutes % 60;
  return [hours ? `${hours} h` : '', rest ? `${rest} min` : ''].filter(Boolean).join(' ');
}

/**
 * A warning about a pantry item's date: { tone, text } such as { tone: 'warn', text: '3 days left' }, or null when it
 * is fresh or has no date, since those need no attention.
 */
export function freshness(item) {
  const days = item.daysLeft;
  if (item.freshness === 'Expired') {
    const ago = days == null ? null : -days;
    return { tone: 'bad', text: ago == null || ago <= 0 ? 'Expired' : `Expired ${ago === 1 ? 'yesterday' : `${ago} days ago`}` };
  }
  if (item.freshness === 'ExpiringSoon') {
    return { tone: 'warn', text: days == null ? 'Use soon' : days <= 0 ? 'Use today' : days === 1 ? 'Use by tomorrow' : `${days} days left` };
  }
  return null;
}

/** "3 items", "1 item". */
export const plural = (count, word) => `${count} ${word}${count === 1 ? '' : 's'}`;

/** "Jamie's", or "James'" for a name that ends in s. */
export const possessive = (name) => (/s$/i.test(name) ? `${name}'` : `${name}'s`);

/** Whose something is, for several people: "Jamie's", "Jamie and Sam's", "Jamie, Sam and Chris'". */
export function owners(names) {
  if (names.length === 0) return '';
  const last = possessive(names[names.length - 1]);
  return names.length === 1 ? last : `${names.slice(0, -1).join(', ')} and ${last}`;
}

/** "Jamie and Sam's favourite", from names in the order they favourited it. */
export const favouriteOf = (names) => `${owners(names)} favourite`;

/**
 * What a recipe can be missing (RecipeIssue): a heading for the Recipe health report, the words for one recipe's
 * page ("Missing a photo and times"), and how to fix it.
 */
export const RECIPE_ISSUES = {
  NoPhoto: { title: 'No photo', words: 'a photo', fix: 'Add one from the recipe page, or from its ⋯ menu.' },
  NoDescription: { title: 'No description', words: 'a description', fix: 'A sentence or two helps choose between recipes.' },
  NoTimes: { title: 'No times', words: 'times', fix: 'Without a time, the recipe is left out of the Ready within filter.' },
  NoTags: { title: 'No tags', words: 'tags', fix: 'Tags make recipes easier to find and filter.' },
  ThinSteps: { title: 'Short or missing steps', words: 'clear steps', fix: 'Cooking mode needs steps to walk through.' },
  MissingAmounts: { title: 'Ingredients without amounts', words: 'ingredient amounts', fix: 'Lines without an amount can\'t be added up on the shopping list.' },
};

/** Words joined the way a sentence lists them: "a, b and c". */
export const listWords = (words) => (words.length < 2 ? words.join('') : `${words.slice(0, -1).join(', ')} and ${words[words.length - 1]}`);
