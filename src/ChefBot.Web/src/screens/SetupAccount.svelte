<!-- Setup step one: create the first account, which is the admin. Once it is made, a welcome card says so and setup
     moves on by itself after a moment, or at once with Continue. Coming back to this step later shows who is signed
     in instead, since setup only ever creates one account. -->
<script>
  import { onDestroy } from 'svelte';
  import { app, loadSystem, moveSetup } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { pendingClick } from '../lib/pending.js';
  import { celebrate, rise } from '../lib/motion.js';
  import AccountForm from './AccountForm.svelte';
  import SetupNav from '../components/SetupNav.svelte';
  import ChefHat from '@lucide/svelte/icons/chef-hat';

  /** How long the welcome stays before setup moves on by itself, in milliseconds. */
  const WELCOME_MS = 2200;

  const user = $derived(app.session.user);

  let welcomed = $state(false); // the account was made just now, on this visit to the step
  let ready = $state(false);    // what the next steps need has loaded, so Continue can go on
  let timer = null;

  async function submit(account) {
    const created = await api('POST', '/api/setup/account', account);
    welcomed = true; // before the user is set, so the welcome shows rather than the "already made" card
    app.session.user = created;
    app.session.setupRequired = false;
    await loadSystem();
    ready = true;
    timer = setTimeout(next, WELCOME_MS);
  }

  function next() {
    clearTimeout(timer);
    timer = null;
    return moveSetup(1);
  }

  onDestroy(() => clearTimeout(timer));

  /** The welcome card arriving: focus moves to its heading, since the form that had it is gone, and the badge bursts. */
  function welcome(node) {
    node.querySelector('h2')?.focus({ preventScroll: true });
    setTimeout(() => celebrate(node.querySelector('.badge')), 250);
  }
</script>

{#if user && welcomed}
  <section class="gate" in:rise={{ duration: 260 }}>
    <div class="card welcome" use:welcome>
      <div class="badge" aria-hidden="true">
        <ChefHat size={36} />
        <svg class="check" viewBox="0 0 24 24"><circle cx="12" cy="12" r="11" /><path d="M6.5 12.5l3.6 3.6 7.4-7.6" /></svg>
      </div>
      <h2 tabindex="-1">Welcome, {user.displayName}.</h2>
      <p class="muted">Your admin account is ready. Next come a few quick choices about how you plan and shop: setup carries on in a moment, or press Continue.</p>
      <!-- A fuse that burns down to the next step, so moving on by itself is no surprise. -->
      <div class="fuse" class:lit={ready} aria-hidden="true" style:--fuse="{WELCOME_MS}ms"><span></span></div>
      <button type="button" class="primary" disabled={!ready} onclick={pendingClick(next)}>Continue</button>
    </div>
  </section>
{:else if user}
  <section class="gate">
    <h2>Your account</h2>
    <p class="muted">Your admin account is ready. Change its password later on the Settings tab.</p>
    <div class="card">
      <strong>{user.displayName}</strong>
      <p class="muted">Signed in as {user.username}</p>
    </div>
    <SetupNav>Account created</SetupNav>
  </section>
{:else}
  <AccountForm
    title="Create your account"
    intro="Welcome to ChefBot. This first account is the admin: it can invite the rest of the household later."
    button="Create account"
    {submit} />
{/if}

<style>
  .welcome { display: grid; justify-items: center; gap: .6rem; text-align: center; padding: 2rem 1.5rem 1.5rem; margin-top: 1rem; }
  .welcome h2 { margin: .5rem 0 0; font-size: 1.35rem; }
  .welcome h2:focus:not(:focus-visible) { outline: none; }
  .welcome p { max-width: 24rem; }
  .welcome button { margin-top: .25rem; min-width: 9rem; }
  .badge { position: relative; display: grid; place-items: center; width: 5rem; height: 5rem; border-radius: 50%;
    color: var(--accent-2); background: color-mix(in srgb, var(--accent-2) 14%, var(--panel)); }
  .check { position: absolute; right: -.2rem; bottom: -.2rem; width: 1.9rem; height: 1.9rem; }
  .check circle { fill: var(--accent); stroke: var(--panel); stroke-width: 2; }
  .check path { fill: none; stroke: var(--accent-ink); stroke-width: 2.6; stroke-linecap: round; stroke-linejoin: round; }
  .fuse { width: min(12rem, 60%); height: 3px; border-radius: 3px; background: var(--line); overflow: hidden; }
  .fuse span { display: block; height: 100%; background: var(--accent); transform-origin: left; transform: scaleX(0); }
  .fuse.lit span { animation: burn var(--fuse) linear forwards; }
  @keyframes burn { from { transform: scaleX(0); } to { transform: scaleX(1); } }
  @media (prefers-reduced-motion: no-preference) {
    .badge { animation: arrive .5s cubic-bezier(.3, 1.5, .5, 1); }
    .badge :global(svg:first-child) { animation: tip .7s ease-in-out .35s; transform-origin: 50% 85%; }
    .check { animation: arrive .4s cubic-bezier(.3, 1.6, .5, 1) .2s backwards; }
    .check path { stroke-dasharray: 20; stroke-dashoffset: 20; animation: draw .35s ease-out .45s forwards; }
  }
  /* With less motion the fuse doesn't burn; the text and the Continue button say what happens. */
  @media (prefers-reduced-motion: reduce) { .fuse { display: none; } }
  @keyframes arrive { from { transform: scale(.4); opacity: 0; } }
  @keyframes tip { 30% { transform: rotate(-12deg); } 60% { transform: rotate(8deg); } }
  @keyframes draw { to { stroke-dashoffset: 0; } }
</style>
