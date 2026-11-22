import { api, upload } from './api.js';
import { app } from './state.svelte.js';
import { onLive } from './live.svelte.js';

// ---------- recipes ----------

/** Counts the library again, and tells the Recipes tab to reload its page. */
export async function loadRecipes() {
  app.recipeCount = (await api('GET', '/api/recipes/search?pageSize=1')).librarySize;
  app.recipesChanged++;
}


// A recipe was added or changed, or its photos arrived, here or on another device.
onLive('recipes', () => loadRecipes());

/** Loads one recipe for its page and gives it to `show`; null when it is gone. */
export async function loadRecipePage(id, show) {
  try {
    show(await api('GET', `/api/recipes/${id}`));
  } catch (error) {
    if (error.status !== 404) throw error;
    show(null);
  }
}

/**
 * Keeps an open recipe page up to date when its photos arrive or someone changes it on another device: loads it
 * again with `show`. Not while a dialog over the page is open (editing it, its photos, cooking), so nothing being
 * worked on is replaced. Returns the function that stops watching.
 */
export const watchRecipe = (id, base, show) => onLive('recipes', async () => {
  if (location.pathname !== base()) return;
  try { show(await api('GET', `/api/recipes/${id}`)); } catch { /* gone or offline: keep what is shown */ }
});
/**
 * Imports one recipe file. A .zip, such as a Mealie backup, is sent as it is so its photos come too; other files are
 * sent as text. Returns what happened: { added, alreadyInLibrary, skipped, photosAdded }.
 */
export async function importFile(file) {
  const result = file.name.toLowerCase().endsWith('.zip')
    ? await upload(`/api/recipes/import/archive?fileName=${encodeURIComponent(file.name)}`, file, 'application/zip')
    : await api('POST', '/api/recipes/import/file', { fileName: file.name, content: await file.text() });
  await loadRecipes();
  return result;
}

/** Loads the kinds of recipe file that can be imported, once. */
export async function loadFileFormats() {
  if (app.fileFormats.length === 0) app.fileFormats = await api('GET', '/api/recipes/import/formats');
}

/**
 * The query for one page of the library search (GET /api/recipes/search), from the Recipes tab's search and filters.
 * favourites is '' for all recipes, 'mine' (the signed-in person, `me`), 'any', or another person's id.
 */
export function searchParams({ page, pageSize, query, course, favourites, tags, slowCooker, readyWithin, withSuggestions, me }) {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  if (query) params.set('query', query);
  if (course) params.set('course', course);
  if (favourites === 'any') params.set('favourites', 'true');
  else if (favourites) params.set('favouredBy', favourites === 'mine' ? me : favourites);
  for (const tag of tags) params.append('tag', tag);
  if (slowCooker) params.set('slowCooker', 'true');
  if (withSuggestions) params.set('withSuggestions', 'true');
  if (readyWithin) params.set('readyWithin', readyWithin);
  return params;
}
