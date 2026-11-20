<!-- A sidebar of sections, one section shown at a time. -->
<script>
  import { tick } from 'svelte';
  import { app, isAdmin, clearMessages } from '../lib/app.svelte.js';
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

  async function open(section) {
    app.settingsSection = section.key;
    clearMessages();
    await tick();
    body?.focus();
  }
</script>

<div class="settings">
  <nav class="side-nav" aria-label="Settings">
    {#each sections as section (section.key)}
      <button type="button" aria-current={section === current ? 'page' : undefined} onclick={() => open(section)}>{section.label}</button>
    {/each}
  </nav>
  <div class="settings-body" id="settings-body" tabindex="-1" bind:this={body}>
    {#if current.heading}<h2>{current.heading}</h2>{/if}
    <current.panel />
  </div>
</div>
