<!-- Setup step three: fill the recipe library by importing (files, Mealie) and/or from the samples.
     One choice is open at a time so the page stays short; the bar at the bottom always shows how to finish. -->
<script>
  import { app, run, enterApp, importFile, importFromMealie, saveMealie, describeMealieImport } from '../lib/app.svelte.js';
  import Chip from '../components/Chip.svelte';
  import SetupProgress from '../components/SetupProgress.svelte';
  import SamplePicker from '../components/SamplePicker.svelte';

  let open = $state(null); // 'import' | 'samples' | null
  let files = $state([]);  // one row per file chosen: { name, busy, result?, error? }
  let dragging = $state(false);
  let editingMealie = $state(false);
  let samplesAdded = $state(0);

  const mealie = $derived(app.mealie);
  const mealieRun = $derived(app.mealieImport);
  const connected = $derived(mealie?.isConfigured && !editingMealie);
  const accept = $derived(app.fileFormats.flatMap((format) => format.extensions).join(','));
  const importedFromFiles = $derived(files.reduce((total, row) => total + (row.result?.added ?? 0), 0));
  const importedFromMealie = $derived(mealieRun?.state === 'Succeeded' ? mealieRun.result.added + mealieRun.result.updated : 0);
  const imported = $derived(importedFromFiles + importedFromMealie);

  function toggle(choice, event) {
    event.preventDefault();
    open = open === choice ? null : choice;
  }

  async function importFiles(list) {
    for (const file of list) {
      const row = { name: file.name, busy: true };
      files.push(row);
      const index = files.length - 1;
      try {
        files[index].result = await importFile(file);
      } catch (error) {
        files[index].error = error.message;
      }
      files[index].busy = false;
    }
  }

  function chooseFiles(event) {
    importFiles([...event.currentTarget.files]);
    event.currentTarget.value = ''; // so the same file can be chosen again
  }

  function drop(event) {
    event.preventDefault();
    dragging = false;
    importFiles([...event.dataTransfer.files]);
  }

  function describeFile({ added, alreadyInLibrary, skipped }) {
    const parts = [`${added} added`];
    if (alreadyInLibrary) parts.push(`${alreadyInLibrary} already in your recipes`);
    if (skipped) parts.push(`${skipped} skipped (no name or ingredients)`);
    return parts.join(', ');
  }

  function connectMealie(event) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    run(async () => {
      await saveMealie(data.get('url'), data.get('apiToken'));
      if (!app.mealie.isConfigured) {
        app.error = 'Mealie needs both an address and an API token. Fill in both, then connect again.';
        return;
      }
      editingMealie = false;
      await importFromMealie();
    });
  }

  function finish() {
    run(async () => {
      const count = app.recipes.length;
      await enterApp();
      app.tab = count ? 'recipes' : 'meals';
      app.notes = [count
        ? `Setup is done and you have ${count} recipes. Stage a week when you are ready.`
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
        <span class="choice-title">
          <strong>Import my recipes</strong>
          <span class="muted">From a recipe file or your Mealie server</span>
        </span>
        {#if imported}<Chip tone="good">{imported} imported</Chip>{/if}
      </summary>

      <div class="choice-body two-up">
        <div class="source">
          <h3>From a file</h3>
          <p class="muted">
            {app.fileFormats.map((format) => `${format.name} (${format.extensions.join(', ')})`).join('; ')}.
            Import as many files as you like. Recipes you already have are skipped.
          </p>
          <label class="drop" class:dragging
                 ondragover={(event) => { event.preventDefault(); dragging = true; }}
                 ondragleave={() => (dragging = false)} ondrop={drop}>
            <input type="file" multiple {accept} onchange={chooseFiles}>
            <span><strong>Choose files</strong> or drop them here</span>
          </label>
          {#if files.length}
            <ul class="plain file-results" aria-live="polite">
              {#each files as row, index (index)}
                <li>
                  <span class="clip">{row.name}</span>
                  {#if row.busy}<span class="muted">Importing…</span>
                  {:else if row.error}<span class="bad-text">{row.error}</span>
                  {:else}<span class="muted">{describeFile(row.result)}</span>{/if}
                </li>
              {/each}
            </ul>
          {/if}
        </div>

        <div class="source">
          <h3>From Mealie</h3>
          {#if connected}
            <p class="muted"><Chip tone="good">Connected</Chip> <span class="clip">{mealie.url}</span>
              <button type="button" class="link" onclick={() => (editingMealie = true)}>Change</button></p>
            {#if mealieRun?.state === 'Running'}
              <p class="muted" aria-live="polite"><span class="spinner"></span> Importing from Mealie…</p>
            {:else if mealieRun?.state === 'Succeeded'}
              <p class="muted" aria-live="polite">{describeMealieImport(mealieRun.result)}</p>
            {:else if mealieRun?.state === 'Failed'}
              <p class="bad-text" role="alert">{mealieRun.message}</p>
            {/if}
            <div class="row-actions">
              <button type="button" class={mealieRun?.state === 'Succeeded' ? '' : 'primary'} disabled={mealieRun?.state === 'Running'}
                      onclick={importFromMealie}>
                {mealieRun?.state === 'Succeeded' ? 'Import again' : 'Import from Mealie'}
              </button>
            </div>
          {:else}
            <p class="muted">Connect once and ChefBot copies your recipes, so planning keeps working when Mealie is down.</p>
            <form class="stack" onsubmit={connectMealie}>
              <label>Mealie address
                <input name="url" type="url" required value={mealie?.url ?? ''} placeholder="http://mealie.local:9000">
              </label>
              <label>API token (in Mealie: your profile, then API Tokens)
                <input name="apiToken" type="password" autocomplete="off" required={!mealie?.hasApiToken}
                       placeholder={mealie?.hasApiToken ? 'Saved. Leave empty to keep it.' : 'Paste a token from Mealie'}>
              </label>
              <div class="row-actions">
                <button type="submit" class="primary">Connect and import</button>
                {#if editingMealie}<button type="button" onclick={() => (editingMealie = false)}>Cancel</button>{/if}
              </div>
            </form>
          {/if}
        </div>
      </div>
    </details>

    <details class="choice" open={open === 'samples'}>
      <summary onclick={(event) => toggle('samples', event)}>
        <span class="choice-title">
          <strong>Start with sample recipes</strong>
          <span class="muted">{app.samples.length} ready-made recipes, so you can try planning straight away</span>
        </span>
        {#if samplesAdded}<Chip tone="good">{samplesAdded} added</Chip>{/if}
      </summary>
      <div class="choice-body">
        <SamplePicker onadded={(added) => { samplesAdded += added; }} />
      </div>
    </details>
  </div>

  <div class="setup-bar">
    <span class="muted">
      {app.recipes.length === 0 ? 'No recipes yet. You can add some later.' : `${app.recipes.length} ${app.recipes.length === 1 ? 'recipe' : 'recipes'} in your library`}
    </span>
    <button type="button" class={app.recipes.length ? 'primary' : ''} onclick={finish}>
      {app.recipes.length ? 'Finish setup' : 'Skip for now'}
    </button>
  </div>
</section>
