<!-- ChefBot's page: first-run setup, sign-in, then reviewing a staged plan, swapping meals, shopping,
     the pantry and (for admins) inviting people. State and actions live in lib/app.svelte.js. -->
<script>
  import { app, isAdmin, isConfirmed, run, start, stage, planAction, clearPlan, regenerate, signOut } from './lib/app.svelte.js';
  import { shortDate } from './lib/format.js';
  import SetupAccount from './screens/SetupAccount.svelte';
  import SetupHttps from './screens/SetupHttps.svelte';
  import SetupRecipes from './screens/SetupRecipes.svelte';
  import SetupStores from './screens/SetupStores.svelte';
  import SetupSamples from './screens/SetupSamples.svelte';
  import SignIn from './screens/SignIn.svelte';
  import Join from './screens/Join.svelte';
  import Reset from './screens/Reset.svelte';
  import Meals from './tabs/Meals.svelte';
  import Schedule from './tabs/Schedule.svelte';
  import Shopping from './tabs/Shopping.svelte';
  import Pantry from './tabs/Pantry.svelte';
  import Recipes from './tabs/Recipes.svelte';
  import People from './tabs/People.svelte';
  import Settings from './tabs/Settings.svelte';
  import Toasts from './components/Toasts.svelte';
  import ConfirmDialog from './components/ConfirmDialog.svelte';
  import Utensils from '@lucide/svelte/icons/utensils';
  import CalendarDays from '@lucide/svelte/icons/calendar-days';
  import ShoppingCart from '@lucide/svelte/icons/shopping-cart';
  import Refrigerator from '@lucide/svelte/icons/refrigerator';
  import ChefHat from '@lucide/svelte/icons/chef-hat';
  import Users from '@lucide/svelte/icons/users';
  import SettingsIcon from '@lucide/svelte/icons/settings';
  import CodeXml from '@lucide/svelte/icons/code-xml';

  const TABS = [
    { key: 'meals', label: 'Meals', icon: Utensils },
    { key: 'schedule', label: 'Schedule', icon: CalendarDays },
    { key: 'shopping', label: 'Shopping', icon: ShoppingCart },
    { key: 'pantry', label: 'Pantry', icon: Refrigerator },
    { key: 'recipes', label: 'Recipes', icon: ChefHat },
    { key: 'people', label: 'People', icon: Users, adminOnly: true },
    { key: 'settings', label: 'Settings', icon: SettingsIcon },
  ];
  const GATES = { 'setup-account': SetupAccount, 'setup-https': SetupHttps, 'setup-recipes': SetupRecipes,
    'setup-stores': SetupStores, 'setup-samples': SetupSamples,
    'sign-in': SignIn, join: Join, reset: Reset };
  const PLAN_TABS = { meals: Meals, schedule: Schedule, shopping: Shopping };
  const OTHER_TABS = { pantry: Pantry, recipes: Recipes, people: People, settings: Settings };

  const inApp = $derived(app.screen === 'app');
  const tabs = $derived(TABS.filter((tab) => !tab.adminOnly || isAdmin()));
  const Gate = $derived(GATES[app.screen]);
  const Other = $derived(OTHER_TABS[app.tab]);
  const PlanTab = $derived(PLAN_TABS[app.tab]);

  // The browser tab's title names the view, so bookmarks and history read well.
  const title = $derived.by(() => {
    if (app.screen === 'setup-samples') return 'Sample recipes';
    if (!inApp) return null;
    if (app.recipeId) return app.recipes.find((recipe) => recipe.id === app.recipeId)?.name ?? 'Recipe';
    return TABS.find((tab) => tab.key === app.tab)?.label;
  });

  run(start);
</script>

<svelte:head><title>{title ? `${title} · ChefBot` : 'ChefBot'}</title></svelte:head>

