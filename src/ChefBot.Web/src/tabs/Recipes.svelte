<!-- The Recipes tab: the library across the full width, as photo cards or a compact list, found by words and course a
     page at a time. Adding a recipe opens a dialog (RecipeAdd.svelte); one recipe opens on its own page. -->
<script>
  import { untrack } from 'svelte';
  import { app, run, isAdmin } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import Chip from '../components/Chip.svelte';
  import RecipeDetail from './RecipeDetail.svelte';
  import RecipeAdd from './RecipeAdd.svelte';
  import LoaderCircle from '@lucide/svelte/icons/loader-circle';
  import ChefHat from '@lucide/svelte/icons/chef-hat';
  import Plus from '@lucide/svelte/icons/plus';
  import LayoutGrid from '@lucide/svelte/icons/layout-grid';
  import List from '@lucide/svelte/icons/list';
  import Download from '@lucide/svelte/icons/download';

  const COURSES = ['Main', 'Breakfast', 'Snack', 'Sweet', 'Drink', 'Other'];
  const PAGE_SIZE = 24;
  /** How long typing pauses before the search runs, in milliseconds. */
  const TYPING_PAUSE = 250;
  /** Where this browser remembers whether the library is shown as cards or a list. */
  const LAYOUT_KEY = 'chefbot.recipes.layout';

  let typed = $state('');   // what is in the search box
  let query = $state('');   // what was searched for, once typing paused
  let course = $state('');  // '' for every course
  let page = $state(1);
  let result = $state(null); // RecipePageResponse for the page shown
  let listTop = $state();
  let layout = $state(remembered());

  function remembered() {
    try { return localStorage.getItem(LAYOUT_KEY) === 'list' ? 'list' : 'grid'; } catch { return 'grid'; }
  }

  function showAs(next) {
    layout = next;
    try { localStorage.setItem(LAYOUT_KEY, next); } catch { /* kept for this visit only */ }
  }

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
  const adding = $derived(app.recipeDialog?.startsWith('new-') ?? false);

  // This component stays while a recipe is open, so the search, course and page are kept; coming back to the list
  // also scrolls to where it was left. scrollY is read before the list is swapped for the recipe.
  let listScroll = 0;
  $effect.pre(() => {
    if (app.recipeId) listScroll = untrack(() => scrollY);
  });
  $effect(() => {
    if (!app.recipeId && listScroll) {
      scrollTo(0, listScroll);
      listScroll = 0;
    }
  });

  function turnTo(next) {
    page = next;
    listTop?.scrollIntoView({ block: 'start' });
  }
</script>

