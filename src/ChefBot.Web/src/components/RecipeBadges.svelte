<!-- What a recipe has going for it, beside its name: the average of its reviews and whose favourite it is. Shows
     nothing when it has neither. `large` for the recipe's own page. -->
<script>
  import { favouriteOf } from '../lib/format.js';
  import Stars from './Stars.svelte';
  import Heart from '@lucide/svelte/icons/heart';

  /** recipe: { rating, favouredBy }, a RecipeResponse or RecipeSummaryResponse. */
  let { recipe, large = false } = $props();

  const fans = $derived(recipe.favouredBy ?? []);
</script>

{#if recipe.rating || fans.length}
  <span class="badges" class:large>
    {#if recipe.rating}<Stars rating={recipe.rating} size={large ? 16 : 13} />{/if}
    {#if fans.length}
      <span class="favoured"><Heart size={large ? 15 : 13} fill="currentColor" aria-hidden="true" />{favouriteOf(fans.map((fan) => fan.name))}</span>
    {/if}
  </span>
{/if}

<style>
  .badges { display: flex; flex-wrap: wrap; align-items: center; gap: .15rem .75rem; margin: .1rem 0 0; font-size: .8rem; }
  .badges.large { margin: .5rem 0 0; font-size: .9rem; }
  .favoured { display: inline-flex; align-items: center; gap: .3rem; color: var(--bad); }
</style>
