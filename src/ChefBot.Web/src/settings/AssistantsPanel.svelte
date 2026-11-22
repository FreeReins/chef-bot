<!-- Settings, AI assistants: the MCP address assistants connect to, and the API tokens they sign in with. Each person
     makes and revokes their own tokens; a token acts as the person who made it. -->
<script>
  import { run, toast, ask } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { pending } from '../lib/pending.js';
  import { copyText } from '../lib/exporting.js';
  import Copy from '@lucide/svelte/icons/copy';

  const address = `${location.origin}/mcp`;

  let tokens = $state(null);
  /** The token just made: its secret is shown until the panel is left, since it can't be shown again. */
  let issued = $state(null);

  run(async () => { tokens = await api('GET', '/api/tokens'); });

  const command = $derived(`claude mcp add --transport http chefbot ${address} --header "Authorization: Bearer ${issued?.token ?? '<token>'}"`);
  const made = (token) => new Date(token.createdAt).toLocaleDateString(undefined, { dateStyle: 'medium' });

  const copy = (text, what) => run(async () => {
    await copyText(text);
    toast(`The ${what} was copied.`);
  });

  function create(event) {
    event.preventDefault();
    const form = event.currentTarget;
    const name = new FormData(form).get('name');
    pending(event, () => run(async () => {
      issued = await api('POST', '/api/tokens', { name });
      tokens = [{ id: issued.id, name: issued.name, createdAt: issued.createdAt }, ...tokens];
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
<p class="muted">
  Assistants that support MCP, such as Claude, can read your recipes, plans, pantry and shopping list, and change
  them for you: plan a week, swap or move meals, mark meals cooked, and add to the pantry or shopping list. Give
  each assistant its own token. A token acts as you until you revoke it, even after you change your password.
</p>

<div class="card stack">
  <label>MCP address
    <input id="mcp-address" readonly value={address} onfocus={(event) => event.currentTarget.select()}>
  </label>
  <div class="row-actions">
    <button type="button" class="with-icon" onclick={() => copy(address, 'MCP address')}><Copy size={16} />Copy address</button>
  </div>
  <p class="muted">
    Assistants send the token as the header <code>Authorization: Bearer &lt;token&gt;</code>. They need to reach
    ChefBot at this address, so an assistant running in the cloud can't use it unless ChefBot is reachable from the
    internet.
  </p>
</div>

<p class="muted">
  Building your own tools? Everything ChefBot does is also a JSON API, described in the
  <a href="/docs" target="_blank" rel="noopener">API reference</a>. Scripts sign in with a password rather than a token.
</p>

<h3>Your tokens</h3>
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
        <li class="card">
          <span><strong>{token.name}</strong> <span class="muted">made {made(token)}</span></span>
          <button type="button" class="bad-text" onclick={() => revoke(token)} aria-label="Revoke {token.name}">Revoke</button>
        </li>
      {/each}
    </ul>
  {/if}
{/if}

<form class="stack card" onsubmit={create}>
  <label>Name for a new token, such as the assistant it is for
    <input id="token-name" name="name" required maxlength="80" autocomplete="off">
  </label>
  <div class="row-actions"><button type="submit">Make a token</button></div>
</form>

<style>
  #mcp-address, #token-secret, #token-command { font-family: ui-monospace, monospace; font-size: .85rem; }
  .issued { border-color: var(--accent); }
  .tokens { list-style: none; padding: 0; margin: 0 0 1rem; display: grid; gap: .5rem; }
  .tokens li { display: flex; align-items: center; justify-content: space-between; gap: 1rem; flex-wrap: wrap; }
</style>
