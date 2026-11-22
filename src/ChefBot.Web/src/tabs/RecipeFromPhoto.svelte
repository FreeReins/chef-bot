<!-- The Photo tab of the Add a recipe dialog, /recipes/new/photo: choose photos of a recipe (a cookbook page, a card or
     a screenshot), have the AI model set up for photos read them, then check what it read in the recipe form before
     saving. Nothing is saved until Save. -->
<script>
  import { app, run, toast, isAdmin, loadRecipes } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { go } from '../lib/addresses.js';
  import { pendingClick } from '../lib/pending.js';
  import { shrinkPhoto } from '../lib/photos.js';
  import RecipeForm from './RecipeForm.svelte';
  import Camera from '@lucide/svelte/icons/camera';
  import X from '@lucide/svelte/icons/x';
  import ScanText from '@lucide/svelte/icons/scan-text';

  /** close(): leave the dialog. cookbookId: a cookbook to put the recipe in, or null. */
  let { close, cookbookId = null } = $props();

  const MAX_PAGES = 4;

  /** The photos chosen, in page order: { file, url } with url a preview made on this device. */
  let photos = $state([]);
  /** What the model read: { draft, readWith }, once it has. */
  let read = $state(null);
  let keepPhoto = $state(true);

  function choose(event) {
    const input = event.currentTarget;
    const chosen = [...(input.files ?? [])];
    input.value = ''; // so the same file can be chosen again
    const room = MAX_PAGES - photos.length;
    if (chosen.length > room) toast(`Choose up to ${MAX_PAGES} photos: the pages of one recipe.`);
    for (const file of chosen.slice(0, room)) photos.push({ file, url: URL.createObjectURL(file) });
  }

  function removePhoto(index) {
    URL.revokeObjectURL(photos[index].url);
    photos.splice(index, 1);
  }

  const readPhotos = () => run(async () => {
    const encoded = await Promise.all(photos.map((photo) => shrinkPhoto(photo.file)));
    read = await api('POST', '/api/recipes/import/photo', { photos: encoded });
  });

  function startOver() {
    read = null;
  }

  async function save(draft) {
    let saved = false;
    await run(async () => {
      const recipe = await api('POST', '/api/recipes', { ...draft, cookbookIds: cookbookId ? [cookbookId] : null });
      saved = true;
      let note = `Added ${recipe.name}.`;
      if (keepPhoto && photos.length) {
        try {
          await api('POST', `/api/recipes/${recipe.id}/images`, { data: await shrinkPhoto(photos[0].file, 2400) });
        } catch (error) {
          note = `Added ${recipe.name}, but its photo wasn't kept: ${error.message}`;
        }
      }
      await loadRecipes();
      toast(note);
      await go(`/recipes/${recipe.id}`, { replace: true });
    });
    return saved;
  }
</script>

{#if !app.aiFeatures.recipeFromPhoto}
  <div class="card empty">
    <span class="empty-icon" style="--area: var(--area-recipes)" aria-hidden="true"><ScanText size={26} /></span>
    <p>Reading recipes from photos needs an AI model.</p>
    {#if isAdmin()}
      <p class="muted">Set one up under <a href="/settings/assistants">Settings, AI assistants</a>: Claude, OpenAI, or a model
        on your own network such as Ollama.</p>
    {:else}
      <p class="muted">Ask whoever looks after ChefBot to set one up under Settings, AI assistants.</p>
    {/if}
  </div>
{:else if read}
  <p class="read-note" role="status">Read with {read.readWith}. Check everything against the photo before saving: AI can misread.</p>
  {#if photos.length}
    <label class="keep"><input type="checkbox" bind:checked={keepPhoto}> Keep the first photo with the recipe</label>
  {/if}
  <RecipeForm draft={read.draft} button="Save recipe" save={save} cancel={startOver} plain />
{:else}
  <div class="pick">
    <label for="recipe-photos">Photos of one recipe, up to {MAX_PAGES} pages in order</label>
    <input id="recipe-photos" type="file" accept="image/*" capture="environment" multiple onchange={choose}
           disabled={photos.length >= MAX_PAGES}>
    {#if photos.length}
      <ul class="pages">
        {#each photos as photo, index (photo.url)}
          <li>
            <img src={photo.url} alt="Page {index + 1}">
            <button type="button" class="small icon" onclick={() => removePhoto(index)} aria-label="Remove page {index + 1}"><X size={14} /></button>
          </li>
        {/each}
      </ul>
    {/if}
    <p class="muted">A cookbook page, a recipe card or a screenshot. Reading takes up to a minute, then you can check and
      fix what was read before it is saved.</p>
    <div class="row-actions">
      <button type="button" class="primary with-icon" disabled={photos.length === 0} onclick={pendingClick(readPhotos)}>
        <Camera size={16} />Read recipe
      </button>
      <button type="button" onclick={close}>Cancel</button>
    </div>
  </div>
{/if}

<style>
  .pick { display: grid; gap: .6rem; }
  .pick p { margin: 0; }
  .pages { list-style: none; margin: 0; padding: 0; display: flex; flex-wrap: wrap; gap: .5rem; }
  .pages li { position: relative; }
  .pages img { width: 6rem; height: 6rem; object-fit: cover; border-radius: .5rem; border: 1px solid var(--line); display: block; }
  .pages button { position: absolute; top: .25rem; right: .25rem; background: var(--panel); }
  .read-note { margin: 0 0 .5rem; padding: .5rem .75rem; border-left: 3px solid var(--accent); background: var(--accent-bg); border-radius: .4rem; }
  .keep { display: flex; gap: .4rem; align-items: center; margin-bottom: .5rem; }
</style>
