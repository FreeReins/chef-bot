<script>
  import { app, loadSystem, openSamples } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import AccountForm from './AccountForm.svelte';

  async function submit(account) {
    app.session.user = await api('POST', '/api/setup/account', account);
    app.session.setupRequired = false;
    await loadSystem();
    if (app.https.enabled) {
      app.screen = 'setup-https';
    } else {
      await openSamples();
    }
  }
</script>

<AccountForm
  step="Setup, step 1 of 3"
  title="Create your account"
  intro="Welcome to ChefBot. This first account is the admin: it can invite the rest of the household later."
  button="Create account"
  {submit} />
