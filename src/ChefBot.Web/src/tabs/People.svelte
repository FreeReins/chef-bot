<!-- Admins only: the household's accounts, invite links and password reset links. -->
<script>
  import { app, run, loadPeople } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { dayMonth } from '../lib/format.js';
  import Chip from '../components/Chip.svelte';
  import Trash2 from '@lucide/svelte/icons/trash-2';
  import UserRoundStar from '@lucide/svelte/icons/user-round-star';

  const day = (iso) => dayMonth(new Date(iso));
  const isLocked = (user) => user.lockedUntil && new Date(user.lockedUntil) > new Date();
  const describe = (link) => (link.purpose === 'Invite' ? 'Invite' : `Password reset for @${link.username ?? 'a removed account'}`);

  let copied = $state(false);
  let linkInput = $state();

  /** Runs a request that returns a new link, then shows it once so it can be copied. */
  const issue = (label, request) => run(async () => {
    const link = await request();
    const parameter = link.purpose === 'Invite' ? 'invite' : 'reset';
    app.people.newLink = { label, url: `${location.origin}/?${parameter}=${link.token}`, expiresAt: link.expiresAt };
    copied = false;
    await loadPeople();
  });

  const change = (request) => run(async () => {
    await request();
    app.people.newLink = null;
    await loadPeople();
  });

  async function copyLink() {
    try {
      await navigator.clipboard.writeText(linkInput.value);
      copied = true;
    } catch {
      linkInput.select();
    }
  }
</script>

<div class="columns">
  <section>
    <h2>People ({app.people.users.length})</h2>
    <div class="card">
      <ul class="plain">
        {#each app.people.users as user (user.id)}
          {@const isYou = user.id === app.session.user.id}
          <li>
            <div class="person-head">
              <span class="clip" title="{user.displayName} @{user.username}">
                <strong>{user.displayName}</strong> <span class="muted">@{user.username}</span>
              </span>
              <div class="person-actions">
                {#if isLocked(user)}
                  <button class="small" onclick={() => change(() => api('POST', `/api/users/${user.id}/unlock`))}>Unlock</button>
                {/if}
                <button class="small" onclick={() => issue(`Password reset link for @${user.username}`,
                  () => api('POST', `/api/users/${user.id}/password-reset`))}>Reset password</button>
                {#if !isYou}
                  <button class="icon remove" aria-label="Remove @{user.username}" title="Remove"
                          onclick={() => change(() => api('DELETE', `/api/users/${user.id}`))}><Trash2 size={16} /></button>
                {:else}
                  <!-- You can't remove yourself, so this marks your own admin account in the trash can's place. -->
                  <span class="row-icon" role="img" aria-label="Administrator" title="Administrator"><UserRoundStar size={16} /></span>
                {/if}
              </div>
            </div>
            <div class="chips">
              {#if user.isAdmin}<Chip tone="good">Admin</Chip>{/if}
              {#if isLocked(user)}<Chip tone="bad">Locked</Chip>{/if}
              {#if isYou}<Chip>You</Chip>{/if}
              <Chip>Joined {day(user.createdAt)}</Chip>
            </div>
          </li>
        {/each}
      </ul>
    </div>
  </section>

  <section>
    <h2>Invite someone</h2>
    <div class="card">
      <p class="muted">An invite link lets one person create an account. It works once and expires after 7 days.</p>
      <div class="row-actions" style="margin-top:.5rem">
        <button class="primary" onclick={() => issue('Invite link', () => api('POST', '/api/invites'))}>Create invite link</button>
      </div>
    </div>

    {#if app.people.newLink}
      {@const newLink = app.people.newLink}
      <div class="card">
        <h3>{newLink.label}</h3>
        <div class="link-box">
          <input id="new-link" readonly value={newLink.url} aria-label={newLink.label} bind:this={linkInput}>
          <button onclick={copyLink}>{copied ? 'Copied' : 'Copy'}</button>
        </div>
        <p class="muted">
          Copy this link now: it is only shown once. It expires {day(newLink.expiresAt)}.
          If you lose it, use Regenerate below to replace it.
        </p>
      </div>
    {/if}

    {#if app.people.links.length}
      <h2>Unused links ({app.people.links.length})</h2>
      <div class="card">
        <ul class="plain">
          {#each app.people.links as link (link.id)}
            <li class="row">
              <div>
                <strong>{describe(link)}</strong>
                <span class="muted" style="display:block">Created {day(link.createdAt)}, expires {day(link.expiresAt)}</span>
              </div>
              <div class="row-actions">
                <button class="small" title="Stop the old link working and show a new one"
                        onclick={() => issue(describe(link), () => api('POST', `/api/links/${link.id}/regenerate`))}>Regenerate</button>
                <button class="small" onclick={() => change(() => api('DELETE', `/api/links/${link.id}`))}>Revoke</button>
              </div>
            </li>
          {/each}
        </ul>
      </div>
    {/if}
  </section>
</div>

<style>
  /* Name on the left, cut off with "…" so it never pushes the buttons onto another line. */
  .person-head { display: flex; align-items: center; gap: .75rem; }
  .person-head .clip { flex: 1 1 auto; }
  .person-actions { display: flex; align-items: center; gap: .4rem; flex: none; }
  .row-icon { display: inline-flex; padding: .2rem; line-height: 1; color: var(--muted); }
  .link-box { display: flex; flex-wrap: wrap; gap: .5rem; margin-top: .5rem; }
  .link-box input { flex: 1 1 16rem; font-family: ui-monospace, Menlo, Consolas, monospace; font-size: .85rem; }
</style>
