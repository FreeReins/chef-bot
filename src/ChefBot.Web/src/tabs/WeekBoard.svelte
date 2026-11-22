<!-- The plan's week day by day, then the snacks, sweets and drinks made on any day. Dinners and breakfasts can be
     dragged from one day to another; dropping one onto a day that already has that meal swaps the two. Each meal's
     Move menu does the same without a mouse. Leftover lunches follow their dinner. Just after a week is planned or
     shuffled, its meals are dealt onto the days one after another. -->
<script>
  import { app, moveMeal } from '../lib/app.svelte.js';
  import { DAYS, addDays, dayMonth, parseDate } from '../lib/format.js';
  import BoardMeal from './BoardMeal.svelte';

  const ORDER = { Breakfast: 0, Lunch: 1, Dinner: 2 };

  const days = $derived(DAYS.map((name, offset) => {
    const date = addDays(app.plan.weekStart, offset);
    return {
      name,
      date,
      label: dayMonth(parseDate(date)),
      meals: app.plan.meals.filter((meal) => meal.date === date).sort((a, b) => (ORDER[a.slot] ?? 3) - (ORDER[b.slot] ?? 3)),
    };
  }));
  const anyDay = $derived(app.plan.meals.filter((meal) => !meal.date));
  // Each meal's place in the deal, day by day and then the extras.
  const order = $derived(new Map([...days.flatMap((day) => day.meals), ...anyDay].map((meal, index) => [meal.id, index])));

  // Dealing lasts a moment after planning or shuffling. Opening the tab later shows the board as it is.
  let dealing = $state(false);
  $effect(() => {
    if (Date.now() - app.dealt > 1500) return undefined;
    dealing = true;
    const timer = setTimeout(() => (dealing = false), 1500);
    return () => clearTimeout(timer);
  });

  let dragging = $state(null); // the meal being dragged
  let over = $state(null);     // the date of the day it would be dropped on

  function allowDrop(event, day) {
    if (!dragging || dragging.date === day.date) return;
    event.preventDefault();
    event.dataTransfer.dropEffect = 'move';
    over = day.date;
  }

  function leave(event, day) {
    if (over === day.date && !event.currentTarget.contains(event.relatedTarget)) over = null;
  }

  function drop(event, day) {
    event.preventDefault();
    const meal = dragging;
    dragging = null;
    over = null;
    if (meal && meal.date !== day.date) moveMeal(meal.id, day.date);
  }

  function stopDragging() {
    dragging = null;
    over = null;
  }

  /** What dropping the dragged meal on a day will do, said in the day while it is dragged over. */
  function dropHint(day) {
    const there = day.meals.find((meal) => meal.slot === dragging?.slot && !meal.isLeftover);
    return there ? `Drop to swap with ${there.title}` : `Drop to move to ${day.name}`;
  }
</script>

<div class="board" class:dealing>
  {#each days as day (day.date)}
    <!-- Dragging is a shortcut for mouse users: every meal's Move menu does the same from the keyboard. -->
    <!-- svelte-ignore a11y_no_noninteractive_element_interactions -->
    <section class="day" class:over={over === day.date} class:dragging={dragging && dragging.date !== day.date}
             aria-labelledby="day-{day.date}"
             ondragover={(event) => allowDrop(event, day)} ondragleave={(event) => leave(event, day)} ondrop={(event) => drop(event, day)}>
      <h3 id="day-{day.date}">{day.name} <span class="muted">{day.label}</span></h3>
      {#each day.meals as meal (meal.id)}
        <BoardMeal {meal} {days} order={order.get(meal.id)} ondragstart={() => (dragging = meal)} ondragend={stopDragging} />
      {:else}
        <p class="muted nothing">Nothing planned</p>
      {/each}
      {#if over === day.date}<p class="hint" aria-hidden="true">{dropHint(day)}</p>{/if}
    </section>
  {/each}
</div>

{#if anyDay.length}
  <section class="card any-day" class:dealing aria-labelledby="any-day">
    <h3 id="any-day">Any day: snacks, sweets and drinks</h3>
    <p class="muted">Make these whenever suits during the week.</p>
    <div class="extras">
      {#each anyDay as meal (meal.id)}<BoardMeal {meal} order={order.get(meal.id)} />{/each}
    </div>
  </section>
{/if}

<style>
  .board { display: grid; gap: .6rem; grid-template-columns: repeat(auto-fill, minmax(min(13.5rem, 100%), 1fr)); margin-top: .75rem; }
  .day { background: var(--panel); border: 1px solid var(--line); border-radius: .5rem; padding: .6rem; display: grid; gap: .5rem; align-content: start; }
  .day.dragging { border-style: dashed; }
  .day.over { border-color: var(--accent); border-style: solid; box-shadow: inset 0 0 0 1px var(--accent); }
  h3 { margin: 0; font-size: 1rem; }
  .nothing { font-size: .85rem; }
  .hint { margin: 0; font-size: .85rem; color: var(--accent); font-weight: 600; }
  .any-day h3 { margin-bottom: .1rem; }
  /* A card dealt onto the table, a little later the further along the week it is. */
  @media (prefers-reduced-motion: no-preference) {
    .dealing :global(.meal) { animation: deal .32s cubic-bezier(.3, 1.3, .5, 1) calc(min(var(--order, 0), 20) * 30ms) backwards; }
  }
  @keyframes deal { from { opacity: 0; transform: translateY(10px) rotate(-1.5deg) scale(.97); } }
  .extras { display: grid; gap: .5rem; grid-template-columns: repeat(auto-fill, minmax(min(13.5rem, 100%), 1fr)); margin-top: .5rem; }
</style>
