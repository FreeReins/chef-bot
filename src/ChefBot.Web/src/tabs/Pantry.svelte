<!-- What is in the kitchen, and which meals on the plan are going to use each item. Items running out of date are
     shown first, since they need doing something about; the rest are grouped by food group, in shop order. Works
     without a plan too: then no item is "used". An item's amount and date can be corrected on its row. -->
<script>
  import { untrack, tick } from 'svelte';
  import { app, run, loadPantry, refreshAfterPantryChange, addToShoppingList, removePantryItem, updatePantryItem, toast } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { amount, shortDate, parseDate, freshness, plural, DAYS, FOOD_GROUPS } from '../lib/format.js';
  import Chip from '../components/Chip.svelte';
  import Trash2 from '@lucide/svelte/icons/trash-2';
  import ListPlus from '@lucide/svelte/icons/list-plus';
  import Pencil from '@lucide/svelte/icons/pencil';

  /** Above this many items, a box to find one by name is shown. */
  const FILTER_FROM = 8;

  // With a plan, the pantry comes with its ingredients view; without one, it is loaded on its own. Only the plan
  // is tracked, so a toast shown by run() doesn't load it again.
  $effect(() => {
    if (!app.plan) untrack(() => run(loadPantry));
  });

  const pantry = $derived(app.plan
    ? (app.ingredients?.pantry ?? [])
    : app.pantry.map((item) => ({ item, usedByPlan: false, usedFor: [] })));
  const soon = $derived(pantry.filter((line) => freshness(line.item))
    .sort((a, b) => (a.item.daysLeft ?? 0) - (b.item.daysLeft ?? 0)));
  const unusedCount = $derived(pantry.filter((line) => !line.usedByPlan).length);

  let filter = $state('');
  const shown = $derived(filter.trim()
    ? pantry.filter((line) => line.item.name.toLowerCase().includes(filter.trim().toLowerCase()))
    : pantry);

  /** Splits lines by food group, in shop order, sorted by name; items the catalog doesn't know go under "Other". */
  const byGroup = (lines) => FOOD_GROUPS
    .map(([group, title]) => ({
      group,
      title,
      lines: lines
        .filter((line) => (line.item.group ?? 'Other') === group)
        .sort((a, b) => a.item.name.localeCompare(b.item.name)),
    }))
    .filter((section) => section.lines.length > 0);

  /** "Tue dinner: Slaw Fish Tacos", or "Snack: Granola" for meals with no set day. */
  function mealLabel({ title, date, slot }) {
    const when = date ? `${DAYS[parseDate(date).getDay()].slice(0, 3)} ${slot.toLowerCase()}` : slot;
    return `${when}: ${title}`;
  }

  /** Puts a pantry item on the shopping list, to buy more of it. */
  async function restock(item) {
    if (await addToShoppingList({ name: item.name, unit: item.unit || null })) {
      toast(`${item.name} was added to the shopping list.`);
    }
  }

  // The item whose amount and date are being corrected, if any.
  let editing = $state(null);

  async function edit(item) {
    editing = item.id;
    await tick();
    document.getElementById(`edit-quantity-${item.id}`)?.focus();
  }

  function save(event, item) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const quantity = data.get('quantity');
    editing = null;
    updatePantryItem(item.id, {
      quantity: quantity === '' ? null : Number(quantity),
      expiresOn: data.get('expiresOn') || null,
    });
  }

  function add(event) {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    const name = data.get('name');
    run(async () => {
      await api('POST', '/api/pantry', {
        name,
        quantity: data.get('quantity') ? Number(data.get('quantity')) : null,
        unit: data.get('unit') || null,
        expiresOn: data.get('expiresOn') || null,
      });
      form.reset();
      form.elements.name.focus();
      await refreshAfterPantryChange();
      toast(`${name} was added to the pantry.`);
    });
  }
</script>

