<!-- Setup step three: fill the recipe library by importing (files, Mealie) and/or from the samples.
     One choice is open at a time so the page stays short; the bar at the bottom always shows how to finish. -->
<script>
  import { app, run, enterApp } from '../lib/app.svelte.js';
  import { go } from '../lib/addresses.js';
  import Chip from '../components/Chip.svelte';
  import SetupProgress from '../components/SetupProgress.svelte';
  import SamplePicker from '../components/SamplePicker.svelte';
  import FileImport from '../components/FileImport.svelte';
  import MealieConnect from '../components/MealieConnect.svelte';

  let open = $state(null); // 'import' | 'samples' | null
  let fromFiles = $state(0);
  let samplesAdded = $state(0);

  const latest = $derived(app.mealieImport);
  const fromMealie = $derived(latest?.state === 'Succeeded' ? latest.result.added + latest.result.updated : 0);
  const imported = $derived(fromFiles + fromMealie);
  const count = $derived(app.recipes.length);
  const importing = $derived(latest?.state === 'Running');

  function toggle(choice, event) {
    event.preventDefault();
    open = open === choice ? null : choice;
  }

  function finish() {
    run(async () => {
      const total = count;
      const stillImporting = importing;
      await enterApp();
      await go(total || stillImporting ? '/recipes' : '/meals');
      app.notes = [stillImporting
        ? 'Setup is done. Your Mealie recipes are still importing and will appear on the Recipes tab when it finishes.'
        : total
          ? `Setup is done and you have ${total} recipes. Stage a week when you are ready.`
          : 'Setup is done. Add recipes on the Recipes tab, then stage a week.'];
    });
  }
</script>

<section class="gate wide">
  <SetupProgress current={3} />
  <h2>Add your recipes</h2>
  <p class="muted">Choose where your recipes come from. You can use both, and add more later on the Recipes tab.</p>

  <div class="choices">
    <details class="choice" open={open === 'import'}>
      <summary onclick={(event) => toggle('import', event)}>
        <span class="title">
          <strong>Import my recipes</strong>
          <span class="muted">From a recipe file or your Mealie server</span>
        </span>
        {#if imported}<Chip tone="good">{imported} imported</Chip>{/if}
      </summary>
      <div class="body two-up">
        <div class="source">
          <h3>From a file</h3>
          <FileImport bind:added={fromFiles} />
        </div>
        <div class="source">
          <h3>From Mealie</h3>
          <MealieConnect />
        </div>
      </div>
    </details>

    <details class="choice" open={open === 'samples'}>
      <summary onclick={(event) => toggle('samples', event)}>
        <span class="title">
          <strong>Start with sample recipes</strong>
          <span class="muted">{app.samples.length} ready-made recipes, so you can try planning straight away</span>
        </span>
        {#if samplesAdded}<Chip tone="good">{samplesAdded} added</Chip>{/if}
      </summary>
      <div class="body">
        <SamplePicker onadded={(added) => { samplesAdded += added; }} />
      </div>
    </details>
  </div>

  <div class="finish">
    <span class="muted">
      {#if importing}Importing from Mealie. You can finish now and it carries on in the background.
      {:else if count === 0}No recipes yet. You can add some later.
      {:else}{count} {count === 1 ? 'recipe' : 'recipes'} in your library{/if}
    </span>
    <button type="button" class={count || importing ? 'primary' : ''} onclick={finish}>
      {count || importing ? 'Finish setup' : 'Skip for now'}
    </button>
  </div>
</section>

<style>
  .choices { display: grid; gap: .6rem; margin-top: 1rem; }
  .choice { background: var(--panel); border: 1px solid var(--line); border-radius: .5rem; }
  .choice[open] { border-color: var(--accent); }
  summary { display: flex; gap: .75rem; align-items: center; justify-content: space-between; padding: .8rem 1rem; cursor: pointer; list-style: none; }
  summary::-webkit-details-marker { display: none; }
  summary::after { content: "▸"; color: var(--muted); transition: transform .15s; }
  .choice[open] summary::after { transform: rotate(90deg); }
  summary:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; border-radius: .5rem; }
  .title { display: grid; flex: 1; min-width: 0; }
  .body { padding: .8rem 1rem 1rem; border-top: 1px solid var(--line); }
  .two-up { display: grid; gap: 1.25rem; grid-template-columns: repeat(auto-fit, minmax(16rem, 1fr)); }
  .source { display: grid; gap: .5rem; align-content: start; }
  /* Stays in view above the page footer, so finishing never needs a scroll. */
  .finish { position: sticky; bottom: calc(3rem + env(safe-area-inset-bottom)); display: flex; gap: 1rem; align-items: center;
    justify-content: space-between; margin-top: 1rem; padding: .6rem 1rem; background: var(--panel); border: 1px solid var(--line);
    border-radius: .5rem; box-shadow: 0 2px 10px rgb(0 0 0 / .08); }
  @media (prefers-reduced-motion: reduce) { summary::after { transition: none; } }
</style>
