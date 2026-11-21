<script>
  import { app, isConfirmed, planAction, setShoppingStore } from '../lib/app.svelte.js';
  import { amount, shortDate } from '../lib/format.js';
  import Chip from '../components/Chip.svelte';

  let { line } = $props();

  const canBuy = $derived(line.status === 'ToBuy' && isConfirmed() && line.shoppingItemId);
  const stores = $derived(app.stores?.stores ?? []);
  const canMove = $derived(line.status === 'ToBuy' && line.shoppingItemId && stores.length > 0);
</script>

<li class="row">
  <div>
    <strong>{line.name}</strong> {amount(line.quantity, line.unit)}
    <div class="chips">
      {#if line.isOptional}<Chip>Optional</Chip>{/if}
      {#if line.expiresOn}<Chip tone="warn">Use by {shortDate(line.expiresOn)}</Chip>{/if}
      {#each line.usedIn as meal}<Chip>{meal}</Chip>{/each}
    </div>
  </div>
  <div class="row-actions">
    {#if canMove}
      <select class="store" aria-label="Where to buy {line.name}" value={line.store ?? ''}
              onchange={(event) => setShoppingStore(line.shoppingItemId, event.currentTarget.value)}>
        {#each stores as store (store.name)}<option value={store.name}>{store.name}</option>{/each}
        <option value="">No particular store</option>
      </select>
    {/if}
    {#if canBuy}
      <button class="small" onclick={() => planAction(`shopping/${line.shoppingItemId}/purchase`, {})}>Bought</button>
    {/if}
  </div>
</li>

<style>
  .store { font-size: .85rem; padding: .1rem .35rem; max-width: 11rem; }
  /* At least 16px on phones, so iPhones don't zoom in on it. */
  @media (max-width: 40rem) { .store { font-size: 1rem; padding: .3rem .4rem; } }
</style>
