<!-- The bell in the header and the messages behind it: what ChefBot said about recent actions, and news such as a
     plan the weekly schedule staged. A dot shows there is something unread; opening the bell reads it all. A plain
     disclosure like OverflowMenu.svelte: Escape or a click outside closes it, and Escape puts focus back on the bell.
     On phones the panel spans the window under the header. State and actions are in lib/inbox.svelte.js. -->
<script>
  import { app, inboxEntries, inboxUnread, readInbox, clearInbox } from '../lib/app.svelte.js';
  import { pendingClick } from '../lib/pending.js';
  import { popIn } from '../lib/motion.js';
  import { ago, plural } from '../lib/format.js';
  import Bell from '@lucide/svelte/icons/bell';

  let open = $state(false);
  let root = $state();
  let button = $state();
  let top = $state(0);
  // The messages that were unread when the bell was opened stay marked as new while it is open.
  let fresh = $state(new Set());

  const entries = $derived(inboxEntries());
  const unread = $derived(inboxUnread());
  const label = $derived(unread ? `Notifications, ${plural(unread, 'unread message')}` : 'Notifications');

  function toggle() {
    open = !open;
    if (!open) return;
    top = button.getBoundingClientRect().bottom + 6;
    fresh = new Set(entries.filter((entry) => !entry.read).map((entry) => entry.id));
    readInbox();
  }

  function onKeydown(event) {
    if (event.key !== 'Escape' || !open) return;
    event.stopPropagation();
    open = false;
    button?.focus();
  }

  function onWindowClick(event) {
    if (open && !root?.contains(event.target)) open = false;
  }

  // A note saved while the bell is open comes back with a new id, so it is no longer marked: it was seen as it arrived.
  const isFresh = (entry) => fresh.has(entry.id);
</script>

<svelte:window onclick={onWindowClick} />

<!-- svelte-ignore a11y_no_static_element_interactions -->
<div class="inbox" bind:this={root} onkeydown={onKeydown} style="--inbox-top: {top}px">
  <button bind:this={button} type="button" class="icon bell" aria-label={label} title="Notifications"
          aria-expanded={String(open)} aria-controls="inbox-panel" onclick={toggle}>
    <Bell size={20} />
    {#if unread}<span class="dot" aria-hidden="true" in:popIn={{ duration: 240 }}></span>{/if}
  </button>
  {#if open}
    <section id="inbox-panel" class="panel" aria-labelledby="inbox-heading">
      <div class="head">
        <h2 id="inbox-heading">Notifications</h2>
        {#if entries.length}
          <button type="button" class="small" onclick={pendingClick(() => clearInbox())}>Clear all</button>
        {/if}
      </div>
      {#if entries.length}
        <ul>
          {#each entries as entry (entry.id)}
            <li class:fresh={isFresh(entry)}>
              <p>{#if isFresh(entry)}<span class="sr-only">New: </span>{/if}{entry.text}</p>
              <time datetime={entry.createdAt}>{ago(entry.createdAt)}</time>
            </li>
          {/each}
        </ul>
      {:else}
        <p class="empty muted">Nothing new. What ChefBot tells you about your changes shows up here.</p>
      {/if}
    </section>
  {/if}
</div>

<style>
  .inbox { position: relative; display: inline-flex; }
  /* Sits beside the theme toggle and looks like it. */
  .bell { position: relative; display: inline-grid; place-items: center; width: 2.4rem; height: 2.4rem; padding: 0;
    border-radius: 50%; color: var(--muted); }
  .bell:hover { color: var(--ink); background: var(--chip); }
  .bell[aria-expanded="true"] { color: var(--accent); background: var(--accent-bg); }
  .dot { position: absolute; top: .45rem; right: .5rem; width: .55rem; height: .55rem; border-radius: 50%;
    background: var(--accent); box-shadow: 0 0 0 2px var(--bg); }
  .panel { position: absolute; right: 0; top: calc(100% + .4rem); z-index: 25; width: min(23rem, calc(100vw - 2rem));
    max-height: min(32rem, 70vh); display: grid; grid-template-rows: auto minmax(0, 1fr); overflow: hidden;
    background: var(--panel); border: 1px solid var(--line); border-radius: .6rem; box-shadow: 0 8px 28px rgb(0 0 0 / .2); }
  .head { display: flex; align-items: center; justify-content: space-between; gap: .5rem; padding: .6rem .7rem .5rem .9rem;
    border-bottom: 1px solid var(--line); }
  .head h2 { margin: 0; font-size: 1rem; }
  ul { list-style: none; margin: 0; padding: .25rem 0; overflow-y: auto; overscroll-behavior: contain; }
  li { display: grid; gap: .15rem; padding: .5rem .9rem; border-left: 3px solid transparent; }
  li + li { border-top: 1px solid var(--line); }
  li.fresh { border-left-color: var(--accent); background: color-mix(in srgb, var(--accent) 6%, var(--panel)); }
  li p { margin: 0; font-size: .92rem; overflow-wrap: anywhere; }
  time { color: var(--muted); font-size: .78rem; }
  .empty { margin: 0; padding: 1rem .9rem 1.1rem; font-size: .92rem; }

  /* Phones: across the window under the header, whichever header button the bell sits beside. */
  @media (max-width: 40rem) {
    .bell { width: 2.1rem; height: 2.1rem; }
    .dot { top: .35rem; right: .4rem; }
    .panel { position: fixed; top: var(--inbox-top); left: .75rem; right: .75rem; width: auto;
      max-height: calc(100dvh - var(--inbox-top) - 5.5rem - env(safe-area-inset-bottom)); }
  }
</style>
