<!-- One recipe on its own page, /recipes/{id}, so it can be bookmarked: its photos, ingredients and steps to read.
     Editing it (/recipes/{id}/edit) and managing its photos (/recipes/{id}/photos) open in dialogs over the page,
     and cooking mode (/recipes/{id}/cook, RecipeCook.svelte) over the whole screen; sharing it as text, printing it, downloading it
     (as a PDF, Markdown, text or a schema.org file, RecipeDownloads.svelte) and archiving it are in the ⋯ menu. -->
<script>
  import { tick } from 'svelte';
  import { app, run, loadRecipes, toast, ask } from '../lib/app.svelte.js';
  import { go, closeDialog } from '../lib/addresses.js';
  import { api } from '../lib/api.js';
  import { duration } from '../lib/format.js';
  import { canShare, share, recipeText } from '../lib/exporting.js';
  import Chip from '../components/Chip.svelte';
  import Modal from '../components/Modal.svelte';
  import RecipeForm from './RecipeForm.svelte';
  import RecipeCook from './RecipeCook.svelte';
  import RecipeDownloads from '../components/RecipeDownloads.svelte';
  import CookingPot from '@lucide/svelte/icons/cooking-pot';
  import ArrowLeft from '@lucide/svelte/icons/arrow-left';
  import ExternalLink from '@lucide/svelte/icons/external-link';
  import ImagePlus from '@lucide/svelte/icons/image-plus';
  import Images from '@lucide/svelte/icons/images';
  import Pencil from '@lucide/svelte/icons/pencil';
  import Ellipsis from '@lucide/svelte/icons/ellipsis';
  import Archive from '@lucide/svelte/icons/archive';
  import Share2 from '@lucide/svelte/icons/share-2';
  import Copy from '@lucide/svelte/icons/copy';
  import Printer from '@lucide/svelte/icons/printer';
  import Trash2 from '@lucide/svelte/icons/trash-2';
  import LoaderCircle from '@lucide/svelte/icons/loader-circle';

  const MAX_PHOTO_MB = 5;

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
  let uploading = $state(false);
  let menuOpen = $state(false);
  let menu = $state();

  const base = $derived(`/recipes/${id}`);
  const images = $derived(recipe?.images ?? []);
  const shown = $derived(images.find((image) => image.id === shownId) ?? images[0]);
  const times = $derived(recipe ? [
    ['Prep', recipe.prepMinutes], ['Cook', recipe.cookMinutes], ['Total', recipe.totalMinutes],
  ].filter(([, minutes]) => minutes) : []);
  const sourceHost = $derived.by(() => {
    try { return recipe?.sourceUrl ? new URL(recipe.sourceUrl).host : null; } catch { return recipe.sourceUrl; }
  });
  const hasMore = $derived(Boolean(recipe && (recipe.notes || recipe.yieldText || recipe.author || recipe.sourceUrl)));
  const sections = $derived([
    { key: 'ingredients', label: 'Ingredients' },
    { key: 'steps', label: 'Steps' },
    ...(hasMore ? [{ key: 'more', label: 'Notes and source' }] : []),
  ]);

  /** Shows a recipe the server sent back, and names the browser tab after it. */
  function show(found) {
    recipe = found;
    app.recipeName = found?.name ?? null;
  }

  // The page is rebuilt for each recipe, so this runs once per id. A recipe that is gone shows the empty state.
  app.recipeName = null;
  run(async () => {
    try {
      show(await api('GET', `/api/recipes/${id}`));
    } catch (error) {
      if (error.status !== 404) throw error;
      show(null);
    } finally {
      loaded = true;
    }
  });

  const close = () => closeDialog(base);

  // In-page links: the address stays the same, the section scrolls into view and takes focus.
  async function jump(event, key) {
    event.preventDefault();
    const target = document.getElementById(`recipe-${key}`);
    target?.scrollIntoView({ block: 'start', behavior: matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth' });
    await tick();
    target?.focus({ preventScroll: true });
  }

  // The ⋯ menu closes on Escape, on a click elsewhere, and once focus leaves it.
  $effect(() => {
    if (!menuOpen) return;
    const away = (event) => { if (!menu?.contains(event.target)) menuOpen = false; };
    const escape = (event) => { if (event.key === 'Escape') { menuOpen = false; menu?.querySelector('button')?.focus(); } };
    document.addEventListener('pointerdown', away);
    document.addEventListener('focusin', away);
    document.addEventListener('keydown', escape);
    return () => {
      document.removeEventListener('pointerdown', away);
      document.removeEventListener('focusin', away);
      document.removeEventListener('keydown', escape);
    };
  });

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

  // Sharing hands the recipe as text to the phone's share sheet; without one it is copied to paste anywhere.
  function shareRecipe() {
    menuOpen = false;
    run(async () => {
      const done = await share(recipe.name, recipeText(recipe));
      if (done === 'copied') toast(`${recipe.name} was copied as text. Paste it wherever you need it.`);
    });
  }

  function print() {
    menuOpen = false;
    window.print();
  }

  async function archive() {
    menuOpen = false;
    if (!(await ask({ title: `Archive ${recipe.name}?`, message: "It won't be planned again, and past plans keep showing it.",
      confirmLabel: 'Archive', danger: true }))) return;
    const name = recipe.name;
    run(async () => {
      await api('DELETE', `/api/recipes/${recipe.id}`);
      await loadRecipes();
      await go('/recipes');
      toast(`${name} was archived and won't be planned again.`);
    });
  }

  /** The file's bytes as base64, the way the API takes them. */
  function base64(file) {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => resolve(String(reader.result).split(',', 2)[1] ?? '');
      reader.onerror = () => reject(new Error(`${file.name} couldn't be read. Choose the file again.`));
      reader.readAsDataURL(file);
    });
  }

  async function addPhoto(event) {
    const input = event.currentTarget;
    const file = input.files?.[0];
    input.value = ''; // so the same file can be chosen again
    if (!file) return;
    if (file.size > MAX_PHOTO_MB * 1024 * 1024) {
      toast(`${file.name} is larger than ${MAX_PHOTO_MB} MB. Choose a smaller photo.`);
      return;
    }
    uploading = true;
    await run(async () => {
      const saved = await api('POST', `/api/recipes/${recipe.id}/images`, { data: await base64(file) });
      show(saved);
      shownId = saved.images.at(-1)?.id ?? null;
      toast('Photo added.');
    });
    uploading = false;
  }

  async function removePhoto(image) {
    if (!(await ask({ title: 'Remove this photo?', message: `It is deleted from ${recipe.name} for good.`,
      confirmLabel: 'Remove', danger: true }))) return;
    run(async () => {
      show(await api('DELETE', `/api/recipes/${recipe.id}/images/${image.id}`));
      shownId = null;
      toast('Photo removed.');
    });
  }
