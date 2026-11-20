<!-- Connecting Mealie once, then importing from it. Until it is set up this is a short form; afterwards it shows the
     connection, an Import button and how the latest import went. Settings → Mealie edits the same settings. -->
<script>
  import { app, run, importFromMealie, saveMealie, describeMealieImport, toast } from '../lib/app.svelte.js';
  import Chip from './Chip.svelte';

  let editing = $state(false);

  const mealie = $derived(app.mealie);
  const latest = $derived(app.mealieImport);
  const connected = $derived(mealie?.isConfigured && !editing);

  function connect(event) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    run(async () => {
      await saveMealie(data.get('url'), data.get('apiToken'));
      if (!app.mealie.isConfigured) {
        toast('Mealie needs both an address and an API token. Fill in both, then connect again.', 'error');
        return;
      }
      editing = false;
      await importFromMealie();
    });
  }
</script>

{#if connected}
  <p class="muted"><Chip tone="good">Connected</Chip> <span class="clip address">{mealie.url}</span>
    <button type="button" class="link" onclick={() => (editing = true)}>Change</button></p>
  {#if latest?.state === 'Running'}
    <p class="muted" aria-live="polite"><span class="spinner"></span> Importing from Mealie…</p>
  {:else if latest?.state === 'Succeeded'}
    <p class="muted" aria-live="polite">{describeMealieImport(latest.result)}</p>
  {:else if latest?.state === 'Failed'}
    <p class="bad-text" role="alert">{latest.message}</p>
  {/if}
  <div class="row-actions">
    <button type="button" class={latest?.state === 'Succeeded' ? '' : 'primary'} disabled={latest?.state === 'Running'}
            onclick={importFromMealie}>
      {latest?.state === 'Succeeded' ? 'Import again' : 'Import from Mealie'}
    </button>
  </div>
{:else}
  <p class="muted">Connect once and ChefBot copies your recipes, so planning keeps working when Mealie is down.</p>
  <form class="stack" onsubmit={connect}>
    <label>Mealie address
      <input name="url" type="url" required value={mealie?.url ?? ''} placeholder="http://mealie.local:9000">
    </label>
    <label>API token (in Mealie: your profile, then API Tokens)
      <input name="apiToken" type="password" autocomplete="off" required={!mealie?.hasApiToken}
             placeholder={mealie?.hasApiToken ? 'Saved. Leave empty to keep it.' : 'Paste a token from Mealie'}>
    </label>
    <div class="row-actions">
      <button type="submit" class="primary">Connect and import</button>
      {#if editing}<button type="button" onclick={() => (editing = false)}>Cancel</button>{/if}
    </div>
  </form>
{/if}

<style>
  p { margin: 0; }
  .address { display: inline-block; max-width: 14rem; vertical-align: bottom; }
  .link { border: 0; background: none; padding: 0 .25rem; color: var(--accent); text-decoration: underline; font-size: .9rem; }
  .spinner { display: inline-block; width: .8rem; height: .8rem; border: 2px solid var(--line); border-top-color: var(--accent);
    border-radius: 50%; animation: spin .8s linear infinite; vertical-align: -.1rem; }
  @keyframes spin { to { transform: rotate(360deg); } }
  @media (prefers-reduced-motion: reduce) { .spinner { animation: none; } }
</style>
