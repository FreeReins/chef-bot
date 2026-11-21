<!-- Setup step three: fill the recipe library by importing recipe files and/or from the samples.
     One choice is open at a time so the page stays short; the bar at the bottom always shows how to go on. -->
<script>
  import { app } from '../lib/app.svelte.js';
  import Chip from '../components/Chip.svelte';
  import ChevronRight from '@lucide/svelte/icons/chevron-right';
  import SetupNav from '../components/SetupNav.svelte';
  import SetupProgress from '../components/SetupProgress.svelte';
  import SamplePicker from '../components/SamplePicker.svelte';
  import FileImport from '../components/FileImport.svelte';

  let open = $state(null); // 'import' | 'samples' | null
  let fromFiles = $state(0);
  let samplesAdded = $state(0);

  const count = $derived(app.recipeCount);

  function toggle(choice, event) {
    event.preventDefault();
    open = open === choice ? null : choice;
  }
</script>

<section class="gate wide">
  <SetupProgress />
  <h2>Add your recipes</h2>
  <p class="muted">Choose where your recipes come from. You can use both, and add more later on the Recipes tab.</p>

  <div class="choices">
    <details class="choice" open={open === 'import'}>
      <summary onclick={(event) => toggle('import', event)}>
        <span class="title">
          <strong>Import my recipes</strong>
          <span class="muted">From a recipe file, such as a Mealie backup .zip</span>
        </span>
        {#if fromFiles}<Chip tone="good">{fromFiles} imported</Chip>{/if}
        <ChevronRight class="chevron" size={18} />
      </summary>
      <div class="body">
        <FileImport bind:added={fromFiles} />
      </div>
    </details>

    <details class="choice" open={open === 'samples'}>
      <summary onclick={(event) => toggle('samples', event)}>
        <span class="title">
          <strong>Start with sample recipes</strong>
          <span class="muted">{app.samples.length} ready-made recipes, so you can try planning straight away</span>
        </span>
        {#if samplesAdded}<Chip tone="good">{samplesAdded} added</Chip>{/if}
        <ChevronRight class="chevron" size={18} />
      </summary>
      <div class="body">
        <SamplePicker onadded={(added) => { samplesAdded += added; }} />
      </div>
    </details>
  </div>

  <SetupNav label={count ? 'Next: stores' : 'Skip for now'} primary={Boolean(count)}>
    {#if count === 0}No recipes yet. You can add some later.
    {:else}{count} {count === 1 ? 'recipe' : 'recipes'} in your library{/if}
  </SetupNav>
</section>

<style>
  .choices { display: grid; gap: .6rem; margin-top: 1rem; }
  .choice { background: var(--panel); border: 1px solid var(--line); border-radius: .5rem; }
  .choice[open] { border-color: var(--accent); }
  summary { display: flex; gap: .75rem; align-items: center; justify-content: space-between; padding: .8rem 1rem; cursor: pointer; list-style: none; }
  summary::-webkit-details-marker { display: none; }
  summary :global(.chevron) { flex: none; color: var(--muted); transition: transform .15s; }
  .choice[open] summary :global(.chevron) { transform: rotate(90deg); }
  summary:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; border-radius: .5rem; }
  .title { display: grid; flex: 1; min-width: 0; }
  .body { padding: .8rem 1rem 1rem; border-top: 1px solid var(--line); }
  @media (prefers-reduced-motion: reduce) { summary :global(.chevron) { transition: none; } }
</style>
