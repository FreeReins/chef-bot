<!-- The last setup step, optional: choose the stores the shopping list is split between. Skipping it groups the
     shopping list by food group; stores can be added later under Settings, Stores. -->
<script>
  import { app, run, enterApp, saveStores, toast } from '../lib/app.svelte.js';
  import { go } from '../lib/addresses.js';
  import { celebrate } from '../lib/motion.js';
  import SetupNav from '../components/SetupNav.svelte';
  import StoreEditor from '../components/StoreEditor.svelte';

  // Kept in app state rather than here, so going back a step and returning keeps the stores chosen so far.
  const stores = $derived(app.storeDraft ?? []);

  function finish() {
    return run(async () => {
      const chosen = $state.snapshot(stores);
      if (chosen.length || app.stores?.stores.length) await saveStores(chosen);
      app.storeDraft = null;
      const total = app.recipeCount;
      await enterApp();
      await go(total ? '/plan' : '/recipes');
      // Setup's finish line: the logo in the header bursts as the app opens.
      celebrate(document.querySelector('header.top .mark'), { count: 18 });
      const shopping = chosen.length
        ? 'Shopping lists are split between your stores.'
        : 'Shopping lists are grouped by food group. Add stores under Settings, Stores at any time.';
      toast(total
        ? `Setup is done and you have ${total} recipes. ${shopping} Plan your first week here when you are ready.`
        : `Setup is done. ${shopping} Add recipes here, then plan a week on the Plan tab.`);
    });
  }
</script>

<section class="gate wide">
  <h2>Where do you shop? <span class="muted">(optional)</span></h2>
  <p class="muted">
    Choose your stores and the shopping list is split between them: your main store gets everything, except what
    another store is set to sell. Skip this and the list is grouped by food group instead.
  </p>

  <div class="card"><StoreEditor bind:stores={app.storeDraft} /></div>

  <SetupNav label={stores.length ? 'Finish setup' : 'Skip and finish'} primary={stores.length > 0} onnext={finish}>
    {#if stores.length === 0}No stores chosen. You can add them later under Settings, Stores.
    {:else}{stores.length} {stores.length === 1 ? 'store' : 'stores'} chosen{/if}
  </SetupNav>
</section>
