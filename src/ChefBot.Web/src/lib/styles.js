// Plan styles in plain words: the choices a rule offers, and one-line summaries of a style and its rules. The
// shapes are PlanStyleDetails and PlanRuleDetails (src/ChefBot.Core/Managers/Contracts/Messages/).
import { DAYS } from './format.js';

export const LIMITS = [['AtLeast', 'At least'], ['AtMost', 'At most'], ['Exactly', 'Exactly'], ['None', 'No']];

/** The kinds of meal a rule can count, plural as they read in a rule. '' stands for every kind of meal. */
export const RULE_SLOTS = [['Dinner', 'dinners'], ['Breakfast', 'breakfasts'], ['Snack', 'snacks'], ['Sweet', 'sweets'], ['Drink', 'drinks'], ['', 'meals of any kind']];

export const MATCHES = [['SlowCooker', 'slow cooker'], ['Tag', 'tagged'], ['Ingredient', 'made with'], ['ReadyWithin', 'ready within'], ['Favourite', "someone's favourite"]];

/** How much a style leans on favourite recipes, with what each choice means. */
export const FAVOURITE_LEANS = [
  ['Off', 'Not at all', 'Favourites are planned like any other recipe.'],
  ['ALittle', 'A little', 'Favourites get a nudge, about as much as produce in season.'],
  ['ALot', 'A lot', 'Favourites come first, after anything planned recently.'],
];

const SHORT_DAYS = DAYS.map((day) => day.slice(0, 3));

/** A new rule, as "Add rule" makes it: at least one slow cooker dinner. */
export const newRule = () => ({ limit: 'AtLeast', count: 1, slot: 'Dinner', match: 'SlowCooker', values: [], minutes: null, day: null });

/** A new style of the household's own, before it is changed: four dinners spaced for leftover lunches. */
export const newStyle = (name) => ({
  id: null, name, description: '', isDefault: false, cookingDays: ['Sunday', 'Tuesday', 'Thursday', 'Saturday'],
  leftoverLunches: true, breakfasts: 0, snacks: 0, sweets: 0, drinks: 0, rules: [], favourites: 'ALittle',
});

/** A copy of a style that can be changed without touching the original. */
export const copyStyle = (style) => ({ ...style, cookingDays: [...style.cookingDays], rules: style.rules.map((rule) => ({ ...rule, values: [...(rule.values ?? [])] })) });

const plural = (count, one, many = `${one}s`) => `${count} ${count === 1 ? one : many}`;

/** "chicken", "chicken or beef", "chicken, beef or 3 more". */
function either(values) {
  if (values.length <= 1) return values[0] ?? 'anything';
  if (values.length <= 3) return `${values.slice(0, -1).join(', ')} or ${values.at(-1)}`;
  return `${values[0]}, ${values[1]} or ${values.length - 2} more`;
}

/** What a rule looks for: "slow cooker", "tagged quick", "made with chicken", "ready within 30 minutes". */
export function matchText(rule) {
  switch (rule.match) {
    case 'SlowCooker': return 'slow cooker';
    case 'Tag': return `tagged ${either(rule.values ?? [])}`;
    case 'Ingredient': return `made with ${either(rule.values ?? [])}`;
    case 'Favourite': return "someone's favourite";
    default: return `ready within ${rule.minutes ?? '?'} minutes`;
  }
}

/** A rule as a sentence: "At least 1 dinner slow cooker, on Tuesday", "No meals made with beef". */
export function ruleText(rule) {
  const slot = RULE_SLOTS.find(([key]) => key === (rule.slot ?? ''))?.[1] ?? 'meals';
  const what = matchText(rule);
  if (rule.limit === 'None') return `No ${rule.slot ? slot : 'meals'} ${what}`;
  const limit = LIMITS.find(([key]) => key === rule.limit)?.[1] ?? '';
  const noun = rule.count === 1 ? slot.replace(/s$/, '') : slot;
  return `${limit} ${rule.count} ${noun} ${what}${rule.day ? `, on ${rule.day}` : ''}`;
}

/** What a week in this style holds, in one line: "Dinners Sun, Tue, Thu, Sat · leftovers for lunch · 2 breakfasts". */
export function styleSummary(style) {
  const days = style.cookingDays.length
    ? `Dinners ${DAYS.filter((day) => style.cookingDays.includes(day)).map((day) => SHORT_DAYS[DAYS.indexOf(day)]).join(', ')}`
    : 'No dinners';
  const parts = [days];
  if (style.cookingDays.length && style.leftoverLunches) parts.push('leftovers for lunch');
  if (style.breakfasts) parts.push(plural(style.breakfasts, 'breakfast'));
  if (style.snacks) parts.push(plural(style.snacks, 'snack'));
  if (style.sweets) parts.push(plural(style.sweets, 'sweet'));
  if (style.drinks) parts.push(plural(style.drinks, 'drink'));
  if (style.favourites === 'ALot') parts.push('favourites first');
  else if (style.favourites === 'Off') parts.push('favourites not preferred');
  return parts.join(' · ');
}

/** The style with this id, or the household's default, or the first starter style when none are saved. */
export function defaultStyle(styles) {
  const saved = styles?.styles ?? [];
  return saved.find((style) => style.isDefault) ?? saved[0] ?? styles?.known?.[0] ?? null;
}

/** The styles to choose from when planning: the household's, or the starter styles until any are saved. */
export const choosableStyles = (styles) => ((styles?.styles?.length ?? 0) > 0 ? styles.styles : (styles?.known ?? []));

const whole = (value) => (Number.isFinite(Number(value)) && value !== null && value !== '' ? Math.trunc(Number(value)) : 0);

/** Styles as the API takes them: an emptied number box counts as 0, and names lose stray spaces. */
export const cleanStyles = (styles) => styles.map((style) => ({
  ...style,
  name: style.name.trim(),
  breakfasts: whole(style.breakfasts),
  snacks: whole(style.snacks),
  sweets: whole(style.sweets),
  drinks: whole(style.drinks),
  rules: style.rules.map((rule) => ({ ...rule, count: whole(rule.count), minutes: rule.match === 'ReadyWithin' ? whole(rule.minutes) : null })),
}));
