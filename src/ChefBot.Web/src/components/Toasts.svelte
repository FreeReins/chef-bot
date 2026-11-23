<!-- The messages in the bottom corner of the window: errors that stay until closed, and notes that carry a button,
     such as Undo, which go away by themselves. Other notes go to the inbox behind the bell (Inbox.svelte) once someone
     is signed in, so they don't cover the page. App.svelte shows this once; anything can add a message with toast()
     from lib/app.svelte.js. -->
<script>
  import { fly } from 'svelte/transition';
  import { flip } from 'svelte/animate';
  import { app, dismissToast } from '../lib/app.svelte.js';
  import X from '@lucide/svelte/icons/x';
</script>

{#snippet messages(kind)}
  {#each app.toasts.filter((item) => item.kind === kind) as item (item.id)}
    <div class="toast {item.kind}" animate:flip={{ duration: 200 }} transition:fly={{ y: 16, duration: 200 }}>
      <p>{item.text}</p>
      {#if item.action}
        <button class="small" onclick={() => { dismissToast(item.id); item.action.run(); }}>{item.action.label}</button>
      {/if}
      <button class="close" aria-label="Close this message" title="Close" onclick={() => dismissToast(item.id)}>
        <X size={16} />
      </button>
    </div>
  {/each}
{/snippet}

<!-- Two live regions that are always on the page, so screen readers hear what is added to them: errors at once,
     notes when the reader is next idle. A live region added together with its text is often not read at all. -->
<div class="toasts">
  <div class="stack" role="alert">{@render messages('error')}</div>
  <div class="stack" role="status">{@render messages('note')}</div>
</div>

<style>
  /* Above the footer pinned to the bottom of the window (App.svelte). */
  .toasts { position: fixed; z-index: 20; right: 1rem; bottom: calc(3.5rem + env(safe-area-inset-bottom));
    display: grid; gap: .5rem; width: min(24rem, calc(100vw - 2rem)); pointer-events: none; }
  .stack:empty { display: none; }
  /* Phones: full width, above the tab bar pinned to the bottom (App.svelte). */
  @media (max-width: 40rem) {
    .toasts { left: .75rem; right: .75rem; width: auto; bottom: calc(4.75rem + env(safe-area-inset-bottom)); }
  }
  .toast { display: flex; gap: .5rem; align-items: flex-start; padding: .6rem .5rem .6rem .8rem; border-radius: .5rem;
    border: 1px solid var(--line); border-left: 4px solid var(--accent); background: var(--panel); color: var(--ink);
    font-size: .92rem; box-shadow: 0 4px 16px rgb(0 0 0 / .15); pointer-events: auto; }
  .toast.error { border-color: var(--bad); color: var(--bad); }
  .toast p { margin: 0; flex: 1; min-width: 0; overflow-wrap: anywhere; }
  .toast button.small { flex: none; }
  .close { flex: none; display: inline-flex; padding: .15rem; border: 0; background: none; color: var(--muted); }
  .close:hover { color: var(--ink); }
</style>
