<!-- Setup step one: create the first account, which is the admin. Coming back to this step after the account
     exists shows who is signed in instead, since setup only ever creates one account. -->
<script>
  import { app, loadSystem, moveSetup } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import AccountForm from './AccountForm.svelte';
  import SetupNav from '../components/SetupNav.svelte';
  import SetupProgress from '../components/SetupProgress.svelte';

  const user = $derived(app.session.user);

  async function submit(account) {
    app.session.user = await api('POST', '/api/setup/account', account);
    app.session.setupRequired = false;
    await loadSystem();
    await moveSetup(1);
  }
</script>

{#if user}
  <section class="gate">
    <SetupProgress />
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
    setup
    title="Create your account"
    intro="Welcome to ChefBot. This first account is the admin: it can invite the rest of the household later."
    button="Create account"
    {submit} />
{/if}
