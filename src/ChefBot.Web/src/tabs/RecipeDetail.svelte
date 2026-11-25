<!-- One recipe on its own page, /recipes/{id}, so it can be bookmarked: a masthead with its photos, name, times and the
     things to do with it, then its ingredients (RecipeIngredients.svelte, scaled to the servings chosen there; ?serves=6
     keeps the choice in the address), steps, notes and reviews (RecipeReviews.svelte). Cooking mode (/recipes/{id}/cook,
     RecipeCook.svelte) opens over the whole screen, and adding it to the plan (/recipes/{id}/plan, RecipeAddToPlan.svelte),
     editing it (/edit), its photos (/photos, RecipePhotos.svelte), AI suggestions (/improve, RecipeSuggestions.svelte) and
     its cookbooks (/cookbooks, RecipeCookbooks.svelte) in dialogs. The ⋯ menu (RecipeMenu.svelte) has the rest. -->
<script>
  import { onMount, tick } from 'svelte';
  import { app, run, loadRecipes, toast, loadRecipePage, watchRecipe } from '../lib/app.svelte.js';
  import { closeDialog } from '../lib/addresses.js';
  import { api } from '../lib/api.js';
  import { duration, RECIPE_ISSUES, listWords } from '../lib/format.js';
  import { rise } from '../lib/motion.js';
  import { scaledLines, servesFrom } from '../lib/scaling.js';
  import Modal from '../components/Modal.svelte';
  import FavouriteButton from '../components/FavouriteButton.svelte';
  import RecipeBadges from '../components/RecipeBadges.svelte';
  import RecipeForm from './RecipeForm.svelte';
  import RecipeCook from './RecipeCook.svelte';
  import RecipeReviews from './RecipeReviews.svelte';
  import RecipeIngredients from './RecipeIngredients.svelte';
  import RecipeMenu from './RecipeMenu.svelte';
  import RecipePhotos from './RecipePhotos.svelte';
  import RecipeAddToPlan from './RecipeAddToPlan.svelte';
  import RecipeSuggestions from './RecipeSuggestions.svelte';
  import RecipeCookbooks from './RecipeCookbooks.svelte';
  import BookMarked from '@lucide/svelte/icons/book-marked';
  import Sparkles from '@lucide/svelte/icons/sparkles';
  import CookingPot from '@lucide/svelte/icons/cooking-pot';
  import CalendarPlus from '@lucide/svelte/icons/calendar-plus';
  import ArrowLeft from '@lucide/svelte/icons/arrow-left';
  import ExternalLink from '@lucide/svelte/icons/external-link';
  import BookOpenText from '@lucide/svelte/icons/book-open-text';
  import ImagePlus from '@lucide/svelte/icons/image-plus';
  import Timer from '@lucide/svelte/icons/timer';
  import Flame from '@lucide/svelte/icons/flame';
  import Clock from '@lucide/svelte/icons/clock';
  import Users from '@lucide/svelte/icons/users';

  /**
   * "Back to recipes" is a link, so it can open in a new tab. When this page was opened from the list, a plain click
   * goes back in history instead, so the list comes back as it was left: the same search, page and scroll position.
   */
  function backToList(event) {
    if (event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
    if (!/^\/recipes(\?|$)/.test(history.state?.from ?? '')) return;
    event.preventDefault();
    history.back();
  }

  let { id } = $props();

  let recipe = $state(null);
  let loaded = $state(false);
  let shownId = $state(null); // the photo shown large; the cover when none was picked
  let chosen = $state(servesFrom(location.search, null)); // servings picked on this page; null for the recipe's own

  const base = $derived(`/recipes/${id}`);
  const query = $derived(chosen == null ? '' : `?serves=${chosen}`);
  const link = (dialog) => `${base}/${dialog}${query}`;
  // What the recipe is missing, in words. A missing photo is left out: the empty photo already offers to add one.
  const missing = $derived((recipe?.issues ?? []).filter((issue) => issue !== 'NoPhoto' && RECIPE_ISSUES[issue]).map((issue) => RECIPE_ISSUES[issue].words));
  const serves = $derived(chosen ?? recipe?.servings ?? 1);
  const images = $derived(recipe?.images ?? []);
  const shown = $derived(images.find((image) => image.id === shownId) ?? images[0]);
  const facts = $derived(recipe ? [
    ['Prep', recipe.prepMinutes && duration(recipe.prepMinutes), Timer],
    ['Cook', recipe.cookMinutes && duration(recipe.cookMinutes), Flame],
    ['Total', recipe.totalMinutes && duration(recipe.totalMinutes), Clock],
    ['Serves', `${serves}`, Users],
  ].filter(([, value]) => value) : []);
  const sourceHost = $derived.by(() => {
    try { return recipe?.sourceUrl ? new URL(recipe.sourceUrl).host : null; } catch { return recipe.sourceUrl; }
  });
  const hasMore = $derived(Boolean(recipe && (recipe.notes || recipe.yieldText || recipe.author || recipe.sourceUrl)));
  const sections = $derived([
    { key: 'ingredients', label: 'Ingredients' },
    { key: 'steps', label: 'Steps' },
    ...(hasMore ? [{ key: 'more', label: 'Notes and source' }] : []),
    ...(app.offline ? [] : [{ key: 'reviews', label: 'Reviews' }]),
  ]);

  /** Shows a recipe the server sent back, and names the browser tab after it. */
  function show(found) {
    recipe = found;
    app.recipeName = found?.name ?? null;
  }

  // The page is rebuilt for each recipe, so this runs once per id. A recipe that is gone shows the empty state.
  app.recipeName = null;
  run(() => loadRecipePage(id, show).finally(() => (loaded = true)));
  onMount(() => watchRecipe(id, () => base, show));

  const close = () => closeDialog(base + query);

  /** Shows the recipe for `count` servings, and keeps the choice in the address so a reload or a shared link keeps it. */
  function choose(count) {
    chosen = count === recipe.servings ? null : count;
    history.replaceState(history.state, '', `${location.pathname}${query}`);
  }

  // In-page links: the address stays the same, the section scrolls into view and takes focus.
  async function jump(event, key) {
    event.preventDefault();
    const target = document.getElementById(`recipe-${key}`);
    target?.scrollIntoView({ block: 'start', behavior: matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth' });
    await tick();
    target?.focus({ preventScroll: true });
  }

  async function save(draft) {
    let saved = false;
    await run(async () => {
      show(await api('PUT', `/api/recipes/${recipe.id}`, draft));
      await loadRecipes();
      saved = true;
      toast('Recipe saved.');
      await close();
    });
    return saved;
  }
</script>

<p class="back-row"><a class="button back" href="/recipes" onclick={backToList}><ArrowLeft size={16} />Back to recipes</a></p>

{#if !loaded}
  <!-- Until the recipe arrives, its outline stands in: the photo beside the name, and the ingredients beside the steps. -->
  <div role="status" aria-label="Loading the recipe">
    <div class="masthead" aria-hidden="true">
      <span class="skeleton hero-ghost"></span>
      <div>
        <span class="skeleton line" style:width="30%"></span>
        <span class="skeleton line title-ghost"></span>
        <span class="skeleton line" style:width="85%"></span>
        <span class="skeleton line" style:width="60%"></span>
      </div>
    </div>
    <div class="cook" aria-hidden="true">
      <div class="card">
        {#each [80, 65, 72, 55, 70, 60] as width, index (index)}<span class="skeleton line" style:width="{width}%"></span>{/each}
      </div>
      <div class="card">
        {#each [95, 88, 92, 70, 90, 84, 60] as width, index (index)}<span class="skeleton line" style:width="{width}%"></span>{/each}
      </div>
    </div>
  </div>
{:else if !recipe}
  <div class="card empty">
    <span class="empty-icon" style="--area: var(--area-recipes)" aria-hidden="true"><BookOpenText size={28} /></span>
    <h2>That recipe isn't in your library</h2>
    <p class="muted">It may have been archived, or the link is from another ChefBot. Pick one from your recipes instead.</p>
  </div>
{:else}
  <article class="recipe" in:rise>
    <header class="masthead" class:bare={!shown}>
      <div class="photo">
        {#if shown}
          <img class="cover" src={shown.url} alt={recipe.name}>
          {#if images.length > 1}
            <div class="strip">
              {#each images as image, index (image.id)}
                <button class="thumb" class:current={image.id === shown.id} onclick={() => (shownId = image.id)}
                        aria-label="Show photo {index + 1}" aria-pressed={image.id === shown.id}>
                  <img src={image.url} alt="" loading="lazy">
                </button>
              {/each}
            </div>
          {/if}
        {:else if app.offline}
          <div class="placeholder"><BookOpenText size={34} aria-hidden="true" /></div>
        {:else}
          <a class="placeholder" href={link('photos')}>
            <BookOpenText size={34} aria-hidden="true" /><span><ImagePlus size={14} aria-hidden="true" /> Add a photo</span>
          </a>
        {/if}
      </div>

      <div class="intro">
        <p class="eyebrow">{recipe.course}{recipe.isSlowCooker ? ' · Slow cooker' : ''}</p>
        <h2>{recipe.name}</h2>
        <RecipeBadges {recipe} large />
        {#if recipe.description}<p class="description">{recipe.description}</p>{/if}

        <ul class="facts" aria-label="At a glance">
          {#each facts as [label, value, Icon] (label)}
            <li class:scaled={label === 'Serves' && chosen != null}>
              <Icon size={18} aria-hidden="true" /><span class="label">{label}</span><span class="value">{value}</span>
            </li>
          {/each}
        </ul>

        <div class="row-actions">
          <a class="button primary with-icon grow" href={link('cook')}><CookingPot size={16} />Start cooking</a>
          {#if !app.offline}
            <a class="button with-icon grow" href={link('plan')}><CalendarPlus size={16} />Add to plan</a>
            <FavouriteButton {recipe} size={18} onchange={(list) => (recipe.favouredBy = list)} />
            <RecipeMenu {recipe} {link} />
          {/if}
        </div>

        {#if app.offline}
          <p class="offline-note muted">Saved on this device for offline use. Photos, editing, favourites and reviews need ChefBot.</p>
        {:else if recipe.hasSuggestion}
          <p class="suggested"><a class="with-icon" href={link('improve')}><Sparkles size={16} />Suggestions are ready to review</a></p>
        {:else if missing.length}
          <p class="missing muted">Missing {listWords(missing)} · <a href={link('edit')}>Edit</a>{#if app.aiFeatures?.recipeDetails} · <a href={link('improve')}>Improve with AI</a>{/if}</p>
        {/if}

        {#if recipe.tags.length}
          <ul class="chips tags" aria-label="Tags">{#each recipe.tags as tag (tag)}<li class="chip">#{tag}</li>{/each}</ul>
        {/if}
        {#if recipe.cookbooks?.length}
          <ul class="chips tags" aria-label="Cookbooks">
            {#each recipe.cookbooks as cookbook (cookbook.id)}<li><a class="chip book" href="/recipes?cookbook={cookbook.id}"><BookMarked size={13} aria-hidden="true" />{cookbook.name}</a></li>{/each}
          </ul>
        {/if}
      </div>
    </header>

    <nav class="sections" aria-label="Parts of this recipe">
      {#each sections as section (section.key)}
        <a href="#recipe-{section.key}" onclick={(event) => jump(event, section.key)}>{section.label}</a>
      {/each}
    </nav>

    <div class="cook">
      <RecipeIngredients {recipe} {serves} onserves={choose} />
      <section class="card" id="recipe-steps" tabindex="-1" aria-labelledby="recipe-steps-title">
        <h3 id="recipe-steps-title">Steps</h3>
        {#if recipe.instructions.length}
          <ol class="steps">
            {#each recipe.instructions as step, index (index)}<li>{step}</li>{/each}
          </ol>
        {:else}
          <p class="muted">No steps yet. <a href={link('edit')}>Edit the recipe</a> to add them.</p>
        {/if}
      </section>
    </div>

    {#if hasMore}
      <section class="card" id="recipe-more" tabindex="-1" aria-labelledby="recipe-more-title">
        <h3 id="recipe-more-title">Notes and source</h3>
        {#if recipe.notes}<p class="notes">{recipe.notes}</p>{/if}
        {#if recipe.yieldText || recipe.author || recipe.sourceUrl}
          <dl class="meta">
            {#if recipe.yieldText}<dt>Makes</dt><dd>{recipe.yieldText}</dd>{/if}
            {#if recipe.author}<dt>By</dt><dd>{recipe.author}</dd>{/if}
            {#if recipe.sourceUrl}
              <dt>Source</dt>
              <dd><a href={recipe.sourceUrl} target="_blank" rel="noopener noreferrer">{sourceHost} <ExternalLink size={14} /></a></dd>
            {/if}
          </dl>
        {/if}
      </section>
    {/if}
    {#if !app.offline}<RecipeReviews {recipe} onrating={(rating) => (recipe.rating = rating)} />{/if}
  </article>

  {#if app.recipeDialog === 'cook'}
    <RecipeCook {recipe} {serves} lines={scaledLines(recipe, serves).map((line) => line.text)} onclose={close} />
  {:else if app.recipeDialog === 'plan'}
    <RecipeAddToPlan {recipe} {serves} onclose={close} />
  {:else if app.recipeDialog === 'edit'}
    <Modal wide title="Edit {recipe.name}" onclose={close}>
      <RecipeForm {recipe} button="Save changes" {save} cancel={close} plain />
    </Modal>
  {:else if app.recipeDialog === 'photos'}
    <RecipePhotos {recipe} onclose={close} onsaved={(saved, photoId) => { show(saved); shownId = photoId; }} />
  {:else if app.recipeDialog === 'improve'}
    <RecipeSuggestions {recipe} onclose={close} onsaved={show} />
  {:else if app.recipeDialog === 'cookbooks'}
    <RecipeCookbooks {recipe} onclose={close} onsaved={show} />
  {/if}
{/if}

<style>
  .back-row { margin: 1rem 0 0; }
  .suggested { margin: .75rem 0 0; }
  .missing, .offline-note { margin: .75rem 0 0; font-size: .9rem; }
  .offline-note { padding-left: .6rem; border-left: 4px solid var(--warn); }
  .suggested a { display: inline-flex; align-items: center; gap: .4rem; font-weight: 600; }

  /* The photo beside the name and what to do with it; stacked on narrow screens. Without a photo, a small tile stands in. */
  .masthead { display: grid; gap: 1.25rem 1.75rem; grid-template-columns: minmax(0, 1.1fr) minmax(0, 1fr); align-items: start; margin-top: .75rem; }
  .masthead.bare { grid-template-columns: 9rem minmax(0, 1fr); }
  .hero-ghost { aspect-ratio: 4 / 3; border-radius: .9rem; }
  .title-ghost { width: min(22rem, 80%); height: 1.8rem; }
  .cover { display: block; width: 100%; aspect-ratio: 4 / 3; object-fit: cover; border-radius: .9rem; background: var(--chip);
    box-shadow: 0 8px 28px var(--shadow); }
  .strip { display: flex; gap: .4rem; margin-top: .5rem; overflow-x: auto; }
  .thumb { padding: 0; border: 2px solid transparent; border-radius: .45rem; background: none; flex: 0 0 auto; }
  .thumb.current { border-color: var(--accent); }
  .thumb img { display: block; width: 4.5rem; height: 3.5rem; object-fit: cover; border-radius: .3rem; }
  .placeholder { display: grid; place-items: center; align-content: center; gap: .5rem; aspect-ratio: 1; border-radius: .9rem;
    border: 2px dashed color-mix(in srgb, var(--area-recipes) 45%, var(--line)); color: var(--area-recipes); text-decoration: none;
    background: color-mix(in srgb, var(--area-recipes) 8%, var(--panel)); }
  .placeholder span { display: inline-flex; align-items: center; gap: .25rem; font-size: .8rem; color: var(--ink); }
  .placeholder:hover { border-style: solid; }

  .intro { display: grid; gap: .6rem; min-width: 0; }
  .eyebrow { margin: 0; font-size: .75rem; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; color: var(--muted); }
  .intro h2 { margin: 0; font-size: clamp(1.5rem, 1.1rem + 1.6vw, 2.1rem); line-height: 1.15; overflow-wrap: anywhere; }
  .description { margin: 0; color: var(--ink); font-size: 1.02rem; }
  /* One joined strip that the facts share equally, however many there are, so no empty cell is left beside them. */
  .facts { list-style: none; margin: .1rem 0; padding: 0; display: grid; grid-auto-flow: column; grid-auto-columns: minmax(0, 1fr);
    border-radius: .6rem; background: var(--panel); border: 1px solid var(--line); overflow: hidden; }
  .facts li { display: grid; grid-template-columns: auto minmax(0, 1fr); grid-template-rows: auto auto; column-gap: .45rem;
    align-items: center; padding: .5rem .65rem; min-width: 0; }
  .facts li + li { border-left: 1px solid var(--line); }
  .facts li :global(svg) { grid-row: span 2; color: var(--area-recipes); }
  .facts .label { font-size: .72rem; color: var(--muted); text-transform: uppercase; letter-spacing: .05em; }
  .facts .value { font-weight: 700; font-variant-numeric: tabular-nums; }
  .facts li.scaled { background: color-mix(in srgb, var(--area-recipes) 12%, var(--panel)); box-shadow: inset 0 -2px var(--area-recipes); }
  .row-actions { align-items: stretch; }
  .tags { list-style: none; margin: 0; padding: 0; }
  .tags + .tags { margin-top: .4rem; }
  /* Links to the library filtered to that cookbook; tall enough to tap. */
  .book { display: inline-flex; align-items: center; gap: .3rem; min-height: 1.75rem; color: var(--ink); text-decoration: none; }
  .book:hover { text-decoration: underline; }

  /* The jump links only help on narrow screens, where the parts stack; they stay at the top while scrolling. */
  .sections { display: none; }
  .cook { display: grid; gap: 1.25rem; grid-template-columns: minmax(15rem, 2fr) minmax(0, 3fr); align-items: start; margin-top: 1.5rem; }
  .cook :global(.ingredients) { position: sticky; top: 1rem; }
  .cook :global(.card), section.card { margin: 0; }
  .cook :global(section[tabindex]:focus), section[tabindex]:focus { outline: none; }
  section[tabindex]:focus-visible, .cook :global(section[tabindex]:focus-visible) { outline: 2px solid var(--accent); outline-offset: 2px; }
  #recipe-steps h3 { font-size: 1.1rem; }
  #recipe-more { margin-top: 1.25rem; }
  /* Numbered steps, each number in a round badge in the Recipes colour. */
  .steps { list-style: none; margin: .5rem 0 0; padding: 0; counter-reset: step; }
  .steps li { counter-increment: step; display: grid; grid-template-columns: 2rem minmax(0, 1fr); gap: .75rem; padding: .55rem 0;
    line-height: 1.6; }
  .steps li::before { content: counter(step); display: grid; place-items: center; width: 2rem; height: 2rem; border-radius: 50%;
    font-weight: 700; font-size: .9rem; color: var(--ink); background: color-mix(in srgb, var(--area-recipes) 18%, var(--panel)); }
  .notes { white-space: pre-line; margin: 0 0 .5rem; }
  .meta { display: grid; grid-template-columns: max-content 1fr; gap: .25rem 1rem; margin: 0; }
  .meta dt { color: var(--muted); }
  .meta dd { margin: 0; overflow-wrap: anywhere; }
  .meta a { display: inline-flex; align-items: center; gap: .25rem; }

  @media (max-width: 48rem) {
    .masthead { grid-template-columns: minmax(0, 1fr); }
    .masthead.bare { grid-template-columns: 6rem minmax(0, 1fr); }
    .cook { grid-template-columns: minmax(0, 1fr); }
    .cook :global(.ingredients) { position: static; }
    .sections { display: flex; gap: .35rem; position: sticky; top: 0; z-index: 4; margin: .75rem -1rem 0; padding: .5rem 1rem;
      background: var(--bg); border-bottom: 1px solid var(--line); overflow-x: auto; }
    .sections a { border: 1px solid var(--line); border-radius: 999px; padding: .25rem .75rem; font-size: .9rem; white-space: nowrap;
      text-decoration: none; }
    section[tabindex], .cook :global(section[tabindex]) { scroll-margin-top: 3.5rem; }
  }
  /* Phones: "Start cooking" takes a row of its own, with "Add to plan", the heart and ⋯ beneath it (the ⋯ menu unfolds
     under them); each fact stacks its label over its value; and the photo runs edge to edge. */
  @media (max-width: 40rem) {
    .row-actions { display: grid; grid-template-columns: minmax(0, 1fr) auto auto; }
    .row-actions .grow { justify-content: center; }
    .row-actions .primary { grid-column: 1 / -1; }
    .row-actions > :global(.favourite) { min-width: 2.75rem; justify-content: center; }
    .facts li { grid-template-columns: minmax(0, 1fr); grid-template-rows: auto auto auto; justify-items: center; text-align: center;
      row-gap: .1rem; padding: .5rem .3rem; }
    .facts li :global(svg) { grid-row: auto; }
    .facts .label { font-size: .66rem; }
    .facts .value { overflow-wrap: anywhere; }
    .masthead:not(.bare) .cover { border-radius: 0; margin: 0 -1rem; width: calc(100% + 2rem); max-width: none; }
    .masthead.bare .intro { grid-column: 1 / -1; }
  }

  /* On paper: one small photo, ingredients beside the steps, and nothing to click. */
  @media print {
    .back-row, .row-actions, .sections, .strip, .placeholder { display: none; }
    .masthead, .masthead.bare { grid-template-columns: 6cm minmax(0, 1fr); }
    .cover { box-shadow: none; }
    .cook { grid-template-columns: minmax(0, 2fr) minmax(0, 3fr); }
    .cook :global(.ingredients) { position: static; }
    .steps li { break-inside: avoid; }
  }
</style>
