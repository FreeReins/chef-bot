<script>
  import { app, run, isAdmin, saveSecurity } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';

  function changePassword(event) {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    run(async () => {
      await api('POST', '/api/account/password', { currentPassword: data.get('current'), newPassword: data.get('next') });
      form.reset();
      app.notes = ['Your password has been changed. Other devices have been signed out.'];
    });
  }

  function saveRules(event) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    run(async () => {
      await saveSecurity({ maxFailedSignIns: Number(data.get('attempts')), lockoutMinutes: Number(data.get('minutes')) });
      app.notes = ['Sign-in rules saved.'];
    });
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
  <form class="stack card" onsubmit={saveRules}>
    <label>Wrong passwords in a row before an account is locked (0 turns lockout off)
      <input id="lockout-attempts" name="attempts" type="number" min="0" max="100" required value={app.security.maxFailedSignIns}>
    </label>
    <label>Minutes an account stays locked
      <input id="lockout-minutes" name="minutes" type="number" min="1" max="1440" required value={app.security.lockoutMinutes}>
    </label>
    <div class="row-actions"><button type="submit">Save</button></div>
  </form>
{/if}
