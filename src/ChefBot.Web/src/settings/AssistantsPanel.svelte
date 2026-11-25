<!-- Settings, AI assistants: for admins, the AI models ChefBot itself uses (AiModelsSection); for everyone, the MCP
     address assistants connect to, and the API tokens they sign in with. Each person makes and revokes their own
     tokens; a token acts as the person who made it. -->
<script>
  import { run, toast, ask, isAdmin } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { pending } from '../lib/pending.js';
  import { copyText } from '../lib/exporting.js';
  import Copy from '@lucide/svelte/icons/copy';
  import AiModelsSection from './AiModelsSection.svelte';

  const address = `${location.origin}/mcp`;

  let tokens = $state(null);
  /** The token just made: its secret is shown until the panel is left, since it can't be shown again. */
  let issued = $state(null);

  run(async () => { tokens = await api('GET', '/api/tokens'); });

  const command = $derived(`claude mcp add --transport http chefbot ${address} --header "Authorization: Bearer ${issued?.token ?? '<token>'}"`);
  const day = (when) => new Date(when).toLocaleDateString(undefined, { dateStyle: 'medium' });
  /** How long a token has, in days, by the choice offered: 0 lasts until it is revoked. */
  const LASTS = [{ days: 90, label: '90 days' }, { days: 30, label: '30 days' }, { days: 365, label: 'A year' }, { days: 0, label: 'Until I revoke it' }];
  const expired = (token) => token.expiresAt && new Date(token.expiresAt) <= new Date();

  const copy = (text, what) => run(async () => {
    await copyText(text);
    toast(`The ${what} was copied.`);
  });

  function create(event) {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    const days = Number(data.get('lasts'));
    const request = { name: data.get('name'), expiresInDays: days > 0 ? days : null, readOnly: data.get('readOnly') === 'on' };
    pending(event, () => run(async () => {
      issued = await api('POST', '/api/tokens', request);
      tokens = [{ id: issued.id, name: issued.name, createdAt: issued.createdAt, expiresAt: issued.expiresAt,
        lastUsedAt: null, readOnly: issued.readOnly }, ...tokens];
      form.reset();
      toast('The token was made. Copy it now: it won\'t be shown again.');
    }));
  }

  async function revoke(token) {
    if (!(await ask({ title: `Revoke ${token.name}?`, message: 'Assistants using this token can no longer reach ChefBot.',
      confirmLabel: 'Revoke', danger: true }))) return;
    run(async () => {
      await api('DELETE', `/api/tokens/${token.id}`);
      tokens = tokens.filter((other) => other.id !== token.id);
      if (issued?.id === token.id) issued = null;
      toast(`${token.name} was revoked.`);
    });
  }
</script>

<h2>AI assistants</h2>

