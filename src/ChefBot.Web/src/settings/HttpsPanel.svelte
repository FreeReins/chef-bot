<!-- How the browser reaches ChefBot securely. Shown in setup step 2 and in Settings. -->
<script>
  import { app, run, isAdmin, saveSecurity, httpsUrl, toast } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { pending } from '../lib/pending.js';
  import Chip from '../components/Chip.svelte';

  const https = $derived(app.https);
  const covered = $derived(https ? [...new Set([...https.hostnames, location.hostname])].join(', ') : '');

  function regenerateCertificate(event) {
    event.preventDefault();
    const hostnames = String(new FormData(event.currentTarget).get('hostnames')).split(',').map((name) => name.trim()).filter(Boolean);
    pending(event, () => run(async () => {
      app.https = await api('POST', '/api/system/https/certificate', { hostnames });
      toast('A new certificate is in use. Your browser may ask you to accept it again.');
    }));
  }

  async function setRequireHttps(event) {
    const box = event.currentTarget;
    await run(() => saveSecurity({ requireHttps: box.checked }));
    box.checked = app.security.requireHttps; // back to the saved value if saving failed
  }
</script>

{#if !https?.enabled}
  <div class="card">
    {#if https?.isSecureNow}
      <p><Chip tone="good">Secure</Chip> HTTPS is handled by a reverse proxy in front of ChefBot.</p>
      <p class="muted">Certificates and redirecting plain HTTP are set up in the proxy.</p>
    {:else}
      <p>HTTPS is switched off.</p>
      <p class="muted">
        Set Https__Mode to SelfSigned (or Certificate) and restart to turn it on. Behind a reverse proxy
        that adds HTTPS itself, leave it off and list the proxy in Https__TrustedProxies.
      </p>
    {/if}
  </div>
{:else}
  <div class="card">
    {#if https.isSecureNow}
      <p><Chip tone="good">Secure</Chip> You are using HTTPS.</p>
    {:else}
      <p><Chip tone="warn">Not secure</Chip> You are using plain HTTP. <a href={httpsUrl()}>Open ChefBot over HTTPS</a></p>
    {/if}
    {#if https.mode === 'SelfSigned'}
      <p class="muted">
        ChefBot made its own certificate, so the first visit over HTTPS shows a browser warning.
        Choose to continue, or install the certificate on your devices to make the warning go away.
      </p>
    {/if}
    <p class="muted">{https.expiresOn ? `The certificate is valid until ${new Date(https.expiresOn).toLocaleDateString()}.` : ''}</p>
    <div class="row-actions" style="margin:.5rem 0">
      <a href="/api/system/https/certificate" download="chefbot.crt">Download the certificate</a>
    </div>
    {#if isAdmin() && https.canRegenerate}
      <form class="stack" onsubmit={regenerateCertificate}>
        <label>Names and addresses people use to reach ChefBot, separated by commas
          <input id="https-hostnames" name="hostnames" value={covered} placeholder="chefbot.local, 192.168.1.20">
        </label>
        <div class="row-actions"><button type="submit">Create a certificate for these names</button></div>
      </form>
    {/if}
    {#if isAdmin() && app.security}
      <label class="check" style="margin-top:.5rem">
        <input id="require-https" type="checkbox" class="switch" checked={app.security.requireHttps} onchange={setRequireHttps}>
        <span>
          Always use HTTPS
          <span class="muted" style="display:block">Sends anyone arriving over plain HTTP to the HTTPS address. Turn it on once HTTPS works in your browser.</span>
        </span>
      </label>
    {/if}
  </div>
{/if}
