<!-- A recipe's photos, /recipes/{id}/photos, in a dialog over its page: add one from this device or remove one. The first
     photo is the cover shown in the recipe list. -->
<script>
  import { run, toast, ask } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import Modal from '../components/Modal.svelte';
  import Plus from '@lucide/svelte/icons/plus';
  import Trash2 from '@lucide/svelte/icons/trash-2';
  import { MAX_PHOTO_MB, base64 } from '../lib/photos.js';

  /** recipe: the RecipeResponse. onsaved(recipe, shownId): it came back changed. onclose(): asked to close. */
  let { recipe, onsaved, onclose } = $props();

  let uploading = $state(false);
  const images = $derived(recipe.images ?? []);

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
      onsaved(saved, saved.images.at(-1)?.id ?? null);
      toast('Photo added.');
    });
    uploading = false;
  }

  async function removePhoto(image) {
    if (!(await ask({ title: 'Remove this photo?', message: `It is deleted from ${recipe.name} for good.`,
      confirmLabel: 'Remove', danger: true }))) return;
    run(async () => {
      onsaved(await api('DELETE', `/api/recipes/${recipe.id}/images/${image.id}`), null);
      toast('Photo removed.');
    });
  }
</script>

<Modal title="Photos of {recipe.name}" {onclose}>
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
      <Plus size={16} />{uploading ? 'Adding…' : 'Add photo'}
    </label>
    <span class="muted">JPEG, PNG, WebP or GIF, up to {MAX_PHOTO_MB} MB.</span>
    <button type="button" class="done" onclick={onclose}>Done</button>
  </div>
</Modal>

<style>
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
