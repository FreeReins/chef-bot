// Making a recipe for more or fewer people: each ingredient's amount is scaled and written the way a cook reads it.
// Metric weights and volumes are rounded to sensible steps (450 g, 1.25 kg); everything else becomes a kitchen
// fraction to the nearest eighth or third (1½ cups, ⅓ tsp). Lines whose amount ChefBot couldn't read stay as written.

const METRIC = new Set(['g', 'gram', 'grams', 'kg', 'ml', 'millilitre', 'millilitres', 'milliliter', 'milliliters', 'l', 'litre', 'litres', 'liter', 'liters', 'cl', 'dl']);
const LARGE_METRIC = new Set(['kg', 'l', 'litre', 'litres', 'liter', 'liters']);
const FRACTIONS = [[0, ''], [1 / 8, '⅛'], [1 / 4, '¼'], [1 / 3, '⅓'], [3 / 8, '⅜'], [1 / 2, '½'], [5 / 8, '⅝'], [2 / 3, '⅔'],
  [3 / 4, '¾'], [7 / 8, '⅞'], [1, '']];
const GLYPHS = { '½': 1 / 2, '⅓': 1 / 3, '⅔': 2 / 3, '¼': 1 / 4, '¾': 3 / 4, '⅛': 1 / 8, '⅜': 3 / 8, '⅝': 5 / 8, '⅞': 7 / 8 };
/** A number at the start of a line: "2", "1.5", "1/2", "1 1/2", "1½", "½", and a range of two: "2-3", "2 to 3". */
const NUMBER = String.raw`(?:\d+\s+\d+\/\d+|\d+\/\d+|\d+[½⅓⅔¼¾⅛⅜⅝⅞]|\d*[.,]\d+|\d+|[½⅓⅔¼¾⅛⅜⅝⅞])`;
const LEADING = new RegExp(String.raw`^(\s*)(${NUMBER})(?:(\s*(?:-|–|to)\s*)(${NUMBER}))?`);

/** Reads one written number: "1 1/2", "1½", "0.5" or "3/4". */
function read(text) {
  const clean = text.trim().replace(',', '.');
  const glyph = clean.match(/^(\d*)([½⅓⅔¼¾⅛⅜⅝⅞])$/);
  if (glyph) return Number(glyph[1] || 0) + GLYPHS[glyph[2]];
  const mixed = clean.match(/^(\d+)\s+(\d+)\/(\d+)$/);
  if (mixed) return Number(mixed[1]) + Number(mixed[2]) / Number(mixed[3]);
  const fraction = clean.match(/^(\d+)\/(\d+)$/);
  if (fraction) return Number(fraction[1]) / Number(fraction[2]);
  return Number(clean);
}

/** An amount as a cook writes it for this unit: "450", "1.25", "1½", "⅓". */
export function niceNumber(value, unit = '') {
  if (!(value > 0)) return '0';
  const metric = METRIC.has(unit.trim().toLowerCase());
  if (metric) {
    const large = LARGE_METRIC.has(unit.trim().toLowerCase());
    const step = large ? (value < 10 ? 0.05 : 0.5) : value < 10 ? 0.5 : value < 100 ? 1 : value < 1000 ? 5 : 10;
    const rounded = Math.max(step, Math.round(value / step) * step);
    return String(Math.round(rounded * 100) / 100);
  }
  if (value >= 10) return String(Math.round(value * 2) / 2).replace(/\.5$/, '½');
  let whole = Math.floor(value);
  const [part, glyph] = FRACTIONS.reduce((best, entry) =>
    (Math.abs(value - whole - entry[0]) < Math.abs(value - whole - best[0]) ? entry : best));
  if (part === 1) whole += 1;
  if (whole === 0 && !glyph) return '⅛';
  return `${whole || ''}${glyph}` || '0';
}

/** An amount and its unit, scaled: "1½ cups", "450 g". */
export const niceAmount = (quantity, unit = '') => `${niceNumber(quantity, unit)}${unit ? ` ${unit}` : ''}`;

/**
 * Each ingredient line of `recipe`, for `serves` servings: { text, amount, rest, scaled }. `amount` is the part to
 * stress (the number, scaled when it could be), `rest` what follows it, and `scaled` false when the line stays as
 * written because it gives no amount ChefBot could read.
 */
export function scaledLines(recipe, serves) {
  const factor = serves / (recipe.servings || serves || 1);
  const details = recipe.ingredientDetails?.length === recipe.ingredients.length ? recipe.ingredientDetails : [];
  return recipe.ingredients.map((line, index) => {
    const detail = details[index];
    const found = line.match(LEADING);
    if (found) {
      const [whole, space, first, joiner, second] = found;
      const unit = detail?.unit ?? '';
      const amount = factor === 1 ? `${first}${joiner ?? ''}${second ?? ''}`
        : `${niceNumber(read(first) * factor, unit)}${second ? `${joiner}${niceNumber(read(second) * factor, unit)}` : ''}`;
      const rest = line.slice(whole.length);
      return { text: `${space}${amount}${rest}`.trim(), amount, rest, scaled: true };
    }
    if (detail?.quantity != null) {
      const amount = niceAmount(detail.quantity * factor, detail.unit);
      const rest = ` ${detail.name}${detail.note ? `, ${detail.note}` : ''}`;
      return { text: `${amount}${rest}`, amount, rest, scaled: true };
    }
    return { text: line, amount: '', rest: line, scaled: factor === 1 };
  });
}

/** The servings a recipe page is for: `?serves=` in the address when it is a sensible number, else the recipe's own. */
export function servesFrom(search, servings) {
  const asked = Math.round(Number(new URLSearchParams(search).get('serves')));
  return asked >= 1 && asked <= 50 ? asked : servings;
}
