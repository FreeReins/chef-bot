<!-- Above the library while one cookbook is chosen: its name and description, and renaming, downloading and removing
     it. Removing a cookbook leaves its recipes in the library. -->
<script>
  import { run, toast, ask, updateCookbook, removeCookbook } from '../lib/app.svelte.js';
  import { pending, pendingClick } from '../lib/pending.js';
  import RecipeDownloads from './RecipeDownloads.svelte';
  import HelpLink from './HelpLink.svelte';
  import BookMarked from '@lucide/svelte/icons/book-marked';
  import Pencil from '@lucide/svelte/icons/pencil';
  import Download from '@lucide/svelte/icons/download';
  import ChevronDown from '@lucide/svelte/icons/chevron-down';
  import Trash2 from '@lucide/svelte/icons/trash-2';

  /** cookbook: the CookbookResponse chosen. onremoved(): it was removed, so the filter should let go of it. */
  let { cookbook, onremoved = () => {} } = $props();

  let renaming = $state(false);
  let downloadsOpen = $state(false);
  let name = $state('');
  let description = $state('');

  function startRenaming() {
    name = cookbook.name;
    description = cookbook.description ?? '';
    renaming = true;
  }

  const save = (event) => {
    event.preventDefault();
    return pending(event, () => run(async () => {
      await updateCookbook(cookbook.id, name, description);
      renaming = false;
    }));
  };

  async function remove() {
    if (!(await ask({ title: `Remove the ${cookbook.name} cookbook?`, confirmLabel: 'Remove cookbook', danger: true,
      message: 'Its recipes stay in the library and in any other cookbooks. Plan style rules that name it stop doing so.' }))) return undefined;
    const gone = cookbook.name;
    return run(async () => {
      await removeCookbook(cookbook.id);
      onremoved();
      toast(`The ${gone} cookbook was removed. Its recipes are still in the library.`);
    });
  }
</script>

<section class="card cookbook" aria-labelledby="cookbook-name">
  {#if renaming}
    <form class="rename" onsubmit={save}>
      <label>Name<input id="cookbook-rename" bind:value={name} maxlength="80" required></label>
      <label>Description <span class="muted">(optional)</span><textarea bind:value={description} maxlength="500" rows="2"></textarea></label>
      <div class="row-actions start">
        <button type="submit" class="primary">Save</button>
        <button type="button" onclick={() => (renaming = false)}>Cancel</button>
      </div>
    </form>
  {:else}
    <div class="title">
      <BookMarked size={22} aria-hidden="true" />
      <div>
        <h3 id="cookbook-name">{cookbook.name}</h3>
        <p class="muted">{cookbook.recipes} {cookbook.recipes === 1 ? 'recipe' : 'recipes'}{#if cookbook.description} · {cookbook.description}{/if}</p>
      </div>
    </div>
    <div class="row-actions start">
      <button type="button" class="small with-icon" onclick={startRenaming}><Pencil size={15} />Rename</button>
      <button type="button" class="small with-icon" aria-expanded={downloadsOpen} aria-controls="cookbook-downloads"
              onclick={() => (downloadsOpen = !downloadsOpen)}><Download size={15} />Download<ChevronDown size={14} /></button>
      <button type="button" class="small with-icon bad-text" onclick={pendingClick(() => remove())}><Trash2 size={15} />Delete cookbook</button>
      <HelpLink page="using/recipes#cookbooks">About cookbooks</HelpLink>
    </div>
    <div id="cookbook-downloads" class="downloads" hidden={!downloadsOpen}>
      <RecipeDownloads href="/api/cookbooks/{cookbook.id}/export" onpick={() => (downloadsOpen = false)} />
    </div>
  {/if}
</section>

<style>
  .cookbook { display: grid; gap: .6rem; margin-top: .75rem; border-left: 4px solid var(--area-recipes); }
  .title { display: flex; gap: .6rem; align-items: flex-start; }
  .title :global(svg) { flex: none; color: var(--area-recipes); margin-top: .15rem; }
  h3 { margin: 0; }
  .title p { margin: .1rem 0 0; }
  .row-actions { align-items: center; }
  .bad-text { color: var(--bad); }
  .downloads:not([hidden]) { display: flex; flex-wrap: wrap; gap: .25rem .5rem; }
  .rename { display: grid; gap: .6rem; }
  .rename label { display: grid; gap: .25rem; }
  .rename textarea { width: 100%; resize: vertical; }
</style>
