<script>
  import { app, run, saveMealie } from '../lib/app.svelte.js';
  import Chip from '../components/Chip.svelte';

  const mealie = $derived(app.mealie);

  function save(event) {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    run(async () => {
      await saveMealie(data.get('url'), data.get('apiToken'));
      form.elements.apiToken.value = '';
      app.notes = [app.mealie.isConfigured ? 'Mealie settings saved. Import from the Recipes tab.' : 'Mealie settings saved.'];
    });
  }

  function remove() {
    if (!confirm('Stop using Mealie? Recipes already imported stay in ChefBot.')) return;
    run(async () => {
      await saveMealie('', null);
      app.notes = ['ChefBot no longer uses Mealie.'];
    });
  }
</script>

<h2>Mealie</h2>
<form class="stack card" onsubmit={save}>
  <p>
    {#if mealie.isConfigured}<Chip tone="good">Set up</Chip>{:else}<Chip>Not set up</Chip>{/if}
    <span class="muted">ChefBot copies recipes from Mealie into its own library, so planning keeps working when Mealie is down.</span>
  </p>
  <label>Mealie address
    <input id="mealie-url" name="url" type="url" value={mealie.url} placeholder="http://mealie.local:9000">
  </label>
  <label>API token (in Mealie: your user profile, then API Tokens)
    <input id="mealie-token" name="apiToken" type="password" autocomplete="off"
           placeholder={mealie.hasApiToken ? 'Saved. Leave empty to keep it.' : 'Paste a token from Mealie'}>
  </label>
  <p class="muted">If Mealie__Url and Mealie__ApiToken are set on the server, they are used until something is saved here.</p>
  <div class="row-actions">
    <button type="submit" class="primary">Save</button>
    {#if mealie.url}<button type="button" onclick={remove}>Stop using Mealie</button>{/if}
  </div>
</form>
