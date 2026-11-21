<!-- The sun and moon button in the header: switches between light and dark. Shows what it switches to, so the sun is
     shown while the page is dark. Settings, Appearance can go back to following the device. -->
<script>
  import { isDark, toggleTheme } from '../lib/theme.svelte.js';
  import Sun from '@lucide/svelte/icons/sun';
  import Moon from '@lucide/svelte/icons/moon';

  const label = $derived(isDark() ? 'Switch to light mode' : 'Switch to dark mode');
</script>

<button type="button" class="icon theme" aria-label={label} title={label} onclick={toggleTheme}>
  <span class="face" class:shown={isDark()}><Sun size={20} /></span>
  <span class="face" class:shown={!isDark()}><Moon size={20} /></span>
</button>

<style>
  .theme { display: inline-grid; place-items: center; width: 2.4rem; height: 2.4rem; padding: 0; border-radius: 50%; color: var(--accent-2); }
  .theme:hover { color: var(--accent-2); background: var(--chip); }
  /* Both icons sit in the same spot; the one not shown turns away and fades. */
  .face { grid-area: 1 / 1; display: inline-flex; opacity: 0; rotate: -90deg; scale: .6; transition: opacity .2s, rotate .3s, scale .3s; }
  .face.shown { opacity: 1; rotate: 0deg; scale: 1; }
  @media (prefers-reduced-motion: reduce) { .face { transition: none; } }
</style>
