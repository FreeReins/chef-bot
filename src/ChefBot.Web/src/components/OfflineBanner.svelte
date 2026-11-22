<!-- Says the page is offline, since when the list shown is, and how many changes wait to be sent. Try again checks for
     ChefBot now rather than at the next check (lib/offline.svelte.js). Shown under the tabs while app.offline. -->
<script>
  import { app, run, sync } from '../lib/app.svelte.js';
  import { plural } from '../lib/format.js';
  import { pendingClick } from '../lib/pending.js';
  import WifiOff from '@lucide/svelte/icons/wifi-off';

  const since = $derived(app.offlineSince
    ? new Date(app.offlineSince).toLocaleString(undefined, { weekday: 'short', hour: 'numeric', minute: '2-digit' })
    : null);
  const tryAgain = pendingClick(() => run(() => sync()));
</script>

<div class="card offline" role="status">
  <WifiOff size={18} aria-hidden="true" />
  <p>
    <strong>Offline.</strong>
    {#if since}Showing what ChefBot had on {since}.{/if}
    {#if app.outbox.length}
      {plural(app.outbox.length, 'change')} will be sent when ChefBot can be reached.
    {:else}
      Changes to the shopping list are sent when ChefBot can be reached.
    {/if}
  </p>
  <button class="small" onclick={tryAgain}>Try again</button>
</div>

<style>
  .offline { display: flex; align-items: center; gap: .6rem; margin-top: 1rem; border-left: 4px solid var(--warn); }
  .offline :global(svg) { flex: none; color: var(--warn); }
  .offline p { flex: 1; margin: 0; font-size: .95rem; }
</style>
