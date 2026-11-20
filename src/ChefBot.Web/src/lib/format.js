export const SLOT_TITLES = { Dinner: 'Dinners', Breakfast: 'Breakfasts', Snack: 'Snacks', Sweet: 'Sweets' };
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

export const dayMonth = (date) => date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });

export const shortDate = (iso) => (iso ? dayMonth(parseDate(iso)) : '');

export function amount(quantity, unit) {
  if (quantity == null) return '';
  const rounded = Math.round(quantity * 100) / 100;
  return unit ? `${rounded} ${unit}` : `${rounded}`;
}
