<script>
  import SetupNav from '../components/SetupNav.svelte';
  import SetupProgress from '../components/SetupProgress.svelte';
  import HttpsPanel from '../settings/HttpsPanel.svelte';
</script>

<section class="gate">
  <SetupProgress />
  <h2>Secure connection</h2>
  <p class="muted">HTTPS keeps passwords private on your network. You can change any of this later on the Settings tab.</p>
  <HttpsPanel />
  <SetupNav>You can come back to this step at any time during setup.</SetupNav>
</section>
