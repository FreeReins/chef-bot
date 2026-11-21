<script>
  import { app, run, toast, ask } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import ChevronRight from '@lucide/svelte/icons/chevron-right';
  import Trash2 from '@lucide/svelte/icons/trash-2';
  import Download from '@lucide/svelte/icons/download';
  import ArchiveArrowUp from '@lucide/svelte/icons/archive-arrow-up';

  const backups = $derived(app.backups);
  const size = (bytes) => (bytes < 1048576 ? `${Math.max(1, Math.round(bytes / 1024))} KB` : `${(bytes / 1048576).toFixed(1)} MB`);

  // Opened rows and the counts loaded for them, by file name. Counts are fetched the first time a row opens.
  let open = $state({});
  let summaries = $state({});

  const counts = [
    ['recipes', 'Recipes'], ['recipePhotos', 'Recipe photos'], ['mealPlans', 'Meal plans'],
    ['plannedMeals', 'Planned meals'], ['shoppingItems', 'Shopping list items'], ['pantryItems', 'Pantry items'],
    ['manualShoppingItems', 'Shopping items added by hand'], ['users', 'Accounts'], ['groceryStores', 'Grocery stores'],
  ];

  function toggle(file) {
    open[file.name] = !open[file.name];
    if (!open[file.name] || summaries[file.name]) return;
    run(async () => {
      summaries[file.name] = await api('GET', `/api/system/backups/${file.name}/summary`);
    }).finally(() => {
      if (!summaries[file.name]) open[file.name] = false;
    });
  }

  function save(event) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    run(async () => {
      app.backups = await api('PUT', '/api/system/backups', {
        enabled: data.get('enabled') === 'on',
        time: data.get('time'),
        keepCount: Number(data.get('keepCount')),
        keepDays: Number(data.get('keepDays')),
      });
      toast('Backup settings saved.');
    });
  }

  const runNow = () => run(async () => {
    app.backups = await api('POST', '/api/system/backups');
    toast('Backup made.');
  });

  const remove = (file) => run(async () => {
    app.backups = await api('DELETE', `/api/system/backups/${file.name}`);
  });

  async function restore(file) {
    if (!(await ask({ title: 'Restore this backup?',
      message: `Everything in ChefBot is replaced with the backup from ${new Date(file.createdAt).toLocaleString()}. Anything saved since then is lost.`,
      confirmLabel: 'Restore backup', danger: true }))) return;
    run(async () => {
      await api('POST', `/api/system/backups/${file.name}/restore`);
      location.reload();
    });
  }
</script>

<h2>Backups</h2>
<form class="stack card" onsubmit={save}>
  <label class="check">
    <input id="backup-enabled" name="enabled" type="checkbox" checked={backups.enabled}>
    <span>Back up automatically every day</span>
  </label>
  <label>At <input id="backup-time" name="time" type="time" required value={backups.time}></label>
  <label>Keep this many backups (0 keeps them all)
    <input id="backup-count" name="keepCount" type="number" min="0" max="1000" required value={backups.keepCount}>
  </label>
  <label>Delete backups older than this many days (0 keeps them whatever their age)
    <input id="backup-days" name="keepDays" type="number" min="0" max="3650" required value={backups.keepDays}>
  </label>
  {#if backups.lastResult}<p class="muted">{backups.lastResult}</p>{/if}
  <div class="row-actions">
    <button type="submit" class="primary">Save</button>
    <button type="button" onclick={runNow}>Back up now</button>
  </div>
</form>
<div class="card">
  {#if backups.files.length === 0}<p class="muted">No backups yet.</p>{/if}
  <ul class="plain">
    {#each backups.files as file (file.name)}
      <li>
        <div class="row">
          <button type="button" class="toggle" aria-expanded={!!open[file.name]} onclick={() => toggle(file)}>
            <ChevronRight class="chevron" size={16} />
            <strong>{new Date(file.createdAt).toLocaleString()}</strong> <span class="muted">{size(file.sizeBytes)}</span>
          </button>
          <div class="row-actions">
            <a class="icon" href="/api/system/backups/{file.name}" download={file.name} aria-label="Download this backup" title="Download"><Download size={16} /></a>
            <button class="icon" aria-label="Restore this backup" title="Restore" onclick={() => restore(file)}><ArchiveArrowUp size={16} /></button>
            <button class="icon remove" aria-label="Delete this backup" title="Delete" onclick={() => remove(file)}><Trash2 size={16} /></button>
          </div>
        </div>
        {#if open[file.name]}
          {@const summary = summaries[file.name]}
          {#if summary}
            <dl class="counts">
              {#each counts as [key, label] (key)}
                <div><dt>{label}</dt><dd>{summary[key]}</dd></div>
              {/each}
            </dl>
          {:else}
            <p class="muted details">Reading the backup…</p>
          {/if}
        {/if}
      </li>
    {/each}
  </ul>
</div>

<style>
  .row .row-actions { align-items: center; }
  .toggle { display: flex; gap: .4rem; align-items: baseline; flex-wrap: wrap; background: none; border: 0; padding: 0; color: inherit; font: inherit; text-align: left; cursor: pointer; }
  .toggle :global(.chevron) { align-self: center; flex: none; color: var(--muted); transition: transform .15s; }
  .toggle[aria-expanded='true'] :global(.chevron) { transform: rotate(90deg); }
  .details { margin: .35rem 0 .2rem 1.4rem; }
  .counts { display: grid; grid-template-columns: repeat(auto-fill, minmax(9.5rem, 1fr)); gap: .3rem 1rem; margin: .4rem 0 .2rem 1.4rem; font-size: .9rem; }
  .counts div { display: flex; justify-content: space-between; gap: .5rem; }
  .counts dt { color: var(--muted); }
  .counts dd { margin: 0; font-variant-numeric: tabular-nums; }
</style>
