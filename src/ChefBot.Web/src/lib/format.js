export const SLOT_TITLES = { Dinner: 'Dinners', Breakfast: 'Breakfasts', Snack: 'Snacks', Sweet: 'Sweets', Drink: 'Drinks' };
export const DAYS = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

/** Food groups and their titles, in the order they are walked through in a shop. */
export const FOOD_GROUPS = [
  ['Vegetable', 'Vegetables'], ['Fruit', 'Fruit'], ['Herb', 'Herbs and aromatics'], ['Protein', 'Meat, fish, eggs and beans'],
  ['Dairy', 'Dairy'], ['WholeGrain', 'Grains'], ['Other', 'Everything else'], ['Staple', 'Staples'],
];

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

/**
 * A date in full, with the time when it is known: "Saturday, November 21, 2026 at 6:42 PM", or
 * "Saturday, November 21, 2026" when there is only a yyyy-mm-dd date.
 */
export function fullDate(iso, at) {
  if (at) return new Date(at).toLocaleString(undefined, { dateStyle: 'full', timeStyle: 'short' });
  return iso ? parseDate(iso).toLocaleDateString(undefined, { dateStyle: 'full' }) : '';
}

export function amount(quantity, unit) {
  if (quantity == null) return '';
  const rounded = Math.round(quantity * 100) / 100;
  return unit ? `${rounded} ${unit}` : `${rounded}`;
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
