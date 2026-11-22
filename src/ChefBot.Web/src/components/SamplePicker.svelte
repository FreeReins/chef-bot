<!-- The sample recipes, one course at a time so the list stays short. While the chosen ones are being added, the
     picker waits and the button says how many are on their way; the ones just added then pop their Added chips in
     turn. Used by setup step three and by the sample page reached from the Recipes tab. -->
<script>
  import { tick } from 'svelte';
  import { app, run, loadRecipes } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { SLOT_TITLES } from '../lib/format.js';
  import { pendingClick } from '../lib/pending.js';
  import { celebrate } from '../lib/motion.js';
  import Chip from './Chip.svelte';

  /**
   * onadded(count) runs after the chosen samples are saved and `app.recipeCount` has been reloaded.
   * adding: true while they are being saved, for the parent to show.
   */
  let { onadded, adding = $bindable(false) } = $props();

  const COURSES = ['Main', 'Breakfast', 'Snack', 'Sweet', 'Drink'];

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

  // The course tabs work like any tab list: Tab reaches only the selected tab, and the arrow keys, Home and End
  // move between them, opening each on the way.
  let tablist = $state();
  async function moveTab(event) {
    const index = groups.findIndex((group) => group.course === shown.course);
    const next = { ArrowRight: index + 1, ArrowLeft: index - 1, Home: 0, End: groups.length - 1 }[event.key];
    if (next === undefined) return;
    event.preventDefault();
    course = groups[(next + groups.length) % groups.length].course;
    await tick();
    tablist.querySelector('[aria-selected="true"]')?.focus();
  }

  function setShown(checked) {
    for (const sample of shown.samples) if (!sample.alreadyAdded) chosen[sample.name] = checked;
  }

  let sending = $state(0);     // how many are being added, for the button
  let justAdded = $state([]); // the names added last time, whose Added chips pop

  const add = pendingClick((event) => {
    const button = event.currentTarget;
    const names = picked.map((sample) => sample.name);
    sending = names.length;
    adding = true;
    return run(async () => {
      const result = await api('POST', '/api/setup/samples', { names });
      [app.samples] = await Promise.all([api('GET', '/api/setup/samples'), loadRecipes()]);
      justAdded = names;
      if (result.added) celebrate(button);
      await onadded(result.added);
    }).finally(() => { sending = 0; adding = false; });
  });

  /** Where a just-added sample comes in its course, so the chips pop one after another. */
  const popDelay = (sample) => `${Math.min(shown.samples.filter((other) => justAdded.includes(other.name)).indexOf(sample), 15) * 45}ms`;
</script>

{#if shown}
  <div class="row picker-head">
    <div class="segmented" role="tablist" aria-label="Courses" tabindex="-1" bind:this={tablist} onkeydown={moveTab}>
      {#each groups as group (group.course)}
        {@const selected = group.course === shown.course}
        <button type="button" role="tab" id="samples-tab-{group.course}" aria-controls="samples-panel"
                aria-selected={String(selected)} tabindex={selected ? 0 : -1} onclick={() => (course = group.course)}>
          {group.title} <span class="count">{pickedIn(group)}/{group.samples.length}</span>
        </button>
      {/each}
    </div>
    <div class="row-actions">
      <button type="button" class="small" disabled={sending > 0} onclick={() => setShown(true)}>All {shown.title.toLowerCase()}</button>
      <button type="button" class="small" disabled={sending > 0} onclick={() => setShown(false)}>None</button>
    </div>
  </div>

  <div class="sample-grid" class:waiting={sending > 0} role="tabpanel" id="samples-panel" aria-labelledby="samples-tab-{shown.course}"
       aria-busy={sending > 0 ? 'true' : undefined}>
    {#each shown.samples as sample (sample.name)}
      <label class="sample" class:added={sample.alreadyAdded}>
        {#if sample.alreadyAdded}
          <input type="checkbox" checked disabled>
        {:else}
          <input type="checkbox" bind:checked={chosen[sample.name]} disabled={sending > 0}>
        {/if}
        <span class="sample-text">
          <strong>{sample.name}</strong>
          {#if sample.isSlowCooker}<Chip tone="good">Slow cooker</Chip>{/if}
          {#if sample.alreadyAdded}
            <span class="added-chip" class:fresh={justAdded.includes(sample.name)} style:--delay={popDelay(sample)}><Chip tone={justAdded.includes(sample.name) ? 'good' : ''}>Added</Chip></span>
          {/if}
          <span class="muted clip" title={sample.ingredients.join(', ')}>{sample.ingredients.join(', ')}</span>
        </span>
      </label>
    {/each}
  </div>

  <div class="row-actions" style="margin-top:.75rem">
    <button type="button" class="primary" disabled={picked.length === 0 && !sending} onclick={add}>
      {#if sending}Adding {sending} {sending === 1 ? 'recipe' : 'recipes'}…
      {:else if available.length === 0}All samples added
      {:else}Add {picked.length} sample {picked.length === 1 ? 'recipe' : 'recipes'}{/if}
    </button>
  </div>
{/if}

<style>
  /* Course tabs over a two-column list. */
  .picker-head { align-items: center; margin-bottom: .5rem; }
  .count { font-size: .8em; }
  .sample-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(min(15rem, 100%), 1fr)); gap: 0 1.25rem; }
  .sample { display: flex; gap: .55rem; align-items: flex-start; padding: .4rem 0; border-top: 1px solid var(--line); color: var(--ink); font-size: .95rem; }
  .sample input { margin-top: .3rem; }
  .sample.added { color: var(--muted); }
  .sample-text { min-width: 0; flex: 1; }
  .sample-text .clip { font-size: .8rem; }
  /* While the chosen samples are being added, the list rests a little, so the button is where the eye goes. */
  .sample-grid { transition: opacity .2s; }
  .sample-grid.waiting { opacity: .6; }
  .added-chip { display: inline-block; }
  @media (prefers-reduced-motion: no-preference) {
    .added-chip.fresh { animation: pop .4s cubic-bezier(.3, 1.6, .5, 1) var(--delay) backwards; }
  }
</style>
