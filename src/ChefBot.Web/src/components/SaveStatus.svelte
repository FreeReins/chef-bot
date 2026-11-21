<!-- Says whether a settings section that saves by itself (lib/autosave.svelte.js) has saved: shown under its form,
     in place of a Save button. Screen readers hear each change. -->
<script>
  import Check from '@lucide/svelte/icons/check';
  import CircleAlert from '@lucide/svelte/icons/circle-alert';
  import LoaderCircle from '@lucide/svelte/icons/loader-circle';

  /** saver: what autosave() returned. invalid: what to fix when something can't be saved yet. */
  let { saver, invalid = 'Not saved yet: fix the field marked in red.' } = $props();
</script>

<p class="save-status {saver.status}" role="status">
  {#if saver.status === 'saving'}
    <LoaderCircle size={15} aria-hidden="true" />Saving…
  {:else if saver.status === 'saved'}
    <Check size={15} aria-hidden="true" />Saved
  {:else if saver.status === 'invalid'}
    <CircleAlert size={15} aria-hidden="true" />{invalid}
  {:else if saver.status === 'error'}
    <CircleAlert size={15} aria-hidden="true" />Not saved.
    <button type="button" class="small" onclick={saver.retry}>Try again</button>
  {:else}
    Changes save by themselves.
  {/if}
</p>

<style>
  .save-status { display: flex; align-items: center; gap: .35rem; min-height: 1.75rem; margin: .25rem 0 0; font-size: .88rem; color: var(--muted); }
  .save-status :global(svg) { flex: none; }
  .saved { color: var(--good); }
  .invalid, .error { color: var(--bad); }
  .error button { margin-left: .25rem; }
</style>
