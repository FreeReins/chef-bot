<script>
  import { app, run } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';

  const backups = $derived(app.backups);
  const size = (bytes) => (bytes < 1048576 ? `${Math.max(1, Math.round(bytes / 1024))} KB` : `${(bytes / 1048576).toFixed(1)} MB`);

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
      app.notes = ['Backup settings saved.'];
    });
  }

  const runNow = () => run(async () => {
    app.backups = await api('POST', '/api/system/backups');
    app.notes = ['Backup made.'];
  });

  const remove = (file) => run(async () => {
    app.backups = await api('DELETE', `/api/system/backups/${file.name}`);
  });

  function restore(file) {
    if (!confirm(`Replace everything in ChefBot with the backup from ${new Date(file.createdAt).toLocaleString()}? Anything saved since then is lost.`)) return;
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
      <li class="row">
        <div><strong>{new Date(file.createdAt).toLocaleString()}</strong> <span class="muted">{size(file.sizeBytes)}</span></div>
        <div class="row-actions">
          <a href="/api/system/backups/{file.name}" download={file.name}>Download</a>
          <button class="small" onclick={() => restore(file)}>Restore</button>
          <button class="small" onclick={() => remove(file)}>Delete</button>
        </div>
      </li>
    {/each}
  </ul>
</div>
