<!-- One line of the shopping list. A line still to buy has a box to tick it off: a plan's line once the plan is
     confirmed, a line added by hand (it has a manualId) any time. Lines added by hand can also be removed. -->
<script>
  import { app, isConfirmed, planAction, setShoppingStore, buyManual, removeManualItem, setManualStore } from '../lib/app.svelte.js';
  import { amount, shortDate } from '../lib/format.js';
  import Chip from '../components/Chip.svelte';
  import Trash2 from '@lucide/svelte/icons/trash-2';

  let { line } = $props();

  const manual = $derived(Boolean(line.manualId));
  const itemId = $derived(line.manualId ?? line.shoppingItemId);
  const toBuy = $derived(line.status === 'ToBuy' && Boolean(itemId));
  const canBuy = $derived(toBuy && (manual || isConfirmed()));
  const stores = $derived(app.stores?.stores ?? []);
  const canMove = $derived(toBuy && stores.length > 0);

  const buy = () => (manual ? buyManual(`${line.manualId}/purchase`) : planAction(`shopping/${line.shoppingItemId}/purchase`, {}));
  const move = (store) => (manual ? setManualStore(line.manualId, store) : setShoppingStore(line.shoppingItemId, store));
</script>

<li class="line">
  {#if toBuy}
    <input type="checkbox" class="tick" aria-label="Bought: {line.name}" disabled={!canBuy}
           title={canBuy ? 'Tick off once bought' : 'Confirm the plan to tick this off'} onchange={buy}>
  {/if}
  <div class="what">
    <strong>{line.name}</strong> <span class="amount">{amount(line.quantity, line.unit)}</span>
    {#if line.isOptional || line.expiresOn}
      <span class="chips">
        {#if line.isOptional}<Chip>Optional</Chip>{/if}
        {#if line.expiresOn}<Chip tone="warn">Use by {shortDate(line.expiresOn)}</Chip>{/if}
      </span>
    {/if}
    {#if line.usedIn.length}<span class="muted for">For {line.usedIn.join(', ')}</span>{/if}
  </div>
  <div class="row-actions">
    {#if canMove}
      <select class="store" aria-label="Where to buy {line.name}" title="Where to buy it" value={line.store ?? ''}
              onchange={(event) => move(event.currentTarget.value)}>
        {#each stores as store (store.name)}<option value={store.name}>{store.name}</option>{/each}
        <option value="">Any store</option>
      </select>
    {/if}
    {#if manual}
      <button class="icon remove" aria-label="Remove {line.name} from the list" title="Remove from the list"
              onclick={() => removeManualItem(line.manualId)}><Trash2 size={16} /></button>
    {/if}
  </div>
</li>

<style>
  .line { display: flex; flex-wrap: wrap; align-items: center; gap: .25rem .75rem; }
  .tick { width: 1.15rem; height: 1.15rem; margin: 0; flex: none; accent-color: var(--accent); cursor: pointer; }
  .tick:disabled { cursor: default; }
  .what { flex: 1 1 12rem; min-width: 0; }
  .amount { color: var(--muted); }
  .what .chips { display: inline-flex; margin: 0 0 0 .25rem; vertical-align: middle; }
  .for { display: block; font-size: .82rem; }
  /* Quiet until used: the section heading already says which store. */
  .store { font-size: .85rem; padding: .1rem .35rem; max-width: 11rem; border-color: transparent; background: none; color: var(--muted); }
  .store:hover, .store:focus-visible { border-color: var(--line); color: var(--ink); }
  /* At least 16px on phones, so iPhones don't zoom in on it; bigger tick box for fingers. */
  @media (max-width: 40rem) { .store { font-size: 1rem; padding: .3rem .4rem; } }
  @media (pointer: coarse) { .tick { width: 1.4rem; height: 1.4rem; } }
</style>
