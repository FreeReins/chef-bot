// The wording of the History tab. The server says what happened (HistoryMoment, HistoryChapter); this picks how to
// say it. Each kind has a few ways to say it, and a moment always gets the same one, chosen from a hash of what it
// is, so the page reads the same every time it is opened. Add a line to a list to give a kind another way to say it.
import { plural, owners } from './format.js';

/**
 * A caption is a list of parts: plain text, or { dish, recipeId } for a name the page links to its recipe when the
 * recipe is still in the library.
 */
const dish = (moment) => ({ dish: moment.title, recipeId: moment.recipeId });

/** A review's stars in words: "4 stars", or "a perfect 5 stars". */
const stars = (moment) => (moment.review.stars === 5 ? 'a perfect 5 stars' : plural(moment.review.stars, 'star'));

const CAPTIONS = {
  Debut: [
    (m) => [dish(m), ' made its dinner-table debut.'],
    (m) => ['First time ever: ', dish(m), '. The critics are cautiously optimistic.'],
    (m) => [dish(m), ' stepped out of the cookbook and onto a plate.'],
  ],
  FanFavourite: [
    (m) => [dish(m), ` achieved legendary status: cooked ${m.count} times.`],
    (m) => [`${m.count} times and counting. `, dish(m), ' has a fan club now.'],
    (m) => [dish(m), ` hit ${m.count} cooks. Someone should frame the recipe.`],
  ],
  Comeback: [
    (m) => ['The return of ', dish(m), `, after ${m.count} days in exile.`],
    (m) => [dish(m), ` is back after ${m.count} days. It never really left our hearts.`],
    (m) => [`After ${m.count} days on the bench, `, dish(m), ' got called back up.'],
  ],
  FavouriteCooked: [
    (m) => [`Cooked ${owners(m.favouredBy)} favourite recipe: `, dish(m), '.'],
    (m) => [dish(m), ` was on the table. That's ${owners(m.favouredBy)} favourite, so someone was smiling.`],
    (m) => [`Someone made ${owners(m.favouredBy)} day: `, dish(m), ' was on the menu.'],
  ],
  DinnerMilestone: [
    (m) => [`Dinner number ${m.count}: `, dish(m), '. Somebody get a cake.'],
    (m) => [`${m.count} dinners cooked! `, dish(m), ' had the honour.'],
  ],
  WeekOfPlates: [
    (m) => [m.count === 1 ? 'Just the one plate this week, but it counted.' : `A good week: ${plural(m.count, 'plate')} cleared.`],
    (m) => [m.count === 1 ? 'A quiet week, one trusty dinner.' : `The weekly spread: ${m.count} dishes on the table.`],
    (m) => [m.count === 1 ? 'One plate, zero regrets.' : `Another week, another stack of ${m.count} dishes to wash.`],
  ],
  RecipeAdded: [
    (m) => [dish(m), ' moved into the cookbook.'],
    (m) => ['A wild ', dish(m), ' appeared in the recipe box.'],
    (m) => [dish(m), ' joined the cookbook. Welcome aboard.'],
  ],
  RecipeStampede: [
    (m) => [`A stampede of ${m.count} recipes thundered into the cookbook.`],
    (m) => [`${m.count} recipes arrived at once. The cookbook needs a bigger shelf.`],
  ],
  RecipeRetired: [
    (m) => [dish(m), ' hung up its apron.'],
    (m) => [dish(m), ' retired to a nice farm upstate.'],
    (m) => [dish(m), ' left the cookbook to pursue other interests.'],
  ],
  Reviewed: [
    (m) => [`${m.review.reviewedBy} gave `, dish(m), ` ${stars(m)}.`],
    (m) => [`The verdict on `, dish(m), ` from ${m.review.reviewedBy}: ${stars(m)}.`],
    (m) => [dish(m), ` earned ${stars(m)} from ${m.review.reviewedBy}.`],
  ],
  Haul: [
    (m) => [`A heroic grocery haul: ${plural(m.count, 'thing')} carried home.`],
    (m) => [`The great shop: ${plural(m.count, 'item')} bagged in one go.`],
  ],
  KitchenOpened: [
    (m) => [`The kitchen opened. ${m.title} lit the stove for the first time.`],
  ],
  PersonJoined: [
    (m) => [`${m.title} pulled up a chair.`],
    (m) => [`${m.title} joined the household. More plates, please.`],
  ],
};

/** The picture on each kind's plate. */
const EMOJI = {
  Debut: '🎉', FanFavourite: '🏆', FavouriteCooked: '❤️', Comeback: '🪃', DinnerMilestone: '🎂', WeekOfPlates: '🍽️', RecipeAdded: '📖',
  RecipeStampede: '🐘', RecipeRetired: '🏖️', Haul: '🛒', KitchenOpened: '🔥', PersonJoined: '🪑', Reviewed: '⭐',
};

/** A small, steady number from text, so the same moment always picks the same wording. */
function hash(text) {
  let value = 0;
  for (const char of text) value = (value * 31 + char.codePointAt(0)) >>> 0;
  return value;
}

/** How a moment is told: { emoji, parts }. */
export function tell(moment) {
  const choices = CAPTIONS[moment.kind] ?? [() => ['Something delicious happened.']];
  const pick = choices[hash(`${moment.kind}|${moment.date}|${moment.title ?? ''}|${moment.count ?? ''}`) % choices.length];
  return { emoji: EMOJI[moment.kind] ?? '🥄', parts: pick(moment) };
}

/** A caption as plain text, for the moment's accessible name and for tests. */
export const captionText = (parts) => parts.map((part) => (typeof part === 'string' ? part : part.dish)).join('');

const titleCase = (text) => text.replace(/\b\p{L}/gu, (letter) => letter.toUpperCase());

/** A chapter's title, from what was eaten that month. */
export function chapterTitle(chapter) {
  if (chapter.topDish && chapter.topDishCooks >= 3) return `The ${chapter.topDish} Era`;
  if (chapter.topTag) return `The Month of ${titleCase(chapter.topTag)}`;
  if (chapter.dinners === 0) {
    return chapter.moments.some((moment) => moment.kind === 'RecipeAdded' || moment.kind === 'RecipeStampede')
      ? 'The Month of Grand Plans' : 'Between Meals';
  }
  if (chapter.dinners < 5) return 'A Quiet Month in the Kitchen';
  if (chapter.dinners >= 25) return 'The Month the Stove Never Cooled';
  return 'A Month of Good Eating';
}

/** "March 2026", from the chapter's yyyy-mm-01. */
export function monthName(iso) {
  const [year, month] = iso.split('-').map(Number);
  return new Date(year, month - 1, 1).toLocaleDateString(undefined, { month: 'long', year: 'numeric' });
}
