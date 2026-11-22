<!-- Adding a recipe to the plan, /recipes/{id}/plan, in a dialog over its page: which week, which day and which meal,
     and how many servings. A slot that already has a meal says what it is, and asks whether to replace it or add this
     one alongside. A week with no plan yet gets one holding just this meal. The shopping list catches up a moment later
     (the server does it in the background and says so on the live stream). -->
<script>
  import { app, run, toast, addToPlan } from '../lib/app.svelte.js';
  import { go } from '../lib/addresses.js';
  import { api } from '../lib/api.js';
  import { untrack } from 'svelte';
  import { addDays, parseDate, todayIso, weekName, shortDate } from '../lib/format.js';
  import { pending } from '../lib/pending.js';
  import Modal from '../components/Modal.svelte';
  import ServingsStepper from '../components/ServingsStepper.svelte';
  import CalendarPlus from '@lucide/svelte/icons/calendar-plus';

  /** recipe: the RecipeResponse. serves: the servings its page shows. onclose(): asked to close. */
  let { recipe, serves, onclose } = $props();

  const SLOTS = ['Breakfast', 'Lunch', 'Dinner', 'Snack', 'Sweet', 'Drink'];
  /** Snacks, sweets and drinks are made once for the week, so they can go on any day. */
  const ANY_DAY = ['Snack', 'Sweet', 'Drink'];
  const COURSE_SLOT = { Main: 'Dinner', Breakfast: 'Breakfast', Snack: 'Snack', Sweet: 'Sweet', Drink: 'Drink' };

  const today = todayIso();
  const thisWeek = addDays(today, -parseDate(today).getDay());

  let plans = $state([]);         // PlanSummaryResponse[] for the weeks that have a plan
  let loaded = $state(false);
  let people = $state(null);      // how many each plan is for, from the planning settings
  let shown = $state(null);       // the PlanResponse of the chosen week, when it has one
  // The dialog starts from the recipe and the servings its page shows; changing them here doesn't change the page.
  const start = untrack(() => ({ slot: COURSE_SLOT[recipe.course] ?? 'Dinner', scaled: serves !== recipe.servings, servings: Math.max(1, Math.round(serves)) }));
  let weekStart = $state(thisWeek);
  let slot = $state(start.slot);
  let date = $state(null);
  let picked = $state(false);     // the day was chosen by hand, so it isn't moved to a free one
  let choose = $state(start.scaled);
  let servings = $state(start.servings);
  let replace = $state(true);

  const weeks = $derived([...new Set([thisWeek, addDays(thisWeek, 7), ...plans.map((plan) => plan.weekStart)])]
    .filter((start) => start >= thisWeek).sort());
  const summary = $derived(plans.find((plan) => plan.weekStart === weekStart) ?? null);
  const days = $derived(Array.from({ length: 7 }, (_, index) => addDays(weekStart, index)));
  const dated = $derived(!ANY_DAY.includes(slot));
  const mealOn = (day, kind) => shown?.meals.find((meal) => meal.date === day && meal.slot === kind && !meal.isLeftover) ?? null;
  const taken = $derived(date ? mealOn(date, slot) : null);
  const canReplace = $derived(taken?.status === 'Planned');
  const where = $derived(`${date ? parseDate(date).toLocaleDateString(undefined, { weekday: 'long' }) : 'any day'} ${slot.toLowerCase()}`);
  const forWhom = $derived(dated ? `For the household${people ? ` (${people})` : ''}` : `As the recipe makes it (${recipe.servings})`);

  run(async () => {
    [plans, { people }] = await Promise.all([api('GET', '/api/plans'), api('GET', '/api/planning')]);
    loaded = true;
  });

  // The chosen week's meals, so taken slots can say what is in them.
  $effect(() => {
    const id = summary?.id;
    shown = null;
    // untrack: run() reads the page's busy count, which would make this run again each time it changes.
    if (id) untrack(() => run(async () => { const plan = await api('GET', `/api/plans/${id}`); if (summary?.id === id) shown = plan; }));
  });

  // A day that suits the meal: the first one from today on that is free, or else the first one still to come.
  $effect(() => {
    if (!dated) { if (!picked) date = null; return; }
    if (picked && date && date >= today && days.includes(date)) return;
    const ahead = days.filter((day) => day >= today);
    date = ahead.find((day) => !mealOn(day, slot)) ?? ahead[0] ?? null;
  });

  $effect(() => { replace = canReplace; });

  function submit(event) {
    event.preventDefault();
    return pending(event, () => addToPlan({
      recipeId: recipe.id, weekStart, date, slot,
      servings: choose ? servings : null,
      replaces: taken && replace ? taken.id : null,
    }, (plan) => {
      const started = (plan.notes ?? []).find((note) => note.startsWith('Started a plan'));
      const how = choose ? ` for ${servings}` : '';
      toast(`${started ? `${started} ` : ''}${taken && replace ? `${recipe.name} replaces ${taken.title}` : `Added ${recipe.name} to ${where}`}${how}. `
        + 'Your shopping list will catch up in a moment.', 'note', { label: 'View plan', run: () => go('/plan') });
      onclose();
    }));
  }

  const dayName = (day) => parseDate(day).toLocaleDateString(undefined, { weekday: 'short' });
  const dayNumber = (day) => parseDate(day).getDate();
  const fullDay = (day) => parseDate(day).toLocaleDateString(undefined, { weekday: 'long', month: 'long', day: 'numeric' });
