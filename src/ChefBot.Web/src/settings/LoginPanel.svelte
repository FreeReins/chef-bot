<script>
  import { app, run, isAdmin, saveSecurity, toast } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { pending } from '../lib/pending.js';
  import { autosave } from '../lib/autosave.svelte.js';
  import SaveStatus from '../components/SaveStatus.svelte';

  function changePassword(event) {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    pending(event, () => run(async () => {
      await api('POST', '/api/account/password', { currentPassword: data.get('current'), newPassword: data.get('next') });
      form.reset();
      toast('Your password has been changed. Other devices have been signed out.');
    }));
  }

  let rules = $state();

  // Saved shortly after each change; a field is changed once it is left or Enter is pressed.
  const saver = autosave(async () => {
    const data = new FormData(rules);
    await saveSecurity({ maxFailedSignIns: Number(data.get('attempts')), lockoutMinutes: Number(data.get('minutes')) });
  }, { validate: () => rules?.checkValidity() ?? false });

  function submitRules(event) {
    event.preventDefault();
    saver.retry();
  }
</script>

<h2>Your password</h2>
<form class="stack card" onsubmit={changePassword}>
  <label>Current password
    <input id="password-current" name="current" type="password" required autocomplete="current-password">
  </label>
  <label>New password (at least 8 characters)
    <input id="password-next" name="next" type="password" required minlength="8" autocomplete="new-password">
  </label>
  <div class="row-actions"><button type="submit">Change password</button></div>
</form>

{#if isAdmin() && app.security}
  <h2>Sign-in lockout</h2>
  <form class="stack card" bind:this={rules} onchange={saver.schedule} onsubmit={submitRules}>
    <label>Wrong passwords in a row before an account is locked (0 turns lockout off)
      <input id="lockout-attempts" name="attempts" type="number" min="0" max="100" required value={app.security.maxFailedSignIns}>
    </label>
    <label>Minutes an account stays locked
      <input id="lockout-minutes" name="minutes" type="number" min="1" max="1440" required value={app.security.lockoutMinutes}>
    </label>
    <SaveStatus {saver} />
  </form>
{/if}
