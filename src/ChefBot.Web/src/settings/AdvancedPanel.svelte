<!-- Settings, Advanced (admins only): how plans weigh repeats, expiring food and perishables, and the words that sort
     imported recipes into courses. Changes save by themselves; each group can go back to ChefBot's own values. -->
<script>
  import { run } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { autosave } from '../lib/autosave.svelte.js';
  import SaveStatus from '../components/SaveStatus.svelte';
  import WordList from '../components/WordList.svelte';

  /** The course lists in the order they are tried, with what each one does. */
  const COURSES = [
    ['breakfast', 'Breakfast', 'Labels that make a recipe a breakfast.'],
    ['snack', 'Snack', 'Labels that make a recipe a snack.'],
    ['sweet', 'Sweet', 'Labels that make a recipe a sweet or dessert.'],
    ['drink', 'Drink', 'Labels that make a recipe a drink.'],
    ['notAMeal', 'Not a meal', 'Sides, sauces and the like, which are never planned on their own.'],
    ['main', 'Main', 'Empty: every recipe that isn\'t anything else is a main. Otherwise only recipes with one of these are.'],
    ['slowCooker', 'Slow cooker', 'Labels, or words in the name, that mark a recipe as made in a slow cooker.'],
  ];

  let values = $state(null);
  let defaults = $state(null);
  let form = $state();

  run(async () => show(await api('GET', '/api/system/advanced')));

  function show(response) {
    values = structuredClone(response.current);
    defaults = response.defaults;
  }

  const saver = autosave(async () => {
    const sent = $state.snapshot(values);
    defaults = (await api('PUT', '/api/system/advanced', sent)).defaults;
  }, { validate: () => !form || form.checkValidity() });

  function submit(event) {
    event.preventDefault();
    saver.retry();
  }

  function resetPlanning() {
    values.avoidRepeatDays = defaults.avoidRepeatDays;
    values.expiringSoonDays = defaults.expiringSoonDays;
    values.perishableWithinDays = defaults.perishableWithinDays;
    saver.schedule();
  }

  function resetWords() {
    values.words = structuredClone($state.snapshot(defaults.words));
    saver.schedule();
  }

  const sameWords = $derived(values && defaults && JSON.stringify(values.words) === JSON.stringify(defaults.words));
  const samePlanning = $derived(values && defaults && ['avoidRepeatDays', 'expiringSoonDays', 'perishableWithinDays']
    .every((key) => values[key] === defaults[key]));
</script>

<h2>Advanced</h2>
<p class="muted">
  These change how ChefBot chooses recipes for a plan and sorts the recipes you import. ChefBot's own values suit most
  homes; each group can go back to them.
</p>

{#if values}
  <h3>Choosing recipes</h3>
  <form class="card stack" bind:this={form} onchange={saver.schedule} onsubmit={submit}>
    <label>Don't repeat a recipe planned within (days)
      <input id="advanced-repeat" type="number" min="0" max="365" step="1" required bind:value={values.avoidRepeatDays}
             aria-describedby="advanced-repeat-about">
    </label>
    <p id="advanced-repeat-about" class="muted note">0 lets a recipe come back any week. Default {defaults.avoidRepeatDays}.</p>
    <label>Plan pantry food first when it expires within (days)
      <input id="advanced-expiring" type="number" min="0" max="30" step="1" required bind:value={values.expiringSoonDays}
             aria-describedby="advanced-expiring-about">
    </label>
    <p id="advanced-expiring-about" class="muted note">
      Also when food is marked as expiring soon in the pantry and reminders. Default {defaults.expiringSoonDays}.
    </p>
    <label>Share ingredients between meals when they keep for (days or less)
      <input id="advanced-perishable" type="number" min="1" max="365" step="1" required bind:value={values.perishableWithinDays}
             aria-describedby="advanced-perishable-about">
    </label>
    <p id="advanced-perishable-about" class="muted note">
      Plans prefer recipes that use up the same fresh food, so less is wasted. Default {defaults.perishableWithinDays}.
    </p>
    {#if !samePlanning}
      <div class="row-actions"><button type="button" class="small" onclick={resetPlanning}>Reset to defaults</button></div>
    {/if}
  </form>

  <h3>Sorting imported recipes</h3>
  <div class="card stack">
    <p class="muted">
      An imported recipe's tags, categories and tools are matched as whole words against these lists, in this order;
      the first that matches gives its course. New words apply to recipes imported from now on. A recipe's course can
      always be changed on the recipe itself.
    </p>
    {#each COURSES as [key, title, about] (key)}
      <div class="course">
        <h4 id="advanced-{key}-title">{title}</h4>
        <p class="muted note">{about}</p>
        <WordList bind:words={values.words[key]} label="{title} words" id="advanced-{key}" onchange={saver.schedule} />
      </div>
    {/each}
    {#if !sameWords}
      <div class="row-actions"><button type="button" class="small" onclick={resetWords}>Reset to defaults</button></div>
    {/if}
  </div>

  <SaveStatus {saver} invalid="Not saved yet: keep each number in its range." />
{/if}

<style>
  .note { margin: -.25rem 0 .25rem; font-size: .85rem; }
  input[type='number'] { max-width: 6rem; }
  .course { display: grid; gap: .25rem; padding-top: .5rem; border-top: 1px solid var(--line); }
  .course h4 { margin: 0; font-size: .95rem; }
  .course .note { margin: 0; }
</style>
