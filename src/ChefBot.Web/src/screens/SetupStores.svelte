<!-- Setup step four, optional: choose the stores the shopping list is split between. Skipping it groups the
     shopping list by food group; stores can be added later under Settings, Stores. -->
<script>
  import { app, run, enterApp, saveStores, toast } from '../lib/app.svelte.js';
  import { go } from '../lib/addresses.js';
  import SetupNav from '../components/SetupNav.svelte';
  import SetupProgress from '../components/SetupProgress.svelte';
  import StoreEditor from '../components/StoreEditor.svelte';

  // Kept in app state rather than here, so going back a step and returning keeps the stores chosen so far.
  const stores = $derived(app.storeDraft ?? []);

  function finish() {
    run(async () => {
      const chosen = $state.snapshot(stores);
      if (chosen.length || app.stores?.stores.length) await saveStores(chosen);
      app.storeDraft = null;
      const total = app.recipes.length;
      const stillImporting = app.mealieImport?.state === 'Running';
      await enterApp();
      await go(total || stillImporting ? '/recipes' : '/meals');
      const shopping = chosen.length
        ? 'Shopping lists are split between your stores.'
        : 'Shopping lists are grouped by food group. Add stores under Settings, Stores at any time.';
      toast(stillImporting
        ? `Setup is done. Your Mealie recipes are still importing and will appear on the Recipes tab when it finishes. ${shopping}`
        : total
          ? `Setup is done and you have ${total} recipes. ${shopping} Stage a week when you are ready.`
          : `Setup is done. ${shopping} Add recipes on the Recipes tab, then stage a week.`);
    });
  }
</script>

<section class="gate wide">
  <SetupProgress />
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
