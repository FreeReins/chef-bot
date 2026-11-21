<script>
  import { app, run, toast, isAdmin, loadRecipes } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { go } from '../lib/addresses.js';
  import Chip from '../components/Chip.svelte';
  import RecipeDetail from './RecipeDetail.svelte';
  import RecipeForm from './RecipeForm.svelte';
  import FileImport from '../components/FileImport.svelte';

  const COURSES = ['Main', 'Breakfast', 'Snack', 'Sweet', 'Drink', 'Other'];

  const groups = $derived(COURSES
    .map((course) => ({ course, recipes: app.recipes.filter((recipe) => recipe.course === course) }))
    .filter((group) => group.recipes.length > 0));

  async function add(draft) {
    let saved = false;
    await run(async () => {
      await api('POST', '/api/recipes', draft);
      await loadRecipes();
      saved = true;
    });
    return saved;
  }

  let link = $state('');
  let reading = $state(false);

  async function addFromLink(event) {
    event.preventDefault();
    reading = true;
    await run(async () => {
      const { recipe, readWith } = await api('POST', '/api/recipes/import/web', { url: link });
      await loadRecipes();
      link = '';
      toast(`Added ${recipe.name} (read with: ${readWith}). Its photos will appear once they have downloaded.`);
      await go(`/recipes/${recipe.id}`);
    });
    reading = false;
  }
</script>

{#if app.recipeId}
  {#key app.recipeId}<RecipeDetail id={app.recipeId} />{/key}
{:else}
  <div class="columns">
    <section>
      <div class="row">
        <h2>Recipes ({app.recipes.length})</h2>
        <div class="row-actions">
          {#if isAdmin()}
            <a class="button small" href="/recipes/samples">Browse sample recipes</a>
          {/if}
        </div>
      </div>
      {#each groups as { course, recipes } (course)}
        <div class="card">
          <h3>{course} ({recipes.length})</h3>
          <ul class="plain">
            {#each recipes as recipe (recipe.id)}
              <li>
                <a href="/recipes/{recipe.id}"><strong>{recipe.name}</strong></a>
                {#if recipe.isSlowCooker}<Chip tone="good">Slow cooker</Chip>{/if}
                <p class="muted">{recipe.ingredients.join(', ')}</p>
              </li>
            {/each}
          </ul>
        </div>
      {/each}
    </section>
    <section>
      <h2>Add a recipe from a link</h2>
      <form class="card link" onsubmit={addFromLink}>
        <label for="recipe-link">Address of a recipe page</label>
        <div class="link-row">
          <input id="recipe-link" type="text" inputmode="url" autocomplete="url" required
                 placeholder="https://example.com/recipes/soup" bind:value={link} disabled={reading}>
          <button type="submit" disabled={reading}>{reading ? 'Reading…' : 'Add'}</button>
        </div>
        <p class="muted">Works with most recipe sites. ChefBot saves the recipe straight away; you can edit it after.</p>
      </form>
      <h2>Add a recipe</h2>
      <RecipeForm button="Save recipe" save={add} />
      <h2>Import recipe files</h2>
      <div class="card import"><FileImport /></div>
    </section>
  </div>
{/if}

<style>
  .import, .link { display: grid; gap: .5rem; }
  .link p { margin: 0; }
  .link-row { display: flex; gap: .5rem; }
  .link-row input { flex: 1; min-width: 0; }
</style>
