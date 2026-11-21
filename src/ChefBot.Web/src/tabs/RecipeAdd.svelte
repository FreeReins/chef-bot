<!-- The Add a recipe dialog over the Recipes tab, with a tab for each way in: from a link, typed in, or from files.
     Each tab has its own address, /recipes/new/{link|write|files}; switching tabs replaces it, so closing the dialog
     and pressing Back returns to wherever it was opened from. -->
<script>
  import { tick } from 'svelte';
  import { app, run, toast, loadRecipes } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { go, closeDialog } from '../lib/addresses.js';
  import Modal from '../components/Modal.svelte';
  import RecipeForm from './RecipeForm.svelte';
  import FileImport from '../components/FileImport.svelte';
  import Link from '@lucide/svelte/icons/link';
  import PencilLine from '@lucide/svelte/icons/pencil-line';
  import FileUp from '@lucide/svelte/icons/file-up';

  const TABS = [
    { key: 'link', label: 'From a link', icon: Link },
    { key: 'write', label: 'Type it in', icon: PencilLine },
    { key: 'files', label: 'Import files', icon: FileUp },
  ];

  const current = $derived(app.recipeDialog?.replace('new-', '') ?? 'link');
  let panel = $state();

  const close = () => closeDialog('/recipes');

  async function switchTo(event) {
    if (event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
    event.preventDefault();
    await go(new URL(event.currentTarget.href).pathname, { replace: true });
    await tick();
    panel?.querySelector('input, textarea, select')?.focus();
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
      await go(`/recipes/${recipe.id}`, { replace: true });
    });
    reading = false;
  }

  async function add(draft) {
    let saved = false;
    await run(async () => {
      const recipe = await api('POST', '/api/recipes', draft);
      await loadRecipes();
      saved = true;
      toast(`Added ${recipe.name}.`);
      await go(`/recipes/${recipe.id}`, { replace: true });
    });
    return saved;
  }
</script>

<Modal wide title="Add a recipe" onclose={close}>
  {#snippet nav()}
    <nav class="sub-nav" aria-label="Ways to add a recipe">
      {#each TABS as tab (tab.key)}
        <a href="/recipes/new/{tab.key}" aria-current={tab.key === current ? 'page' : undefined} onclick={switchTo}>
          <tab.icon size={16} />{tab.label}
        </a>
      {/each}
    </nav>
  {/snippet}

  <div bind:this={panel}>
    {#if current === 'link'}
      <form class="link" onsubmit={addFromLink}>
        <label for="recipe-link">Address of a recipe page</label>
        <div class="link-row">
          <input id="recipe-link" type="text" inputmode="url" autocomplete="url" required
                 placeholder="https://example.com/recipes/soup" bind:value={link} disabled={reading}>
          <button type="submit" class="primary" disabled={reading}>{reading ? 'Reading…' : 'Add'}</button>
        </div>
        <p class="muted">Works with most recipe sites. ChefBot saves the recipe straight away; you can edit it after.</p>
      </form>
    {:else if current === 'write'}
      <RecipeForm button="Save recipe" save={add} cancel={close} plain />
    {:else}
      <div class="import">
        <FileImport />
        <div class="row-actions"><button type="button" onclick={close}>Done</button></div>
      </div>
    {/if}
  </div>
</Modal>

<style>
  .sub-nav { display: flex; flex-wrap: wrap; gap: .35rem; }
  .sub-nav a { display: inline-flex; align-items: center; gap: .4rem; border: 1px solid var(--line); border-radius: 999px;
    padding: .3rem .8rem; color: var(--muted); text-decoration: none; font-size: .9rem; white-space: nowrap; }
  .sub-nav a :global(svg) { flex: none; }
  .sub-nav a:hover { color: var(--ink); border-color: var(--accent); }
  .sub-nav a:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }
  .sub-nav a[aria-current="page"] { color: var(--accent-ink); background: var(--accent); border-color: var(--accent); font-weight: 600; }
  .link, .import { display: grid; gap: .6rem; }
  .link p { margin: 0; }
  .link-row { display: flex; gap: .5rem; }
  .link-row input { flex: 1; min-width: 0; }
  .import .row-actions { justify-content: flex-end; }
</style>
