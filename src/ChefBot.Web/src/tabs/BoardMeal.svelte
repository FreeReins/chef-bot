<!-- One meal on the week board, as a single line: a coloured stripe and label for its slot, the title, and at most two
     buttons for what is done with it now. In a draft that is Swap and Keep (kept for the next shuffle); once
     confirmed, Swap and Cooked. Moving to another day, Skip and Undo are in its ⋯ menu (OverflowMenu.svelte), which
     works from the keyboard and on touch screens; a planned dinner or breakfast can also be dragged to another day.
     Swap opens a dialog of replacements (SwapDialog.svelte). A recipe just marked cooked asks how it was, a review in
     one tap (QuickReview.svelte); it is never required, and the prompt goes once answered or closed, or when the page
     reloads. A meal with no recipe, such as a plate from Swap's "Or make up a plate", has a lightbulb after its
     title, so the week shows which meals are creative ideas. -->
<script>
  import { tick } from 'svelte';
  import { app, openSwap, toggleKeep, moveMeal, planAction } from '../lib/app.svelte.js';
  import { pendingClick } from '../lib/pending.js';
  import { celebrate } from '../lib/motion.js';
  import OverflowMenu from '../components/OverflowMenu.svelte';
  import QuickReview from '../components/QuickReview.svelte';
  import ArrowLeftRight from '@lucide/svelte/icons/arrow-left-right';
  import Pin from '@lucide/svelte/icons/pin';
  import CookingPot from '@lucide/svelte/icons/cooking-pot';
  import Lightbulb from '@lucide/svelte/icons/lightbulb';
  import Check from '@lucide/svelte/icons/check';
  import Minus from '@lucide/svelte/icons/minus';

  /**
   * meal: MealResponse. days: the week's days as { name, date }, for Move to.
   * ondragstart, ondragend: told when this meal starts and stops being dragged. order: its place on the board, for
   * dealing the cards in turn (WeekBoard.svelte).
   */
  let { meal, days = [], order = 0, ondragstart = null, ondragend = null } = $props();

  const draft = $derived(app.plan.status === 'Staged');
  const planned = $derived(meal.status === 'Planned');
  const movable = $derived(planned && Boolean(meal.date) && !meal.isLeftover);
  const kept = $derived(app.keep.includes(meal.id));
  const creative = $derived(!meal.recipeId && !meal.isLeftover); // made up from ingredients, not a recipe
  const label = $derived(`${meal.isLeftover ? 'Leftovers' : meal.slot}${meal.isOptional ? ', optional' : ''}`);
  const targets = $derived(movable ? days.filter((day) => day.date !== meal.date) : []);

  let card = $state();
  let asking = $state(false); // just marked cooked here, so it asks how it was

  /** Marks the meal, and celebrates once the week's last meal is done. */
  async function mark(status) {
    await planAction(`meals/${meal.id}/status`, { status });
    asking = status === 'Cooked' && meal.status === 'Cooked' && Boolean(meal.recipeId) && !meal.isLeftover;
    const meals = app.plan?.meals.filter((other) => !other.isOptional) ?? [];
    if (status === 'Cooked' && meals.length && meals.every((other) => other.status !== 'Planned')) celebrate(card, { count: 18 });
  }
  const setStatus = (status) => pendingClick(() => mark(status));
  const swap = pendingClick(() => openSwap(meal));

  /** Moves the meal from its ⋯ menu, then puts focus on that menu's button, which is now in the meal's new day. */
  async function moveTo(date, close) {
    close();
    await moveMeal(meal.id, date);
    await tick();
    document.getElementById(`more-${meal.id}`)?.focus();
  }

  function startDrag(event) {
    event.dataTransfer.effectAllowed = 'move';
    event.dataTransfer.setData('text/plain', meal.id);
    ondragstart?.();
  }
</script>

