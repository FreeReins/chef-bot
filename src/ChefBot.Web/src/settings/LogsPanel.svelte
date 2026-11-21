<!-- The server's recent log, newest first, a page at a time. New entries are fetched every few seconds while the panel
     is open. Long messages are cut to a few lines until opened. -->
<script>
  import { run } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import Chip from '../components/Chip.svelte';
  import LoaderCircle from '@lucide/svelte/icons/loader-circle';

  const REFRESH_MS = 5000;
  const PAGE = 100;
  /** A message longer than this many lines or characters starts collapsed. */
  const LONG_LINES = 4;
  const LONG_CHARS = 400;
  const LEVELS = { Trace: 0, Debug: 1, Information: 2, Warning: 3, Error: 4, Critical: 5 };
  const TONES = { Warning: 'warn', Error: 'bad', Critical: 'bad' };

  let entries = $state([]);
  let capacity = $state(1000);
  let loaded = $state(false);
  let auto = $state(true);
  let minimum = $state('0');
  let search = $state('');
  let limit = $state(PAGE);
  let expanded = $state({});

  const shown = $derived.by(() => {
    const words = search.trim().toLowerCase();
    return entries
      .filter((entry) => (LEVELS[entry.level] ?? 0) >= Number(minimum))
      .filter((entry) => !words || `${entry.category} ${entry.message}`.toLowerCase().includes(words))
      .reverse();
  });
  const page = $derived(shown.slice(0, limit));

  const isLong = (message) => message.length > LONG_CHARS || message.split('\n').length > LONG_LINES;
  const toggle = (sequence) => { expanded[sequence] = !expanded[sequence]; };
  // A new filter or search starts again from the newest page.
  const restart = () => { limit = PAGE; };

  /** Fetches the entries newer than the last one shown. A failure stops auto-refresh, so its message stays up. */
  const refresh = () => run(async () => {
    try {
      const after = entries.at(-1)?.sequence ?? 0;
      const logs = await api('GET', `/api/system/logs?after=${after}`);
      capacity = logs.capacity;
      if (logs.entries.length) entries = [...entries, ...logs.entries].slice(-capacity);
      loaded = true;
    } catch (error) {
      auto = false;
      throw error;
    }
  });

  refresh();

  $effect(() => {
    if (!auto) return;
    const timer = setInterval(refresh, REFRESH_MS);
    return () => clearInterval(timer);
  });

  const time = (value) => new Date(value).toLocaleString(undefined, { dateStyle: 'short', timeStyle: 'medium' });
</script>

<h2>Logs</h2>
<p class="muted">The last {capacity} entries since ChefBot started. Older ones are in <code>journalctl --user -u chefbot</code>.</p>

<div class="card filters">
  <label>Show
    <select id="log-level" bind:value={minimum} onchange={restart}>
      <option value="0">Everything</option>
      <option value="3">Warnings and errors</option>
      <option value="4">Errors only</option>
    </select>
  </label>
  <label class="grow">Search <input id="log-search" type="search" bind:value={search} oninput={restart} placeholder="Text or category"></label>
  <label class="inline"><input id="log-auto" type="checkbox" bind:checked={auto}> Update every {REFRESH_MS / 1000} seconds</label>
  {#if !auto}<button type="button" class="small" onclick={refresh}>Refresh</button>{/if}
</div>

<div class="card">
  {#if !loaded}
    <p class="loading muted" role="status" aria-label="Loading the log"><LoaderCircle size={24} /></p>
  {:else if entries.length === 0}
    <p class="muted">No log entries yet. Entries since ChefBot last started show here.</p>
  {:else if shown.length === 0}
    <p class="muted">No entries match. Show everything or change the search.</p>
  {/if}
  <ul class="plain log">
    {#each page as entry (entry.sequence)}
      <li>
        <div class="meta">
          <Chip tone={TONES[entry.level] ?? ''}>{entry.level}</Chip>
          <time datetime={entry.time}>{time(entry.time)}</time>
          <span class="muted">{entry.category}</span>
        </div>
        {#if isLong(entry.message)}
          <div class="message" class:clamped={!expanded[entry.sequence]}>{entry.message}</div>
          <button type="button" class="link" aria-expanded={!!expanded[entry.sequence]} onclick={() => toggle(entry.sequence)}>
            {expanded[entry.sequence] ? 'Show less' : 'Show all'}
          </button>
        {:else}
          <div class="message">{entry.message}</div>
        {/if}
        {#if entry.exception}
          <details><summary>Error details</summary><pre>{entry.exception}</pre></details>
        {/if}
      </li>
    {/each}
  </ul>
  {#if shown.length > PAGE}
    <div class="row pager">
      <span class="muted">Showing {page.length} of {shown.length}</span>
      <div class="row-actions">
        {#if limit > PAGE}<button type="button" class="small" onclick={restart}>Back to newest</button>{/if}
        {#if page.length < shown.length}<button type="button" class="small" onclick={() => (limit += PAGE)}>Show {Math.min(PAGE, shown.length - page.length)} older</button>{/if}
      </div>
    </div>
  {/if}
</div>

<style>
  .filters { display: flex; flex-wrap: wrap; gap: .5rem 1rem; align-items: center; }
  .filters label { display: flex; gap: .4rem; align-items: center; }
  .filters .grow { flex: 1 1 14rem; }
  .filters .grow input { flex: 1; }
  .log li { padding: .45rem 0; border-top: 1px solid var(--line); }
  .log li:first-child { border-top: 0; }
  .meta { display: flex; flex-wrap: wrap; gap: .25rem .6rem; align-items: baseline; font-size: .85rem; }
  .meta .muted { font-size: .85rem; overflow-wrap: anywhere; }
  .message, pre { font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace; font-size: .85rem; white-space: pre-wrap; overflow-wrap: anywhere; margin: .2rem 0 0; }
  .message.clamped { display: -webkit-box; -webkit-box-orient: vertical; -webkit-line-clamp: 4; line-clamp: 4; overflow: hidden; }
  .link { background: none; border: 0; padding: 0; margin-top: .15rem; font-size: .85rem; color: var(--accent); cursor: pointer; }
  .link:hover { text-decoration: underline; }
  .pager { border-top: 1px solid var(--line); padding-top: .5rem; margin-top: .25rem; align-items: center; }
  details summary { cursor: pointer; font-size: .85rem; color: var(--muted); margin-top: .2rem; }
  .loading { display: flex; justify-content: center; padding: 1.5rem 0; }
  .loading :global(svg) { animation: spin .8s linear infinite; }
  @keyframes spin { to { transform: rotate(360deg); } }
  @media (prefers-reduced-motion: reduce) { .loading :global(svg) { animation: none; } }
</style>