{#snippet when(item)}
  {@const warning = freshness(item)}
  {#if warning}
    <Chip tone={warning.tone}>{warning.text}{item.expiresOn ? `, ${shortDate(item.expiresOn)}` : ''}</Chip>
  {:else if item.expiresOn}
    <span class="muted">Good until {shortDate(item.expiresOn)}</span>
  {/if}
{/snippet}

{#snippet items(lines)}
  <ul class="plain">
    {#each lines as { item, usedFor } (item.id)}
      <li>
        {#if editing === item.id}
          <form class="inline" onsubmit={(event) => save(event, item)}>
            <strong class="edit-name">{item.name}</strong>
            <label>Amount{item.unit ? ` (${item.unit})` : ''}
              <input id="edit-quantity-{item.id}" name="quantity" type="number" min="0" step="any" value={item.quantity ?? ''}>
            </label>
            <label>Expires <input name="expiresOn" type="date" value={item.expiresOn ?? ''}></label>
            <div class="row-actions">
              <button type="submit" class="small primary">Save</button>
              <button type="button" class="small" onclick={() => (editing = null)}>Cancel</button>
            </div>
          </form>
        {:else}
          <div class="row">
            <div>
              <strong>{item.name}</strong> <span class="muted inline-text">{amount(item.quantity, item.unit)}</span>
              <div class="detail">
                {@render when(item)}
                {#if usedFor.length}
                  <span class="muted">For {usedFor.map(mealLabel).join(', ')}</span>
                {/if}
              </div>
            </div>
            <div class="row-actions">
              <button class="icon" aria-label="Change the amount or date of {item.name}" title="Change the amount or date"
                      onclick={() => edit(item)}><Pencil size={16} /></button>
              <button class="icon" aria-label="Add {item.name} to the shopping list" title="Add to the shopping list"
                      onclick={() => restock(item)}><ListPlus size={16} /></button>
              <button class="icon remove" aria-label="Remove {item.name}" title="Remove"
                      onclick={() => removePantryItem(item)}><Trash2 size={16} /></button>
            </div>
          </div>
        {/if}
      </li>
    {/each}
  </ul>
{/snippet}

<section>
  <form class="inline card" onsubmit={add}>
    <label>Add an item <input id="pantry-name" name="name" required placeholder="e.g. cabbage"></label>
    <label>Amount <input id="pantry-quantity" name="quantity" type="number" min="0" step="any"></label>
    <label>Unit <input id="pantry-unit" name="unit" size="6" placeholder="head"></label>
    <label>Expires <input id="pantry-expires" name="expiresOn" type="date"></label>
    <button type="submit">Add</button>
  </form>

  {#if pantry.length === 0}
    <div class="card">
      <p class="muted">The pantry is empty. Items appear here when you mark shopping as bought, or add them above.</p>
    </div>
  {:else}
    {#if soon.length}
      <h2>Use soon ({soon.length})</h2>
      <div class="card soon">
        <ul class="plain">
          {#each soon as { item, usedFor } (item.id)}
            <li class="row">
              <div>
                <strong>{item.name}</strong> {@render when(item)}
                {#if usedFor.length}<span class="muted" style="display:block">For {usedFor.map(mealLabel).join(', ')}</span>{/if}
              </div>
              {#if item.freshness === 'Expired'}
                <button class="small" aria-label="Buy more {item.name}" onclick={() => restock(item)}>Buy more</button>
              {/if}
            </li>
          {/each}
        </ul>
      </div>
    {/if}

    <div class="row list-head">
      <h2>In the pantry ({pantry.length})</h2>
      {#if pantry.length > FILTER_FROM}
        <input type="search" class="filter" aria-label="Find in the pantry" placeholder="Find an item" bind:value={filter}>
      {/if}
    </div>
    {#if app.plan && unusedCount}
      <p class="muted">
        {plural(unusedCount, 'item')} {unusedCount === 1 ? "isn't" : "aren't"} used by this week's meals.
        Swap a meal on the <a href="/plan">Plan tab</a> to use {unusedCount === 1 ? 'it' : 'them'} up.
      </p>
    {/if}
    <div class="card">
      {#each byGroup(shown) as section (section.group)}
        <h3>{section.title}</h3>
        {@render items(section.lines)}
      {:else}
        <p class="muted">Nothing in the pantry matches "{filter}".</p>
      {/each}
    </div>
  {/if}
</section>

<style>
  h3 {
    margin: 0.75rem 0 0.25rem;
    font-size: .85rem;
    text-transform: uppercase;
    letter-spacing: .04em;
    color: var(--muted);
  }

  h3:first-child {
    margin-top: 0;
  }

  .detail { display: flex; flex-wrap: wrap; align-items: center; gap: .2rem .6rem; margin-top: .15rem; }
  .detail:empty { display: none; }
  .inline-text { font-size: .95rem; }
  .soon { border-left: 4px solid var(--warn); }
  .list-head { align-items: center; }
  .list-head h2 { flex: 1 1 auto; }
  .filter { flex: 0 1 14rem; }
  .edit-name { flex-basis: 100%; }
</style>
