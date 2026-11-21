<!-- Settings, Stores: the stores the shopping list is split between. Changes save by themselves, and saving rebuilds
     open shopping lists. -->
<script>
  import { app, saveStores } from '../lib/app.svelte.js';
  import { autosave } from '../lib/autosave.svelte.js';
  import StoreEditor from '../components/StoreEditor.svelte';
  import SaveStatus from '../components/SaveStatus.svelte';

  const opened = JSON.stringify(app.stores.stores);
  let stores = $state(JSON.parse(opened));
  // What the server has now, as JSON: what the section opened with, then each list sent.
  let saved = opened;

  const current = () => JSON.stringify($state.snapshot(stores));
  const changedSinceOpening = $derived(JSON.stringify(stores) !== opened);

  // Saving reloads the plan and its shopping list, so it waits a little longer for the editing to pause.
  const saver = autosave(async () => {
    const sending = current();
    await saveStores(JSON.parse(sending));
    saved = sending;
  }, { delay: 1000 });

  $effect(() => {
    if (current() !== saved) saver.schedule();
  });
</script>

<h2>Stores</h2>
<p class="muted">
  Each item on the shopping list goes to the first store that lists it, then in-season produce to a seasonal market
  while it is open, then to your main store. You can also move an item from the Shopping tab. Open shopping lists
  follow changes here as soon as they are saved; with no stores, they are grouped by food group.
</p>
<div class="card"><StoreEditor bind:stores /></div>
<div class="row status-row">
  <SaveStatus {saver} />
  {#if changedSinceOpening}
    <button type="button" class="small" onclick={() => (stores = JSON.parse(opened))}>Undo changes</button>
  {/if}
</div>

<style>
  .status-row { align-items: center; }
</style>
