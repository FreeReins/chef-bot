<!-- Admins only: the household's accounts, invite links and password reset links. A new link is shown once, where it
     was asked for: a password reset link under its person, an invite link under "Invite someone". -->
<script>
  import { app, run, ask, loadPeople } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { dayMonth } from '../lib/format.js';
  import Chip from '../components/Chip.svelte';
  import Trash2 from '@lucide/svelte/icons/trash-2';

  const day = (iso) => dayMonth(new Date(iso));
  const isLocked = (user) => user.lockedUntil && new Date(user.lockedUntil) > new Date();
  const describe = (link) => (link.purpose === 'Invite' ? 'Invite' : `Password reset for @${link.username ?? 'a removed account'}`);
  const canShare = typeof navigator !== 'undefined' && typeof navigator.share === 'function';

  let copied = $state(false);
  let linkInput = $state();

  /**
   * Runs a request that returns a new link, then shows it once so it can be copied. `place` says where: 'invite',
   * or a user's id for their password reset link.
   */
  const issue = (place, label, request) => run(async () => {
    const link = await request();
    const parameter = link.purpose === 'Invite' ? 'invite' : 'reset';
    app.people.newLink = { place, label, url: `${location.origin}/?${parameter}=${link.token}`, expiresAt: link.expiresAt };
    copied = false;
    await loadPeople();
  });

  /** Where a replaced link is shown: under its person for a password reset, otherwise with the invites. */
  const placeOf = (link) => (link.purpose === 'Invite'
    ? 'invite'
    : app.people.users.find((user) => user.username === link.username)?.id ?? 'invite');

  const change = (request) => run(async () => {
    await request();
    app.people.newLink = null;
    await loadPeople();
  });

  async function removeUser(user) {
    if (!(await ask({
      title: `Remove ${user.displayName}?`,
      message: `@${user.username} will no longer be able to sign in. The household's recipes and plans are kept.`,
      confirmLabel: 'Remove',
      danger: true,
    }))) return;
    change(() => api('DELETE', `/api/users/${user.id}`));
  }

  async function replaceLink(link) {
    if (!(await ask({
      title: 'Replace this link?',
      message: 'The old link stops working, and a new one is shown for you to send instead.',
      confirmLabel: 'Replace link',
    }))) return;
    issue(placeOf(link), describe(link), () => api('POST', `/api/links/${link.id}/regenerate`));
  }

  async function deleteLink(link) {
    if (!(await ask({
      title: 'Delete this link?',
      message: 'It stops working straight away. You can make a new one at any time.',
      confirmLabel: 'Delete link',
      danger: true,
    }))) return;
    change(() => api('DELETE', `/api/links/${link.id}`));
  }

  async function copyLink() {
    try {
      await navigator.clipboard.writeText(linkInput.value);
      copied = true;
    } catch {
      linkInput.select();
    }
  }

  function shareLink() {
    navigator.share({ title: 'ChefBot', url: app.people.newLink.url }).catch(() => {});
  }
</script>

{#snippet newLink(place)}
  {#if app.people.newLink?.place === place}
    {@const link = app.people.newLink}
    <div class="new-link">
      <p class="new-link-title">{link.label}</p>
      <div class="link-box">
        <input id="new-link" readonly value={link.url} aria-label={link.label} bind:this={linkInput}>
        <button class="primary" onclick={copyLink}>{copied ? 'Copied' : 'Copy'}</button>
        {#if canShare}<button onclick={shareLink}>Share</button>{/if}
        <button onclick={() => (app.people.newLink = null)}>Done</button>
      </div>
      <p class="muted">
        Send this link to them. It works once and expires {day(link.expiresAt)}. It won't be shown again; if it is
        lost, use Replace link under Pending links.
      </p>
    </div>
  {/if}
{/snippet}

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
                <button class="small" onclick={() => issue(user.id, `Password reset link for @${user.username}`,
                  () => api('POST', `/api/users/${user.id}/password-reset`))}>Reset password</button>
                {#if !isYou}
                  <button class="icon remove" aria-label="Remove @{user.username}" title="Remove"
                          onclick={() => removeUser(user)}><Trash2 size={16} /></button>
                {/if}
              </div>
            </div>
            <div class="chips">
              {#if isYou}<Chip>You</Chip>{/if}
              {#if user.isAdmin}<Chip tone="good">Admin</Chip>{/if}
              {#if isLocked(user)}<Chip tone="bad">Locked after too many tries</Chip>{/if}
              <span class="muted joined">Joined {day(user.createdAt)}</span>
            </div>
            {@render newLink(user.id)}
          </li>
        {/each}
      </ul>
    </div>
  </section>

  <section>
    <h2>Invite someone</h2>
    <div class="card">
      <p class="muted">An invite link lets one person create their own account. It works once and expires after 7 days.</p>
      <div class="row-actions" style="margin-top:.5rem">
        <button class="primary" onclick={() => issue('invite', 'Invite link', () => api('POST', '/api/invites'))}>Create invite link</button>
      </div>
      {@render newLink('invite')}
    </div>

    {#if app.people.links.length}
      <h2>Pending links ({app.people.links.length})</h2>
      <p class="muted">Links sent but not used yet.</p>
      <div class="card">
        <ul class="plain">
          {#each app.people.links as link (link.id)}
            <li class="row">
              <div>
                <strong>{describe(link)}</strong>
                <span class="muted" style="display:block">Expires {day(link.expiresAt)}</span>
              </div>
              <div class="row-actions">
                <button class="small" aria-label="Replace link: {describe(link)}" onclick={() => replaceLink(link)}>Replace link</button>
                <button class="small" aria-label="Delete link: {describe(link)}" onclick={() => deleteLink(link)}>Delete</button>
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
  .chips { align-items: center; }
  .joined { font-size: .8rem; }
  .new-link { margin-top: .6rem; padding: .6rem .75rem; border: 1px solid var(--accent); border-radius: .4rem; }
  .new-link-title { margin: 0; font-weight: 600; font-size: .95rem; }
  .new-link .muted { margin-top: .4rem; }
  .link-box { display: flex; flex-wrap: wrap; gap: .5rem; margin-top: .4rem; }
  .link-box input { flex: 1 1 16rem; font-family: ui-monospace, Menlo, Consolas, monospace; font-size: .85rem; }
  /* At least 16px on phones, so iPhones don't zoom in on it. */
  @media (max-width: 40rem) { .link-box input { font-size: 1rem; } }
</style>
