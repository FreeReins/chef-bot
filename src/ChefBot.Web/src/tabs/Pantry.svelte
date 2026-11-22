<!-- What is in the kitchen, and which meals on the plan are going to use each item. Items running out of date are
     shown first, since they need doing something about; the rest are grouped by food group, in shop order. Works
     without a plan too: then no item is "used". An item's amount and date can be corrected on its row. -->
<script>
  import { untrack, tick } from 'svelte';
  import { app, run, loadPantry, refreshAfterPantryChange, addToShoppingList, removePantryItem, updatePantryItem, toast } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { pending, pendingClick } from '../lib/pending.js';
  import { amount, shortDate, parseDate, freshness, plural, DAYS, FOOD_GROUPS } from '../lib/format.js';
  import Chip from '../components/Chip.svelte';
  import Trash2 from '@lucide/svelte/icons/trash-2';
  import Pencil from '@lucide/svelte/icons/pencil';
  import Refrigerator from '@lucide/svelte/icons/refrigerator';
  import Plus from '@lucide/svelte/icons/plus';
  import ChevronDown from '@lucide/svelte/icons/chevron-down';

  /** Above this many items, a box to find one by name is shown. */
  const FILTER_FROM = 8;
  /** How many of the items to use soon are shown until the rest are asked for: the ones expiring first. */
  const SOON_SHOWN = 3;

  // With a plan, the pantry comes with its ingredients view; without one, it is loaded on its own. Only the plan
  // is tracked, so a toast shown by run() doesn't load it again.
  $effect(() => {
    if (!app.plan && !app.offline) untrack(() => run(loadPantry));
  });

  const pantry = $derived(app.plan
    ? (app.ingredients?.pantry ?? [])
    : app.pantry.map((item) => ({ item, usedByPlan: false, usedFor: [] })));
  const soon = $derived(pantry.filter((line) => freshness(line.item))
    .sort((a, b) => (a.item.daysLeft ?? 0) - (b.item.daysLeft ?? 0)));
  const unusedCount = $derived(pantry.filter((line) => !line.usedByPlan).length);

  let soonOpen = $state(false);
  const soonShown = $derived(soonOpen ? soon : soon.slice(0, SOON_SHOWN));

  /** Shows all the items to use soon, or only the first few; a correction on a row that is hidden is closed. */
  function toggleSoon() {
    soonOpen = !soonOpen;
    if (!soonOpen && editing?.startsWith('soon-')
        && !soon.slice(0, SOON_SHOWN).some((line) => editing === `soon-${line.item.id}`)) {
      editing = null;
    }
  }

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

  // The row whose amount and date are being corrected, if any, as "<list>-<item id>": an item to use soon is listed
  // twice, under Use soon ("soon") and in its food group ("all"), and only the row pressed opens.
  let editing = $state(null);

  async function edit(item, where) {
    editing = `${where}-${item.id}`;
    await tick();
    document.getElementById(`edit-quantity-${where}-${item.id}`)?.focus();
  }

  /** Closes the item's edit form and puts focus back on its pencil button. */
  async function stopEditing(item, where) {
    editing = null;
    await tick();
    document.getElementById(`edit-${where}-${item.id}`)?.focus();
  }

  function save(event, item, where) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const quantity = data.get('quantity');
    stopEditing(item, where);
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
    pending(event, () => run(async () => {
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
    }));
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

