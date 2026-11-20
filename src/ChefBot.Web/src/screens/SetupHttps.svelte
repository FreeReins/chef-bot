<script>
  import { run, openSetupRecipes } from '../lib/app.svelte.js';
  import SetupProgress from '../components/SetupProgress.svelte';
  import HttpsPanel from '../settings/HttpsPanel.svelte';
</script>

<section class="gate">
  <SetupProgress current={2} />
  <h2>Secure connection</h2>
  <p class="muted">HTTPS keeps passwords private on your network. You can change any of this later on the Settings tab.</p>
  <HttpsPanel />
  <div class="row-actions" style="margin-top:1rem">
    <button class="primary" onclick={() => run(openSetupRecipes)}>Continue</button>
  </div>
</section>
