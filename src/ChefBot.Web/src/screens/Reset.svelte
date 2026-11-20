<script>
  import { app, run, enterApp } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';

  function onsubmit(event) {
    event.preventDefault();
    const password = new FormData(event.currentTarget).get('password');
    run(async () => {
      app.session.user = await api('POST', '/api/account/reset-password', { token: app.linkToken, password });
      await enterApp();
      app.notes = ['Your password has been changed.'];
    });
  }
</script>

<section class="gate">
  <h2>Choose a new password</h2>
  <p class="muted">{app.link?.username ? `For the account @${app.link.username}.` : ''}</p>
  <form class="stack card" {onsubmit}>
    <label>New password (at least 8 characters)
      <input id="reset-password" name="password" type="password" required minlength="8" autocomplete="new-password">
    </label>
    <button type="submit" class="primary">Save password</button>
  </form>
</section>
