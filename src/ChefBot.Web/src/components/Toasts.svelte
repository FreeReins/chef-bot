<!-- The messages in the bottom corner of the window: notes that go away by themselves, and errors that stay until
     closed. App.svelte shows this once; anything can add a message with toast() from lib/app.svelte.js. -->
<script>
  import { fly } from 'svelte/transition';
  import { flip } from 'svelte/animate';
  import { app, dismissToast } from '../lib/app.svelte.js';
  import X from '@lucide/svelte/icons/x';
</script>

<div class="toasts" aria-live="polite">
  {#each app.toasts as item (item.id)}
    <div class="toast {item.kind}" role={item.kind === 'error' ? 'alert' : 'status'}
         animate:flip={{ duration: 200 }} transition:fly={{ y: 16, duration: 200 }}>
      <p>{item.text}</p>
      <button class="close" aria-label="Close this message" title="Close" onclick={() => dismissToast(item.id)}>
        <X size={16} />
      </button>
    </div>
  {/each}
</div>

<style>
  /* Above the footer pinned to the bottom of the window (App.svelte). */
  .toasts { position: fixed; z-index: 20; right: 1rem; bottom: calc(3.5rem + env(safe-area-inset-bottom));
    display: grid; gap: .5rem; width: min(24rem, calc(100vw - 2rem)); pointer-events: none; }
  .toast { display: flex; gap: .5rem; align-items: flex-start; padding: .6rem .5rem .6rem .8rem; border-radius: .5rem;
    border: 1px solid var(--line); border-left: 4px solid var(--accent); background: var(--panel); color: var(--ink);
    font-size: .92rem; box-shadow: 0 4px 16px rgb(0 0 0 / .15); pointer-events: auto; }
  .toast.error { border-color: var(--bad); color: var(--bad); }
  .toast p { margin: 0; flex: 1; min-width: 0; overflow-wrap: anywhere; }
  .close { flex: none; display: inline-flex; padding: .15rem; border: 0; background: none; color: var(--muted); }
  .close:hover { color: var(--ink); }
</style>
