<!-- The recipes the plan's meals follow, under the shopping list, so they open from the shop or the kitchen. They are
     kept on this device (offline.svelte.js), draft or confirmed, so offline each one still opens as text; one that
     hasn't been kept yet says so instead of linking to a page that needs ChefBot. -->
<script>
  import { app, isConfirmed, hasSavedRecipe } from '../lib/app.svelte.js';
  import BookOpenText from '@lucide/svelte/icons/book-open-text';

  // One entry per recipe, named after its first meal that isn't leftovers, in the order the plan lists them.
  const recipes = $derived.by(() => {
    const byRecipe = new Map();
    for (const meal of app.plan?.meals ?? []) {
      if (!meal.recipeId) continue;
      const known = byRecipe.get(meal.recipeId);
      if (!known || (known.isLeftover && !meal.isLeftover)) byRecipe.set(meal.recipeId, meal);
    }
    return [...byRecipe.values()].map((meal) => ({ id: meal.recipeId, title: meal.title, day: meal.isLeftover ? null : meal.day }));
  });
</script>

{#if recipes.length}
  <h2 class="no-print">{isConfirmed() ? 'Recipes on this plan' : 'Recipes in the draft'} ({recipes.length})</h2>
  <div class="card no-print">
    <ul class="plain">
      {#each recipes as recipe (recipe.id)}
        <li class="row">
          <span class="name">
            <BookOpenText size={16} aria-hidden="true" />
            {#if !app.offline || hasSavedRecipe(recipe.id)}
              <a href="/recipes/{recipe.id}">{recipe.title}</a>
            {:else}
              {recipe.title} <span class="muted">(not saved on this device)</span>
            {/if}
          </span>
          {#if recipe.day}<span class="muted">{recipe.day}</span>{/if}
        </li>
      {/each}
    </ul>
    {#if !app.offline}<p class="muted note">These open on this device offline too, without their photos.</p>{/if}
  </div>
{/if}

<style>
  .name { display: inline-flex; align-items: center; gap: .45rem; min-width: 0; overflow-wrap: anywhere; }
  .name :global(svg) { flex: none; color: var(--area-recipes); }
  .note { margin: .5rem 0 0; font-size: .85rem; }
</style>
