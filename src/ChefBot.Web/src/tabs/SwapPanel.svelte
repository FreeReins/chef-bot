<!-- Alternatives for one meal: recipes ranked by how much of the pantry they use, then made-up plates. Shown in
     SwapDialog.svelte. Each recipe says in one line how much comes from the pantry and how much is to buy; only food
     about to expire and slow cooking get a badge, and the ingredient lists open under "What's needed". -->
<script>
  import { replaceMeal } from '../lib/app.svelte.js';
  import { pendingClick } from '../lib/pending.js';
  import { shortDate } from '../lib/format.js';
  import Chip from '../components/Chip.svelte';
  import CookingPot from '@lucide/svelte/icons/cooking-pot';
  import Check from '@lucide/svelte/icons/check';
  import ShoppingCart from '@lucide/svelte/icons/shopping-cart';
  import Lightbulb from '@lucide/svelte/icons/lightbulb';

  /** options: AlternativesResponse { recipes, creative }. */
  let { meal, options } = $props();

  /** The order a made-up plate's parts are listed in. */
  const GROUP_ORDER = { WholeGrain: 0, Protein: 1 };
  const SHOWN_EXPIRING = 2;

  /** How much of a recipe the pantry covers: { have, buy, total, line }. */
  function fit(option) {
    const have = option.onHand.length + option.expiringSoon.length;
    const buy = option.toBuy.length;
    const total = have + buy;
    let line;
    if (!total) line = 'No ingredients listed';
    else if (!buy) line = 'Everything is in your pantry';
    else if (!have) line = `All ${buy} to buy`;
    else line = `${have} of ${total} from your pantry · ${buy} to buy`;
    return { have, buy, total, line };
  }

  const sortedParts = (idea) => [...idea.components].sort((a, b) => (GROUP_ORDER[a.group] ?? 2) - (GROUP_ORDER[b.group] ?? 2));
</script>

<div class="swap">
  <h3>Recipes that use what you have</h3>
  {#if options.recipes.length === 0}<p class="muted">No other recipes fit this slot.</p>{/if}
  <ul class="options">
    {#each options.recipes as option, index (option.recipeId)}
      {@const f = fit(option)}
      <li class="option" class:best={index === 0 && options.recipes.length > 1}>
        <div class="head">
          <div class="name">
            {#if index === 0 && options.recipes.length > 1}<span class="best-tag">Best fit</span>{/if}
            <strong>{option.name}</strong>
          </div>
          <button class="small" onclick={pendingClick(() => replaceMeal(meal.id, { recipeId: option.recipeId }))}>Use this</button>
        </div>
        <div class="fit">
          {#if f.total}<span class="bar" aria-hidden="true"><span style:width="{(f.have / f.total) * 100}%"></span></span>{/if}
          <span>{f.line}</span>
        </div>
        {#if option.expiringSoon.length || option.isSlowCooker}
          <div class="chips">
            {#each option.expiringSoon.slice(0, SHOWN_EXPIRING) as name}<Chip tone="warn">Uses up {name}</Chip>{/each}
            {#if option.expiringSoon.length > SHOWN_EXPIRING}<Chip tone="warn">+{option.expiringSoon.length - SHOWN_EXPIRING} more expiring</Chip>{/if}
            {#if option.isSlowCooker}<Chip><span class="with-icon"><CookingPot size={12} aria-hidden="true" />Slow cooker</span></Chip>{/if}
          </div>
        {/if}
        {#if f.total}
          <details>
            <summary>What's needed</summary>
            {#if f.have}
              <p class="needs"><Check size={14} aria-hidden="true" /><span><strong>Have:</strong> {[...option.expiringSoon, ...option.onHand].join(', ')}</span></p>
            {/if}
            {#if f.buy}
              <p class="needs"><ShoppingCart size={14} aria-hidden="true" /><span><strong>Buy:</strong> {option.toBuy.join(', ')}</span></p>
            {/if}
          </details>
        {/if}
      </li>
    {/each}
  </ul>

  {#if options.creative.length}
    <section class="creative" aria-labelledby="creative-heading">
      <h3 id="creative-heading" class="with-icon"><Lightbulb size={16} aria-hidden="true" />Or make up a plate</h3>
      <p class="muted intro">A whole grain, a protein and produce, from what you have.</p>
      <ul class="options">
        {#each options.creative as idea, index (index)}
          {@const parts = sortedParts(idea)}
          {@const onHand = parts.filter((part) => part.onHand).length}
          <li class="option">
            <div class="head">
              <strong>{idea.title}</strong>
              <button class="small" onclick={pendingClick(() => replaceMeal(meal.id, { custom: { title: idea.title, ingredients: idea.ingredients } }))}>Use this</button>
            </div>
            <p class="parts">
              <span class="muted">{onHand === parts.length ? 'All on hand' : `${onHand} of ${parts.length} on hand`}:</span>
              {#each parts as part, partIndex (partIndex)}
                {#if partIndex > 0}<span class="sep" aria-hidden="true">·</span>{/if}
                {#if !part.onHand}
                  <span class="muted">{part.name}, to buy</span>
                {:else if part.expiresOn}
                  <span>{part.name} <span class="use-by">(use by {shortDate(part.expiresOn)})</span></span>
                {:else}
                  <span>{part.name}</span>
                {/if}
              {/each}
            </p>
          </li>
        {/each}
      </ul>
    </section>
  {/if}
</div>

<style>
  h3 { margin: 0 0 .4rem; font-size: 1rem; }
  .options { list-style: none; margin: 0; padding: 0; display: grid; gap: .5rem; }
  .option { display: grid; gap: .3rem; padding: .6rem .75rem; border: 1px solid var(--line); border-radius: .45rem; }
  .option.best { border-color: var(--good); }
  .head { display: flex; gap: .75rem; align-items: center; justify-content: space-between; }
  .name { display: grid; }
  .name strong, .head > strong { font-size: 1rem; }
  .best-tag { font-size: .72rem; font-weight: 700; text-transform: uppercase; letter-spacing: .04em; color: var(--good); }
  .fit { display: flex; align-items: center; gap: .5rem; font-size: .88rem; color: var(--muted); }
  .bar { flex: 0 0 4.5rem; height: .4rem; border-radius: 1rem; background: var(--chip); overflow: hidden; }
  .bar span { display: block; height: 100%; background: var(--good); border-radius: inherit; }
  .chips { margin-top: 0; }
  .with-icon { display: inline-flex; align-items: center; gap: .25rem; }
  details { font-size: .88rem; }
  summary { cursor: pointer; color: var(--muted); width: fit-content; }
  .needs { display: flex; gap: .4rem; align-items: baseline; margin: .3rem 0 0; color: var(--muted); }
  .needs :global(svg) { flex: none; position: relative; top: 2px; }
  .creative { margin-top: 1rem; padding: .75rem; border: 1px dashed var(--line); border-radius: .5rem; }
  .creative h3 { margin-bottom: .1rem; }
  .creative .intro { margin: 0 0 .5rem; font-size: .88rem; }
  .creative .option { background: var(--bg); }
  .parts { margin: 0; font-size: .88rem; }
  .use-by { color: var(--warn); }
  .sep { margin: 0 .35rem; color: var(--muted); }
</style>
