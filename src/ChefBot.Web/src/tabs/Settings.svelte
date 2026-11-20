<!-- A sidebar of sections, one section shown at a time. -->
<script>
  import { tick } from 'svelte';
  import { app, isAdmin } from '../lib/app.svelte.js';
  import { followLink } from '../lib/addresses.js';
  import LoginPanel from '../settings/LoginPanel.svelte';
  import HttpsPanel from '../settings/HttpsPanel.svelte';
  import MealiePanel from '../settings/MealiePanel.svelte';
  import NotificationsPanel from '../settings/NotificationsPanel.svelte';
  import BackupsPanel from '../settings/BackupsPanel.svelte';

  /** The Settings sidebar: key, label, who sees it, and what it shows. */
  const SECTIONS = [
    { key: 'login', label: 'Login', visible: () => true, panel: LoginPanel },
    { key: 'https', label: 'HTTPS', visible: () => true, panel: HttpsPanel, heading: 'Secure connection (HTTPS)' },
    { key: 'mealie', label: 'Mealie', visible: () => isAdmin() && app.mealie, panel: MealiePanel },
    { key: 'notifications', label: 'Notifications', visible: () => isAdmin() && app.notifications, panel: NotificationsPanel },
    { key: 'backups', label: 'Backups', visible: () => isAdmin() && app.backups, panel: BackupsPanel },
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
      <a href="/settings/{section.key}" aria-current={section === current ? 'page' : undefined} onclick={open}>{section.label}</a>
    {/each}
  </nav>
  <div class="settings-body" id="settings-body" tabindex="-1" bind:this={body}>
    {#if current.heading}<h2>{current.heading}</h2>{/if}
    <current.panel />
  </div>
</div>

<style>
  /* A sidebar of sections, one section shown at a time. On narrow screens the sidebar becomes a row. */
  .settings { display: grid; grid-template-columns: 11rem minmax(0, 1fr); gap: 1.75rem; align-items: start; margin-top: 1.25rem; }
  .side-nav { display: grid; gap: .15rem; position: sticky; top: 1rem; }
  .side-nav a { border-left: 3px solid transparent; border-radius: 0 .4rem .4rem 0; color: var(--muted); text-decoration: none;
    padding: .5rem .8rem; }
  .side-nav a:hover { color: var(--ink); background: var(--chip); }
  .side-nav a:focus-visible { outline: 2px solid var(--accent); outline-offset: -2px; }
  .side-nav a[aria-current="page"] { color: var(--ink); border-left-color: var(--accent); background: var(--panel); font-weight: 600; }
  .settings-body { max-width: 40rem; outline: none; }
  .settings-body > h2:first-child { margin-top: 0; }
  @media (max-width: 40rem) {
    .settings { grid-template-columns: minmax(0, 1fr); gap: 1rem; }
    .side-nav { display: flex; overflow-x: auto; position: static; border-bottom: 1px solid var(--line); }
    .side-nav a { border-left: 0; border-bottom: 3px solid transparent; border-radius: 0; white-space: nowrap; }
    .side-nav a[aria-current="page"] { border-bottom-color: var(--accent); background: none; }
  }
</style>
