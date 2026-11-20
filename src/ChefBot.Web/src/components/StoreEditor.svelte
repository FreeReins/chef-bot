<!-- The household's stores: add known ones or your own, choose the main store, and say what each one is used for.
     It edits a list the parent owns (bind:stores) and saves nothing itself. Used by setup step four and by
     Settings, Stores. -->
<script>
  import { app } from '../lib/app.svelte.js';
  import Chip from './Chip.svelte';
  import X from '@lucide/svelte/icons/x';
  import Trash2 from '@lucide/svelte/icons/trash-2';

  /** stores: [{ name, isMain, items, seasonalProduce, months }], changed in place. */
  let { stores = $bindable() } = $props();

  const KINDS = [
    ['Supermarket', 'Supermarkets'],
    ['Discount', 'Discount'],
    ['WarehouseClub', 'Warehouse clubs'],
    ['NaturalFoods', 'Natural foods'],
    ['Specialty', 'Specialty markets'],
    ['FarmersMarket', 'Farmers markets'],
  ];
  const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

  let kind = $state('Supermarket');
  let search = $state('');
  let ownName = $state('');
  let editing = $state(null); // index of the store whose details are open
  let typed = $state('');     // the ingredient being typed into the open store's item box

  const known = $derived(app.stores?.known ?? []);
  const kinds = $derived(KINDS.filter(([key]) => known.some((store) => store.kind === key)));
  const wanted = $derived(search.trim().toLowerCase());
  const shown = $derived(wanted
    ? known.filter((store) => `${store.name} ${store.description}`.toLowerCase().includes(wanted))
    : known.filter((store) => store.kind === kind));
  const ingredients = $derived(app.stores?.ingredients ?? []);

  const has = (name) => stores.some((store) => store.name.trim().toLowerCase() === name.trim().toLowerCase());
  const monthNames = (months) => months.map((month) => MONTHS[month - 1]).join(', ');

  function add(store) {
    // The first everyday store added becomes the main store; ones with their own list or season don't.
    const isMain = !stores.some((other) => other.isMain) && store.items.length === 0 && !store.seasonalProduce;
    stores.push({ name: store.name, isMain, items: [...store.items], seasonalProduce: store.seasonalProduce, months: [...store.months] });
  }

  function addOwn(event) {
    event.preventDefault();
    const name = ownName.trim();
    if (!name || has(name)) return;
    add({ name, items: [], seasonalProduce: false, months: [] });
    ownName = '';
    open(stores.length - 1);
  }

  function open(index) {
    editing = editing === index ? null : index;
    typed = '';
  }

  function remove(index) {
    stores.splice(index, 1);
    editing = null;
  }

  function makeMain(store) {
    for (const other of stores) other.isMain = other === store;
  }

  function addItem(store, event) {
    event.preventDefault();
    const item = typed.trim().toLowerCase();
    if (item && !store.items.includes(item)) store.items.push(item);
    typed = '';
  }

  function setMonth(store, month, on) {
    store.months = on ? [...store.months, month].sort((a, b) => a - b) : store.months.filter((other) => other !== month);
  }
</script>

