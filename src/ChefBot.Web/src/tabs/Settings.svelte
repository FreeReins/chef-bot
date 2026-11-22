<!-- A sidebar of sections, one section shown at a time. On phones the sidebar is a drawer opened from a menu button. -->
<script>
  import { tick } from 'svelte';
  import { app, isAdmin } from '../lib/app.svelte.js';
  import { followLink } from '../lib/addresses.js';
  import LoginPanel from '../settings/LoginPanel.svelte';
  import HttpsPanel from '../settings/HttpsPanel.svelte';
  import StoresPanel from '../settings/StoresPanel.svelte';
  import PlanStylesPanel from '../settings/PlanStylesPanel.svelte';
  import PlanningPanel from '../settings/PlanningPanel.svelte';
  import AdvancedPanel from '../settings/AdvancedPanel.svelte';
  import NotificationsPanel from '../settings/NotificationsPanel.svelte';
  import ToDoAppsPanel from '../settings/ToDoAppsPanel.svelte';
  import CalendarPanel from '../settings/CalendarPanel.svelte';
  import AssistantsPanel from '../settings/AssistantsPanel.svelte';
  import BackupsPanel from '../settings/BackupsPanel.svelte';
  import LogsPanel from '../settings/LogsPanel.svelte';
  import AppearancePanel from '../settings/AppearancePanel.svelte';
  import HelpLink from '../components/HelpLink.svelte';
  import Palette from '@lucide/svelte/icons/palette';
  import KeyRound from '@lucide/svelte/icons/key-round';
  import Lock from '@lucide/svelte/icons/lock';
  import Store from '@lucide/svelte/icons/store';
  import ListChecks from '@lucide/svelte/icons/list-checks';
  import Bell from '@lucide/svelte/icons/bell';
  import ListTodo from '@lucide/svelte/icons/list-todo';
  import CalendarDays from '@lucide/svelte/icons/calendar-days';
  import Bot from '@lucide/svelte/icons/bot';
  import Archive from '@lucide/svelte/icons/archive';
  import ScrollText from '@lucide/svelte/icons/scroll-text';
  import CalendarClock from '@lucide/svelte/icons/calendar-clock';
  import SlidersHorizontal from '@lucide/svelte/icons/sliders-horizontal';
  import Menu from '@lucide/svelte/icons/menu';
  import X from '@lucide/svelte/icons/x';

  /**
   * The Settings sidebar: key, label, icon, who sees it, and what it shows. A wide section uses the full width; help is
   * the guide in the documentation (a page under docs/, and what it is about), linked at the end of the section.
   */
  const SECTIONS = [
    { key: 'login', label: 'Login', icon: KeyRound, visible: () => true, panel: LoginPanel, help: ['admin/people-and-accounts#passwords', 'Passwords and lockout'] },
    { key: 'appearance', label: 'Appearance', icon: Palette, visible: () => true, panel: AppearancePanel },
    { key: 'https', label: 'HTTPS', icon: Lock, visible: () => true, panel: HttpsPanel, heading: 'Secure connection (HTTPS)', help: ['admin/https-and-certificates', 'HTTPS and certificates'] },
    { key: 'planning', label: 'Planning', icon: CalendarClock, visible: () => Boolean(app.planning), panel: PlanningPanel, help: ['admin/settings#planning', 'Planning settings'] },
    { key: 'styles', label: 'Plan styles', icon: ListChecks, visible: () => Boolean(app.styles), panel: PlanStylesPanel, help: ['using/plan-styles', 'Plan styles'] },
    { key: 'stores', label: 'Stores', icon: Store, visible: () => isAdmin() && app.stores, panel: StoresPanel, help: ['using/shopping-list#stores', 'Stores'] },
    { key: 'calendar', label: 'Calendar', icon: CalendarDays, visible: () => true, panel: CalendarPanel, help: ['using/calendar', 'The meal plan in your calendar'] },
    { key: 'assistants', label: 'AI assistants', icon: Bot, visible: () => true, panel: AssistantsPanel, help: ['admin/ai-assistants', 'AI models and assistants'] },
    { key: 'todo-apps', label: 'To-do apps', icon: ListTodo, visible: () => isAdmin(), panel: ToDoAppsPanel, help: ['admin/to-do-apps', 'To-do apps'] },
    { key: 'notifications', label: 'Notifications', icon: Bell, visible: () => isAdmin() && app.notifications, panel: NotificationsPanel, help: ['admin/notifications', 'Notifications and Home Assistant'] },
    { key: 'backups', label: 'Backups', icon: Archive, visible: () => isAdmin() && app.backups, panel: BackupsPanel, help: ['admin/backups-and-restore', 'Backups and restore'] },
    { key: 'logs', label: 'Logs', icon: ScrollText, visible: () => isAdmin(), panel: LogsPanel, wide: true, help: ['admin/monitoring-and-logs', 'Monitoring and logs'] },
    { key: 'advanced', label: 'Advanced', icon: SlidersHorizontal, visible: () => isAdmin(), panel: AdvancedPanel, help: ['admin/settings#advanced', 'Advanced settings'] },
  ];

  const sections = $derived(SECTIONS.filter((section) => section.visible()));
  const current = $derived(sections.find((section) => section.key === app.settingsSection) ?? sections[0]);

  let body = $state();
  let drawer = $state();
  let menuOpen = $state(false);

  // Phones (the same width as the page's other phone layouts) get the sidebar as a drawer.
  const narrowQuery = globalThis.matchMedia?.('(max-width: 40rem)');
  let narrow = $state(narrowQuery?.matches ?? false);
  $effect(() => {
    if (!narrowQuery) return;
    const changed = () => { narrow = narrowQuery.matches; };
    narrowQuery.addEventListener('change', changed);
    return () => narrowQuery.removeEventListener('change', changed);
  });
  $effect(() => { if (!narrow) menuOpen = false; });

  function openMenu() {
    menuOpen = true;
    drawer?.showModal();
  }
  function closeMenu() {
    if (drawer?.open) drawer.close();
    menuOpen = false;
  }
  const clickedOutside = (event) => { if (event.target === drawer) closeMenu(); };

  // Each section has its own address, /settings/{key}. Following one closes the drawer and moves focus to the section.
  async function open(event) {
    if (!followLink(event)) return;
    closeMenu();
    await tick();
    body?.focus();
  }
