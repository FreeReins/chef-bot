<!-- Importing recipe files: a drop zone that takes any number of files, as often as you like, with a line per file
     saying what happened. Used by setup step three and the Recipes tab. -->
<script>
  import { app, run, importFile, loadFileFormats } from '../lib/app.svelte.js';

  /** added: how many recipes the files chosen here have added, for the parent to show. */
  let { added = $bindable(0) } = $props();

  let files = $state([]); // one row per file chosen: { name, busy, result?, error? }
  let dragging = $state(false);

  const accept = $derived([...new Set(app.fileFormats.flatMap((format) => format.extensions))].join(','));

  run(loadFileFormats);

  async function importFiles(list) {
    for (const file of list) {
      files.push({ name: file.name, busy: true });
      const row = files[files.length - 1];
      try {
        row.result = await importFile(file);
        added += row.result.added;
      } catch (error) {
        row.error = error.message;
      }
      row.busy = false;
    }
  }

  function choose(event) {
    importFiles([...event.currentTarget.files]);
    event.currentTarget.value = ''; // so the same file can be chosen again
  }

  function drop(event) {
    event.preventDefault();
    dragging = false;
    importFiles([...event.dataTransfer.files]);
  }

  function describe({ added, alreadyInLibrary, skipped }) {
    const parts = [`${added} added`];
    if (alreadyInLibrary) parts.push(`${alreadyInLibrary} already in your recipes`);
    if (skipped) parts.push(`${skipped} skipped (no name or ingredients)`);
    return parts.join(', ');
  }
</script>

<p class="muted">
  {app.fileFormats.map((format) => `${format.name} (${format.extensions.join(', ')})`).join('; ')}.
  Import as many files as you like. Recipes you already have are skipped.
</p>
<label class="drop" class:dragging
       ondragover={(event) => { event.preventDefault(); dragging = true; }}
       ondragleave={() => (dragging = false)} ondrop={drop}>
  <input type="file" multiple {accept} onchange={choose}>
  <span><strong>Choose files</strong> or drop them here</span>
</label>
{#if files.length}
  <ul class="plain results" aria-live="polite">
    {#each files as row, index (index)}
      <li>
        <span class="clip">{row.name}</span>
        {#if row.busy}<span class="muted">Importing…</span>
        {:else if row.error}<span class="bad-text">{row.error}</span>
        {:else}<span class="muted">{describe(row.result)}</span>{/if}
      </li>
    {/each}
  </ul>
{/if}

<style>
  p { margin: 0; }
  .drop { position: relative; display: grid; place-items: center; min-height: 4.5rem; border: 2px dashed var(--line); border-radius: .5rem;
    color: var(--muted); text-align: center; padding: .75rem; cursor: pointer; }
  .drop:hover, .drop.dragging, .drop:focus-within { border-color: var(--accent); color: var(--ink); }
  .drop input { position: absolute; inset: 0; opacity: 0; cursor: pointer; }
  .results li { display: grid; font-size: .9rem; }
</style>
