<!-- One meal on the week board: what it is, and what can be done with it now. A planned dinner or breakfast can be
     dragged to another day, or moved with its Move menu, which also works from the keyboard and on touch screens.
     Swap opens a dialog of replacements (SwapDialog.svelte). In a draft, meals can be kept for the next shuffle; once confirmed, they are marked as cooked or skipped. -->
<script>
  import { tick } from 'svelte';
  import { app, openSwap, toggleKeep, moveMeal, planAction } from '../lib/app.svelte.js';
  import { pendingClick } from '../lib/pending.js';
  import { celebrate, popIn } from '../lib/motion.js';
  import Chip from '../components/Chip.svelte';
  import ArrowLeftRight from '@lucide/svelte/icons/arrow-left-right';
  import Pin from '@lucide/svelte/icons/pin';
  import GripVertical from '@lucide/svelte/icons/grip-vertical';

  /**
   * meal: MealResponse. days: the week's days as { name, date }, for the Move menu.
   * ondragstart, ondragend: told when this meal starts and stops being dragged. order: its place on the board, for
   * dealing the cards in turn (WeekBoard.svelte).
   */
  let { meal, days = [], order = 0, ondragstart = null, ondragend = null } = $props();

  const draft = $derived(app.plan.status === 'Staged');
  const planned = $derived(meal.status === 'Planned');
  const movable = $derived(planned && Boolean(meal.date) && !meal.isLeftover);
  const kept = $derived(app.keep.includes(meal.id));
  const label = $derived(meal.isLeftover ? 'Lunch, leftovers' : meal.slot);

  let card = $state();

  /** Marks the meal, and celebrates once the week's last meal is done. */
  const setStatus = (status) => pendingClick(async () => {
    await planAction(`meals/${meal.id}/status`, { status });
    const meals = app.plan?.meals.filter((other) => !other.isOptional) ?? [];
    if (status === 'Cooked' && meals.length && meals.every((other) => other.status !== 'Planned')) celebrate(card, { count: 18 });
  });
  const swap = pendingClick(() => openSwap(meal));

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

<article bind:this={card} class="meal" style:--order={order} class:done={!planned} class:leftover={meal.isLeftover} class:kept class:movable
         draggable={movable ? 'true' : undefined} ondragstart={movable ? startDrag : undefined} ondragend={movable ? ondragend : undefined}>
  <div class="slot" data-slot={meal.isLeftover ? 'Leftovers' : meal.slot}>
    {#if movable}<span class="grip" title="Drag to another day"><GripVertical size={14} /></span>{/if}
    <span class="slot-name">{label}{meal.isOptional ? ', optional' : ''}</span>
  </div>
  <h4>
    {#if meal.recipeId && !meal.isLeftover}<a href="/recipes/{meal.recipeId}" class="quiet" draggable="false">{meal.title}</a>{:else}{meal.title}{/if}
  </h4>
  {#if meal.isSlowCooker || kept || !planned}
    <div class="chips">
      {#if meal.isSlowCooker}<Chip tone="good">Slow cooker</Chip>{/if}
      {#if kept}<span class="chip-wrap" in:popIn><Chip tone="good">Kept</Chip></span>{/if}
      {#if !planned}<span class="chip-wrap" in:popIn><Chip>{meal.status}</Chip></span>{/if}
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
              onclick={swap}><ArrowLeftRight size={16} /></button>
    {/if}
    {#if draft && !meal.isLeftover}
      <button class="small keep" aria-pressed={String(kept)} aria-label="Keep {meal.title}" title="Keep this meal when shuffling"
              onclick={() => toggleKeep(meal.id)}><Pin size={13} /> Keep</button>
    {/if}
    {#if !draft}
      {#if planned}
        <button class="small" aria-label="{meal.isLeftover ? 'Eaten' : 'Cooked'}: {meal.title}" onclick={setStatus('Cooked')}>{meal.isLeftover ? 'Eaten' : 'Cooked'}</button>
        <button class="small" aria-label="Skip {meal.title}" onclick={setStatus('Skipped')}>Skip</button>
      {:else}
        <button class="small" aria-label="Undo {meal.status.toLowerCase()}: {meal.title}" onclick={setStatus('Planned')}>Undo</button>
      {/if}
    {/if}
  </div>
</article>

<style>
  .meal { display: grid; gap: .25rem; padding: .55rem .6rem; border: 1px solid var(--line); border-radius: .45rem; background: var(--panel);
    transition: background-color .35s, border-color .35s, color .35s; }
  .meal :is(h4, a), .slot-name { transition: color .35s, background-color .35s; }
  .chip-wrap { display: inline-flex; }
  .meal.movable { cursor: grab; }
  .meal.movable:active { cursor: grabbing; }
  .meal.leftover { background: var(--bg); border-style: dashed; }
  .meal.kept { border-color: var(--good); }
  .meal.done, .meal.done :is(h4, a) { color: var(--muted); }
  .meal.done { background: var(--bg); }
  .slot { display: flex; align-items: center; gap: .2rem; color: var(--muted); font-size: .8rem; }
  /* Each kind of meal has its colour, so a week reads at a glance. */
  .slot-name { border-radius: 1rem; padding: 0 .45rem; background: var(--chip); }
  [data-slot="Dinner"] .slot-name { color: var(--good); background: var(--good-bg); }
  [data-slot="Breakfast"] .slot-name { color: var(--warn); background: var(--warn-bg); }
  :is([data-slot="Lunch"], [data-slot="Leftovers"]) .slot-name { color: var(--info); background: var(--info-bg); }
  [data-slot="Sweet"] .slot-name { color: var(--bad); background: var(--bad-bg); }
  .meal.done .slot-name { color: var(--muted); background: var(--chip); }
  .grip { display: inline-flex; color: var(--muted); }
  h4 { margin: 0; font-size: .95rem; line-height: 1.3; }
  .quiet { color: inherit; text-decoration: none; }
  .quiet:hover { text-decoration: underline; }
  .chips { margin-top: 0; }
  .row-actions { align-items: center; gap: .35rem; }
  .move { font-size: .85rem; padding: .15rem .3rem; max-width: 100%; }
  .keep { display: inline-flex; align-items: center; gap: .25rem; }
  .keep[aria-pressed="true"] { background: var(--accent); color: var(--accent-ink); border-color: var(--accent); }
  /* The pin drops in when a meal is kept. */
  @media (prefers-reduced-motion: no-preference) {
    .keep[aria-pressed="true"] :global(svg) { animation: pin-drop .3s cubic-bezier(.3, 1.6, .5, 1); }
  }
  @keyframes pin-drop { from { transform: translateY(-4px) rotate(-25deg); } }
  @media (prefers-reduced-motion: reduce) { .meal, .meal :is(h4, a), .slot-name { transition: none; } }
  @media (pointer: coarse) {
    .move { padding: .4rem .4rem; }
  }
</style>
