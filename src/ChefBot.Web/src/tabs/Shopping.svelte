<!-- The shopping list: what the plan needs and what was added by hand, and where each item comes from. What is left
     to buy is split by store, and anything without a store is grouped by food group. It works without a plan: then
     only the hand-added items are listed. Pantry items running out of date are offered for the list. The pantry
     itself is on the Pantry tab. -->
<script>
  import { untrack } from 'svelte';
  import { app, isConfirmed, run, loadPantry, addToShoppingList, buyEverything } from '../lib/app.svelte.js';
  import { FOOD_GROUPS, amount, shortDate } from '../lib/format.js';
  import Chip from '../components/Chip.svelte';
  import IngredientLine from './IngredientLine.svelte';

  // With a plan the pantry comes with its ingredients view; without one it is loaded on its own, as on the Pantry tab.
  $effect(() => {
    if (!app.plan) untrack(() => run(loadPantry));
  });

  const lines = $derived(app.plan ? (app.ingredients?.ingredients ?? []) : []);
  const byStatus = (status) => lines.filter((line) => line.status === status);
  const planToBuy = $derived(byStatus('ToBuy'));
  const handAdded = $derived(app.manualItems.map((item) => ({
    name: item.name, status: 'ToBuy', quantity: item.quantity, unit: item.unit, store: item.store || null,
    group: item.group, isOptional: false, usedIn: [], manualId: item.id,
  })));
  const toBuy = $derived([...planToBuy, ...handAdded]);
  const canBuyAll = $derived(handAdded.length > 0 || (isConfirmed() && planToBuy.length > 0));

  // Items with a store go under it, in the order the stores were saved; the rest go under their food group.
  const sections = $derived.by(() => {
    const saved = (app.stores?.stores ?? []).map((store) => store.name);
    const named = [...new Set(toBuy.filter((line) => line.store).map((line) => line.store))]
      .sort((a, b) => (saved.indexOf(a) + 1 || Infinity) - (saved.indexOf(b) + 1 || Infinity));
    const loose = toBuy.filter((line) => !line.store);
    const byName = (a, b) => a.name.localeCompare(b.name);
    return [
      ...named.map((store) => ({ key: `store:${store}`, title: store, lines: toBuy.filter((line) => line.store === store).sort(byName) })),
      ...FOOD_GROUPS.map(([group, title]) => ({ key: `group:${group}`, title, lines: loose.filter((line) => (line.group ?? 'Other') === group).sort(byName) })),
    ].filter((section) => section.lines.length > 0);
  });

  // Pantry items that are out of date or soon will be, and aren't on the list already.
  const pantryItems = $derived(app.plan ? (app.ingredients?.pantry ?? []).map((line) => line.item) : app.pantry);
  const restock = $derived.by(() => {
    const listed = new Set(toBuy.map((line) => line.name.toLowerCase()));
    const seen = new Set();
    return pantryItems
      .filter((item) => item.freshness === 'Expired' || item.freshness === 'ExpiringSoon')
      .filter((item) => !listed.has(item.name.toLowerCase()) && !seen.has(item.name.toLowerCase()) && seen.add(item.name.toLowerCase()));
  });

  async function add(event) {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    const added = await addToShoppingList({
      name: data.get('name'),
      quantity: data.get('quantity') ? Number(data.get('quantity')) : null,
      unit: data.get('unit') || null,
    });
    if (added) {
      form.reset();
      form.elements.name.focus();
    }
  }
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

<section>
  <form class="inline card" onsubmit={add}>
    <label>Add to the list <input id="shopping-name" name="name" required placeholder="e.g. milk"></label>
    <label>Amount <input id="shopping-quantity" name="quantity" type="number" min="0" step="any"></label>
    <label>Unit <input id="shopping-unit" name="unit" size="6" placeholder="l"></label>
    <button type="submit">Add</button>
  </form>

  <div class="row">
    <h2>To buy ({toBuy.length})</h2>
    {#if canBuyAll}
      <button class="small" onclick={buyEverything}>
        {isConfirmed() || planToBuy.length === 0 ? 'Mark all as bought' : 'Mark hand-added items as bought'}
      </button>
    {/if}
  </div>
  {#if toBuy.length === 0}
    <p class="muted">
      {app.plan
        ? 'Nothing left to buy for this plan. Add anything else you need above.'
        : 'Your shopping list is empty. Add an item above, or add pantry items that are running out.'}
    </p>
  {:else if planToBuy.length && !isConfirmed()}
    <p class="muted">Confirm the plan to start ticking off its items. Items you added yourself can be ticked off now.</p>
  {/if}
  {#each sections as section (section.key)}
    <div class="card">
      <h3>{section.title}</h3>
      <ul class="plain">
        {#each section.lines as line (line.manualId ?? line.shoppingItemId ?? line.name)}<IngredientLine {line} />{/each}
      </ul>
    </div>
  {/each}

  {#if restock.length}
    <div>
      <h2>Running out of date ({restock.length})</h2>
      <p class="muted">These are in the pantry but expired or will soon. Add them to the list to buy more.</p>
      <div class="card">
        <ul class="plain">
          {#each restock as item (item.id)}
            <li class="row">
              <div>
                <strong>{item.name}</strong> {amount(item.quantity, item.unit)}
                <div class="chips">
                  <Chip tone={item.freshness === 'Expired' ? 'bad' : 'warn'}>
                    {item.freshness === 'Expired' ? 'Expired' : 'Use soon'}{item.expiresOn ? `, ${shortDate(item.expiresOn)}` : ''}
                  </Chip>
                </div>
              </div>
              <button class="small" aria-label="Add to list: {item.name}" onclick={() => addToShoppingList({ name: item.name, unit: item.unit || null })}>Add to list</button>
            </li>
          {/each}
        </ul>
      </div>
    </div>
  {/if}

  {#if app.plan}
    {@render group('From your pantry', byStatus('InPantry'))}
    {@render group('Already bought', byStatus('Purchased'))}
    {@render group('Staples assumed on hand', byStatus('Staple'))}
  {/if}
</section>
