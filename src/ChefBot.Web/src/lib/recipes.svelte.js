import { api, upload } from './api.js';
import { app } from './state.svelte.js';
import { refreshAchievements } from './achievements.svelte.js';

// ---------- recipes ----------

/** Counts the library again, and tells the Recipes tab to reload its page. */
export async function loadRecipes() {
  app.recipeCount = (await api('GET', '/api/recipes/search?pageSize=1')).librarySize;
  app.recipesChanged++;
  refreshAchievements();
}

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
