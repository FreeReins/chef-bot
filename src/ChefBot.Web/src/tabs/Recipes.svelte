<script>
  import { app, run, isAdmin, importFromMealie, loadRecipes } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import Chip from '../components/Chip.svelte';
  import RecipeDetail from './RecipeDetail.svelte';
  import RecipeForm from './RecipeForm.svelte';
  import FileImport from '../components/FileImport.svelte';

  const COURSES = ['Main', 'Breakfast', 'Snack', 'Sweet', 'Other'];

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
          {#if app.session.mealieConfigured}
            <button class="small" onclick={importFromMealie}>Import from Mealie</button>
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
                {#if recipe.origin === 'Mealie'}<Chip>From Mealie</Chip>{/if}
                <p class="muted">{recipe.ingredients.join(', ')}</p>
              </li>
            {/each}
          </ul>
        </div>
      {/each}
    </section>
    <section>
      <h2>Add a recipe</h2>
      <RecipeForm button="Save recipe" save={add} />
      <h2>Import recipe files</h2>
      <div class="card import"><FileImport /></div>
    </section>
  </div>
{/if}

<style>
  .import { display: grid; gap: .5rem; }
</style>
