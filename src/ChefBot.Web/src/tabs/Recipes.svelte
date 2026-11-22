<!-- The Recipes tab: the library across the full width, as photo cards or a compact list, found a page at a time by
     words, and under "Filters" by course, favourites, tags, slow cooker and time. Each recipe has a heart to make it a favourite, and says whose favourite it is. Adding a recipe opens a dialog (RecipeAdd.svelte); one recipe opens on its own page. -->
<script>
  import { untrack } from 'svelte';
  import { app, run, isAdmin } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import Chip from '../components/Chip.svelte';
  import RecipeDetail from './RecipeDetail.svelte';
  import RecipeAdd from './RecipeAdd.svelte';
  import { deal } from '../lib/motion.js';
  import ChefHat from '@lucide/svelte/icons/chef-hat';
  import Plus from '@lucide/svelte/icons/plus';
  import LayoutGrid from '@lucide/svelte/icons/layout-grid';
  import List from '@lucide/svelte/icons/list';
  import Download from '@lucide/svelte/icons/download';
  import ChevronDown from '@lucide/svelte/icons/chevron-down';
  import RecipeDownloads from '../components/RecipeDownloads.svelte';
  import TagFilter from '../components/TagFilter.svelte';
  import FavouriteButton from '../components/FavouriteButton.svelte';
  import { favouriteOf, possessive } from '../lib/format.js';
  import SlidersHorizontal from '@lucide/svelte/icons/sliders-horizontal';
  import Heart from '@lucide/svelte/icons/heart';
  import X from '@lucide/svelte/icons/x';

  const COURSES = ['Main', 'Breakfast', 'Snack', 'Sweet', 'Drink', 'Other'];
  const PAGE_SIZE = 24;
  /** The choices for "Ready within", in minutes. */
  const TIMES = [15, 30, 45, 60, 90];
  /** How long typing pauses before the search runs, in milliseconds. */
  const TYPING_PAUSE = 250;
  /** Where this browser remembers whether the library is shown as cards or a list. */
  const LAYOUT_KEY = 'chefbot.recipes.layout';

  let typed = $state('');   // what is in the search box
  let query = $state('');   // what was searched for, once typing paused
  let course = $state('');  // '' for every course
  let favourites = $state(''); // '' for all recipes, 'mine', 'any', or another person's id
  let tags = $state([]);       // tags a recipe must all have
  let slowCooker = $state(false);
  let readyWithin = $state(''); // minutes as text, '' for any time
  let filtersOpen = $state(false); // whether the filters panel shows
  let filterChoices = $state({ tags: [], people: [] }); // RecipeFiltersResponse
  let page = $state(1);
  let result = $state(null); // RecipePageResponse for the page shown
  let loading = $state(false); // a newer page is on its way; the one shown stays, dimmed, until it arrives
  let listTop = $state();
  let layout = $state(remembered());
  let downloadsOpen = $state(false); // whether the "Download all" choices show

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
    if (favourites === 'any') params.set('favourites', 'true');
    else if (favourites) params.set('favouredBy', favourites === 'mine' ? app.session?.user?.id : favourites);
    for (const tag of tags) params.append('tag', tag);
    if (slowCooker) params.set('slowCooker', 'true');
    if (readyWithin) params.set('readyWithin', readyWithin);
    const ticket = ++latest;
    loading = true;
    // run() clears old messages as it starts; untracked, so that doesn't make this effect run again.
    untrack(() => run(async () => {
      const found = await api('GET', `/api/recipes/search?${params}`);
      if (ticket !== latest) return;
      // After an archive the page asked for can be past the end; step back to the last one.
      if (found.recipes.length === 0 && found.total > 0) page = Math.ceil(found.total / found.pageSize);
      else result = found;
    }).finally(() => { if (ticket === latest) loading = false; }));
  });

  // What the library can be filtered by, again whenever it may have changed.
  $effect(() => {
    void app.recipesChanged;
    untrack(() => run(async () => { filterChoices = await api('GET', '/api/recipes/filters'); }));
  });

  const librarySize = $derived(result?.librarySize ?? app.recipeCount);
  /** How many filters are on, for the Filters button. Each tag counts as one. */
  const filterCount = $derived((course ? 1 : 0) + (favourites ? 1 : 0) + tags.length + (slowCooker ? 1 : 0) + (readyWithin ? 1 : 0));
  const filtering = $derived(query !== '' || filterCount > 0);
  const otherPeople = $derived(filterChoices.people.filter((person) => person.userId !== app.session?.user?.id));

  /** Starts again from the first page after any filter changes. */
  const refilter = () => (page = 1);

  /** Turns every filter off. The search words stay unless `everything` is true. */
  function clearFilters(everything = false) {
    if (everything) { typed = ''; query = ''; }
    course = ''; favourites = ''; tags = []; slowCooker = false; readyWithin = '';
    refilter();
  }

  /** The filters that are on, as removable labels under the search while the panel is closed. */
  const activeFilters = $derived([
    ...(course ? [{ key: 'course', label: course, remove: () => { course = ''; refilter(); } }] : []),
    ...(favourites ? [{
      key: 'favourites',
      label: favourites === 'mine' ? 'My favourites' : favourites === 'any' ? "Anyone's favourites"
        : `${possessive(otherPeople.find((person) => person.userId === favourites)?.name ?? 'Someone')} favourites`,
      remove: () => { favourites = ''; refilter(); },
    }] : []),
    ...tags.map((tag) => ({ key: `tag:${tag}`, label: `#${tag}`, remove: () => { tags = tags.filter((chosen) => chosen !== tag); refilter(); } })),
    ...(slowCooker ? [{ key: 'slow', label: 'Slow cooker', remove: () => { slowCooker = false; refilter(); } }] : []),
    ...(readyWithin ? [{ key: 'time', label: `Within ${readyWithin} min`, remove: () => { readyWithin = ''; refilter(); } }] : []),
  ]);

  /** A heart changed on a card: the people with favourites may have changed too, so the choices are fetched again. */
  function favouriteChanged(recipe, list) {
    recipe.favouredBy = list;
    run(async () => { filterChoices = await api('GET', '/api/recipes/filters'); });
  }
  const pageCount = $derived(result ? Math.max(1, Math.ceil(result.total / result.pageSize)) : 1);
  const groups = $derived(COURSES
    .map((name) => ({ course: name, recipes: (result?.recipes ?? []).filter((recipe) => recipe.course === name) }))
    .filter((group) => group.recipes.length > 0));
  /** How many empty tiles stand in while the first page loads: as many as there are recipes, up to two rows or so. */
  const ghostCount = $derived(Math.min(app.recipeCount, 8));
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

