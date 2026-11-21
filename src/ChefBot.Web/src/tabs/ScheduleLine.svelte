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

<style>
  .slot { display: grid; grid-template-columns: 5.5rem 1fr auto; gap: .5rem; align-items: baseline; padding: .25rem 0; border-top: 1px solid var(--line); }
  .slot:first-of-type { border-top: 0; }
  .label { color: var(--muted); font-size: .85rem; }
  /* Phones: the buttons go on their own line under the meal, so they don't squeeze its name. */
  @media (max-width: 40rem) {
    .slot { grid-template-columns: 4.5rem minmax(0, 1fr); }
    .slot .row-actions { grid-column: 2; }
    .slot .row-actions:empty { display: none; }
  }
</style>
