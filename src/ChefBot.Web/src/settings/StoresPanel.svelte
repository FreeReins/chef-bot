<!-- Settings, Stores: the stores the shopping list is split between. Saving rebuilds open shopping lists. -->
<script>
  import { app, run, saveStores, toast } from '../lib/app.svelte.js';
  import StoreEditor from '../components/StoreEditor.svelte';

  let stores = $state($state.snapshot(app.stores.stores));

  const changed = $derived(JSON.stringify($state.snapshot(stores)) !== JSON.stringify(app.stores.stores));

  function save() {
    run(async () => {
      await saveStores($state.snapshot(stores));
      stores = $state.snapshot(app.stores.stores);
      toast(stores.length
        ? 'Stores saved. Open shopping lists now use them.'
        : 'Stores saved. Shopping lists are grouped by food group.');
    });
  }
</script>

<h2>Stores</h2>
<p class="muted">
  Each item on the shopping list goes to the first store that lists it, then in-season produce to a seasonal market
  while it is open, then to your main store. You can also move an item from the Shopping tab.
</p>
<div class="card"><StoreEditor bind:stores /></div>
<div class="row-actions">
  <button type="button" class="primary" disabled={!changed} onclick={save}>Save stores</button>
  {#if changed}<button type="button" onclick={() => (stores = $state.snapshot(app.stores.stores))}>Undo changes</button>{/if}
</div>