{#snippet favoured(recipe)}
  <span class="favoured"><Heart size={13} fill="currentColor" aria-hidden="true" />{favouriteOf(recipe.favouredBy.map((favourite) => favourite.name))}</span>
{/snippet}

{#if app.recipeId}
  {#key app.recipeId}<RecipeDetail id={app.recipeId} />{/key}
{:else}
  <section class="library" bind:this={listTop}>
    <div class="row head">
      <h2>Recipes ({librarySize})</h2>
      <div class="row-actions">
        {#if isAdmin()}<a class="button" href="/recipes/samples">Browse sample recipes</a>{/if}
        {#if librarySize > 0}
          <button type="button" class="add" aria-expanded={downloadsOpen} aria-controls="download-all"
                  onclick={() => (downloadsOpen = !downloadsOpen)}><Download size={18} />Download all<ChevronDown size={16} /></button>
        {/if}
        <a class="button primary add" href="/recipes/new/link"><Plus size={18} />Add recipe</a>
      </div>
    </div>
    <!-- Every recipe in one file: a PDF, Markdown, text, or the schema.org file other recipe apps import. It opens in
         the page rather than over it, so on a phone it never covers the search. -->
    <div id="download-all" class="card downloads" hidden={!downloadsOpen}>
      <span>Download every recipe in one file:</span>
      <RecipeDownloads href="/api/recipes/export" onpick={() => (downloadsOpen = false)} />
    </div>

    {#if librarySize > 0}
      <div class="card filters" role="search" aria-label="Find recipes">
        <div class="bar">
          <label class="search">
            <span class="sr-only">Search</span>
            <input id="recipe-search" type="search" placeholder="Search by name, ingredient or tag" bind:value={typed} oninput={onType}>
          </label>
          <button type="button" class="filters-toggle" class:on={filterCount > 0} aria-expanded={filtersOpen} aria-controls="recipe-filters"
                  onclick={() => (filtersOpen = !filtersOpen)}>
            <SlidersHorizontal size={16} />Filters{#if filterCount}<span class="badge" aria-label="{filterCount} on">{filterCount}</span>{/if}
            <ChevronDown size={16} class="chevron" />
          </button>
          <div class="segmented layout" role="group" aria-label="Show recipes as">
            <button type="button" aria-pressed={layout === 'grid'} onclick={() => showAs('grid')}><LayoutGrid size={16} /><span class="label">Cards</span></button>
            <button type="button" aria-pressed={layout === 'list'} onclick={() => showAs('list')}><List size={16} /><span class="label">List</span></button>
          </div>
        </div>

        <div id="recipe-filters" class="panel" hidden={!filtersOpen}>
          <div class="choices">
            <label>Course
              <select id="recipe-course" bind:value={course} onchange={refilter}>
                <option value="">All courses</option>
                {#each COURSES as name (name)}<option value={name}>{name}</option>{/each}
              </select>
            </label>
            <label>Favourites
              <select id="recipe-favourites" bind:value={favourites} onchange={refilter}>
                <option value="">All recipes</option>
                <option value="mine">My favourites</option>
                <option value="any">Anyone's favourites</option>
                {#each otherPeople as person (person.userId)}<option value={person.userId}>{possessive(person.name)} favourites</option>{/each}
              </select>
            </label>
            <label>Ready within
              <select id="recipe-time" bind:value={readyWithin} onchange={refilter}>
                <option value="">Any time</option>
                {#each TIMES as minutes (minutes)}<option value={String(minutes)}>{minutes} minutes</option>{/each}
              </select>
            </label>
            <label class="slow"><input type="checkbox" bind:checked={slowCooker} onchange={refilter}>Slow cooker only</label>
          </div>
          <TagFilter id="recipe-tags" tags={filterChoices.tags} bind:chosen={tags} onchange={refilter} />
          {#if filterCount}
            <p class="panel-foot"><button type="button" class="small" onclick={() => clearFilters()}>Clear filters</button></p>
          {/if}
        </div>

        <p class="muted status" aria-live="polite">
          {#if result && filtering}{result.total} {result.total === 1 ? 'recipe matches' : 'recipes match'}{/if}
          {#if filterCount && !filtersOpen}
            {#each activeFilters as filter (filter.key)}
              <span class="active">{filter.label}<button type="button" class="icon" aria-label="Remove filter {filter.label}" onclick={filter.remove}><X size={13} /></button></span>
            {/each}
          {/if}
        </p>
      </div>
    {/if}

    {#if !result}
      <!-- Until the first page arrives, empty tiles stand where the recipes will go, in the layout chosen. -->
      <div role="status" aria-label="Loading recipes">
        <span class="skeleton line course-ghost" aria-hidden="true"></span>
        {#if layout === 'grid'}
          <ul class="cards" aria-hidden="true">
            {#each { length: ghostCount }, index (index)}
              <li class="ghost">
                <span class="skeleton cover"></span>
                <span class="text">
                  <span class="skeleton line" style:width="{70 - (index % 3) * 12}%"></span>
                  <span class="skeleton line" style:width="90%"></span>
                </span>
              </li>
            {/each}
          </ul>
        {:else}
          <ul class="plain card rows" aria-hidden="true">
            {#each { length: ghostCount }, index (index)}
              <li class="ghost">
                <span class="skeleton thumb"></span>
                <span class="text">
                  <span class="skeleton line" style:width="{45 - (index % 3) * 8}%"></span>
                  <span class="skeleton line" style:width="75%"></span>
                </span>
              </li>
            {/each}
          </ul>
        {/if}
      </div>
    {:else if librarySize === 0}
      <div class="card empty">
        <ChefHat size={36} aria-hidden="true" />
        <h3>No recipes yet</h3>
        <p class="muted">Add one from a recipe site, type one in, or import files from another app.</p>
        <div class="row-actions start">
          <a class="button primary with-icon" href="/recipes/new/link"><Plus size={16} />Add from a link</a>
          <a class="button" href="/recipes/new/write">Type one in</a>
          <a class="button" href="/recipes/new/files">Import files</a>
        </div>
      </div>
    {:else if result.total === 0}
      <p class="muted">No recipes match. Try fewer words or filters, or <button type="button" class="link" onclick={() => clearFilters(true)}>clear them all</button>.</p>
    {/if}

    <div class="results" class:loading={loading && result} aria-busy={loading && result ? 'true' : undefined}>
    {#each groups as { course: heading, recipes } (heading)}
      <h3 class="course">{heading} <span class="muted">({recipes.length})</span></h3>
      {#if layout === 'grid'}
        <ul class="cards">
          {#each recipes as recipe, index (recipe.id)}
            <li in:deal={{ index }}>
              <a class="recipe-card" href="/recipes/{recipe.id}">
                {#if recipe.coverUrl}
                  <img class="cover" src={recipe.coverUrl} alt="" loading="lazy">
                {:else}
                  <span class="cover placeholder" aria-hidden="true"><ChefHat size={32} /></span>
                {/if}
                <span class="text">
                  <strong>{recipe.name}</strong>
                  {#if recipe.favouredBy?.length}{@render favoured(recipe)}{/if}
                  {#if recipe.description?.trim()}<span class="blurb">{recipe.description}</span>{/if}
                </span>
              </a>
              <FavouriteButton {recipe} class="on-cover" onchange={(list) => favouriteChanged(recipe, list)} />
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
          {#each recipes as recipe, index (recipe.id)}
            <li in:deal={{ index }}>
              {#if recipe.coverUrl}
                <img class="thumb" src={recipe.coverUrl} alt="" loading="lazy">
              {:else}
                <span class="thumb placeholder" aria-hidden="true"><ChefHat size={18} /></span>
              {/if}
              <div class="text">
                <a href="/recipes/{recipe.id}"><strong>{recipe.name}</strong></a>
                {#if recipe.isSlowCooker}<Chip tone="good">Slow cooker</Chip>{/if}
                {#if recipe.favouredBy?.length}<p class="row-favoured">{@render favoured(recipe)}</p>{/if}
                {#if recipe.description?.trim()}<p class="muted blurb">{recipe.description}</p>{/if}
              </div>
              <FavouriteButton {recipe} class="icon" onchange={(list) => favouriteChanged(recipe, list)} />
            </li>
          {/each}
        </ul>
      {/if}
    {/each}
    </div>

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
  /* While the next page loads, the one shown rests; only if it takes more than a moment, so quick answers don't flicker. */
  .results { transition: opacity .2s; }
  .results.loading { opacity: .55; transition-delay: .15s; }
  .head h2 { margin: 0; }
  .add { display: inline-flex; align-items: center; gap: .35rem; }
  .downloads:not([hidden]) { display: flex; flex-wrap: wrap; align-items: center; gap: .25rem .5rem; }
  .downloads span { color: var(--muted); margin-right: .25rem; }
  .filters { display: grid; gap: .6rem; }
  .bar { display: flex; flex-wrap: wrap; align-items: center; gap: .5rem; }
  .search { flex: 1 1 14rem; min-width: 0; }
  .search input { width: 100%; }
  .filters-toggle { display: inline-flex; align-items: center; gap: .4rem; }
  .filters-toggle.on { border-color: var(--accent); color: var(--accent); }
  .filters-toggle :global(.chevron) { transition: rotate .15s; }
  .filters-toggle[aria-expanded="true"] :global(.chevron) { rotate: 180deg; }
  @media (prefers-reduced-motion: reduce) { .filters-toggle :global(.chevron) { transition: none; } }
  .badge { min-width: 1.3rem; padding: 0 .35rem; border-radius: 999px; background: var(--accent); color: var(--accent-ink);
    font-size: .75rem; line-height: 1.3rem; text-align: center; }
  .panel:not([hidden]) { display: grid; gap: .9rem; padding-top: .8rem; border-top: 1px solid var(--line); }
  .choices { display: grid; gap: .6rem 1rem; align-items: end; grid-template-columns: repeat(auto-fill, minmax(min(8.5rem, 100%), 1fr)); }
  .choices label { display: grid; gap: .25rem; }
  .choices select { width: 100%; }
  .choices label.slow { display: flex; align-items: center; gap: .45rem; min-height: 2.4rem; margin: 0; padding: 0; border: 0; }
  .panel-foot { margin: 0; display: flex; justify-content: flex-end; }
  .status { margin: 0; display: flex; flex-wrap: wrap; align-items: center; gap: .35rem .5rem; }
  .status:empty { display: none; }
  .active { display: inline-flex; align-items: center; gap: .1rem; padding: .05rem .15rem .05rem .55rem; border-radius: 999px;
    background: var(--accent-bg); color: var(--ink); font-size: .8rem; }
  .active .icon { padding: .15rem; border-radius: 999px; }
  button.link { border: 0; background: none; padding: 0; color: var(--accent); text-decoration: underline; font: inherit; cursor: pointer; }
  /* On a phone the layout buttons keep their icons only, so the search keeps its room. */
  @media (max-width: 32rem) { .layout .label { position: absolute; width: 1px; height: 1px; overflow: hidden; clip-path: inset(50%); } }
  .layout button { display: inline-flex; align-items: center; gap: .3rem; }
  .empty :global(svg) { color: var(--area-recipes); }
  .empty h3 { margin: .5rem 0 .25rem; }
  .empty .row-actions { justify-content: center; margin-top: 1rem; }
  .course { margin: 1.25rem 0 .5rem; }

  .cards { list-style: none; margin: 0; padding: 0; display: grid; gap: .9rem;
    grid-template-columns: repeat(auto-fill, minmax(min(13.5rem, 100%), 1fr)); }
  .cards li { position: relative; display: flex; flex-direction: column; background: var(--panel); border: 1px solid var(--line); border-radius: .6rem;
    overflow: hidden; transition: border-color .15s, box-shadow .15s, translate .15s; }
  .cards li:hover, .cards li:focus-within { border-color: var(--area-recipes); box-shadow: 0 6px 18px var(--shadow); translate: 0 -2px; }
  @media (prefers-reduced-motion: reduce) { .cards li { transition: none; } .cards li:hover, .cards li:focus-within { translate: none; } }
  .recipe-card { display: flex; flex-direction: column; color: var(--ink); text-decoration: none; flex: 1; }
  .recipe-card:focus-visible { outline: 2px solid var(--accent); outline-offset: -2px; }
  .cover { display: block; width: 100%; aspect-ratio: 4 / 3; object-fit: cover; background: var(--chip); }
  .placeholder { display: grid; place-items: center; color: var(--muted); }
  .recipe-card .text { display: grid; gap: .2rem; padding: .6rem .8rem 0; }
  .recipe-card strong { color: var(--accent); }
  /* A recipe's description, cut to two lines in both layouts. Recipes without one show only their name. */
  .blurb { display: -webkit-box; -webkit-line-clamp: 2; line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }
  .recipe-card .blurb { color: var(--muted); font-size: .85rem; }
  /* The heart sits on the cover's top corner, outside the card's link so it can be pressed on its own. */
  .cards :global(.on-cover) { position: absolute; top: .45rem; right: .45rem; padding: .4rem; border-radius: 999px;
    border-color: transparent; background: color-mix(in srgb, var(--panel) 88%, transparent); box-shadow: 0 1px 4px var(--shadow); }
  .favoured { display: inline-flex; align-items: center; gap: .3rem; color: var(--bad); font-size: .8rem; }
  .row-favoured { margin: .1rem 0 0; }
  .cards .chips { padding: 0 .8rem; }
  .cards li::after { content: ""; padding-bottom: .7rem; }
  /* The empty tiles shown while the first page loads don't react to the pointer. */
  .cards li.ghost:hover { border-color: var(--line); box-shadow: none; translate: none; }
  .cards .ghost .cover { border-radius: 0; }
  .cards .ghost .text { padding: .6rem .8rem 0; }
  .course-ghost { width: 6rem; height: 1.1rem; margin: 1.25rem 0 .75rem; }

  .rows li { display: flex; gap: .75rem; align-items: flex-start; }
  .rows li:first-child { border-top: 0; }
  .rows .text { min-width: 0; flex: 1; }
  .thumb { flex: none; width: 3.5rem; height: 2.75rem; object-fit: cover; border-radius: .3rem; background: var(--chip); }

  .pager { justify-content: center; align-items: center; gap: 1rem; margin-top: 1rem; }
  .pager > :first-child { flex: none; }
</style>
