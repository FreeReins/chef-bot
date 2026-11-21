<!-- Settings, To-do apps: where the Shopping tab can send the shopping list. Admins only. Saved with a button rather than
     by itself, since tokens are typed in once and never shown again: the server only says whether one is saved, and
     a token field left empty keeps it. -->
<script>
  import { app, run, toast, saveShoppingTargets } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { plural } from '../lib/format.js';
  import Plus from '@lucide/svelte/icons/plus';
  import Trash2 from '@lucide/svelte/icons/trash-2';

  const KINDS = [
    ['HomeAssistant', 'Home Assistant to-do list'],
    ['Todoist', 'Todoist'],
    ['Webhook', 'Webhook'],
  ];

  const draftsOf = (targets) => targets.map((target) => ({ ...target, token: '' }));

  let drafts = $state(draftsOf(app.shoppingTargets));
  let saving = $state(false);
  const changed = $derived(JSON.stringify(drafts) !== JSON.stringify(draftsOf(app.shoppingTargets)));

  function addTarget() {
    drafts.push({ id: null, name: '', kind: 'HomeAssistant', url: '', list: '', hasToken: false, token: '' });
  }

  async function save(event) {
    event.preventDefault();
    saving = true;
    await run(async () => {
      // An empty token field keeps the saved token.
      await saveShoppingTargets(drafts.map(({ id, name, kind, url, list, token }) => ({ id, name, kind, url, list, token: token.trim() || null })));
      drafts = draftsOf(app.shoppingTargets);
      toast('To-do apps saved. They are offered on the Shopping tab.');
    });
    saving = false;
  }

  const test = (target) => run(async () => {
    const result = await api('POST', `/api/shopping/targets/${target.id}/test`);
    if (result.problem) toast(`The test item didn't reach ${target.name}. ${result.problem}`, 'error');
    else toast(`A test item was added to ${target.name}. Delete it there once you've seen it.`);
  });
</script>

<h2>To-do apps</h2>
<p class="muted">
  The Shopping tab can send what is left to buy to these, one item per thing to buy with its amount, store and meals.
  Nothing is ticked off in ChefBot when you send, and sending again adds the items again.
</p>

<form class="stack" onsubmit={save}>
  {#each drafts as target, index (target.id ?? `new-${index}`)}
    <fieldset class="card target">
      <legend>{target.name || 'New to-do app'}</legend>
      <div class="fields">
        <label>Name <input bind:value={target.name} required maxlength="60" placeholder="Kitchen list"></label>
        <label>Kind
          <select bind:value={target.kind}>
            {#each KINDS as [kind, title] (kind)}<option value={kind}>{title}</option>{/each}
          </select>
        </label>
        {#if target.kind !== 'Todoist'}
          <label class="wide">{target.kind === 'Webhook' ? 'Webhook address' : 'Home Assistant address'}
            <input type="url" bind:value={target.url} required
                   placeholder={target.kind === 'Webhook' ? 'https://example.com/hooks/shopping' : 'http://homeassistant.local:8123'}>
          </label>
        {/if}
        {#if target.kind === 'HomeAssistant'}
          <label>To-do list <input bind:value={target.list} required pattern="todo\..+" placeholder="todo.shopping_list"></label>
        {:else if target.kind === 'Todoist'}
          <label>Project id (empty for the Inbox) <input bind:value={target.list} placeholder="2203306141"></label>
        {/if}
        <label class="wide">{target.kind === 'Webhook' ? 'Bearer token (optional)' : 'Access token'}
          <input type="password" autocomplete="off" bind:value={target.token}
                 placeholder={target.hasToken ? 'Saved. Type a new one to replace it' : ''}>
        </label>
      </div>
      <p class="muted hint">
        {#if target.kind === 'HomeAssistant'}
          Make a long-lived access token on your Home Assistant profile page, under Security. The list is a to-do
          entity, such as Home Assistant's own Shopping List or one it syncs with Bring!, Todoist or another app.
        {:else if target.kind === 'Todoist'}
          Copy your API token from Todoist's settings, under Integrations, then Developer.
        {:else}
          The whole list is posted at once as JSON: <code>{'{"type": "shopping_list", "items": [{"title", "description"}]}'}</code>.
        {/if}
      </p>
      <div class="row-actions">
        {#if target.id}
          <button type="button" class="small" onclick={() => test(target)} disabled={changed}
                  title={changed ? 'Save your changes first' : 'Adds one item to check the connection'}>Send a test item</button>
        {/if}
        <button type="button" class="small with-icon" onclick={() => drafts.splice(index, 1)}
                aria-label="Remove {target.name || 'this to-do app'}"><Trash2 size={14} />Remove</button>
      </div>
    </fieldset>
  {:else}
    <p class="card muted">No to-do apps yet. Add one to send the shopping list there from the Shopping tab.</p>
  {/each}

  <div class="row-actions">
    <button type="button" class="with-icon" onclick={addTarget}><Plus size={16} />Add a to-do app</button>
    <button type="submit" class="primary" disabled={!changed || saving}>{saving ? 'Saving…' : 'Save'}</button>
    {#if changed}<span class="muted unsaved">{plural(drafts.length, 'to-do app')}, not saved yet</span>{/if}
  </div>
</form>

<style>
  .target { border: 1px solid var(--line); }
  legend { font-weight: 600; padding: 0 .25rem; }
  .fields { display: grid; gap: .5rem .75rem; grid-template-columns: repeat(auto-fit, minmax(min(14rem, 100%), 1fr)); }
  .fields .wide { grid-column: 1 / -1; }
  .hint { margin: .5rem 0; }
  .with-icon { display: inline-flex; align-items: center; gap: .3rem; }
  .unsaved { align-self: center; }
</style>
