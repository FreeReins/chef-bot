<script>
  import { isConfirmed, planAction } from '../lib/app.svelte.js';
  import { amount, shortDate } from '../lib/format.js';
  import Chip from '../components/Chip.svelte';

  let { line } = $props();

  const canBuy = $derived(line.status === 'ToBuy' && isConfirmed() && line.shoppingItemId);
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
  {#if canBuy}
    <button class="small" onclick={() => planAction(`shopping/${line.shoppingItemId}/purchase`, {})}>Bought</button>
  {/if}
</li>
