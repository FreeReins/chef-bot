<!-- Alternatives for one meal: recipes ranked by how much of the pantry they use, then made-up plates. -->
<script>
  import { replaceMeal } from '../lib/app.svelte.js';
  import { shortDate } from '../lib/format.js';
  import Chip from '../components/Chip.svelte';

  /** options: AlternativesResponse { recipes, creative }. */
  let { meal, options } = $props();
</script>

<div class="swap">
  <h3>Recipes that use what you have</h3>
  {#if options.recipes.length === 0}<p class="muted">No other recipes fit this slot.</p>{/if}
  {#each options.recipes as option (option.recipeId)}
    <div class="option row">
      <div>
        <strong>{option.name}</strong>
        <div class="chips">
          {#if option.isSlowCooker}<Chip tone="good">Slow cooker</Chip>{/if}
          {#each option.expiringSoon as name}<Chip tone="warn">Uses up {name}</Chip>{/each}
          {#if option.onHand.length}<Chip tone="good">Have: {option.onHand.join(', ')}</Chip>{/if}
          {#if option.toBuy.length}<Chip>Buy: {option.toBuy.join(', ')}</Chip>{/if}
        </div>
      </div>
      <button class="small" onclick={() => replaceMeal(meal.id, { recipeId: option.recipeId })}>Use this</button>
    </div>
  {/each}

  {#if options.creative.length}
    <h3 style="margin-top:.75rem">Creative ideas: grain, protein and produce</h3>
  {/if}
  {#each options.creative as idea, index (index)}
    <div class="option row">
      <div>
        <strong>{idea.title}</strong>
        <div class="chips">
          {#each idea.components as part, partIndex (partIndex)}
            {#if !part.onHand}
              <Chip>{part.name}, to buy</Chip>
            {:else if part.expiresOn}
              <Chip tone="good">{part.name}, use by {shortDate(part.expiresOn)}</Chip>
            {:else}
              <Chip tone="good">{part.name}, on hand</Chip>
            {/if}
          {/each}
        </div>
      </div>
      <button class="small" onclick={() => replaceMeal(meal.id, { custom: { title: idea.title, ingredients: idea.ingredients } })}>Use this</button>
    </div>
  {/each}
</div>

<style>
  .swap { margin-top: .75rem; padding-top: .75rem; border-top: 1px dashed var(--line); }
  .option { padding: .5rem 0; border-top: 1px solid var(--line); }
</style>
