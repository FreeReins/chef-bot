<!-- Importing recipe files: a drop zone that takes any number of files, as often as you like, with a line per file
     saying what happened. Files are sent one at a time; more can be dropped meanwhile and wait their turn. While any
     are being sent, `app.importing` is above 0, so setup won't move on, and leaving the page asks first.
     Used by setup step three and the Recipes tab. -->
<script>
  import { app, run, importFile, loadFileFormats } from '../lib/app.svelte.js';
  import { rise } from '../lib/motion.js';
  import LoaderCircle from '@lucide/svelte/icons/loader-circle';
  import Check from '@lucide/svelte/icons/check';
  import CircleAlert from '@lucide/svelte/icons/circle-alert';
  import Clock from '@lucide/svelte/icons/clock';
  import FileUp from '@lucide/svelte/icons/file-up';

  /** added: how many recipes the files chosen here have added, for the parent to show. cookbookId: a cookbook to put them in. */
  let { added = $bindable(0), cookbookId = null } = $props();

  let files = $state([]); // one row per file chosen: { name, status: 'waiting' | 'importing' | 'done' | 'failed', result?, error? }
  let dragging = $state(false);
  let sending = false;    // whether the loop below is running
  const queue = [];       // [row, File] waiting to be sent

  const accept = $derived([...new Set(app.fileFormats.flatMap((format) => format.extensions))].join(','));
  const open = $derived(files.filter((row) => row.status === 'waiting' || row.status === 'importing'));
  // "2 of 3" counts the files in this batch: since the last time nothing was waiting.
  let batchStart = $state(0);
  const batch = $derived(files.length - batchStart);
  const current = $derived(files.findIndex((row) => row.status === 'importing') - batchStart + 1);

  run(loadFileFormats);

  // Leaving the page would stop the upload half way, so the browser asks first.
  $effect(() => {
    if (open.length === 0) return undefined;
    const warn = (event) => event.preventDefault();
    window.addEventListener('beforeunload', warn);
    return () => window.removeEventListener('beforeunload', warn);
  });

  function importFiles(list) {
    if (list.length === 0) return;
    if (open.length === 0) batchStart = files.length;
    for (const file of list) {
      files.push({ name: file.name, status: 'waiting' });
      queue.push([files[files.length - 1], file]);
      app.importing++;
    }
    if (!sending) sendAll();
  }

  async function sendAll() {
    sending = true;
    while (queue.length) {
      const [row, file] = queue.shift();
      row.status = 'importing';
      try {
        row.result = await importFile(file, cookbookId);
        added += row.result.added;
        row.status = 'done';
      } catch (error) {
        row.error = error.message;
        row.status = 'failed';
      } finally {
        app.importing--;
      }
    }
    sending = false;
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

  function describe({ added, alreadyInLibrary, skipped, photosAdded }) {
    const parts = [`${added} added`];
    if (photosAdded) parts.push(`${photosAdded} ${photosAdded === 1 ? 'photo' : 'photos'} added`);
    if (alreadyInLibrary) parts.push(`${alreadyInLibrary} already in your recipes`);
    if (skipped) parts.push(`${skipped} skipped (no name or ingredients)`);
    return parts.join(', ');
  }
</script>

<p class="muted">
  {app.fileFormats.map((format) => `${format.name} (${format.extensions.join(', ')})`).join('; ')}.
  Import as many files as you like. Recipes you already have are skipped.
</p>
<label class="drop" class:dragging class:busy={open.length > 0}
       ondragover={(event) => { event.preventDefault(); dragging = true; }}
       ondragleave={() => (dragging = false)} ondrop={drop}>
  <input type="file" multiple {accept} onchange={choose} aria-describedby="import-progress">
  <span class="drop-icon" aria-hidden="true">
    {#if open.length}<LoaderCircle size={22} />{:else}<FileUp size={22} />{/if}
  </span>
  <span id="import-progress">
    {#if open.length}
      <strong>Importing {batch > 1 ? `${Math.max(current, 1)} of ${batch}` : 'your file'}…</strong>
      <span class="muted">Stay on this page until it's done. You can drop more files meanwhile.</span>
    {:else}
      <strong>Choose files</strong> or drop them here
    {/if}
  </span>
</label>
{#if files.length}
  <ul class="plain results" aria-live="polite">
    {#each files as row, index (index)}
      <li class={row.status} in:rise>
        <span class="state" aria-hidden="true">
          {#if row.status === 'waiting'}<Clock size={16} />
          {:else if row.status === 'importing'}<LoaderCircle size={16} class="spin" />
          {:else if row.status === 'failed'}<CircleAlert size={16} />
          {:else}<Check size={16} />{/if}
        </span>
        <span class="what">
          <span class="clip" title={row.name}>{row.name}</span>
          {#if row.status === 'waiting'}<span class="muted">Waiting its turn</span>
          {:else if row.status === 'importing'}<span class="muted">Importing…</span>
          {:else if row.status === 'failed'}<span class="bad-text">{row.error}</span>
          {:else}<span class="muted">{describe(row.result)}</span>{/if}
        </span>
      </li>
    {/each}
  </ul>
{/if}

<style>
  p { margin: 0; }
  .drop { position: relative; display: flex; gap: .75rem; align-items: center; justify-content: center; min-height: 4.5rem;
    border: 2px dashed var(--line); border-radius: .5rem; color: var(--muted); text-align: center; padding: .75rem; cursor: pointer;
    transition: border-color .15s, background-color .15s, transform .15s; }
  .drop > span:last-child { display: grid; }
  .drop:hover, .drop.dragging, .drop:focus-within { border-color: var(--accent); color: var(--ink); }
  .drop.dragging { background: var(--accent-bg); transform: scale(1.01); }
  .drop.busy { border-style: solid; border-color: var(--accent); color: var(--ink); background: color-mix(in srgb, var(--accent-bg) 50%, transparent); }
  .drop input { position: absolute; inset: 0; opacity: 0; cursor: pointer; }
  .drop-icon { flex: none; display: inline-grid; place-items: center; width: 2.4rem; height: 2.4rem; border-radius: 50%;
    color: var(--accent); background: var(--accent-bg); }
  .results li { display: flex; gap: .55rem; align-items: flex-start; font-size: .9rem; }
  .what { display: grid; min-width: 0; flex: 1; }
  .state { flex: none; display: inline-flex; margin-top: .15rem; color: var(--muted); }
  .done .state { color: var(--good); }
  .failed .state { color: var(--bad); }
  @media (prefers-reduced-motion: no-preference) {
    .drop.busy .drop-icon :global(svg), .state :global(.spin) { animation: spin .8s linear infinite; }
    .drop.dragging .drop-icon { animation: hop .5s ease-in-out infinite alternate; }
    .done .state, .failed .state { animation: pop .35s cubic-bezier(.3, 1.6, .5, 1); }
    li.done { animation: flash 1.2s ease-out; }
  }
  @keyframes hop { to { transform: translateY(-3px); } }
  @keyframes flash { from { background: var(--good-bg); } }
</style>
