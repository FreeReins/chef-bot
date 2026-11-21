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

export function amount(quantity, unit) {
  if (quantity == null) return '';
  const rounded = Math.round(quantity * 100) / 100;
  return unit ? `${rounded} ${unit}` : `${rounded}`;
}
