<script>
  import { run, openSamples } from '../lib/app.svelte.js';
  import HttpsPanel from '../settings/HttpsPanel.svelte';
</script>

<section class="gate">
  <p class="steps">Setup, step 2 of 3</p>
  <h2>Secure connection</h2>
  <p class="muted">HTTPS keeps passwords private on your network. You can change any of this later on the Settings tab.</p>
  <HttpsPanel />
  <div class="row-actions" style="margin-top:1rem">
    <button class="primary" onclick={() => run(openSamples)}>Continue</button>
  </div>
</section>
