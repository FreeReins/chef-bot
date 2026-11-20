<script>
  import { app, openSwap, closeSwap } from '../lib/app.svelte.js';
  import { shortDate } from '../lib/format.js';
  import Chip from '../components/Chip.svelte';
  import SwapPanel from './SwapPanel.svelte';

  let { meal } = $props();

  const swapOpen = $derived(app.swap?.mealId === meal.id);
</script>

<article class="card" class:done={meal.status !== 'Planned'}>
  <div class="row">
    <div>
      <h3>{meal.title}</h3>
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
        <button class="small" onclick={() => (swapOpen ? closeSwap() : openSwap(meal))}>{swapOpen ? 'Close' : 'Swap'}</button>
      {/if}
    </div>
  </div>
  {#if swapOpen}
    <SwapPanel {meal} options={app.swap.options} />
  {/if}
</article>
