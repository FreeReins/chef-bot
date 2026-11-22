<!-- A ⋯ button that opens a small panel of less common actions, such as a meal's Move to and Skip. It is a plain
     disclosure, not an ARIA menu: Tab moves through the panel's buttons as usual. Escape, a click outside or choosing
     an item closes it; Escape puts focus back on the ⋯ button. -->
<script>
  import Ellipsis from '@lucide/svelte/icons/ellipsis';

  /**
   * label: the button's accessible name ("More for Chickpea Curry"). id: the button's id, so callers can focus it.
   * children: a snippet given close(), to call after an item does its work.
   */
  let { label, id, children } = $props();

  let open = $state(false);
  let root = $state();
  let button = $state();

  const close = () => (open = false);

  function onKeydown(event) {
    if (event.key !== 'Escape' || !open) return;
    event.stopPropagation();
    open = false;
    button?.focus();
  }

  function onWindowClick(event) {
    if (open && !root?.contains(event.target)) open = false;
  }
</script>

<svelte:window onclick={onWindowClick} />

<!-- svelte-ignore a11y_no_static_element_interactions -->
<div class="overflow" bind:this={root} onkeydown={onKeydown}>
  <button bind:this={button} {id} type="button" class="icon" aria-label={label} title="More" aria-expanded={String(open)}
          onclick={() => (open = !open)}><Ellipsis size={16} /></button>
  {#if open}
    <div class="panel">{@render children(close)}</div>
  {/if}
</div>

<style>
  .overflow { position: relative; display: inline-flex; }
  .panel { position: absolute; right: 0; top: calc(100% + .25rem); z-index: 20; min-width: 10rem; display: grid; gap: .1rem;
    padding: .3rem; background: var(--panel); border: 1px solid var(--line); border-radius: .45rem;
    box-shadow: 0 6px 20px rgb(0 0 0 / .18); }
  .panel :global(button) { justify-content: flex-start; text-align: left; border: 0; background: none; padding: .35rem .55rem;
    font-size: .9rem; border-radius: .3rem; width: 100%; white-space: nowrap; }
  .panel :global(button:hover) { background: var(--accent-bg); color: var(--accent); }
</style>
