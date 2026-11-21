<!-- A sidebar of sections, one section shown at a time. -->
<script>
  import { tick } from 'svelte';
  import { app, isAdmin } from '../lib/app.svelte.js';
  import { followLink } from '../lib/addresses.js';
  import LoginPanel from '../settings/LoginPanel.svelte';
  import HttpsPanel from '../settings/HttpsPanel.svelte';
  import MealiePanel from '../settings/MealiePanel.svelte';
  import StoresPanel from '../settings/StoresPanel.svelte';
  import NotificationsPanel from '../settings/NotificationsPanel.svelte';
  import BackupsPanel from '../settings/BackupsPanel.svelte';
  import LogsPanel from '../settings/LogsPanel.svelte';
  import KeyRound from '@lucide/svelte/icons/key-round';
  import Lock from '@lucide/svelte/icons/lock';
  import Store from '@lucide/svelte/icons/store';
  import Plug from '@lucide/svelte/icons/plug';
  import Bell from '@lucide/svelte/icons/bell';
  import Archive from '@lucide/svelte/icons/archive';
  import ScrollText from '@lucide/svelte/icons/scroll-text';

  /** The Settings sidebar: key, label, icon, who sees it, and what it shows. A wide section uses the full width. */
  const SECTIONS = [
    { key: 'login', label: 'Login', icon: KeyRound, visible: () => true, panel: LoginPanel },
    { key: 'https', label: 'HTTPS', icon: Lock, visible: () => true, panel: HttpsPanel, heading: 'Secure connection (HTTPS)' },
    { key: 'stores', label: 'Stores', icon: Store, visible: () => isAdmin() && app.stores, panel: StoresPanel },
    { key: 'mealie', label: 'Mealie', icon: Plug, visible: () => isAdmin() && app.mealie, panel: MealiePanel },
    { key: 'notifications', label: 'Notifications', icon: Bell, visible: () => isAdmin() && app.notifications, panel: NotificationsPanel },
    { key: 'backups', label: 'Backups', icon: Archive, visible: () => isAdmin() && app.backups, panel: BackupsPanel },
    { key: 'logs', label: 'Logs', icon: ScrollText, visible: () => isAdmin(), panel: LogsPanel, wide: true },
  ];

  const sections = $derived(SECTIONS.filter((section) => section.visible()));
  const current = $derived(sections.find((section) => section.key === app.settingsSection) ?? sections[0]);

  let body = $state();

  // Each section has its own address, /settings/{key}. Following one moves focus to the section it opens.
  async function open(event) {
    if (!followLink(event)) return;
    await tick();
    body?.focus();
  }
</script>

<div class="settings">
  <nav class="side-nav" aria-label="Settings">
    {#each sections as section (section.key)}
      <a href="/settings/{section.key}" aria-current={section === current ? 'page' : undefined} onclick={open}><section.icon size={16} />{section.label}</a>
    {/each}
  </nav>
  <div class="settings-body" class:wide={current.wide} id="settings-body" tabindex="-1" bind:this={body}>
    {#if current.heading}<h2>{current.heading}</h2>{/if}
    <current.panel />
  </div>
</div>

<style>
  /* A sidebar of sections, one section shown at a time. On narrow screens the sidebar becomes wrapping chips. */
  .settings { display: grid; grid-template-columns: 11rem minmax(0, 1fr); gap: 1.75rem; align-items: start; margin-top: 1.25rem; }
  .side-nav { display: grid; gap: .15rem; position: sticky; top: 1rem; }
  .side-nav a { display: flex; align-items: center; gap: .5rem; border-left: 3px solid transparent; border-radius: 0 .4rem .4rem 0;
    color: var(--muted); text-decoration: none; padding: .5rem .8rem; }
  .side-nav a :global(svg) { flex: none; }
  .side-nav a:hover { color: var(--ink); background: var(--chip); }
  .side-nav a:focus-visible { outline: 2px solid var(--accent); outline-offset: -2px; }
  .side-nav a[aria-current="page"] { color: var(--ink); border-left-color: var(--accent); background: var(--panel); font-weight: 600; }
  .settings-body { max-width: 40rem; outline: none; }
  .settings-body.wide { max-width: none; }
  .settings-body > h2:first-child { margin-top: 0; }
  @media (max-width: 40rem) {
    .settings { grid-template-columns: minmax(0, 1fr); gap: 1rem; }
    .side-nav { display: flex; flex-wrap: wrap; gap: .35rem; position: static; }
    .side-nav a { border: 1px solid var(--line); border-radius: 999px; padding: .35rem .75rem; white-space: nowrap; }
    .side-nav a[aria-current="page"] { border-color: var(--accent); background: var(--panel); }
  }
</style>
