<!-- Settings, Plan styles: the ways weeks can be planned. Anyone signed in can change them, and changes save by
     themselves; plans already made keep the style they were made with. -->
<script>
  import { app, saveStyles } from '../lib/app.svelte.js';
  import { autosave } from '../lib/autosave.svelte.js';
  import PlanStyleEditor from '../components/PlanStyleEditor.svelte';
  import SaveStatus from '../components/SaveStatus.svelte';

  const opened = JSON.stringify(app.styles.styles);
  let styles = $state(JSON.parse(opened));
  // What the server has now, as JSON: what the section opened with, then each list sent.
  let saved = opened;
  let editor;

  const current = () => JSON.stringify($state.snapshot(styles));
  const changedSinceOpening = $derived(JSON.stringify(styles) !== opened);

  /** Every style needs a name of its own, and the editor's number fields must hold numbers in range. */
  function complete() {
    const names = styles.map((style) => style.name.trim().toLowerCase());
    return names.every(Boolean) && new Set(names).size === names.length && !editor?.querySelector(':invalid');
  }

  // Names and numbers are typed in, so it waits a little longer for the typing to pause.
  const saver = autosave(async () => {
    const sending = current();
    await saveStyles(JSON.parse(sending));
    saved = sending;
  }, { validate: complete, delay: 1000 });

  $effect(() => {
    if (current() !== saved) saver.schedule();
  });
</script>

<h2>Plan styles</h2>
<p class="muted">
  A plan style says which days you cook dinner, what else to plan, and rules for the recipes it picks. The default is
  preselected when you plan a week, and used when next week is planned automatically (see
  <a href="/settings/planning">Planning</a>); you can choose another each time. With no styles, weeks are planned as
  the Classic week.
</p>
<div class="card" bind:this={editor}><PlanStyleEditor bind:styles /></div>
<div class="row status-row">
  <SaveStatus {saver} invalid="Not saved yet: give every style a name of its own, and keep the numbers in range." />
  {#if changedSinceOpening}
    <button type="button" class="small" onclick={() => (styles = JSON.parse(opened))}>Undo changes</button>
  {/if}
</div>

<style>
  .status-row { align-items: center; }
</style>
