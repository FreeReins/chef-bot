<!-- The plan day by day, where meals are marked as cooked or skipped once the plan is confirmed. -->
<script>
  import { app, isConfirmed } from '../lib/app.svelte.js';
  import { DAYS, parseDate, dayMonth } from '../lib/format.js';
  import ScheduleLine from './ScheduleLine.svelte';

  const days = $derived.by(() => {
    const start = parseDate(app.plan.weekStart);
    return DAYS.map((name, offset) => ({
      name,
      date: new Date(start.getFullYear(), start.getMonth(), start.getDate() + offset),
      meals: app.plan.meals.filter((meal) => meal.day === name),
    }));
  });
  const anyDay = $derived(app.plan.meals.filter((meal) => !meal.day));
</script>

<div class="grid">
  {#each days as day (day.name)}
    <section class="card">
      <h2>{day.name} <span class="muted">{dayMonth(day.date)}</span></h2>
      {#each day.meals as meal (meal.id)}
        <ScheduleLine {meal} />
      {:else}
        <p class="muted">Nothing planned</p>
      {/each}
    </section>
  {/each}
</div>
{#if anyDay.length}
  <section class="card">
    <h2>Any day: snacks and sweets</h2>
    {#each anyDay as meal (meal.id)}<ScheduleLine {meal} />{/each}
  </section>
{/if}
{#if !isConfirmed()}
  <p class="muted">Confirm the plan to start marking meals as cooked.</p>
{/if}

<style>
  /* The days are the sections of the page, so they are h2, sized like the cards' headings elsewhere. */
  h2 { margin: 0; font-size: 1rem; }
</style>