<datalist id="store-ingredients">
  {#each ingredients as name (name)}<option value={name}></option>{/each}
</datalist>

<h3>Your stores</h3>
{#if stores.length === 0}
  <p class="muted">No stores yet. Until you add one, the shopping list is grouped by food group.</p>
{:else}
  <ul class="plain">
    {#each stores as store, index}
      <li>
        <div class="row">
          <div>
            <strong>{store.name || 'New store'}</strong>
            <div class="chips">
              {#if store.isMain}<Chip tone="good">Main store</Chip>{/if}
              {#if store.items.length}<Chip>{store.items.length} {store.items.length === 1 ? 'item' : 'items'}</Chip>{/if}
              {#if store.seasonalProduce}<Chip>In-season produce{store.months.length ? `, ${monthNames(store.months)}` : ''}</Chip>{/if}
            </div>
          </div>
          <div class="row-actions">
            {#if store.isMain}
              <button type="button" class="small" onclick={() => (store.isMain = false)}>Not the main store</button>
            {:else}
              <button type="button" class="small" onclick={() => makeMain(store)}>Make main store</button>
            {/if}
            <button type="button" class="small" aria-expanded={String(editing === index)} onclick={() => open(index)}>
              {editing === index ? 'Done' : 'Edit'}
            </button>
            <button type="button" class="icon remove" aria-label="Remove {store.name}" title="Remove" onclick={() => remove(index)}><Trash2 size={16} /></button>
          </div>
        </div>

        {#if editing === index}
          <div class="details">
            <label>Name <input bind:value={store.name} maxlength="60"></label>
            <div>
              <p class="muted">Always bought here</p>
              {#if store.items.length}
                <div class="chips">
                  {#each store.items as item (item)}
                    <span class="chip item">{item}
                      <button type="button" class="remove" aria-label="Stop buying {item} at {store.name}"
                              onclick={() => store.items.splice(store.items.indexOf(item), 1)}><X size={14} /></button>
                    </span>
                  {/each}
                </div>
              {/if}
              <form class="inline add-item" onsubmit={(event) => addItem(store, event)}>
                <input list="store-ingredients" bind:value={typed} placeholder="Add an ingredient, such as chicken"
                       aria-label="Ingredient always bought at {store.name}">
                <button type="submit" class="small">Add</button>
              </form>
            </div>
            <label class="check">
              <input type="checkbox" bind:checked={store.seasonalProduce}>
              <span>In-season fruit, vegetables and herbs
                <span class="muted">Sent here during the months below, unless another store lists them.</span>
              </span>
            </label>
            {#if store.seasonalProduce}
              <div class="months" role="group" aria-label="Months {store.name} is open">
                {#each MONTHS as label, month (label)}
                  <label class="month">
                    <input type="checkbox" checked={store.months.includes(month + 1)}
                           onchange={(event) => setMonth(store, month + 1, event.currentTarget.checked)}>
                    {label}
                  </label>
                {/each}
              </div>
            {/if}
          </div>
        {/if}
      </li>
    {/each}
  </ul>
  {#if !stores.some((store) => store.isMain)}
    <p class="muted">No main store: anything a store doesn't list is grouped by food group.</p>
  {/if}
{/if}

<h3 class="spaced">Add a known store</h3>
<div class="row picker-head">
  <div class="segmented" role="tablist" aria-label="Kinds of store">
    {#each kinds as [key, label] (key)}
      <button type="button" role="tab" aria-selected={String(!wanted && kind === key)} onclick={() => { kind = key; search = ''; }}>{label}</button>
    {/each}
  </div>
  <input type="search" bind:value={search} placeholder="Search stores" aria-label="Search known stores">
</div>
<div class="known" role="tabpanel">
  {#each shown as store (store.name)}
    <div class="known-store">
      <span class="known-text">
        <strong>{store.name}</strong>
        <span class="muted">{store.description}</span>
        {#if store.items.length}<span class="muted clip" title={store.items.join(', ')}>For {store.items.join(', ')}</span>{/if}
      </span>
      <button type="button" class="small" disabled={has(store.name)} onclick={() => add(store)}>{has(store.name) ? 'Added' : 'Add'}</button>
    </div>
  {:else}
    <p class="muted">No known store matches. Add it as your own below.</p>
  {/each}
</div>

<form class="inline spaced" onsubmit={addOwn}>
  <label>Or add your own
    <input bind:value={ownName} placeholder="Store name" maxlength="60">
  </label>
  <button type="submit" disabled={!ownName.trim() || has(ownName)}>Add store</button>
</form>

<style>
  .spaced { margin-top: 1.25rem; }
  .details { display: grid; gap: .6rem; margin: .5rem 0 .25rem; padding: .6rem .8rem; background: var(--bg); border-radius: .4rem; }
  .item { display: inline-flex; gap: .25rem; align-items: center; color: var(--ink); }
  .item .remove { padding: 0 .1rem; }
  .add-item { margin-top: .4rem; }
  .add-item input { flex: 1 1 12rem; }
  .check { border-top: 0; padding: 0; }
  .check .muted { display: block; }
  .months { display: grid; grid-template-columns: repeat(auto-fill, minmax(4rem, 1fr)); gap: .2rem .5rem; }
  .month { display: flex; gap: .3rem; align-items: center; color: var(--ink); font-size: .9rem; }
  .picker-head { align-items: center; margin-bottom: .5rem; }
  .picker-head input { flex: 0 1 12rem; }
  .known { display: grid; grid-template-columns: repeat(auto-fill, minmax(15rem, 1fr)); gap: 0 1.25rem; }
  .known-store { display: flex; gap: .55rem; align-items: flex-start; justify-content: space-between; padding: .4rem 0;
    border-top: 1px solid var(--line); }
  .known-text { display: grid; min-width: 0; flex: 1; }
  .known-text .muted { font-size: .8rem; }
</style>
