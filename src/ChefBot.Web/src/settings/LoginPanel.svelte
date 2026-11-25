<script>
  import { app, run, isAdmin, saveSecurity, toast, ask } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { pending, pendingClick } from '../lib/pending.js';
  import { autosave } from '../lib/autosave.svelte.js';
  import SaveStatus from '../components/SaveStatus.svelte';

  function changePassword(event) {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    const revokeApiTokens = data.get('revokeTokens') === 'on';
    pending(event, () => run(async () => {
      await api('POST', '/api/account/password', { currentPassword: data.get('current'), newPassword: data.get('next'), revokeApiTokens });
      form.reset();
      toast(revokeApiTokens
        ? 'Your password has been changed. Other devices have been signed out and your API tokens revoked.'
        : 'Your password has been changed. Other devices have been signed out.');
    }));
  }

  async function signOutEverywhere() {
    if (!(await ask({ title: 'Sign out everywhere?', message: 'Every other device you are signed in on, such as a lost phone, is signed out. This one stays signed in, and API tokens keep working.',
      confirmLabel: 'Sign out everywhere' }))) return;
    return run(async () => {
      await api('POST', '/api/account/sign-out-everywhere');
      toast('You have been signed out on every other device.');
    });
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
  <label class="check"><input id="password-revoke-tokens" name="revokeTokens" type="checkbox"> Also revoke my API tokens, so AI assistants need new ones</label>
  <div class="row-actions"><button type="submit">Change password</button></div>
</form>

<h2>Your devices</h2>
<div class="stack card">
  <p class="muted">Lost a phone, or signed in on someone else's computer? Sign out there without changing your password.</p>
  <div class="row-actions"><button type="button" onclick={pendingClick(() => signOutEverywhere())}>Sign out everywhere</button></div>
</div>

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
