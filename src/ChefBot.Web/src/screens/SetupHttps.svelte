<script>
  import SetupNav from '../components/SetupNav.svelte';
  import HttpsPanel from '../settings/HttpsPanel.svelte';
  import HelpLink from '../components/HelpLink.svelte';
</script>

<section class="gate">
  <h2>Secure connection</h2>
  <p class="muted">HTTPS keeps passwords private on your network. You can change any of this later on the Settings tab.</p>
  <HttpsPanel />
  <p class="muted"><HelpLink page="admin/https-and-certificates">What the browser warning means, and how to trust ChefBot on your devices</HelpLink></p>
  <SetupNav>You can come back to this step at any time during setup.</SetupNav>
</section>
