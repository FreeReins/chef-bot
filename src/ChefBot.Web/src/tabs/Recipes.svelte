<script>
  import { untrack } from 'svelte';
  import { app, run, toast, isAdmin, loadRecipes } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { go } from '../lib/addresses.js';
  import Chip from '../components/Chip.svelte';
  import RecipeDetail from './RecipeDetail.svelte';
  import RecipeForm from './RecipeForm.svelte';
  import FileImport from '../components/FileImport.svelte';
  import LoaderCircle from '@lucide/svelte/icons/loader-circle';

  const COURSES = ['Main', 'Breakfast', 'Snack', 'Sweet', 'Drink', 'Other'];
  const PAGE_SIZE = 25;
  /** How long typing pauses before the search runs, in milliseconds. */
  const TYPING_PAUSE = 250;

  let typed = $state('');   // what is in the search box
  let query = $state('');   // what was searched for, once typing paused
  let course = $state('');  // '' for every course
  let page = $state(1);
  let result = $state(null); // RecipePageResponse for the page shown
  let listTop = $state();

  let typingTimer;
  function onType() {
    clearTimeout(typingTimer);
    typingTimer = setTimeout(() => { query = typed.trim(); page = 1; }, TYPING_PAUSE);
  }

  // Fetches the page again whenever the search, course or page changes, or the library does. An answer that arrives
  // after a newer request was sent is dropped.
  let latest = 0;
  $effect(() => {
    if (app.recipeId) return;
    void app.recipesChanged;
    const params = new URLSearchParams({ page: String(page), pageSize: String(PAGE_SIZE) });
    if (query) params.set('query', query);
    if (course) params.set('course', course);
    const ticket = ++latest;
    // run() clears old messages as it starts; untracked, so that doesn't make this effect run again.
    untrack(() => run(async () => {
      const found = await api('GET', `/api/recipes/search?${params}`);
      if (ticket !== latest) return;
      // After an archive the page asked for can be past the end; step back to the last one.
      if (found.recipes.length === 0 && found.total > 0) page = Math.ceil(found.total / found.pageSize);
      else result = found;
    }));
  });

  const librarySize = $derived(result?.librarySize ?? app.recipeCount);
  const filtering = $derived(query !== '' || course !== '');
  const pageCount = $derived(result ? Math.max(1, Math.ceil(result.total / result.pageSize)) : 1);
  const groups = $derived(COURSES
    .map((name) => ({ course: name, recipes: (result?.recipes ?? []).filter((recipe) => recipe.course === name) }))
    .filter((group) => group.recipes.length > 0));

  function turnTo(next) {
    page = next;
    listTop?.scrollIntoView({ block: 'start' });
  }

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
    <section bind:this={listTop}>
      <div class="row">
        <h2>Recipes ({librarySize})</h2>
        <div class="row-actions">
          {#if isAdmin()}
            <a class="button small" href="/recipes/samples">Browse sample recipes</a>
          {/if}
        </div>
      </div>
      {#if librarySize > 0}
        <div class="card filters" role="search" aria-label="Find recipes">
          <label class="grow">Search
            <input id="recipe-search" type="search" placeholder="Name, ingredient or tag" bind:value={typed} oninput={onType}>
          </label>
          <label>Course
            <select id="recipe-course" bind:value={course} onchange={() => (page = 1)}>
              <option value="">All courses</option>
              {#each COURSES as name (name)}<option value={name}>{name}</option>{/each}
            </select>
          </label>
          <p class="muted" aria-live="polite">
            {#if result && filtering}{result.total} {result.total === 1 ? 'recipe matches' : 'recipes match'}{/if}
          </p>
        </div>
      {/if}
      {#if !result}
        <p class="loading muted" role="status" aria-label="Loading recipes"><LoaderCircle size={24} /></p>
      {:else if result.total === 0 && librarySize > 0}
        <p class="muted">No recipes match. Try fewer words or another course.</p>
      {/if}
      {#each groups as { course: heading, recipes } (heading)}
        <div class="card">
          <h3>{heading} ({recipes.length})</h3>
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
      {#if pageCount > 1}
        <nav class="row pager" aria-label="Recipe pages">
          <button type="button" class="small" disabled={page <= 1} onclick={() => turnTo(page - 1)}>Previous</button>
          <span class="muted" aria-live="polite">Page {page} of {pageCount}</span>
          <button type="button" class="small" disabled={page >= pageCount} onclick={() => turnTo(page + 1)}>Next</button>
        </nav>
      {/if}
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
  .filters { display: flex; flex-wrap: wrap; gap: .5rem 1rem; align-items: end; }
  .filters label { display: grid; gap: .25rem; }
  .filters .grow { flex: 1; min-width: 12rem; }
  .filters p { margin: 0; flex-basis: 100%; }
  .filters p:empty { display: none; }
  .pager { justify-content: space-between; align-items: center; }
  .link p { margin: 0; }
  .link-row { display: flex; gap: .5rem; }
  .link-row input { flex: 1; min-width: 0; }
</style>