</script>

{#snippet links()}
  {#each sections as section (section.key)}
    <a href="/settings/{section.key}" aria-current={section === current ? 'page' : undefined} onclick={open}><section.icon size={16} />{section.label}</a>
  {/each}
{/snippet}

<div class="settings">
  {#if narrow}
    <button type="button" class="menu-button" aria-haspopup="dialog" aria-expanded={menuOpen} aria-label="Settings sections, {current.label} open" onclick={openMenu}>
      <Menu size={18} /><current.icon size={16} /><span>{current.label}</span>
    </button>
    <dialog class="drawer" bind:this={drawer} aria-labelledby="settings-drawer-title" onclose={() => { menuOpen = false; }} onclick={clickedOutside}>
      <header>
        <h2 id="settings-drawer-title">Settings</h2>
        <button type="button" class="icon" aria-label="Close" onclick={closeMenu}><X size={20} /></button>
      </header>
      <nav class="side-nav" aria-label="Settings">{@render links()}</nav>
    </dialog>
  {:else}
    <nav class="side-nav" aria-label="Settings">{@render links()}</nav>
  {/if}
  <div class="settings-body" class:wide={current.wide} id="settings-body" tabindex="-1" bind:this={body}>
    {#if current.heading}<h2>{current.heading}</h2>{/if}
    <current.panel />
    {#if current.help}<p class="section-help muted"><HelpLink page={current.help[0]}>Guide: {current.help[1]}</HelpLink></p>{/if}
  </div>
</div>

<style>
  /* A sidebar of sections, one section shown at a time. On narrow screens a menu button opens it as a drawer. */
  .settings { display: grid; grid-template-columns: 11rem minmax(0, 1fr); gap: 1.75rem; align-items: start; margin-top: 1.25rem; }
  .side-nav { display: grid; gap: .15rem; position: sticky; top: 1rem; }
  .side-nav a { display: flex; align-items: center; gap: .5rem; border-left: 3px solid transparent; border-radius: 0 .4rem .4rem 0;
    color: var(--muted); text-decoration: none; padding: .5rem .8rem; }
  .side-nav a :global(svg) { flex: none; color: var(--area-settings); }
  .side-nav a[aria-current="page"] :global(svg) { color: var(--accent); }
  .side-nav a:hover { color: var(--ink); background: var(--chip); }
  .side-nav a:focus-visible { outline: 2px solid var(--accent); outline-offset: -2px; }
  .side-nav a[aria-current="page"] { color: var(--ink); border-left-color: var(--accent); background: var(--accent-bg); font-weight: 600; }
  .settings-body { max-width: 40rem; outline: none; }
  .settings-body.wide { max-width: none; }
  .settings-body > h2:first-child { margin-top: 0; }
  .section-help { margin: 1.5rem 0 0; padding-top: .75rem; border-top: 1px solid var(--line); }
  @media (max-width: 40rem) {
    .settings { grid-template-columns: minmax(0, 1fr); gap: 1rem; }
  }

  /* Phones: a button naming the open section, and the sidebar in a drawer from the left. */
  .menu-button { display: flex; align-items: center; gap: .55rem; width: 100%; text-align: left; padding: .6rem .8rem; font-weight: 600; }
  .menu-button :global(svg:nth-child(2)) { color: var(--area-settings); }
  .drawer { margin: 0; padding: 0; border: 0; border-right: 1px solid var(--line); border-radius: 0 .6rem .6rem 0;
    width: min(18rem, 85vw); max-width: none; height: 100dvh; max-height: 100dvh; background: var(--bg); color: var(--ink);
    box-shadow: 0 10px 40px rgb(0 0 0 / .3); }
  .drawer[open] { display: flex; flex-direction: column; }
  .drawer::backdrop { background: rgb(0 0 0 / .45); }
  .drawer header { display: flex; align-items: center; justify-content: space-between; gap: 1rem; padding: .9rem 1rem .5rem 1.1rem; }
  .drawer h2 { margin: 0; font-size: 1.2rem; }
  .drawer .side-nav { position: static; overflow-y: auto; padding: 0 .6rem calc(1rem + env(safe-area-inset-bottom)) 0; }
  .drawer .side-nav a { padding: .75rem 1rem; }
  @media (prefers-reduced-motion: no-preference) {
    .drawer[open] { animation: drawer-in .25s cubic-bezier(.2, .8, .3, 1); }
  }
  @keyframes drawer-in { from { transform: translateX(-100%); } }
</style>
