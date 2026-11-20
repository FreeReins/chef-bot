<script>
  import { isConfirmed, planAction } from '../lib/app.svelte.js';
  import Chip from '../components/Chip.svelte';

  let { meal } = $props();

  const setStatus = (status) => planAction(`meals/${meal.id}/status`, { status });
</script>

<div class="slot">
  <span class="label">{meal.slot}</span>
  <span>{meal.title}{meal.isOptional ? ' (optional)' : ''}</span>
  <span class="row-actions">
    {#if isConfirmed()}
      {#if meal.status === 'Planned'}
        <button class="small" onclick={() => setStatus('Cooked')}>{meal.isLeftover ? 'Eaten' : 'Cooked'}</button>
        <button class="small" onclick={() => setStatus('Skipped')}>Skip</button>
      {:else}
        <Chip>{meal.status}</Chip>
        <button class="small" onclick={() => setStatus('Planned')}>Undo</button>
      {/if}
    {/if}
  </span>
</div>
