<script>
  import { app, enterApp } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import AccountForm from './AccountForm.svelte';

  async function submit(account) {
    app.session.user = await api('POST', '/api/account/accept-invite', { token: app.linkToken, ...account });
    await enterApp();
  }
</script>

<AccountForm
  title="Join this household"
  intro="You have been invited to ChefBot. Create an account to see the meal plan, shopping list and pantry."
  button="Join"
  {submit} />
