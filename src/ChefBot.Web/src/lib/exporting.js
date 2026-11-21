// Getting recipes and shopping lists out of ChefBot: as plain text to share or paste, as a CSV file, or on paper.
// The formatters are plain functions of what the screens already hold; printing itself is styles.css's @media print.
import { amount, duration, plural } from './format.js';

/** True when the browser can hand text to the phone's share sheet; otherwise sharing copies it instead. */
export const canShare = () => typeof navigator.share === 'function';

/**
 * Sends text to the share sheet, or copies it where there is none. Returns 'shared', 'copied' or 'cancelled' (the
 * share sheet was closed), so the caller can say what happened.
 */
export async function share(title, text) {
  if (canShare()) {
    try {
      await navigator.share({ title, text });
      return 'shared';
    } catch (error) {
      if (error.name === 'AbortError') return 'cancelled';
      // Some browsers have the share sheet but refuse it here, for example without a click; copy instead.
    }
  }
  await copyText(text);
  return 'copied';
}

/** Puts text on the clipboard. Browsers only allow it over a secure connection, so this says so when they refuse. */
export async function copyText(text) {
  if (!navigator.clipboard?.writeText) {
    throw new Error('This browser can only copy over a secure connection. Open ChefBot over HTTPS, or select the text and copy it yourself.');
  }
  await navigator.clipboard.writeText(text);
}

/** Saves text as a file, named `name`, through the browser's usual download. */
export function download(name, type, text) {
  const url = URL.createObjectURL(new Blob([text], { type }));
  const link = Object.assign(document.createElement('a'), { href: url, download: name });
  document.body.append(link);
  link.click();
  link.remove();
  setTimeout(() => URL.revokeObjectURL(url), 0);
}

/** A RecipeResponse as plain text: what it makes, its ingredients, numbered steps, then notes and where it came from. */
export function recipeText(recipe) {
  const facts = [
    `Serves ${recipe.servings}`,
    ...[['Prep', recipe.prepMinutes], ['Cook', recipe.cookMinutes], ['Total', recipe.totalMinutes]]
      .filter(([, minutes]) => minutes)
      .map(([label, minutes]) => `${label} ${duration(minutes)}`),
  ];
  const parts = [[recipe.name, facts.join(' · ')].join('\n')];
  if (recipe.description) parts.push(recipe.description);
  parts.push(['Ingredients', ...recipe.ingredients.map((line) => `- ${line}`)].join('\n'));
  if (recipe.instructions.length) {
    parts.push(['Steps', ...recipe.instructions.map((step, index) => `${index + 1}. ${step}`)].join('\n'));
  }
  if (recipe.notes) parts.push(`Notes\n${recipe.notes}`);
  const credits = [
    recipe.yieldText && `Makes ${recipe.yieldText}`,
    recipe.author && `By ${recipe.author}`,
    recipe.sourceUrl && `Source: ${recipe.sourceUrl}`,
  ].filter(Boolean);
  if (credits.length) parts.push(credits.join('\n'));
  return `${parts.join('\n\n')}\n`;
}

/** "fish fillets, 600 g (optional)": one shopping line as words. */
function item(line) {
  const howMuch = amount(line.quantity, line.unit);
  return `${line.name}${howMuch ? `, ${howMuch}` : ''}${line.isOptional ? ' (optional)' : ''}`;
}

/**
 * The shopping list as a checklist to paste into notes or a message. `sections` are the Shopping tab's:
 * { title, lines } in the order they are shown, one per store and then per food group.
 */
export function shoppingText(title, sections) {
  const count = sections.reduce((total, section) => total + section.lines.length, 0);
  const groups = sections.map((section) => [section.title, ...section.lines.map((line) => `☐ ${item(line)}`)].join('\n'));
  return `${[`${title} (${plural(count, 'item')})`, ...groups].join('\n\n')}\n`;
}

/** One CSV field, quoted when it needs to be (RFC 4180). */
function field(value) {
  const text = value == null ? '' : String(value);
  return /[",\r\n]/.test(text) ? `"${text.replaceAll('"', '""')}"` : text;
}

/**
 * The shopping list as CSV, one row per item, for a spreadsheet. It starts with a byte-order mark so Excel reads it
 * as UTF-8, and ends lines with CRLF as RFC 4180 asks.
 */
export function shoppingCsv(sections) {
  const rows = [['Name', 'Quantity', 'Unit', 'Store', 'Food group', 'Optional', 'For']];
  for (const section of sections) {
    for (const line of section.lines) {
      rows.push([line.name, line.quantity, line.unit, line.store, line.group, line.isOptional ? 'yes' : '', line.usedIn.join('; ')]);
    }
  }
  return `\uFEFF${rows.map((row) => row.map(field).join(',')).join('\r\n')}\r\n`;
}
