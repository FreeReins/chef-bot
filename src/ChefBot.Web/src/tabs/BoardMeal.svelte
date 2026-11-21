<!-- One meal on the week board: what it is, and what can be done with it now. A planned dinner or breakfast can be
     dragged to another day, or moved with its Move menu, which also works from the keyboard and on touch screens.
     Swap opens a dialog of replacements (SwapDialog.svelte). In a draft, meals can be kept for the next shuffle; once confirmed, they are marked as cooked or skipped. -->
<script>
  import { tick } from 'svelte';
  import { app, openSwap, toggleKeep, moveMeal, planAction } from '../lib/app.svelte.js';
  import Chip from '../components/Chip.svelte';
  import ArrowLeftRight from '@lucide/svelte/icons/arrow-left-right';
  import Pin from '@lucide/svelte/icons/pin';
  import GripVertical from '@lucide/svelte/icons/grip-vertical';

  /**
   * meal: MealResponse. days: the week's days as { name, date }, for the Move menu.
   * ondragstart, ondragend: told when this meal starts and stops being dragged.
   */
  let { meal, days = [], ondragstart = null, ondragend = null } = $props();

  const draft = $derived(app.plan.status === 'Staged');
  const planned = $derived(meal.status === 'Planned');
  const movable = $derived(planned && Boolean(meal.date) && !meal.isLeftover);
  const kept = $derived(app.keep.includes(meal.id));
  const label = $derived(meal.isLeftover ? 'Lunch, leftovers' : meal.slot);

  const setStatus = (status) => planAction(`meals/${meal.id}/status`, { status });

  /** Moves the meal from its Move menu, then puts focus back on that menu, which is now in the meal's new day. */
  async function moveTo(date) {
    await moveMeal(meal.id, date);
    await tick();
    document.getElementById(`move-${meal.id}`)?.focus();
  }

  function startDrag(event) {
    event.dataTransfer.effectAllowed = 'move';
    event.dataTransfer.setData('text/plain', meal.id);
    ondragstart?.();
  }
</script>

<article class="meal" class:done={!planned} class:leftover={meal.isLeftover} class:kept class:movable
         draggable={movable ? 'true' : undefined} ondragstart={movable ? startDrag : undefined} ondragend={movable ? ondragend : undefined}>
  <div class="slot">
    {#if movable}<span class="grip" title="Drag to another day"><GripVertical size={14} /></span>{/if}
    <span>{label}{meal.isOptional ? ', optional' : ''}</span>
  </div>
  <h4>
    {#if meal.recipeId && !meal.isLeftover}<a href="/recipes/{meal.recipeId}" class="quiet" draggable="false">{meal.title}</a>{:else}{meal.title}{/if}
  </h4>
  {#if meal.isSlowCooker || kept || !planned}
    <div class="chips">
      {#if meal.isSlowCooker}<Chip tone="good">Slow cooker</Chip>{/if}
      {#if kept}<Chip tone="good">Kept</Chip>{/if}
      {#if !planned}<Chip>{meal.status}</Chip>{/if}
    </div>
  {/if}

  <div class="row-actions">
    {#if movable}
      <select class="move" id="move-{meal.id}" aria-label="Move {meal.title} to another day" value={meal.date}
              onchange={(event) => moveTo(event.currentTarget.value)}>
        {#each days as day (day.date)}
          <option value={day.date}>{day.date === meal.date ? day.name : `Move to ${day.name}`}</option>
        {/each}
      </select>
    {/if}
    {#if planned && !meal.isLeftover}
      <button class="icon" aria-label="Swap {meal.title}" title="Swap for another recipe" aria-haspopup="dialog"
              onclick={() => openSwap(meal)}><ArrowLeftRight size={16} /></button>
    {/if}
    {#if draft && !meal.isLeftover}
      <button class="small keep" aria-pressed={String(kept)} aria-label="Keep {meal.title}" title="Keep this meal when shuffling"
              onclick={() => toggleKeep(meal.id)}><Pin size={13} /> Keep</button>
    {/if}
    {#if !draft}
      {#if planned}
        <button class="small" aria-label="{meal.isLeftover ? 'Eaten' : 'Cooked'}: {meal.title}" onclick={() => setStatus('Cooked')}>{meal.isLeftover ? 'Eaten' : 'Cooked'}</button>
        <button class="small" aria-label="Skip {meal.title}" onclick={() => setStatus('Skipped')}>Skip</button>
      {:else}
        <button class="small" aria-label="Undo {meal.status.toLowerCase()}: {meal.title}" onclick={() => setStatus('Planned')}>Undo</button>
      {/if}
    {/if}
  </div>
</article>

<style>
  .meal { display: grid; gap: .25rem; padding: .55rem .6rem; border: 1px solid var(--line); border-radius: .45rem; background: var(--panel); }
  .meal.movable { cursor: grab; }
  .meal.movable:active { cursor: grabbing; }
  .meal.leftover { background: var(--bg); border-style: dashed; }
  .meal.kept { border-color: var(--good); }
  .meal.done, .meal.done :is(h4, a) { color: var(--muted); }
  .meal.done { background: var(--bg); }
  .slot { display: flex; align-items: center; gap: .2rem; color: var(--muted); font-size: .8rem; }
  .grip { display: inline-flex; color: var(--muted); }
  h4 { margin: 0; font-size: .95rem; line-height: 1.3; }
  .quiet { color: inherit; text-decoration: none; }
  .quiet:hover { text-decoration: underline; }
  .chips { margin-top: 0; }
  .row-actions { align-items: center; gap: .35rem; }
  .move { font-size: .85rem; padding: .15rem .3rem; max-width: 100%; }
  .keep { display: inline-flex; align-items: center; gap: .25rem; }
  .keep[aria-pressed="true"] { background: var(--accent); color: var(--accent-ink); border-color: var(--accent); }
  @media (pointer: coarse) {
    .move { padding: .4rem .4rem; }
  }
</style>
