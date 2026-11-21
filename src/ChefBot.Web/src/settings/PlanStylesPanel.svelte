<!-- Settings, Plan styles: the ways weeks can be planned. Anyone signed in can change them; plans already made keep
     the style they were made with. -->
<script>
  import { app, run, saveStyles, toast } from '../lib/app.svelte.js';
  import PlanStyleEditor from '../components/PlanStyleEditor.svelte';

  let styles = $state($state.snapshot(app.styles.styles));

  const changed = $derived(JSON.stringify($state.snapshot(styles)) !== JSON.stringify(app.styles.styles));

  function save() {
    run(async () => {
      await saveStyles($state.snapshot(styles));
      styles = $state.snapshot(app.styles.styles);
      toast(styles.length
        ? 'Plan styles saved. The next week you plan can use them.'
        : 'Plan styles saved. Weeks are planned as the Classic week until you add one.');
    });
  }
</script>

<h2>Plan styles</h2>
<p class="muted">
  A plan style says which days you cook dinner, what else to plan, and rules for the recipes it picks. The default is
  used for the weekly schedule and preselected when you plan a week; you can choose another each time.
</p>
<div class="card"><PlanStyleEditor bind:styles /></div>
<div class="row-actions">
  <button type="button" class="primary" disabled={!changed} onclick={save}>Save plan styles</button>
  {#if changed}<button type="button" onclick={() => (styles = $state.snapshot(app.styles.styles))}>Undo changes</button>{/if}
</div>
