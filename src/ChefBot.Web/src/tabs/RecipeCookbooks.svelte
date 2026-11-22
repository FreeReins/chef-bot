<!-- Which cookbooks a recipe is in, /recipes/{id}/cookbooks, in a dialog over its page: a box for each cookbook,
     saved as it is ticked, and a field to make a new one with this recipe in it. This is the way to the first
     cookbook: until there is one, the rest of the page says nothing about cookbooks. -->
<script>
  import { untrack } from 'svelte';
  import { run, toast, loadRecipes, loadCookbooks, createCookbook, addToCookbook, removeFromCookbook } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { pending } from '../lib/pending.js';
  import Modal from '../components/Modal.svelte';
  import HelpLink from '../components/HelpLink.svelte';
  import BookPlus from '@lucide/svelte/icons/book-plus';

  /** recipe: the RecipeResponse. onclose(): asked to close. onsaved(recipe): its cookbooks changed. */
  let { recipe, onclose, onsaved } = $props();

  let cookbooks = $state(null); // CookbookResponse[] once loaded
  // Starts from the cookbooks the recipe was in when the dialog opened; each tick changes it from there.
  let chosen = $state(untrack(() => (recipe.cookbooks ?? []).map((cookbook) => cookbook.id)));
  let name = $state('');
  let changed = false;

  run(async () => { cookbooks = await loadCookbooks(); });

  function toggle(event, cookbook) {
    const box = event.currentTarget;
    const on = box.checked;
    return run(async () => {
      if (on) await addToCookbook(cookbook.id, [recipe.id]);
      else await removeFromCookbook(cookbook.id, recipe.id);
      chosen = on ? [...chosen, cookbook.id] : chosen.filter((id) => id !== cookbook.id);
      changed = true;
    }).finally(() => { box.checked = chosen.includes(cookbook.id); });
  }

  const make = (event) => {
    event.preventDefault();
    return pending(event, () => run(async () => {
      const made = await createCookbook(name.trim());
      await addToCookbook(made.id, [recipe.id]);
      cookbooks = await loadCookbooks();
      chosen = [...chosen, made.id];
      changed = true;
      name = '';
      toast(`${recipe.name} is in the new ${made.name} cookbook.`);
    }));
  };

  function finish() {
    onclose();
    if (changed) run(async () => { await loadRecipes(); onsaved(await api('GET', `/api/recipes/${recipe.id}`)); });
  }
</script>

<Modal title="Cookbooks for {recipe.name}" onclose={finish}>
  {#if cookbooks === null}
    <p class="muted" role="status">Loading cookbooks…</p>
  {:else}
    {#if cookbooks.length === 0}
      <p>Cookbooks group recipes, such as weeknight dinners or a family's favourites. A recipe can be in several, or
        none. Make the first one here.</p>
    {:else}
      <fieldset>
        <legend>Keep {recipe.name} in</legend>
        {#each cookbooks as cookbook (cookbook.id)}
          <label class="check"><input type="checkbox" checked={chosen.includes(cookbook.id)} onchange={(event) => toggle(event, cookbook)}>
            {cookbook.name}</label>
        {/each}
      </fieldset>
      <p class="muted small">Changes save as you tick them.</p>
    {/if}

    <form class="new" onsubmit={make}>
      <label for="new-cookbook">New cookbook</label>
      <div class="row">
        <input id="new-cookbook" bind:value={name} maxlength="80" placeholder="Weeknight dinners" required>
        <button type="submit" class="with-icon"><BookPlus size={16} />Make and add</button>
      </div>
    </form>
  {/if}

  <div class="foot">
    <HelpLink page="using/recipes#cookbooks">About cookbooks</HelpLink>
    <button type="button" class="primary" onclick={finish}>Done</button>
  </div>
</Modal>

<style>
  fieldset { border: 0; margin: 0 0 .25rem; padding: 0; display: grid; gap: .35rem; }
  legend { font-weight: 600; margin-bottom: .35rem; padding: 0; }
  .check { display: flex; align-items: center; gap: .5rem; }
  .small { font-size: .85rem; margin: .25rem 0 0; }
  .new { display: grid; gap: .3rem; margin-top: 1rem; }
  .new .row { display: flex; gap: .5rem; flex-wrap: wrap; }
  .new input { flex: 1 1 12rem; min-width: 0; }
  .foot { display: flex; justify-content: space-between; align-items: center; gap: 1rem; margin-top: 1.25rem; }
</style>