{#if isAdmin()}
  <section aria-labelledby="ai-models-heading" class="group">
    <h3 id="ai-models-heading">ChefBot's AI models <span class="chip">Admins</span></h3>
    <AiModelsSection />
  </section>
{/if}

<section aria-labelledby="connect-heading" class="group">
  <h3 id="connect-heading">Connect your assistant</h3>
  <p class="muted">
    Assistants that support MCP, such as Claude, can read your recipes, plans, pantry and shopping list, and change
    them for you: plan a week, swap or move meals, mark meals cooked, and add to the pantry or shopping list.
  </p>

  <div class="card stack">
    <h4>1. ChefBot's address</h4>
    <label>MCP address
      <input id="mcp-address" readonly value={address} onfocus={(event) => event.currentTarget.select()}>
    </label>
    <div class="row-actions">
      <button type="button" class="with-icon" onclick={() => copy(address, 'MCP address')}><Copy size={16} />Copy address</button>
    </div>
    <p class="muted small">
      The assistant has to reach ChefBot at this address, so one running in the cloud can't use it unless ChefBot is
      reachable from the internet.
    </p>
  </div>

  <div class="card stack">
    <h4>2. Your tokens</h4>
    <p class="muted small">
      Give each assistant its own token. It sends it as the header <code>Authorization: Bearer &lt;token&gt;</code>.
      A token acts as you until it expires or you revoke it. A read-only one can look things up but not change them:
      give an assistant more only when it needs it.
    </p>

    {#if issued}
      <div class="card stack issued" role="status">
        <p><strong>{issued.name}</strong> is ready. Copy its token now: it won't be shown again.</p>
        <label>Token
          <input id="token-secret" readonly value={issued.token} onfocus={(event) => event.currentTarget.select()}>
        </label>
        <label>To add ChefBot to Claude Code, run
          <input id="token-command" readonly value={command} onfocus={(event) => event.currentTarget.select()}>
        </label>
        <div class="row-actions">
          <button type="button" class="with-icon primary" onclick={() => copy(issued.token, 'token')}><Copy size={16} />Copy token</button>
          <button type="button" class="with-icon" onclick={() => copy(command, 'command')}><Copy size={16} />Copy command</button>
          <button type="button" onclick={() => { issued = null; }}>Done</button>
        </div>
      </div>
    {/if}

    {#if tokens}
      {#if tokens.length === 0}
        <p class="muted">You have no tokens yet.</p>
      {:else}
        <ul class="tokens">
          {#each tokens as token (token.id)}
            <li>
              <span>
                <strong>{token.name}</strong>
                {#if token.readOnly}<span class="chip">Read-only</span>{/if}
                <span class="muted small-line">
                  made {day(token.createdAt)},
                  {#if !token.expiresAt}lasts until revoked{:else if expired(token)}<span class="bad-text">expired {day(token.expiresAt)}</span>{:else}expires {day(token.expiresAt)}{/if},
                  {token.lastUsedAt ? `last used ${day(token.lastUsedAt)}` : 'not used yet'}
                </span>
              </span>
              <button type="button" class="bad-text" onclick={() => revoke(token)} aria-label="Revoke {token.name}">Revoke</button>
            </li>
          {/each}
        </ul>
      {/if}
    {/if}

    <form class="stack" onsubmit={create}>
      <label>Name for a new token, such as the assistant it is for
        <input id="token-name" name="name" required maxlength="80" autocomplete="off">
      </label>
      <label>Lasts
        <select id="token-lasts" name="lasts">
          {#each LASTS as choice (choice.days)}<option value={choice.days}>{choice.label}</option>{/each}
        </select>
      </label>
      <label class="check"><input id="token-read-only" name="readOnly" type="checkbox"> Read-only: it can look things up but not change them</label>
      <div class="row-actions"><button type="submit">Make a token</button></div>
    </form>
  </div>

  <p class="muted small">
    Building your own tools? Everything ChefBot does is also a JSON API, described in the
    <a href="/docs" target="_blank" rel="noopener">API reference</a>. Scripts sign in with a password rather than a token.
  </p>
</section>

<style>
  .group + .group { margin-top: 2rem; padding-top: 1rem; border-top: 1px solid var(--line); }
  h3 { display: flex; align-items: center; gap: .5rem; font-size: 1.15rem; margin: 1rem 0 .25rem; }
  h4 { margin: 0; font-size: 1rem; }
  .card + .card { margin-top: .75rem; }
  .small { font-size: .9rem; margin: 0; }
  #mcp-address, #token-secret, #token-command { font-family: ui-monospace, monospace; font-size: .85rem; }
  .issued { border-color: var(--accent); }
  .tokens { list-style: none; padding: 0; margin: 0; display: grid; gap: .25rem; }
  .small-line { display: block; font-size: .9rem; }
  .tokens li { display: flex; align-items: center; justify-content: space-between; gap: 1rem; flex-wrap: wrap;
    padding: .4rem 0; border-bottom: 1px solid var(--line); }
</style>
