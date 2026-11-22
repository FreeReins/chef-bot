<!-- The ⋯ menu on a recipe's page: editing it, its photos and AI suggestions (dialogs with their own addresses), sharing it as text,
     printing it, downloading it (RecipeDownloads.svelte) and archiving it. It closes on Escape, on a click elsewhere,
     and once focus leaves it. -->
<script>
  import { app, run, loadRecipes, toast, ask } from '../lib/app.svelte.js';
  import { go } from '../lib/addresses.js';
  import { api } from '../lib/api.js';
  import { canShare, share, recipeText } from '../lib/exporting.js';
  import RecipeDownloads from '../components/RecipeDownloads.svelte';
  import Ellipsis from '@lucide/svelte/icons/ellipsis';
  import Pencil from '@lucide/svelte/icons/pencil';
  import Images from '@lucide/svelte/icons/images';
  import Archive from '@lucide/svelte/icons/archive';
  import Share2 from '@lucide/svelte/icons/share-2';
  import Copy from '@lucide/svelte/icons/copy';
  import Printer from '@lucide/svelte/icons/printer';
  import Sparkles from '@lucide/svelte/icons/sparkles';

  /** recipe: the RecipeResponse. link(dialog): the address of one of its dialogs, such as "edit". */
  let { recipe, link } = $props();

  let open = $state(false);
  let menu = $state();

  $effect(() => {
    if (!open) return;
    const away = (event) => { if (!menu?.contains(event.target)) open = false; };
    const escape = (event) => { if (event.key === 'Escape') { open = false; menu?.querySelector('button')?.focus(); } };
    document.addEventListener('pointerdown', away);
    document.addEventListener('focusin', away);
    document.addEventListener('keydown', escape);
    return () => {
      document.removeEventListener('pointerdown', away);
      document.removeEventListener('focusin', away);
      document.removeEventListener('keydown', escape);
    };
  });

  // Sharing hands the recipe as text to the phone's share sheet; without one it is copied to paste anywhere.
  function shareRecipe() {
    open = false;
    run(async () => {
      const done = await share(recipe.name, recipeText(recipe));
      if (done === 'copied') toast(`${recipe.name} was copied as text. Paste it wherever you need it.`);
    });
  }

  function print() {
    open = false;
    window.print();
  }

  async function archive() {
    open = false;
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
</script>

<div class="menu" bind:this={menu}>
  <button type="button" class="with-icon toggle" aria-label="More actions for {recipe.name}" aria-expanded={open}
          aria-controls="recipe-menu" onclick={() => (open = !open)}><Ellipsis size={18} /></button>
  <div id="recipe-menu" class="menu-panel" hidden={!open}>
    <a class="with-icon" href={link('edit')} onclick={() => (open = false)}><Pencil size={16} />Edit recipe</a>
    <a class="with-icon" href={link('photos')} onclick={() => (open = false)}><Images size={16} />Photos ({recipe.images.length})</a>
    {#if app.aiFeatures.recipeDetails || recipe.hasSuggestion}
      <a class="with-icon" href={link('improve')} onclick={() => (open = false)}><Sparkles size={16} />Improve with AI</a>
    {/if}
    <hr>
    {#if canShare()}
      <button type="button" class="with-icon" onclick={shareRecipe}><Share2 size={16} />Share recipe</button>
    {:else}
      <button type="button" class="with-icon" onclick={shareRecipe}><Copy size={16} />Copy as text</button>
    {/if}
    <button type="button" class="with-icon" onclick={print}><Printer size={16} />Print recipe</button>
    <RecipeDownloads href="/api/recipes/{recipe.id}/export" onpick={() => (open = false)} />
    <hr>
    <button type="button" class="with-icon bad-text" onclick={archive}><Archive size={16} />Archive recipe</button>
  </div>
</div>

<style>
  .menu { position: relative; }
  .toggle { height: 100%; }
  .menu-panel { position: absolute; right: 0; top: calc(100% + .3rem); z-index: 5; min-width: 13rem; padding: .3rem;
    background: var(--panel); border: 1px solid var(--line); border-radius: .5rem; box-shadow: 0 6px 24px rgb(0 0 0 / .18); }
  .menu-panel :is(button, a) { display: flex; width: 100%; border: 0; justify-content: flex-start; font-size: .95rem;
    padding: .4rem .8rem; border-radius: .4rem; color: var(--ink); text-decoration: none; background: none; }
  .menu-panel :is(button, a):hover { background: var(--chip); }
  .menu-panel .bad-text { color: var(--bad); }
  hr { border: 0; border-top: 1px solid var(--line); margin: .3rem .2rem; }
  /* Phones: the menu unfolds in place on its own line, pushing the page down rather than covering what is below. */
  @media (max-width: 40rem) {
    .menu { display: contents; }
    .menu-panel { position: static; flex: 1 1 100%; order: 1; box-shadow: none; }
  }
</style>
