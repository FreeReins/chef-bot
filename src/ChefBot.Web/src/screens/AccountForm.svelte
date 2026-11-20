<!-- Shared by the setup wizard and the invite page: both create an account. -->
<script>
  import { run } from '../lib/app.svelte.js';

  /** submit receives { displayName, username, password }. */
  let { step = null, title, intro, button, submit } = $props();

  function onsubmit(event) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    run(() => submit({
      displayName: form.get('displayName'),
      username: form.get('username'),
      password: form.get('password'),
    }));
  }
</script>

<section class="gate">
  {#if step}<p class="steps">{step}</p>{/if}
  <h2>{title}</h2>
  <p class="muted">{intro}</p>
  <form class="stack card" {onsubmit}>
    <label>Your name <input id="account-name" name="displayName" required autocomplete="name"></label>
    <label>Username
      <input id="account-username" name="username" required minlength="3" maxlength="32"
             pattern={'[A-Za-z0-9._\\-]+'} autocomplete="username" autocapitalize="none">
    </label>
    <label>Password (at least 8 characters)
      <input id="account-password" name="password" type="password" required minlength="8" autocomplete="new-password">
    </label>
    <button type="submit" class="primary">{button}</button>
  </form>
</section>