<!-- An item's name and amount, then how fresh it is and the meals that will use it. -->
{#snippet summary(item, usedFor)}
  {@const quantity = amount(item.quantity, item.unit)}
  <div class="summary">
    <div class="name-line">
      <strong>{item.name}</strong>
      {#if quantity}<span class="quantity">{quantity}</span>{/if}
    </div>
    <div class="detail">
      {@render when(item)}
      {#if usedFor.length}
        <span class="uses"><span class="muted">For</span>
          {#each usedFor as meal, i (i)}<Chip>{mealLabel(meal)}</Chip>{/each}
        </span>
      {/if}
    </div>
  </div>
{/snippet}

<!-- The rows of one list, each with buttons to correct, restock or remove its item. `where` names the list, so ids
     stay unique when an item is in both. -->
{#snippet items(lines, where, id = undefined)}
  <ul class="plain" {id}>
    {#each lines as { item, usedFor } (item.id)}
      <li>
        {#if editing === `${where}-${item.id}`}
          {@const escape = (event) => { if (event.key === 'Escape') { event.preventDefault(); stopEditing(item, where); } }}
          <form class="inline" onsubmit={(event) => save(event, item, where)}>
            <strong class="edit-name">{item.name}</strong>
            <label>Amount{item.unit ? ` (${item.unit})` : ''}
              <input id="edit-quantity-{where}-{item.id}" onkeydown={escape} name="quantity" type="number" min="0" step="any" value={item.quantity ?? ''}>
            </label>
            <label>Expires <input name="expiresOn" type="date" onkeydown={escape} value={item.expiresOn ?? ''}></label>
            <div class="row-actions">
              <button type="submit" class="small primary">Save</button>
              <button type="button" class="small" onclick={() => stopEditing(item, where)}>Cancel</button>
            </div>
          </form>
        {:else}
          <div class="row item">
            {@render summary(item, usedFor)}
            <div class="row-actions">
              <button class="icon" id="edit-{where}-{item.id}" aria-label="Change the amount or date of {item.name}" title="Change the amount or date"
                      disabled={app.offline} onclick={() => edit(item, where)}><Pencil size={16} /></button>
              <button class="icon" aria-label="Add {item.name} to the shopping list" title="Add to the shopping list"
                      onclick={pendingClick(() => restock(item))}><Plus size={16} /></button>
              <button class="icon remove" aria-label="Remove {item.name}" title="Remove"
                      disabled={app.offline} onclick={pendingClick(() => removePantryItem(item))}><Trash2 size={16} /></button>
            </div>
          </div>
        {/if}
      </li>
    {/each}
  </ul>
{/snippet}

<section>
  {#if app.offline}
    <p class="card offline-note">Offline: the pantry can be changed once ChefBot can be reached. You can still add items to the shopping list.</p>
  {:else}
    <form class="inline card" onsubmit={add}>
      <label>Add an item <input id="pantry-name" name="name" required placeholder="e.g. cabbage"></label>
      <label>Amount <input id="pantry-quantity" name="quantity" type="number" min="0" step="any"></label>
      <label>Unit <input id="pantry-unit" name="unit" size="6" placeholder="head"></label>
      <label>Expires <input id="pantry-expires" name="expiresOn" type="date"></label>
      <button type="submit" class="with-icon"><Plus size={16} />Add</button>
    </form>
  {/if}

  {#if pantry.length === 0}
    <div class="card empty">
      <span class="empty-icon" style="--area: var(--area-pantry)" aria-hidden="true"><Refrigerator size={26} /></span>
      <p class="muted">The pantry is empty. Items appear here when you mark shopping as bought, or add them above.</p>
    </div>
  {:else}
    {#if soon.length}
      <h2>Use soon ({soon.length})</h2>
      <div class="card soon">
        {@render items(soonShown, 'soon', 'use-soon')}
        {#if soon.length > SOON_SHOWN}
          <button type="button" class="small with-icon toggle" aria-expanded={soonOpen} aria-controls="use-soon" onclick={toggleSoon}>
            {soonOpen ? 'Show fewer' : `Show ${soon.length - SOON_SHOWN} more`}<ChevronDown class="chevron" size={16} />
          </button>
        {/if}
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
        <div class="group" style="--group: var(--group-{section.group.toLowerCase()})">
          <h3><span class="swatch" aria-hidden="true"></span>{section.title} <span class="count">{section.lines.length}</span></h3>
          {@render items(section.lines, 'all')}
        </div>
      {:else}
        <p class="muted">Nothing in the pantry matches "{filter}".</p>
      {/each}
    </div>
  {/if}
</section>

<style>
  .offline-note { border-left: 4px solid var(--warn); font-size: .95rem; }
  /* Each food group is a block in its own colour (--group-* in styles.css): a coloured heading over a rule, and a
     bar down the side of its items, so the sections tell apart as you scroll. */
  .group { margin-top: 1.4rem; }
  .group:first-child { margin-top: .2rem; }

  h3 {
    display: flex;
    align-items: center;
    gap: .5rem;
    margin: 0 0 .2rem;
    padding-bottom: .35rem;
    border-bottom: 2px solid color-mix(in srgb, var(--group) 35%, transparent);
    font-size: .85rem;
    text-transform: uppercase;
    letter-spacing: .04em;
    color: var(--group);
  }

  .swatch { flex: none; width: .65rem; height: .65rem; border-radius: 50%; background: var(--group); }
  .group ul { padding-left: .75rem; border-left: 3px solid color-mix(in srgb, var(--group) 30%, transparent); }

  .count { font-weight: 400; letter-spacing: 0; color: var(--muted); }

  /* Roomier rows than the shared lists, with the buttons centred on the item's two lines. */
  .card li { padding: .65rem 0; }
  .item { align-items: center; }
  .summary { display: grid; gap: .3rem; }
  .name-line { display: flex; flex-wrap: wrap; align-items: baseline; gap: .15rem .6rem; }
  .quantity { color: var(--muted); font-size: .95rem; }
  .detail { display: flex; flex-wrap: wrap; align-items: center; gap: .35rem .75rem; font-size: .9rem; }
  .detail:empty { display: none; }
  .uses { display: inline-flex; flex-wrap: wrap; align-items: center; gap: .3rem; }
  .soon { border-left: 4px solid var(--warn); }
  .toggle { margin-top: .4rem; }
  .toggle :global(.chevron) { flex: none; }
  .toggle[aria-expanded='true'] :global(.chevron) { transform: rotate(180deg); }
  @media (prefers-reduced-motion: no-preference) { .toggle :global(.chevron) { transition: transform .15s; } }
  .list-head { align-items: center; }
  .list-head h2 { flex: 1 1 auto; }
  .filter { flex: 0 1 14rem; }
  .edit-name { flex-basis: 100%; }
</style>
