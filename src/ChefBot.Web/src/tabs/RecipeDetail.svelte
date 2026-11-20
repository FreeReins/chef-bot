<!-- One recipe on its own page, /recipes/{id}, so it can be bookmarked. Shows it, its photos and steps, and lets it be
     edited or archived. -->
<script>
  import { app, run, loadRecipes, toast, ask } from '../lib/app.svelte.js';
  import { go } from '../lib/addresses.js';
  import { api } from '../lib/api.js';
  import Chip from '../components/Chip.svelte';
  import RecipeForm from './RecipeForm.svelte';
  import ArrowLeft from '@lucide/svelte/icons/arrow-left';
  import ExternalLink from '@lucide/svelte/icons/external-link';
  import ImagePlus from '@lucide/svelte/icons/image-plus';
  import Trash2 from '@lucide/svelte/icons/trash-2';

  const MAX_PHOTO_MB = 5;

  let { id } = $props();

  const recipe = $derived(app.recipes.find((candidate) => candidate.id === id));
  let editing = $state(false);
  let shownId = $state(null); // the photo shown large; the cover when none was picked
  let uploading = $state(false);

  const images = $derived(recipe?.images ?? []);
  const shown = $derived(images.find((image) => image.id === shownId) ?? images[0]);
  const times = $derived(recipe ? [
    ['Prep', recipe.prepMinutes], ['Cook', recipe.cookMinutes], ['Total', recipe.totalMinutes],
  ].filter(([, minutes]) => minutes) : []);
  const sourceHost = $derived.by(() => {
    try { return recipe?.sourceUrl ? new URL(recipe.sourceUrl).host : null; } catch { return recipe.sourceUrl; }
  });

  /** "45 min", "1 h", "1 h 10 min". */
  function duration(minutes) {
    const hours = Math.floor(minutes / 60);
    const rest = minutes % 60;
    return [hours ? `${hours} h` : '', rest ? `${rest} min` : ''].filter(Boolean).join(' ');
  }

  async function save(draft) {
    let saved = false;
    await run(async () => {
      await api('PUT', `/api/recipes/${recipe.id}`, draft);
      await loadRecipes();
      editing = false;
      saved = true;
      toast('Recipe saved.');
    });
    return saved;
  }

  async function archive() {
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
      await loadRecipes();
      shownId = saved.images.at(-1)?.id ?? null;
      toast('Photo added.');
    });
    uploading = false;
  }

  async function removePhoto(image) {
    if (!(await ask({ title: 'Remove this photo?', message: `It is deleted from ${recipe.name} for good.`,
      confirmLabel: 'Remove', danger: true }))) return;
    run(async () => {
      await api('DELETE', `/api/recipes/${recipe.id}/images/${image.id}`);
      await loadRecipes();
      shownId = null;
      toast('Photo removed.');
    });
  }
</script>

<p style="margin:1rem 0 0"><a class="back" href="/recipes"><ArrowLeft size={16} />All recipes</a></p>

