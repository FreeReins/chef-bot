<!-- Settings, Appearance: light, dark, or the device's setting. The sun and moon button in the header switches
     between light and dark; this is where to go back to following the device. Kept in this browser only. -->
<script>
  import { theme, setTheme } from '../lib/theme.svelte.js';
  import Sun from '@lucide/svelte/icons/sun';
  import Moon from '@lucide/svelte/icons/moon';
  import Monitor from '@lucide/svelte/icons/monitor';

  const CHOICES = [
    { value: 'light', label: 'Light', icon: Sun },
    { value: 'dark', label: 'Dark', icon: Moon },
    { value: null, label: 'Match this device', icon: Monitor },
  ];
</script>

<h2>Appearance</h2>
<div class="card">
  <div class="segmented" role="group" aria-label="Colour theme">
    {#each CHOICES as choice (choice.label)}
      <button type="button" aria-pressed={theme.choice === choice.value} onclick={() => setTheme(choice.value)}>
        <choice.icon size={16} />{choice.label}
      </button>
    {/each}
  </div>
  <p class="muted note">
    {theme.choice === null
      ? `Following this device, which is set to ${theme.deviceDark ? 'dark' : 'light'} now.`
      : 'Kept in this browser only. Other devices and browsers follow their own setting.'}
  </p>
</div>

<style>
  .segmented button { display: inline-flex; align-items: center; gap: .4rem; font-size: .95rem; padding: .35rem .85rem; }
  .note { margin-top: .6rem; }
</style>
