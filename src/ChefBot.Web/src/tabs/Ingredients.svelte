<!-- Everything the plan needs, where each item comes from, and the pantry beside it. -->
<script>
  import { app, isConfirmed, planAction } from '../lib/app.svelte.js';
  import IngredientLine from './IngredientLine.svelte';
  import Pantry from './Pantry.svelte';

  const lines = $derived(app.ingredients?.ingredients ?? []);
  const byStatus = (status) => lines.filter((line) => line.status === status);
  const toBuy = $derived(byStatus('ToBuy'));
  const stores = $derived([...new Set(toBuy.map((line) => line.store))]);
</script>

{#snippet group(title, items)}
  {#if items.length}
    <div>
      <h2>{title} ({items.length})</h2>
      <div class="card">
        <ul class="plain">
          {#each items as line, index (index)}<IngredientLine {line} />{/each}
        </ul>
      </div>
    </div>
  {/if}
{/snippet}

<div class="columns">
  <section>
    <div class="row">
      <h2>To buy ({toBuy.length})</h2>
      {#if isConfirmed() && toBuy.length}
        <button class="small" onclick={() => planAction('shopping/purchase-all', {})}>Mark all as bought</button>
      {/if}
    </div>
    {#if toBuy.length === 0}
      <p class="muted">Nothing left to buy for this plan.</p>
    {:else if !isConfirmed()}
      <p class="muted">Confirm the plan to start ticking items off.</p>
    {/if}
    {#each stores as store (store)}
      <div class="card">
        <h3>{store}</h3>
        <ul class="plain">
          {#each toBuy.filter((line) => line.store === store) as line, index (index)}<IngredientLine {line} />{/each}
        </ul>
      </div>
    {/each}
    {@render group('From your pantry', byStatus('InPantry'))}
    {@render group('Already bought', byStatus('Purchased'))}
    {@render group('Staples assumed on hand', byStatus('Staple'))}
  </section>
  <Pantry />
</div>
