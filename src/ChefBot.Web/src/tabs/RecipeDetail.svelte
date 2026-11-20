<!-- One recipe on its own page, /recipes/{id}, so it can be bookmarked. Shows it, and lets it be edited or archived. -->
<script>
  import { app, run, loadRecipes } from '../lib/app.svelte.js';
  import { go } from '../lib/addresses.js';
  import { api } from '../lib/api.js';
  import Chip from '../components/Chip.svelte';
  import RecipeForm from './RecipeForm.svelte';
  import ArrowLeft from '@lucide/svelte/icons/arrow-left';

  let { id } = $props();

  const recipe = $derived(app.recipes.find((candidate) => candidate.id === id));
  let editing = $state(false);

  async function save(draft) {
    let saved = false;
    await run(async () => {
      await api('PUT', `/api/recipes/${recipe.id}`, draft);
      await loadRecipes();
      editing = false;
      saved = true;
      app.notes = ['Recipe saved.'];
    });
    return saved;
  }

  function archive() {
    if (!confirm(`Archive ${recipe.name}? It won't be planned again, and past plans keep showing it.`)) return;
    const name = recipe.name;
    run(async () => {
      await api('DELETE', `/api/recipes/${recipe.id}`);
      await loadRecipes();
      await go('/recipes');
      app.notes = [`${name} was archived and won't be planned again.`];
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
    {#if recipe.isSlowCooker}<Chip tone="good">Slow cooker</Chip>{/if}
    {#if recipe.origin === 'Mealie'}<Chip>From Mealie</Chip>{/if}
  </div>
  <div class="card">
    <h3>Ingredients ({recipe.ingredients.length})</h3>
    <ul class="plain">
      {#each recipe.ingredients as line, index (index)}<li>{line}</li>{/each}
    </ul>
  </div>
{/if}

<style>
  .back { display: inline-flex; align-items: center; gap: .3rem; }
</style>
