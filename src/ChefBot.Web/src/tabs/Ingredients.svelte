<!-- Everything the plan needs, where each item comes from, and the pantry beside it. The shopping list is split by
     store, and anything without a store is grouped by food group. -->
<script>
  import { app, isConfirmed, planAction } from '../lib/app.svelte.js';
  import IngredientLine from './IngredientLine.svelte';
  import Pantry from './Pantry.svelte';

  const lines = $derived(app.ingredients?.ingredients ?? []);
  const byStatus = (status) => lines.filter((line) => line.status === status);
  const toBuy = $derived(byStatus('ToBuy'));

  /** Food groups in the order they are walked through in a shop, for items no store was chosen for. */
  const FOOD_GROUPS = [
    ['Vegetable', 'Vegetables'], ['Fruit', 'Fruit'], ['Herb', 'Herbs and aromatics'], ['Protein', 'Meat, fish, eggs and beans'],
    ['Dairy', 'Dairy'], ['WholeGrain', 'Grains'], ['Other', 'Everything else'], ['Staple', 'Staples'],
  ];

  // Items with a store go under it, in the order the stores were saved; the rest go under their food group.
  const sections = $derived.by(() => {
    const saved = (app.stores?.stores ?? []).map((store) => store.name);
    const named = [...new Set(toBuy.filter((line) => line.store).map((line) => line.store))]
      .sort((a, b) => (saved.indexOf(a) + 1 || Infinity) - (saved.indexOf(b) + 1 || Infinity));
    const loose = toBuy.filter((line) => !line.store);
    return [
      ...named.map((store) => ({ key: `store:${store}`, title: store, lines: toBuy.filter((line) => line.store === store) })),
      ...FOOD_GROUPS.map(([group, title]) => ({ key: `group:${group}`, title, lines: loose.filter((line) => (line.group ?? 'Other') === group) })),
    ].filter((section) => section.lines.length > 0);
  });
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
    {#each sections as section (section.key)}
      <div class="card">
        <h3>{section.title}</h3>
        <ul class="plain">
          {#each section.lines as line, index (index)}<IngredientLine {line} />{/each}
        </ul>
      </div>
    {/each}
    {@render group('From your pantry', byStatus('InPantry'))}
    {@render group('Already bought', byStatus('Purchased'))}
    {@render group('Staples assumed on hand', byStatus('Staple'))}
  </section>
  <Pantry />
</div>