{#if app.recipeId}
  {#key app.recipeId}<RecipeDetail id={app.recipeId} />{/key}
{:else}
  <section class="library" bind:this={listTop}>
    <div class="row head">
      <h2>Recipes ({librarySize})</h2>
      <div class="row-actions">
        {#if isAdmin()}<a class="button" href="/recipes/samples">Browse sample recipes</a>{/if}
        {#if librarySize > 0}
          <a class="button add" href="/api/recipes/export" download title="Every recipe in one schema.org file, which other recipe apps import">
            <Download size={18} />Download all</a>
        {/if}
        <a class="button primary add" href="/recipes/new/link"><Plus size={18} />Add recipe</a>
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
        <div class="segmented layout" role="group" aria-label="Show recipes as">
          <button type="button" aria-pressed={layout === 'grid'} onclick={() => showAs('grid')}><LayoutGrid size={16} />Cards</button>
          <button type="button" aria-pressed={layout === 'list'} onclick={() => showAs('list')}><List size={16} />List</button>
        </div>
        <p class="muted" aria-live="polite">
          {#if result && filtering}{result.total} {result.total === 1 ? 'recipe matches' : 'recipes match'}{/if}
        </p>
      </div>
    {/if}

    {#if !result}
      <p class="loading muted" role="status" aria-label="Loading recipes"><LoaderCircle size={24} /></p>
    {:else if librarySize === 0}
      <div class="card empty">
        <ChefHat size={36} aria-hidden="true" />
        <h3>No recipes yet</h3>
        <p class="muted">Add one from a recipe site, type one in, or import files from another app.</p>
        <div class="row-actions start">
          <a class="button primary" href="/recipes/new/link">Add from a link</a>
          <a class="button" href="/recipes/new/write">Type one in</a>
          <a class="button" href="/recipes/new/files">Import files</a>
        </div>
      </div>
    {:else if result.total === 0}
      <p class="muted">No recipes match. Try fewer words or another course.</p>
    {/if}

    {#each groups as { course: heading, recipes } (heading)}
      <h3 class="course">{heading} <span class="muted">({recipes.length})</span></h3>
      {#if layout === 'grid'}
        <ul class="cards">
          {#each recipes as recipe (recipe.id)}
            <li>
              <a class="recipe-card" href="/recipes/{recipe.id}">
                {#if recipe.coverUrl}
                  <img class="cover" src={recipe.coverUrl} alt="" loading="lazy">
                {:else}
                  <span class="cover placeholder" aria-hidden="true"><ChefHat size={32} /></span>
                {/if}
                <span class="text">
                  <strong>{recipe.name}</strong>
                  <span class="ingredients">{recipe.ingredients.join(', ')}</span>
                </span>
              </a>
              {#if recipe.isSlowCooker || recipe.tags.length}
                <div class="chips">
                  {#if recipe.isSlowCooker}<Chip tone="good">Slow cooker</Chip>{/if}
                  {#each recipe.tags.slice(0, 2) as tag (tag)}<Chip>#{tag}</Chip>{/each}
                </div>
              {/if}
            </li>
          {/each}
        </ul>
      {:else}
        <ul class="plain card rows">
          {#each recipes as recipe (recipe.id)}
            <li>
              {#if recipe.coverUrl}
                <img class="thumb" src={recipe.coverUrl} alt="" loading="lazy">
              {:else}
                <span class="thumb placeholder" aria-hidden="true"><ChefHat size={18} /></span>
              {/if}
              <div class="text">
                <a href="/recipes/{recipe.id}"><strong>{recipe.name}</strong></a>
                {#if recipe.isSlowCooker}<Chip tone="good">Slow cooker</Chip>{/if}
                <p class="muted">{recipe.ingredients.join(', ')}</p>
              </div>
            </li>
          {/each}
        </ul>
      {/if}
    {/each}

    {#if pageCount > 1}
      <nav class="row pager" aria-label="Recipe pages">
        <button type="button" class="small" disabled={page <= 1} onclick={() => turnTo(page - 1)}>Previous</button>
        <span class="muted" aria-live="polite">Page {page} of {pageCount}</span>
        <button type="button" class="small" disabled={page >= pageCount} onclick={() => turnTo(page + 1)}>Next</button>
      </nav>
    {/if}
  </section>

  {#if adding}<RecipeAdd />{/if}
{/if}

<style>
  .head { align-items: center; margin-top: 1rem; }
  .head h2 { margin: 0; }
  .add { display: inline-flex; align-items: center; gap: .35rem; }
  .filters { display: flex; flex-wrap: wrap; gap: .5rem 1rem; align-items: end; }
  .filters label { display: grid; gap: .25rem; }
  .filters .grow { flex: 1; min-width: 12rem; }
  .filters p { margin: 0; flex-basis: 100%; }
  .filters p:empty { display: none; }
  .layout button { display: inline-flex; align-items: center; gap: .3rem; }
  .empty :global(svg) { color: var(--area-recipes); }
  .empty h3 { margin: .5rem 0 .25rem; }
  .empty .row-actions { justify-content: center; margin-top: 1rem; }
  .course { margin: 1.25rem 0 .5rem; }

  .cards { list-style: none; margin: 0; padding: 0; display: grid; gap: .9rem;
    grid-template-columns: repeat(auto-fill, minmax(min(13.5rem, 100%), 1fr)); }
  .cards li { display: flex; flex-direction: column; background: var(--panel); border: 1px solid var(--line); border-radius: .6rem;
    overflow: hidden; transition: border-color .15s, box-shadow .15s, translate .15s; }
  .cards li:hover, .cards li:focus-within { border-color: var(--area-recipes); box-shadow: 0 6px 18px var(--shadow); translate: 0 -2px; }
  @media (prefers-reduced-motion: reduce) { .cards li { transition: none; } .cards li:hover, .cards li:focus-within { translate: none; } }
  .recipe-card { display: flex; flex-direction: column; color: var(--ink); text-decoration: none; flex: 1; }
  .recipe-card:focus-visible { outline: 2px solid var(--accent); outline-offset: -2px; }
  .cover { display: block; width: 100%; aspect-ratio: 4 / 3; object-fit: cover; background: var(--chip); }
  .placeholder { display: grid; place-items: center; color: var(--muted); }
  .recipe-card .text { display: grid; gap: .2rem; padding: .6rem .8rem 0; }
  .recipe-card strong { color: var(--accent); }
  .ingredients { color: var(--muted); font-size: .85rem; display: -webkit-box; -webkit-line-clamp: 2; line-clamp: 2;
    -webkit-box-orient: vertical; overflow: hidden; }
  .cards .chips { padding: 0 .8rem; }
  .cards li::after { content: ""; padding-bottom: .7rem; }

  .rows li { display: flex; gap: .75rem; align-items: flex-start; }
  .rows li:first-child { border-top: 0; }
  .rows .text { min-width: 0; flex: 1; }
  .thumb { flex: none; width: 3.5rem; height: 2.75rem; object-fit: cover; border-radius: .3rem; background: var(--chip); }

  .pager { justify-content: center; align-items: center; gap: 1rem; margin-top: 1rem; }
  .pager > :first-child { flex: none; }
</style>
