<script>
  import { app, run, enterApp } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';

  function onsubmit(event) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    run(async () => {
      app.session.user = await api('POST', '/api/account/sign-in', { username: form.get('username'), password: form.get('password') });
      await enterApp();
    });
  }
</script>

<section class="gate">
  <h2>Sign in</h2>
  <form class="stack card" {onsubmit}>
    <label>Username <input id="signin-username" name="username" required autocomplete="username" autocapitalize="none"></label>
    <label>Password <input id="signin-password" name="password" type="password" required autocomplete="current-password"></label>
    <button type="submit" class="primary">Sign in</button>
  </form>
  <p class="muted">No account, or forgot your password? Ask an admin for an invite or a password reset link.</p>
</section>
