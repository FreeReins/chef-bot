<!-- ChefBot's page: first-run setup, sign-in, then planning and reviewing a week, shopping, the pantry, the food history and (for
     admins) inviting people. State and actions live in lib/app.svelte.js. -->
<script>
  import { app, isAdmin, run, start, signOut, needsConnection } from './lib/app.svelte.js';
  import { shortDate } from './lib/format.js';
  import SetupAccount from './screens/SetupAccount.svelte';
  import SetupHttps from './screens/SetupHttps.svelte';
  import SetupRecipes from './screens/SetupRecipes.svelte';
  import SetupPlanStyle from './screens/SetupPlanStyle.svelte';
  import SetupStores from './screens/SetupStores.svelte';
  import SetupSamples from './screens/SetupSamples.svelte';
  import SignIn from './screens/SignIn.svelte';
  import Join from './screens/Join.svelte';
  import Reset from './screens/Reset.svelte';
  import Plan from './tabs/Plan.svelte';
  import Shopping from './tabs/Shopping.svelte';
  import Pantry from './tabs/Pantry.svelte';
  import Recipes from './tabs/Recipes.svelte';
  import History from './tabs/History.svelte';
  import Achievements from './tabs/Achievements.svelte';
  import RankBadge from './components/RankBadge.svelte';
  import People from './tabs/People.svelte';
  import Settings from './tabs/Settings.svelte';
  import Toasts from './components/Toasts.svelte';
  import ConfirmDialog from './components/ConfirmDialog.svelte';
  import ThemeToggle from './components/ThemeToggle.svelte';
  import SetupProgress from './components/SetupProgress.svelte';
  import OfflineBanner from './components/OfflineBanner.svelte';
  import { stepIn, stepOut, rise } from './lib/motion.js';
  import CalendarDays from '@lucide/svelte/icons/calendar-days';
  import ShoppingCart from '@lucide/svelte/icons/shopping-cart';
  import Refrigerator from '@lucide/svelte/icons/refrigerator';
  import ChefHat from '@lucide/svelte/icons/chef-hat';
  import BookOpenText from '@lucide/svelte/icons/book-open-text';
  import ScrollText from '@lucide/svelte/icons/scroll-text';
  import Users from '@lucide/svelte/icons/users';
  import SettingsIcon from '@lucide/svelte/icons/settings';
  import CircleHelp from '@lucide/svelte/icons/circle-help';
  import Sprout from '@lucide/svelte/icons/sprout';
  import WifiOff from '@lucide/svelte/icons/wifi-off';

  const TABS = [
    { key: 'plan', label: 'Plan', icon: CalendarDays },
    { key: 'shopping', label: 'Shopping', icon: ShoppingCart },
    { key: 'pantry', label: 'Pantry', icon: Refrigerator },
    { key: 'recipes', label: 'Recipes', icon: BookOpenText },
    { key: 'history', label: 'History', icon: ScrollText },
    { key: 'people', label: 'People', icon: Users, adminOnly: true },
    { key: 'settings', label: 'Settings', icon: SettingsIcon },
  ].map((tab) => ({ ...tab, area: `var(--area-${tab.key})` }));
  const GATES = { 'setup-account': SetupAccount, 'setup-https': SetupHttps, 'setup-recipes': SetupRecipes,
    'setup-styles': SetupPlanStyle, 'setup-stores': SetupStores, 'setup-samples': SetupSamples,
    'sign-in': SignIn, join: Join, reset: Reset };
  const TAB_VIEWS = { plan: Plan, shopping: Shopping, pantry: Pantry, recipes: Recipes, history: History, people: People, settings: Settings,
    achievements: Achievements };

  const inApp = $derived(app.screen === 'app');
  const tabs = $derived(TABS.filter((tab) => !tab.adminOnly || isAdmin()));
  const Gate = $derived(GATES[app.screen]);
  const View = $derived(TAB_VIEWS[app.tab]);
  // The setup steps share one progress bar, which stays put while the steps slide beneath it.
  const inSetup = $derived(app.screen.startsWith('setup-') && app.screen !== 'setup-samples');

  // When a new screen arrives after another (a setup step, or sign-in after a link), the page goes back to its top and
  // focus moves to the screen's heading, so keyboard and screen reader users start where everyone else is looking.
  let firstScreen = true;
  function arrive(node) {
    if (firstScreen) { firstScreen = false; return; }
    window.scrollTo({ top: 0 });
    const heading = node.querySelector('h2');
    if (!heading) return;
    heading.tabIndex = -1;
    heading.focus({ preventScroll: true });
  }

  // The browser tab's title names the view, so bookmarks and history read well.
  const title = $derived.by(() => {
    if (app.screen === 'setup-samples') return 'Sample recipes';
    if (!inApp) return null;
    if (app.recipeId) return app.recipeName ?? 'Recipe';
    if (app.tab === 'achievements') return 'Achievements'; // opened from the award in the header, not a tab
    return TABS.find((tab) => tab.key === app.tab)?.label;
  });

  run(start);