</script>

<p class="back-row"><a class="button back" href="/recipes" onclick={backToList}><ArrowLeft size={16} />Back to recipes</a></p>

{#if !loaded}
  <p class="loading muted" role="status" aria-label="Loading the recipe"><LoaderCircle size={24} /></p>
{:else if !recipe}
  <div class="card empty">
    <h2>That recipe isn't in your library</h2>
    <p class="muted">It may have been archived, or the link is from another ChefBot. Pick one from your recipes instead.</p>
  </div>
{:else}
  <article class="recipe">
    {#if shown}
      <div class="hero">
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
      </div>
    {/if}

    <header class="title-row">
      <h2>{recipe.name}</h2>
      <div class="row-actions">
        <a class="button primary with-icon" href="{base}/cook"><CookingPot size={16} />Start cooking</a>
        <a class="button with-icon" href="{base}/edit"><Pencil size={16} />Edit</a>
        <a class="button with-icon" href="{base}/photos"><Images size={16} />Photos ({images.length})</a>
        <div class="menu" bind:this={menu}>
          <button type="button" class="with-icon" aria-label="More actions for {recipe.name}" aria-expanded={menuOpen}
                  aria-controls="recipe-menu" onclick={() => (menuOpen = !menuOpen)}><Ellipsis size={18} /></button>
          <div id="recipe-menu" class="menu-panel" hidden={!menuOpen}>
            {#if canShare()}
              <button type="button" class="with-icon" onclick={shareRecipe}><Share2 size={16} />Share recipe</button>
            {:else}
              <button type="button" class="with-icon" onclick={shareRecipe}><Copy size={16} />Copy as text</button>
            {/if}
            <button type="button" class="with-icon" onclick={print}><Printer size={16} />Print recipe</button>
            <RecipeDownloads href="/api/recipes/{recipe.id}/export" onpick={() => (menuOpen = false)} />
            <button type="button" class="with-icon bad-text" onclick={archive}><Archive size={16} />Archive recipe</button>
          </div>
        </div>
      </div>
    </header>

    <div class="chips">
      <Chip>{recipe.course}</Chip>
      <Chip>Serves {recipe.servings}</Chip>
      {#each times as [label, minutes] (label)}<Chip>{label} {duration(minutes)}</Chip>{/each}
      {#if recipe.isSlowCooker}<Chip tone="good">Slow cooker</Chip>{/if}
      {#each recipe.tags as tag (tag)}<Chip>#{tag}</Chip>{/each}
    </div>

    {#if recipe.description}<p class="description">{recipe.description}</p>{/if}
    {#if !shown}
      <p class="muted"><a href="{base}/photos">Add a photo</a> so this recipe is easy to spot in the list.</p>
    {/if}

    <nav class="sections" aria-label="Parts of this recipe">
      {#each sections as section (section.key)}
        <a href="#recipe-{section.key}" onclick={(event) => jump(event, section.key)}>{section.label}</a>
      {/each}
    </nav>

    <div class="cook">
      <section class="card ingredients" id="recipe-ingredients" tabindex="-1" aria-labelledby="recipe-ingredients-title">
        <h3 id="recipe-ingredients-title">Ingredients ({recipe.ingredients.length})</h3>
        <ul class="plain">
          {#each recipe.ingredients as line, index (index)}<li>{line}</li>{/each}
        </ul>
      </section>
      <section class="card" id="recipe-steps" tabindex="-1" aria-labelledby="recipe-steps-title">
        <h3 id="recipe-steps-title">Steps</h3>
        {#if recipe.instructions.length}
          <ol class="steps">
            {#each recipe.instructions as step, index (index)}<li>{step}</li>{/each}
          </ol>
        {:else}
          <p class="muted">No steps yet. <a href="{base}/edit">Edit the recipe</a> to add them.</p>
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
  </article>

  {#if app.recipeDialog === 'cook'}
    <RecipeCook {recipe} onclose={close} />
  {:else if app.recipeDialog === 'edit'}
    <Modal wide title="Edit {recipe.name}" onclose={close}>
      <RecipeForm {recipe} button="Save changes" {save} cancel={close} plain />
    </Modal>
  {:else if app.recipeDialog === 'photos'}
    <Modal title="Photos of {recipe.name}" onclose={close}>
      {#if images.length}
        <p class="muted">The first photo is the one shown in the recipe list.</p>
        <ul class="photo-grid">
          {#each images as image, index (image.id)}
            <li>
              <img src={image.url} alt="Photo {index + 1}" loading="lazy">
              <button type="button" class="small remove-photo" onclick={() => removePhoto(image)}
                      aria-label="Remove photo {index + 1}"><Trash2 size={16} /></button>
            </li>
          {/each}
        </ul>
      {:else}
        <p class="muted">No photos yet.</p>
      {/if}
      <div class="photo-actions">
        <label class="add-photo" class:busy={uploading}>
          <input type="file" accept="image/jpeg,image/png,image/webp,image/gif" onchange={addPhoto} disabled={uploading}>
          <ImagePlus size={16} />{uploading ? 'Adding…' : 'Add photo'}
        </label>
        <span class="muted">JPEG, PNG, WebP or GIF, up to {MAX_PHOTO_MB} MB.</span>
        <button type="button" class="done" onclick={close}>Done</button>
      </div>
    </Modal>
  {/if}
{/if}

<style>
  .back-row { margin: 1rem 0 0; }
  .with-icon { display: inline-flex; align-items: center; gap: .35rem; }

  .hero { margin-top: .75rem; }
  .cover { display: block; width: 100%; max-height: 26rem; object-fit: cover; border-radius: .6rem; background: var(--chip); }
  .strip { display: flex; gap: .4rem; margin-top: .5rem; overflow-x: auto; }
  .thumb { padding: 0; border: 2px solid transparent; border-radius: .35rem; background: none; flex: 0 0 auto; }
  .thumb.current { border-color: var(--accent); }
  .thumb img { display: block; width: 4.5rem; height: 3.5rem; object-fit: cover; border-radius: .25rem; }

  .title-row { display: flex; flex-wrap: wrap; gap: .5rem 1rem; justify-content: space-between; align-items: center; margin-top: 1rem; }
  .title-row h2 { margin: 0; min-width: 0; flex: 1 1 14rem; }
  .menu { position: relative; }
  .menu > button { height: 100%; }
  .menu-panel { position: absolute; right: 0; top: calc(100% + .3rem); z-index: 5; min-width: 12rem; padding: .3rem;
    background: var(--panel); border: 1px solid var(--line); border-radius: .5rem; box-shadow: 0 6px 24px rgb(0 0 0 / .18); }
  .menu-panel button { width: 100%; border: 0; justify-content: flex-start; font-size: .95rem; }
  .menu-panel button:hover { background: var(--chip); }
  .description { margin: .75rem 0 .25rem; }

  /* The jump links only help on narrow screens, where the parts stack; they stay at the top while scrolling. */
  .sections { display: none; }
  .cook { display: grid; gap: 1.25rem; grid-template-columns: minmax(15rem, 2fr) minmax(0, 3fr); align-items: start; margin-top: 1rem; }
  .ingredients { position: sticky; top: 1rem; }
  .cook .card, section.card { margin: 0; }
  section[tabindex]:focus { outline: none; }
  section[tabindex]:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }
  #recipe-more { margin-top: 1.25rem; }
  .steps { margin: 0; padding-left: 1.4rem; }
  .steps li { padding: .35rem 0; }
  .notes { white-space: pre-line; margin: 0 0 .5rem; }
  .meta { display: grid; grid-template-columns: max-content 1fr; gap: .25rem 1rem; margin: 0; }
  .meta dt { color: var(--muted); }
  .meta dd { margin: 0; overflow-wrap: anywhere; }
  .meta a { display: inline-flex; align-items: center; gap: .25rem; }

  @media (max-width: 48rem) {
    .cook { grid-template-columns: minmax(0, 1fr); }
    .ingredients { position: static; }
    .sections { display: flex; gap: .35rem; position: sticky; top: 0; z-index: 4; margin: .75rem -1rem 0; padding: .5rem 1rem;
      background: var(--bg); border-bottom: 1px solid var(--line); overflow-x: auto; }
    .sections a { border: 1px solid var(--line); border-radius: 999px; padding: .25rem .75rem; font-size: .9rem; white-space: nowrap;
      text-decoration: none; }
    section[tabindex] { scroll-margin-top: 3.5rem; }
  }

  /* On paper: one small photo, ingredients beside the steps, and nothing to click. */
  @media print {
    .back-row, .row-actions, .sections, .strip { display: none; }
    .cover { max-height: 7cm; }
    .cook { grid-template-columns: minmax(0, 2fr) minmax(0, 3fr); }
    .ingredients { position: static; }
    .steps li, .ingredients li { break-inside: avoid; }
  }

  .photo-grid { list-style: none; margin: .5rem 0 0; padding: 0; display: grid; gap: .6rem;
    grid-template-columns: repeat(auto-fill, minmax(8rem, 1fr)); }
  .photo-grid li { position: relative; }
  .photo-grid img { display: block; width: 100%; aspect-ratio: 4 / 3; object-fit: cover; border-radius: .4rem; background: var(--chip); }
  .photo-grid li:first-child img { outline: 2px solid var(--accent); outline-offset: 2px; }
  .remove-photo { position: absolute; top: .35rem; right: .35rem; display: inline-flex; align-items: center; }
  .photo-actions { display: flex; flex-wrap: wrap; gap: .5rem .75rem; align-items: center; margin-top: 1rem; }
  .photo-actions .done { margin-left: auto; }
  .add-photo { position: relative; display: inline-flex; align-items: center; gap: .35rem; cursor: pointer; color: var(--ink);
    font-size: 1rem; border: 1px solid var(--line); border-radius: .4rem; background: var(--panel); padding: .4rem .8rem; }
  .add-photo:hover { border-color: var(--accent); }
  .add-photo input { position: absolute; width: 1px; height: 1px; opacity: 0; }
  .add-photo:focus-within { outline: 2px solid var(--accent); outline-offset: 2px; }
  .add-photo.busy { cursor: progress; }
</style>
