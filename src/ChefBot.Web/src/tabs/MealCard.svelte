<script>
  import { app, openSwap, closeSwap } from '../lib/app.svelte.js';
  import { shortDate } from '../lib/format.js';
  import Chip from '../components/Chip.svelte';
  import SwapPanel from './SwapPanel.svelte';
  import ArrowLeftRight from '@lucide/svelte/icons/arrow-left-right';
  import X from '@lucide/svelte/icons/x';

  let { meal } = $props();

  const swapOpen = $derived(app.swap?.mealId === meal.id);
</script>

<article class="card" class:done={meal.status !== 'Planned'}>
  <div class="row">
    <div>
      <h3>{#if meal.recipeId}<a href="/recipes/{meal.recipeId}" class="quiet">{meal.title}</a>{:else}{meal.title}{/if}</h3>
      <div class="chips">
        {#if meal.day}<Chip>{meal.day} {shortDate(meal.date)}</Chip>{:else}<Chip>Any day</Chip>{/if}
        {#if meal.isSlowCooker}<Chip tone="good">Slow cooker</Chip>{/if}
        {#if meal.isOptional}<Chip>Optional</Chip>{/if}
        {#if meal.status !== 'Planned'}<Chip>{meal.status}</Chip>{/if}
      </div>
      <p class="muted">{meal.ingredients.join(', ')}</p>
    </div>
    <div class="row-actions">
      {#if meal.status === 'Planned'}
        <button class="icon" aria-label="{swapOpen ? 'Close swap for' : 'Swap'} {meal.title}" title={swapOpen ? 'Close' : 'Swap'}
                aria-expanded={String(swapOpen)} onclick={() => (swapOpen ? closeSwap() : openSwap(meal))}>
          {#if swapOpen}<X size={16} />{:else}<ArrowLeftRight size={16} />{/if}
        </button>
      {/if}
    </div>
  </div>
  {#if swapOpen}
    <SwapPanel {meal} options={app.swap.options} />
  {/if}
</article>

<style>
  .quiet { color: inherit; text-decoration: none; }
  .quiet:hover { text-decoration: underline; }
</style>
