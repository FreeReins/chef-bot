// Cookbooks: named groups of recipes. A recipe can be in any number of them, or none. The page shows nothing about
// cookbooks until someone makes one, apart from "Add to cookbook" on a recipe's page, which is how the first is made.
import { api } from './api.js';
import { loadRecipes } from './recipes.svelte.js';

/** Every cookbook, by name, with how many recipes are in each (CookbookResponse). */
export const loadCookbooks = () => api('GET', '/api/cookbooks');

/** Makes a cookbook and returns it; the library reloads, so cookbook filters appear once the first one exists. */
export async function createCookbook(name, description = null) {
  const made = await api('POST', '/api/cookbooks', { name, description });
  await loadRecipes();
  return made;
}

/** Renames a cookbook, or changes its description. */
export async function updateCookbook(id, name, description) {
  const saved = await api('PUT', `/api/cookbooks/${id}`, { name, description });
  await loadRecipes();
  return saved;
}

/** Removes a cookbook. Its recipes stay in the library. */
export async function removeCookbook(id) {
  await api('DELETE', `/api/cookbooks/${id}`);
  await loadRecipes();
}

/** Puts recipes in a cookbook, or takes one recipe out, without reloading the library. */
export const addToCookbook = (id, recipeIds) => api('POST', `/api/cookbooks/${id}/recipes`, { recipeIds });
export const removeFromCookbook = (id, recipeId) => api('DELETE', `/api/cookbooks/${id}/recipes/${recipeId}`);
