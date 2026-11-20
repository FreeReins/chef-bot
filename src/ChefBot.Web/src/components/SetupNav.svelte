<!-- The bar at the bottom of each setup step: Back, a short status, and the way forward. It stays in view above the
     page footer, so moving on never needs a scroll. -->
<script>
  import { app, setupSteps, moveSetup } from '../lib/app.svelte.js';

  /**
   * label: the forward button's text. primary: whether going forward is the suggested action.
   * onnext: runs instead of moving to the next step; the last step uses it to finish setup.
   * children: the status shown between the buttons.
   */
  let { label = 'Next', primary = true, onnext = null, children } = $props();

  const isFirst = $derived(setupSteps().findIndex((step) => step.screen === app.screen) <= 0);
</script>

<div class="setup-nav">
  {#if !isFirst}<button type="button" onclick={() => moveSetup(-1)}>Back</button>{/if}
  <span class="muted status">{@render children?.()}</span>
  <button type="button" class:primary onclick={onnext ?? (() => moveSetup(1))}>{label}</button>
</div>

<style>
  .setup-nav { position: sticky; bottom: calc(3rem + env(safe-area-inset-bottom)); display: flex; gap: 1rem; align-items: center;
    margin-top: 1rem; padding: .6rem 1rem; background: var(--panel); border: 1px solid var(--line);
    border-radius: .5rem; box-shadow: 0 2px 10px rgb(0 0 0 / .08); }
  .status { flex: 1; min-width: 0; }
</style>
