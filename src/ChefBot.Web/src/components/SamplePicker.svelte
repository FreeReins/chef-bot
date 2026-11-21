<!-- The sample recipes, one course at a time so the list stays short. Used by setup step three and by the
     sample page reached from the Recipes tab. -->
<script>
  import { app, run, loadRecipes } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { SLOT_TITLES } from '../lib/format.js';
  import Chip from './Chip.svelte';

  /** onadded(count) runs after the chosen samples are saved and `app.recipes` has been reloaded. */
  let { onadded } = $props();

  const COURSES = ['Main', 'Breakfast', 'Snack', 'Sweet'];

  const groups = $derived(COURSES
    .map((course) => ({ course, title: SLOT_TITLES[course] ?? 'Dinners', samples: app.samples.filter((sample) => sample.course === course) }))
    .filter((group) => group.samples.length > 0));
  const available = $derived(app.samples.filter((sample) => !sample.alreadyAdded));

  // Everything not yet added starts ticked.
  let chosen = $state(Object.fromEntries(app.samples.filter((sample) => !sample.alreadyAdded).map((sample) => [sample.name, true])));
  let course = $state('Main');

  const shown = $derived(groups.find((group) => group.course === course) ?? groups[0]);
  const picked = $derived(available.filter((sample) => chosen[sample.name]));
  const pickedIn = (group) => group.samples.filter((sample) => !sample.alreadyAdded && chosen[sample.name]).length;

  function setShown(checked) {
    for (const sample of shown.samples) if (!sample.alreadyAdded) chosen[sample.name] = checked;
  }

  function add() {
    const names = picked.map((sample) => sample.name);
    run(async () => {
      const result = await api('POST', '/api/setup/samples', { names });
      [app.samples] = await Promise.all([api('GET', '/api/setup/samples'), loadRecipes()]);
      await onadded(result.added);
    });
  }
</script>

{#if shown}
  <div class="row picker-head">
    <div class="segmented" role="tablist" aria-label="Courses">
      {#each groups as group (group.course)}
        <button type="button" role="tab" aria-selected={String(group.course === shown.course)} onclick={() => (course = group.course)}>
          {group.title} <span class="count">{pickedIn(group)}/{group.samples.length}</span>
        </button>
      {/each}
    </div>
    <div class="row-actions">
      <button type="button" class="small" onclick={() => setShown(true)}>All {shown.title.toLowerCase()}</button>
      <button type="button" class="small" onclick={() => setShown(false)}>None</button>
    </div>
  </div>

  <div class="sample-grid" role="tabpanel">
    {#each shown.samples as sample (sample.name)}
      <label class="sample" class:added={sample.alreadyAdded}>
        {#if sample.alreadyAdded}
          <input type="checkbox" checked disabled>
        {:else}
          <input type="checkbox" bind:checked={chosen[sample.name]}>
        {/if}
        <span class="sample-text">
          <strong>{sample.name}</strong>
          {#if sample.isSlowCooker}<Chip tone="good">Slow cooker</Chip>{/if}
          {#if sample.alreadyAdded}<Chip>Added</Chip>{/if}
          <span class="muted clip" title={sample.ingredients.join(', ')}>{sample.ingredients.join(', ')}</span>
        </span>
      </label>
    {/each}
  </div>

  <div class="row-actions" style="margin-top:.75rem">
    <button type="button" class="primary" disabled={picked.length === 0} onclick={add}>
      {available.length === 0 ? 'All samples added' : `Add ${picked.length} sample ${picked.length === 1 ? 'recipe' : 'recipes'}`}
    </button>
  </div>
{/if}

<style>
  /* Course tabs over a two-column list. */
  .picker-head { align-items: center; margin-bottom: .5rem; }
  .count { opacity: .75; font-size: .8em; }
  .sample-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(min(15rem, 100%), 1fr)); gap: 0 1.25rem; }
  .sample { display: flex; gap: .55rem; align-items: flex-start; padding: .4rem 0; border-top: 1px solid var(--line); color: var(--ink); font-size: .95rem; }
  .sample input { margin-top: .3rem; }
  .sample.added { opacity: .6; }
  .sample-text { min-width: 0; flex: 1; }
  .sample-text .clip { font-size: .8rem; }
</style>
