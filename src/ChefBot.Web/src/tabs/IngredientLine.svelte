<!-- One line of the shopping list. A plan's line can be ticked off once the plan is confirmed; a line added by hand
     (it has a manualId) can be ticked off or removed any time. -->
<script>
  import { app, isConfirmed, planAction, setShoppingStore, buyManual, removeManualItem, setManualStore } from '../lib/app.svelte.js';
  import { amount, shortDate } from '../lib/format.js';
  import Chip from '../components/Chip.svelte';
  import Trash2 from '@lucide/svelte/icons/trash-2';

  let { line } = $props();

  const manual = $derived(Boolean(line.manualId));
  const itemId = $derived(line.manualId ?? line.shoppingItemId);
  const canBuy = $derived(line.status === 'ToBuy' && itemId && (manual || isConfirmed()));
  const stores = $derived(app.stores?.stores ?? []);
  const canMove = $derived(line.status === 'ToBuy' && itemId && stores.length > 0);

  const buy = () => (manual ? buyManual(`${line.manualId}/purchase`) : planAction(`shopping/${line.shoppingItemId}/purchase`, {}));
  const move = (store) => (manual ? setManualStore(line.manualId, store) : setShoppingStore(line.shoppingItemId, store));
</script>

<li class="row">
  <div>
    <strong>{line.name}</strong> {amount(line.quantity, line.unit)}
    <div class="chips">
      {#if manual}<Chip>Added by hand</Chip>{/if}
      {#if line.isOptional}<Chip>Optional</Chip>{/if}
      {#if line.expiresOn}<Chip tone="warn">Use by {shortDate(line.expiresOn)}</Chip>{/if}
      {#each line.usedIn as meal}<Chip>{meal}</Chip>{/each}
    </div>
  </div>
  <div class="row-actions">
    {#if canMove}
      <select class="store" aria-label="Where to buy {line.name}" value={line.store ?? ''}
              onchange={(event) => move(event.currentTarget.value)}>
        {#each stores as store (store.name)}<option value={store.name}>{store.name}</option>{/each}
        <option value="">No particular store</option>
      </select>
    {/if}
    {#if canBuy}
      <button class="small" aria-label="Bought: {line.name}" onclick={buy}>Bought</button>
    {/if}
    {#if manual}
      <button class="icon remove" aria-label="Remove {line.name} from the list" title="Remove from the list"
              onclick={() => removeManualItem(line.manualId)}><Trash2 size={16} /></button>
    {/if}
  </div>
</li>

<style>
  .store { font-size: .85rem; padding: .1rem .35rem; max-width: 11rem; }
  /* At least 16px on phones, so iPhones don't zoom in on it. */
  @media (max-width: 40rem) { .store { font-size: 1rem; padding: .3rem .4rem; } }
</style>