<header class="top">
  <div>
    <h1>ChefBot</h1>
    <p id="week" class="muted">
      {#if !inApp}
        Meal planning around what you have
      {:else if !app.plan}
        No plan yet
      {:else}
        Week of {shortDate(app.plan.weekStart)} to {shortDate(app.plan.weekEnd)}
        <span class="pill {app.plan.status.toLowerCase()}">{app.plan.status}</span>
      {/if}
    </p>
  </div>
  <div class="actions">
    {#if inApp}
      {#if !app.plan}
        <button class="primary" onclick={() => stage({})}>Stage next week</button>
      {:else}
        <button title="Remove this plan and keep the pantry" onclick={clearPlan}>Clear plan</button>
        <button title="Build a new plan that uses what you have already bought" onclick={regenerate}>Regenerate from pantry</button>
        {#if isConfirmed()}
          <button onclick={() => stage({})}>Stage next week</button>
        {:else}
          <button onclick={() => stage({ weekStart: app.plan.weekStart })}>Shuffle</button>
          <button class="primary" onclick={() => planAction('confirm')}>Confirm plan</button>
        {/if}
      {/if}
      <span class="muted" style="align-self:center">{app.session.user.displayName}</span>
      <button onclick={signOut}>Sign out</button>
    {/if}
  </div>
</header>

<Toasts />
<ConfirmDialog />

{#if inApp}
  <nav class="tabs" aria-label="Views">
    {#each tabs as tab (tab.key)}
      <a href="/{tab.key}" aria-current={app.tab === tab.key ? 'page' : undefined}><tab.icon size={18} />{tab.label}</a>
    {/each}
  </nav>
{/if}

<main id="view">
  {#if app.screen === 'loading'}
    <p class="muted">Loading…</p>
  {:else if !inApp}
    <Gate />
  {:else if Other}
    <Other />
  {:else if !app.plan}
    <div class="card empty">
      <h2>Nothing is staged yet</h2>
      <p class="muted">
        {app.recipes.length === 0
          ? 'Add some recipes on the Recipes tab first, then stage a week.'
          : 'Stage a week to see proposed meals, a schedule and a shopping list.'}
      </p>
    </div>
  {:else}
    <PlanTab />
  {/if}
</main>

<!-- Pinned to the bottom of the window. Add a link by adding an <li>; the | between items comes from the CSS.
     Icons are drawn in the text colour. The GitHub mark is inline SVG from GitHub's Octicons (MIT licence), since
     Lucide has no brand logos; the rest come from Lucide like the other icons on the page. -->
<footer class="site-footer">
  <ul class="footer-links">
    <li>
      <a href="https://github.com/Jamiethenextkevinbaconhamm/chef-bot" target="_blank" rel="noopener"
         aria-label="ChefBot on GitHub" title="ChefBot on GitHub">
        <svg viewBox="0 0 16 16" width="18" height="18" aria-hidden="true" focusable="false"><path fill="currentColor" d="M8 0c4.42 0 8 3.58 8 8a8.013 8.013 0 0 1-5.45 7.59c-.4.08-.55-.17-.55-.38 0-.27.01-1.13.01-2.2 0-.75-.25-1.23-.54-1.48 1.78-.2 3.65-.88 3.65-3.95 0-.88-.31-1.59-.82-2.15.08-.2.36-1.02-.08-2.12 0 0-.67-.22-2.2.82-.64-.18-1.32-.27-2-.27-.68 0-1.36.09-2 .27-1.53-1.03-2.2-.82-2.2-.82-.44 1.1-.16 1.92-.08 2.12-.51.56-.82 1.28-.82 2.15 0 3.06 1.86 3.75 3.64 3.95-.23.2-.44.55-.51 1.07-.46.21-1.61.55-2.33-.66-.15-.24-.6-.83-1.23-.82-.67.01-.27.38.01.53.34.19.73.9.82 1.13.16.45.68 1.31 2.69.94 0 .67.01 1.3.01 1.49 0 .21-.15.45-.55.38A7.995 7.995 0 0 1 0 8c0-4.42 3.58-8 8-8Z"/></svg>
      </a>
    </li>
    <li hidden={!inApp}>
      <a href="/docs" aria-label="API reference" title="API reference">
        <CodeXml size={18} />
      </a>
    </li>
  </ul>
</footer>

<style>
  .top { display: flex; flex-wrap: wrap; gap: 1rem; justify-content: space-between; align-items: flex-start; }
  .tabs { display: flex; gap: .25rem; margin: 1.25rem 0 0; border-bottom: 1px solid var(--line); overflow-x: auto; }
  .tabs a { display: inline-flex; align-items: center; gap: .45rem; border-bottom: 3px solid transparent; color: var(--muted);
    padding: .6rem .9rem; text-decoration: none; white-space: nowrap; }
  .tabs a :global(svg) { flex: none; }
  .tabs a:hover { color: var(--ink); }
  .tabs a:focus-visible { outline: 2px solid var(--accent); outline-offset: -2px; }
  .tabs a[aria-current="page"] { color: var(--ink); border-bottom-color: var(--accent); font-weight: 600; }
  /* Pinned to the bottom of the window; the body's bottom padding in styles.css keeps content clear of it. */
  .site-footer { position: fixed; inset: auto 0 0 0; z-index: 10; background: var(--bg); border-top: 1px solid var(--line);
    padding: .45rem 1rem calc(.45rem + env(safe-area-inset-bottom)); }
  .footer-links { list-style: none; margin: 0 auto; padding: 0; max-width: 62rem; display: flex; justify-content: center;
    align-items: center; flex-wrap: wrap; }
  .footer-links li { display: flex; align-items: center; }
  .footer-links li + li:not([hidden])::before { content: "|"; color: var(--line); padding: 0 .6rem; }
  .footer-links a { display: inline-flex; padding: .3rem; border-radius: .4rem; color: var(--muted); }
  .footer-links a:hover { color: var(--ink); background: var(--chip); }
  .footer-links a:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }
</style>