</script>

<Modal title="Add {recipe.name} to the plan" {onclose}>
  <form class="add" onsubmit={submit}>
    <fieldset>
      <legend>Week</legend>
      <div class="segmented">
        {#each weeks as start (start)}
          <button type="button" aria-pressed={start === weekStart} onclick={() => { weekStart = start; picked = false; }}>
            {weekName(start, addDays(start, 6))}<span class="sub"> · {shortDate(start)}</span>
          </button>
        {/each}
      </div>
      {#if loaded && !summary}
        <p class="hint">No plan for this week yet. Adding this starts one with just this meal; the planner can fill the rest later.</p>
      {:else if summary?.status === 'Staged'}
        <p class="hint">This week's plan is still a draft.</p>
      {/if}
    </fieldset>

    <fieldset>
      <legend>Meal</legend>
      <div class="segmented">
        {#each SLOTS as kind (kind)}
          <button type="button" aria-pressed={kind === slot} onclick={() => { slot = kind; picked = false; }}>{kind}</button>
        {/each}
      </div>
    </fieldset>

    <fieldset>
      <legend>Day</legend>
      <div class="days">
        {#if !dated}
          <button type="button" class="day any" aria-pressed={date === null} onclick={() => { date = null; picked = true; }}>Any day</button>
        {/if}
        {#each days as day (day)}
          {@const there = mealOn(day, slot)}
          <button type="button" class="day" aria-pressed={day === date} disabled={day < today} onclick={() => { date = day; picked = true; }}
                  title="{fullDay(day)}{there ? `: ${there.title}` : ''}">
            <span class="name">{dayName(day)}</span> <span class="number">{dayNumber(day)}</span>
            {#if there}<span class="sr-only">, {there.title} planned</span>{/if}
            <span class="dot" class:on={there} aria-hidden="true"></span>
          </button>
        {/each}
      </div>
    </fieldset>

    {#if taken}
      <fieldset class="taken">
        <legend>{where[0].toUpperCase() + where.slice(1)} has <strong>{taken.title}</strong></legend>
        <label class="check"><input type="radio" name="replace" checked={replace} disabled={!canReplace}
                                    onchange={() => (replace = true)}>Replace it</label>
        <label class="check"><input type="radio" name="replace" checked={!replace} onchange={() => (replace = false)}>Add alongside it</label>
        {#if !canReplace}<p class="hint">It is already marked {taken.status.toLowerCase()}, so this one goes alongside it.</p>{/if}
      </fieldset>
    {/if}

    <fieldset>
      <legend>How many</legend>
      <label class="check"><input type="radio" name="servings" checked={!choose} onchange={() => (choose = false)}>{forWhom}</label>
      <label class="check"><input type="radio" name="servings" checked={choose} onchange={() => (choose = true)}>Choose servings</label>
      {#if choose}
        <div class="stepper-row"><ServingsStepper servings={recipe.servings} serves={servings} multiples={false}
                                                  onchange={(count) => (servings = Math.max(1, Math.round(count)))} /></div>
      {/if}
    </fieldset>

    <div class="actions end">
      <button type="button" onclick={onclose}>Cancel</button>
      <button type="submit" class="primary with-icon" disabled={dated && !date}>
        <CalendarPlus size={16} />{taken && replace ? `Replace ${taken.title}` : `Add to ${where}`}
      </button>
    </div>
  </form>
</Modal>

<style>
  .add { display: grid; gap: 1rem; }
  fieldset { border: 0; margin: 0; padding: 0; min-width: 0; display: grid; gap: .4rem; }
  legend { padding: 0; margin-bottom: .4rem; font-weight: 600; font-size: .95rem; }
  .sub { color: inherit; opacity: .8; }
  .hint { margin: 0; font-size: .85rem; color: var(--muted); }
  .days { display: grid; grid-template-columns: repeat(auto-fill, minmax(3.4rem, 1fr)); gap: .35rem; }
  .day { display: grid; justify-items: center; gap: .05rem; padding: .35rem .2rem .3rem; border-radius: .6rem; line-height: 1.15; }
  .day.any { grid-column: 1 / -1; padding: .45rem; }
  .day .name { font-size: .75rem; color: var(--muted); }
  .day .number { font-weight: 700; font-size: 1.05rem; font-variant-numeric: tabular-nums; }
  .dot { width: .4rem; height: .4rem; border-radius: 50%; }
  .dot.on { background: var(--area-recipes); }
  .day[aria-pressed="true"] { background: var(--accent); border-color: var(--accent); color: var(--accent-ink); }
  .day[aria-pressed="true"] .name { color: inherit; }
  .day[aria-pressed="true"] .dot.on { background: var(--accent-ink); }
  .taken { padding: .7rem .8rem; border-radius: .5rem; background: var(--warn-bg); }
  .taken legend { float: left; width: 100%; font-weight: 500; }
  .stepper-row { padding-left: 1.6rem; }
  .end { justify-content: flex-end; }
</style>
