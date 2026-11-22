<!-- A recipe's ingredients on its page, for the number of servings chosen with the stepper above them. Each amount is
     scaled and written as a cook reads it (lib/scaling.js); a line ChefBot couldn't read an amount from stays as
     written and is marked so. -->
<script>
  import ServingsStepper from '../components/ServingsStepper.svelte';
  import { scaledLines } from '../lib/scaling.js';

  /** recipe: the RecipeResponse. serves: the servings shown. onserves(count): a new count was chosen. */
  let { recipe, serves, onserves } = $props();

  const lines = $derived(scaledLines(recipe, serves));
  const scaled = $derived(serves !== recipe.servings);
  const asWritten = $derived(scaled ? lines.filter((line) => !line.scaled).length : 0);
</script>

<section class="card ingredients" id="recipe-ingredients" tabindex="-1" aria-labelledby="recipe-ingredients-title">
  <h3 id="recipe-ingredients-title">Ingredients <span class="count">{recipe.ingredients.length}</span></h3>
  {#if recipe.ingredients.length}
    <ServingsStepper servings={recipe.servings} {serves} onchange={onserves} />
    <p class="scale-note" aria-live="polite">
      {#if scaled}
        Scaled from {recipe.servings} to {serves} servings.
        <button type="button" class="link" onclick={() => onserves(recipe.servings)}>Back to {recipe.servings}</button>
      {:else}
        As written, for {recipe.servings} {recipe.servings === 1 ? 'serving' : 'servings'}.
      {/if}
    </p>
    <ul class="plain lines">
      {#each lines as line, index (index)}
        <li class:as-written={scaled && !line.scaled}>
          {#if line.amount}<strong class="amount">{line.amount}</strong>{/if}{line.rest}
          {#if scaled && !line.scaled}<span class="sr-only">(amount as written)</span>{/if}
        </li>
      {/each}
    </ul>
    {#if asWritten}
      <p class="muted small"><span aria-hidden="true">*</span> {asWritten === 1 ? 'One line keeps its' : `${asWritten} lines keep their`} amount
        as written, so check {asWritten === 1 ? 'it' : 'them'} yourself.</p>
    {/if}
  {:else}
    <p class="muted">No ingredients yet.</p>
  {/if}
</section>

<style>
  h3 { display: flex; align-items: center; gap: .5rem; font-size: 1.1rem; }
  .count { display: inline-grid; place-items: center; min-width: 1.6rem; height: 1.6rem; padding: 0 .4rem; border-radius: 999px;
    font-size: .8rem; color: var(--ink); background: color-mix(in srgb, var(--area-recipes) 16%, var(--panel)); }
  .ingredients :global(.servings) { margin-top: .75rem; }
  .scale-note { margin: .5rem 0 .25rem; font-size: .85rem; color: var(--muted); }
  .link { padding: 0; border: 0; background: none; color: var(--accent); text-decoration: underline; font-size: inherit; }
  .link:hover { background: none; }
  .lines li { padding: .45rem 0; border-bottom: 1px dashed var(--line); overflow-wrap: anywhere; }
  .lines li:last-child { border-bottom: 0; }
  .amount { font-variant-numeric: tabular-nums; }
  .as-written::after { content: " *"; color: var(--muted); }
  .small { font-size: .8rem; margin-top: .5rem; }
  @media print {
    .ingredients :global(.servings), .link { display: none; }
  }
</style>