{#if !recipe}
  <div class="card empty">
    <h2>That recipe isn't in your library</h2>
    <p class="muted">It may have been archived, or the link is from another ChefBot. Pick one from your recipes instead.</p>
  </div>
{:else if editing}
  <h2>Edit {recipe.name}</h2>
  {#if recipe.origin === 'Mealie'}
    <p class="muted">Saving makes this a ChefBot recipe, so later Mealie imports leave your changes alone.</p>
  {/if}
  <RecipeForm {recipe} button="Save changes" {save} cancel={() => (editing = false)} />
{:else}
  <div class="row" style="margin-top:1rem">
    <h2 style="margin:0">{recipe.name}</h2>
    <div class="row-actions">
      <button class="small" onclick={() => (editing = true)}>Edit</button>
      <button class="small" onclick={archive}>Archive</button>
    </div>
  </div>
  <div class="chips">
    <Chip>{recipe.course}</Chip>
    <Chip>Serves {recipe.servings}</Chip>
    {#each times as [label, minutes] (label)}<Chip>{label} {duration(minutes)}</Chip>{/each}
    {#if recipe.isSlowCooker}<Chip tone="good">Slow cooker</Chip>{/if}
    {#if recipe.origin === 'Mealie'}<Chip>From Mealie</Chip>{/if}
    {#each recipe.tags as tag (tag)}<Chip>#{tag}</Chip>{/each}
  </div>

  {#if recipe.description}<p class="description">{recipe.description}</p>{/if}

  <div class="card photos">
    {#if shown}
      <figure>
        <img class="cover" src={shown.url} alt="{recipe.name}">
        <button class="small remove" onclick={() => removePhoto(shown)} aria-label="Remove this photo"><Trash2 size={16} /></button>
      </figure>
      {#if images.length > 1}
        <div class="strip">
          {#each images as image, index (image.id)}
            <button class="thumb" class:current={image.id === shown.id} onclick={() => (shownId = image.id)}
                    aria-label="Show photo {index + 1}">
              <img src={image.url} alt="" loading="lazy">
            </button>
          {/each}
        </div>
      {/if}
    {:else}
      <p class="muted">No photos yet.</p>
    {/if}
    <label class="add-photo" class:busy={uploading}>
      <input type="file" accept="image/jpeg,image/png,image/webp,image/gif" onchange={addPhoto} disabled={uploading}>
      <ImagePlus size={16} />{uploading ? 'Adding…' : 'Add photo'}
    </label>
    {#if recipe.origin === 'Mealie'}
      <p class="muted">Adding or removing a photo makes this a ChefBot recipe, so later Mealie imports leave it alone.</p>
    {/if}
  </div>

  <div class="columns">
    <div class="card">
      <h3>Ingredients ({recipe.ingredients.length})</h3>
      <ul class="plain">
        {#each recipe.ingredients as line, index (index)}<li>{line}</li>{/each}
      </ul>
    </div>
    <div class="card">
      <h3>Steps</h3>
      {#if recipe.instructions.length}
        <ol class="steps">
          {#each recipe.instructions as step, index (index)}<li>{step}</li>{/each}
        </ol>
      {:else}
        <p class="muted">No steps yet. Choose Edit to add them.</p>
      {/if}
    </div>
  </div>

  {#if recipe.notes}
    <div class="card">
      <h3>Notes</h3>
      <p class="notes">{recipe.notes}</p>
    </div>
  {/if}

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
{/if}

<style>
  .back { display: inline-flex; align-items: center; gap: .3rem; }
  .description { margin: .75rem 0 .25rem; }
  .photos figure { position: relative; margin: 0; }
  .cover { display: block; width: 100%; max-height: 24rem; object-fit: cover; border-radius: .4rem; }
  .remove { position: absolute; top: .5rem; right: .5rem; display: inline-flex; align-items: center; }
  .strip { display: flex; gap: .4rem; margin-top: .5rem; overflow-x: auto; }
  .thumb { padding: 0; border: 2px solid transparent; border-radius: .35rem; background: none; flex: 0 0 auto; }
  .thumb.current { border-color: var(--accent); }
  .thumb img { display: block; width: 4.5rem; height: 3.5rem; object-fit: cover; border-radius: .25rem; }
  .add-photo { display: inline-flex; align-items: center; gap: .35rem; margin-top: .6rem; cursor: pointer; color: var(--accent); }
  .add-photo input { position: absolute; width: 1px; height: 1px; opacity: 0; }
  .add-photo:focus-within { outline: 2px solid var(--accent); outline-offset: 2px; border-radius: .25rem; }
  .add-photo.busy { cursor: progress; opacity: .7; }
  .steps { margin: 0; padding-left: 1.4rem; }
  .steps li { padding: .3rem 0; }
  .notes { white-space: pre-line; margin: 0; }
  .meta { display: grid; grid-template-columns: max-content 1fr; gap: .25rem 1rem; margin: .5rem 0 1rem; }
  .meta dt { color: var(--muted); }
  .meta dd { margin: 0; overflow-wrap: anywhere; }
  .meta a { display: inline-flex; align-items: center; gap: .25rem; }
</style>
