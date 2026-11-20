<!-- The sample recipe picker: step three of setup, and reachable later from the Recipes tab. -->
<script>
  import { app, run, enterApp } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { SLOT_TITLES } from '../lib/format.js';
  import Chip from '../components/Chip.svelte';

  const COURSES = ['Main', 'Breakfast', 'Snack', 'Sweet'];

  const firstRun = app.recipes.length === 0 && !app.plan;
  const available = $derived(app.samples.filter((sample) => !sample.alreadyAdded));
  const groups = $derived(COURSES
    .map((course) => ({ course, samples: app.samples.filter((sample) => sample.course === course) }))
    .filter((group) => group.samples.length > 0));

  // Everything not yet added starts ticked.
  let chosen = $state(Object.fromEntries(app.samples.filter((sample) => !sample.alreadyAdded).map((sample) => [sample.name, true])));

  function setAll(checked) {
    for (const sample of available) chosen[sample.name] = checked;
  }

  function onsubmit(event) {
    event.preventDefault();
    const names = available.filter((sample) => chosen[sample.name]).map((sample) => sample.name);
    run(async () => {
      const result = names.length ? await api('POST', '/api/setup/samples', { names }) : { added: 0 };
      await enterApp();
      app.tab = 'recipes';
      app.notes = [result.added ? `Added ${result.added} recipes. Stage a week when you are ready.` : 'No sample recipes were added.'];
    });
  }
</script>

<section class="gate wide">
  {#if firstRun}<p class="steps">Setup, step 3 of 3</p>{/if}
  <h2>Choose starter recipes</h2>
  <p class="muted">
    These sample recipes let you try planning straight away. Tick the ones you want, or skip them and add your own.
    {app.session.mealieConfigured ? 'You can also import your recipes from Mealie on the Recipes tab.' : ''}
  </p>
  <form id="samples" {onsubmit}>
    <div class="row-actions" style="margin:.75rem 0">
      <button type="button" class="small" onclick={() => setAll(true)}>Select all</button>
      <button type="button" class="small" onclick={() => setAll(false)}>Select none</button>
    </div>
    {#each groups as { course, samples } (course)}
      <div class="card">
        <h3>{SLOT_TITLES[course] ?? 'Dinners'} ({samples.length})</h3>
        {#each samples as sample, index (sample.name)}
          <label class="check">
            {#if sample.alreadyAdded}
              <input id="sample-{course}-{index}" type="checkbox" disabled>
            {:else}
              <input id="sample-{course}-{index}" type="checkbox" bind:checked={chosen[sample.name]}>
            {/if}
            <span>
              <strong>{sample.name}</strong>
              {#if sample.isSlowCooker}<Chip tone="good">Slow cooker</Chip>{/if}
              {#if sample.alreadyAdded}<Chip>Already in your recipes</Chip>{/if}
              <span class="muted" style="display:block">{sample.ingredients.join(', ')}</span>
            </span>
          </label>
        {/each}
      </div>
    {/each}
    <div class="row-actions" style="margin-top:1rem">
      <button type="submit" class="primary" disabled={available.length === 0}>Add selected recipes</button>
      <button type="button" onclick={() => run(enterApp)}>{firstRun ? 'Skip for now' : 'Back'}</button>
    </div>
  </form>
</section>