</script>

<svelte:head><title>{title ? `${title} · ChefBot` : 'ChefBot'}</title></svelte:head>

<header class="top">
  <div class="brand">
    <span class="mark" aria-hidden="true"><ChefHat size={22} /></span>
    <h1>ChefBot</h1>
    <p id="week" class="muted">
      {#if !inApp}
        Meal planning around what you have
      {:else if !app.plan}
        No plan yet
      {:else}
        Week of {shortDate(app.plan.weekStart)} to {shortDate(app.plan.weekEnd)}
        <span class="pill {app.plan.status.toLowerCase()}">{app.plan.status === 'Staged' ? 'Draft' : 'Confirmed'}</span>
      {/if}
    </p>
  </div>
  <div class="actions account">
    {#if inApp}<RankBadge />{/if}
    <!-- Help is on every screen, sign-in and setup included: the documentation is built in and needs no sign-in. -->
    <a class="icon help" href="/help/" target="_blank" rel="noopener" aria-label="Help: open the ChefBot guide"
       title="Help"><CircleHelp size={20} /></a>
    <ThemeToggle />
    {#if inApp}
      <span class="muted name">{app.session.user.displayName}</span>
      <button onclick={signOut}>Sign out</button>
    {/if}
  </div>
  <!-- Shown while something takes a moment to save or load, so a slow click doesn't look ignored. Fixed to the top of
       the window; it sits last in the header (whose first div is the title) so it is inside a landmark. -->
  {#if app.busy}<div class="busy-bar" role="progressbar" aria-label="Working"></div>{/if}
</header>

<Toasts />
<ConfirmDialog />

{#if inApp}
  <nav class="tabs" aria-label="Views">
    {#each tabs as tab (tab.key)}
      <a href="/{tab.key}" style="--area: {tab.area}" aria-current={app.tab === tab.key ? 'page' : undefined}><tab.icon size={18} /><span class="tab-label">{tab.label}</span></a>
    {/each}
  </nav>
  {#if app.offline}<OfflineBanner />{/if}
{/if}

<main id="view">
  {#if app.screen === 'loading'}
    <p class="loading muted" role="status" aria-label="Loading">
      <span class="loading-mark" aria-hidden="true"><ChefHat size={30} /></span>
      <span class="warming" aria-hidden="true">Warming up the kitchen…</span>
    </p>
  {:else if !inApp}
    {#if inSetup}<div class="gate wide setup-head"><SetupProgress /></div>{/if}
    <!-- One cell, so the step leaving and the step arriving overlap while they slide instead of stacking. -->
    <div class="stage" class:in-setup={inSetup}>
      {#key app.screen}
        <div class="scene" use:arrive in:stepIn={{ direction: app.setupDirection }} out:stepOut={{ direction: app.setupDirection }}>
          <Gate />
        </div>
      {/key}
    </div>
  {:else}
    {#key app.tab}
      <div in:rise={{ duration: 160 }}>
        {#if needsConnection()}
          <!-- Offline, only the shopping list and the pantry were saved on this device. -->
          <div class="card needs-connection">
            <span class="empty-icon" style="--area: var(--area-{app.tab})" aria-hidden="true"><WifiOff size={24} /></span>
            <p class="muted">This needs ChefBot. The <a href="/shopping">Shopping</a> and <a href="/pantry">Pantry</a> tabs work offline.</p>
          </div>
        {:else}
          <View />
        {/if}
      </div>
    {/key}
  {/if}
</main>

<!-- Pinned to the bottom of the window (at the end of the page on phones). Add a link by adding an <li>; the | between
     items comes from the CSS. Each link has an icon and a short name, so none can be mistaken for another. The GitHub
     mark is inline SVG from GitHub's Octicons (MIT licence), since Lucide has no brand logos; the rest come from
     Lucide like the other icons on the page. The API reference for developers is linked from Settings, AI assistants. -->
<footer class="site-footer">
  <ul class="footer-links">
    <li>
      <a href="/help/" target="_blank" rel="noopener"><CircleHelp size={16} /><span>Help</span></a>
    </li>
    <li>
      <a href="https://github.com/Jamiethenextkevinbaconhamm/chef-bot" target="_blank" rel="noopener">
        <svg viewBox="0 0 16 16" width="16" height="16" aria-hidden="true" focusable="false"><path fill="currentColor" d="M8 0c4.42 0 8 3.58 8 8a8.013 8.013 0 0 1-5.45 7.59c-.4.08-.55-.17-.55-.38 0-.27.01-1.13.01-2.2 0-.75-.25-1.23-.54-1.48 1.78-.2 3.65-.88 3.65-3.95 0-.88-.31-1.59-.82-2.15.08-.2.36-1.02-.08-2.12 0 0-.67-.22-2.2.82-.64-.18-1.32-.27-2-.27-.68 0-1.36.09-2 .27-1.53-1.03-2.2-.82-2.2-.82-.44 1.1-.16 1.92-.08 2.12-.51.56-.82 1.28-.82 2.15 0 3.06 1.86 3.75 3.64 3.95-.23.2-.44.55-.51 1.07-.46.21-1.61.55-2.33-.66-.15-.24-.6-.83-1.23-.82-.67.01-.27.38.01.53.34.19.73.9.82 1.13.16.45.68 1.31 2.69.94 0 .67.01 1.3.01 1.49 0 .21-.15.45-.55.38A7.995 7.995 0 0 1 0 8c0-4.42 3.58-8 8-8Z"/></svg><span>GitHub</span>
      </a>
    </li>
    <li>
      <a href="https://buymeacoffee.com/strifer.btw" target="_blank" rel="noopener"><Sprout size={16} /><span>Support ChefBot</span></a>
    </li>
  </ul>
</footer>

<style>
  .top { display: flex; flex-wrap: wrap; gap: 1rem; justify-content: space-between; align-items: flex-start; }
  .top > div:first-child { margin-right: auto; }
  /* The logo mark and the name share the first line; the week is under the name. */
  .brand { display: grid; grid-template-columns: auto minmax(0, 1fr); column-gap: .6rem; align-items: center; }
  /* The plan's status pill keeps clear of the dates beside it, and drops to its own line when space is short. */
  .brand #week { grid-column: 2; display: flex; flex-wrap: wrap; align-items: center; gap: .3rem .65rem; }
  .mark { grid-row: 1 / span 2; display: inline-grid; place-items: center; width: 2.6rem; height: 2.6rem; border-radius: .7rem;
    color: var(--accent-2); background: color-mix(in srgb, var(--accent-2) 14%, var(--panel)); }
  .account { align-items: center; }
  /* Sits beside the theme toggle and looks like it. */
  a.help { display: inline-grid; place-items: center; width: 2.4rem; height: 2.4rem; padding: 0; border-radius: 50%;
    color: var(--muted); }
  a.help:hover { color: var(--ink); background: var(--chip); }
  .name { align-self: center; }
  .busy-bar { position: fixed; inset: 0 0 auto 0; z-index: 30; height: 3px; overflow: hidden; pointer-events: none;
    background: color-mix(in srgb, var(--accent) 25%, transparent); animation: appear .3s step-end; }
  .busy-bar::after { content: ""; position: absolute; inset: 0 auto 0 0; width: 35%; background: linear-gradient(90deg, var(--accent), var(--accent-2));
    animation: slide 1.1s ease-in-out infinite; }
  /* Hidden for its first moments, so quick actions don't flicker it. */
  @keyframes appear { from { visibility: hidden; } }
  @keyframes slide { from { left: -35%; } to { left: 100%; } }
  @media (prefers-reduced-motion: reduce) { .busy-bar::after { animation: none; width: 100%; } }
  .tabs { display: flex; gap: .25rem; margin: 1.25rem 0 0; border-bottom: 1px solid var(--line); overflow-x: auto; }
  .tabs a { display: inline-flex; align-items: center; gap: .45rem; border-bottom: 3px solid transparent; color: var(--muted);
    padding: .6rem .9rem; text-decoration: none; white-space: nowrap; }
  .tabs a :global(svg) { flex: none; color: var(--area); }
  .tabs a:hover { color: var(--ink); background: color-mix(in srgb, var(--area) 8%, transparent); }
  .tabs a:focus-visible { outline: 2px solid var(--accent); outline-offset: -2px; }
  .tabs a[aria-current="page"] { color: var(--ink); border-bottom-color: var(--area); font-weight: 600; }
  /* Pinned to the bottom of the window; the body's bottom padding in styles.css keeps content clear of it. */
  .site-footer { position: fixed; inset: auto 0 0 0; z-index: 10; background: var(--bg); border-top: 1px solid var(--line);
    padding: .45rem 1rem calc(.45rem + env(safe-area-inset-bottom)); }
  .footer-links { list-style: none; margin: 0 auto; padding: 0; max-width: 62rem; display: flex; justify-content: center;
    align-items: center; flex-wrap: wrap; }
  .footer-links li { display: flex; align-items: center; }
  .footer-links li + li:not([hidden])::before { content: "|"; color: var(--line); padding: 0 .6rem; }
  .footer-links a { display: inline-flex; align-items: center; gap: .35rem; padding: .25rem .4rem; border-radius: .4rem;
    color: var(--muted); font-size: .875rem; text-decoration: none; }
  .footer-links a:hover { color: var(--ink); background: var(--chip); }
  .footer-links a:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }
  /* The first load: the logo mark bobs like a lid on a simmering pot, and a line appears if it takes a moment. */
  .loading { display: grid; justify-items: center; gap: .75rem; padding: 3rem 0; }
  .loading-mark { display: inline-grid; place-items: center; width: 3.5rem; height: 3.5rem; border-radius: 1rem;
    color: var(--accent-2); background: color-mix(in srgb, var(--accent-2) 14%, var(--panel)); }
  .warming { animation: appear .6s step-end; }
  @media (prefers-reduced-motion: no-preference) {
    .loading-mark :global(svg) { animation: simmer 1.1s ease-in-out infinite; transform-origin: 50% 90%; }
  }
  @keyframes simmer { 0%, 100% { transform: translateY(0) rotate(0); } 30% { transform: translateY(-4px) rotate(-6deg); } 60% { transform: translateY(-2px) rotate(4deg); } }
  .needs-connection { display: flex; gap: .9rem; align-items: center; margin-top: 1rem; padding: 1rem; }
  .needs-connection .empty-icon { flex: none; margin: 0; }
  .needs-connection p { margin: 0; }
  .stage { display: grid; }
  .scene { grid-area: 1 / 1; min-width: 0; }
  /* The progress bar above already makes the gap at the top of the step. */
  .setup-head { margin-bottom: 0; }
  .stage.in-setup :global(.gate) { margin-top: 0; }
  .scene :global(h2[tabindex="-1"]:focus:not(:focus-visible)) { outline: none; }

  /* Tablets and narrow windows: the tabs share the width evenly, label under icon, so the row never scrolls. */
  @media (max-width: 56rem) {
    .tabs { gap: 0; overflow: visible; }
    .tabs a { flex: 1 1 0; flex-direction: column; justify-content: center; gap: .15rem; min-width: 0;
      padding: .5rem .2rem; font-size: .8rem; }
    .tab-label { max-width: 100%; overflow: hidden; text-overflow: ellipsis; }
  }

  /* Phones: the title and Sign out share the top line, the tabs become a bar pinned to the bottom of the window,
     and the footer links sit at the end of the page instead. */
  @media (max-width: 40rem) {
    .top { display: grid; grid-template-columns: minmax(0, 1fr) auto; grid-template-areas: "title account"; gap: .75rem; }
    .top > div:first-child { grid-area: title; margin: 0; }
    .mark { width: 2.2rem; height: 2.2rem; }
    .account { grid-area: account; }
    .account .name { display: none; }
    /* Room for the title beside the award, Help, the theme button and Sign out. */
    .account { flex-wrap: nowrap; gap: .2rem; }
    a.help { width: 2.1rem; height: 2.1rem; }
    .tabs { position: fixed; inset: auto 0 0 0; z-index: 10; display: grid; grid-auto-flow: column; grid-auto-columns: minmax(0, 1fr);
      gap: 0; margin: 0; overflow: visible; background: var(--bg); border-bottom: 0; border-top: 1px solid var(--line);
      padding-bottom: env(safe-area-inset-bottom); }
    .tabs a { flex-direction: column; gap: .15rem; min-width: 0; padding: .45rem .1rem .4rem; font-size: .7rem;
      border-bottom: 0; border-top: 3px solid transparent; }
    .tabs a[aria-current="page"] { border-top-color: var(--area); background: color-mix(in srgb, var(--area) 10%, var(--bg)); }
    .tab-label { max-width: 100%; overflow: hidden; text-overflow: ellipsis; }
    .site-footer { position: static; margin-top: 1.5rem; padding-bottom: .45rem; background: none; }
  }
</style>