<article bind:this={card} class="meal" style:--order={order} data-slot={meal.isLeftover ? 'Leftovers' : meal.slot}
         class:done={!planned} class:leftover={meal.isLeftover} class:kept class:movable
         draggable={movable ? 'true' : undefined} ondragstart={movable ? startDrag : undefined} ondragend={movable ? ondragend : undefined}>
  <div class="line">
    <span class="slot">{label}</span>
    <h4>
      {#if meal.recipeId && !meal.isLeftover}<a href="/recipes/{meal.recipeId}" class="quiet" draggable="false">{meal.title}</a>{:else}{meal.title}{/if}
      {#if meal.isSlowCooker}<span class="slow" title="Slow cooker"><CookingPot size={14} aria-hidden="true" /><span class="sr-only">, slow cooker</span></span>{/if}
      {#if creative}<span class="mark" title="Creative idea"><Lightbulb size={14} aria-hidden="true" /><span class="sr-only">, creative idea</span></span>{/if}
      {#if kept}<span class="sr-only">, kept</span>{/if}
    </h4>

    <div class="buttons">
      {#if !planned}
        <span class="status">
          {#if meal.status === 'Skipped'}<Minus size={14} aria-hidden="true" />{:else}<Check size={14} aria-hidden="true" />{/if}{meal.status}
        </span>
      {:else if !meal.isLeftover}
        <button class="icon" aria-label="Swap {meal.title}" title="Swap for another recipe" aria-haspopup="dialog"
                onclick={swap}><ArrowLeftRight size={16} /></button>
      {/if}
      {#if draft && !meal.isLeftover}
        <button class="small keep" aria-pressed={String(kept)} aria-label="Keep {meal.title}" title="Keep this meal when shuffling"
                onclick={() => toggleKeep(meal.id)}><Pin size={13} /> Keep</button>
      {:else if !draft && planned}
        <button class="small" aria-label="{meal.isLeftover ? 'Eaten' : 'Cooked'}: {meal.title}" onclick={setStatus('Cooked')}>{meal.isLeftover ? 'Eaten' : 'Cooked'}</button>
      {/if}
      {#if targets.length || !draft}
        <OverflowMenu id="more-{meal.id}" label="More for {meal.title}">
          {#snippet children(close)}
            {#each targets as day (day.date)}
              <button type="button" onclick={() => moveTo(day.date, close)}>Move to {day.name}</button>
            {/each}
            {#if !draft && planned}
              <button type="button" aria-label="Skip {meal.title}" onclick={pendingClick(async () => { close(); await mark('Skipped'); })}>Skip</button>
            {:else if !draft}
              <button type="button" aria-label="Undo {meal.status.toLowerCase()}: {meal.title}" onclick={pendingClick(async () => { close(); await mark('Planned'); })}>Undo {meal.status.toLowerCase()}</button>
            {/if}
          {/snippet}
        </OverflowMenu>
      {/if}
    </div>
  </div>

  {#if asking && !planned}
    <QuickReview compact recipe={{ id: meal.recipeId, name: meal.title }} ondismiss={() => (asking = false)} />
  {/if}
</article>

<style>
  /* Each kind of meal has its colour, on the stripe and the label, so a week reads at a glance. */
  .meal { --slot: var(--muted); display: grid; gap: .35rem; padding: .35rem .5rem .35rem .65rem; border-left: 3px solid var(--slot);
    border-radius: .3rem; background: var(--panel); transition: background-color .35s, border-color .35s, color .35s; }
  .meal[data-slot="Dinner"] { --slot: var(--good); }
  .meal[data-slot="Breakfast"] { --slot: var(--warn); }
  .meal:is([data-slot="Lunch"], [data-slot="Leftovers"]) { --slot: var(--info); }
  .meal[data-slot="Sweet"] { --slot: var(--bad); }
  .meal.leftover { border-left-style: dashed; background: none; }
  .meal.kept { background: var(--good-bg); }
  .meal.done { --slot: var(--line); background: none; }
  .meal.done :is(h4, a, .slot) { color: var(--muted); }
  .meal.movable { cursor: grab; }
  .meal.movable:active { cursor: grabbing; }

  .line { display: flex; flex-wrap: wrap; align-items: center; gap: .25rem .75rem; min-height: 2rem; }
  .slot { flex: 0 0 5.5rem; font-size: .78rem; font-weight: 600; text-transform: uppercase; letter-spacing: .03em; color: var(--slot); }
  .meal.done .slot { color: var(--muted); }
  h4 { flex: 1 1 10rem; margin: 0; font-size: 1rem; font-weight: 600; line-height: 1.3; min-width: 0; }
  .meal.done h4 { font-weight: 400; }
  .quiet { color: inherit; text-decoration: none; }
  .quiet:hover { text-decoration: underline; }
  .slow, .mark { display: inline-flex; vertical-align: -2px; margin-left: .3rem; color: var(--muted); }
  .buttons { display: flex; align-items: center; gap: .35rem; margin-left: auto; }
  .status { display: inline-flex; align-items: center; gap: .2rem; font-size: .85rem; color: var(--muted); }
  .keep { display: inline-flex; align-items: center; gap: .25rem; }
  .keep[aria-pressed="true"] { background: var(--accent); color: var(--accent-ink); border-color: var(--accent); }
  /* The pin drops in when a meal is kept. */
  @media (prefers-reduced-motion: no-preference) {
    .keep[aria-pressed="true"] :global(svg) { animation: pin-drop .3s cubic-bezier(.3, 1.6, .5, 1); }
  }
  @keyframes pin-drop { from { transform: translateY(-4px) rotate(-25deg); } }
  @media (prefers-reduced-motion: reduce) { .meal { transition: none; } }
  /* On a phone the label sits above the title, which gets the full width. */
  @media (max-width: 30rem) {
    .slot { flex-basis: 100%; }
  }
</style>
