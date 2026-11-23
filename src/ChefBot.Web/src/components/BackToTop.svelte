<!-- A round button in the bottom corner that goes back to the top of the page. It shows only while `watch` (something at
     the top, such as the header or a menu button) is scrolled out of view. Pressing it scrolls up and moves focus to
     `focus()` when given, otherwise to the view's first visible heading, so keyboard and screen reader users land at
     the top too. Used by App.svelte on the long tabs and by Settings for its phone menu. -->
<script>
  import { app } from '../lib/app.svelte.js';
  import { popIn } from '../lib/motion.js';
  import ArrowUp from '@lucide/svelte/icons/arrow-up';

  let { watch, focus = null, label = 'Back to the top' } = $props();

  let hidden = $state(false);
  $effect(() => {
    if (!watch || !globalThis.IntersectionObserver) return;
    const watcher = new IntersectionObserver(([entry]) => { hidden = !entry.isIntersecting; });
    watcher.observe(watch);
    return () => { watcher.disconnect(); hidden = false; };
  });

  function backToTop() {
    const still = globalThis.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
    window.scrollTo({ top: 0, behavior: still ? 'auto' : 'smooth' });
    const target = focus ? focus() : [...document.querySelectorAll('#view h2')].find((heading) => heading.offsetParent);
    if (!target) return;
    if (!focus) target.tabIndex = -1;
    target.focus({ preventScroll: true });
  }
</script>

<!-- Messages (Toasts.svelte) use the same corner, so it steps aside while one is showing. -->
{#if hidden && !app.toasts.length}
  <button type="button" class="to-top no-print" aria-label={label} title={label} onclick={backToTop}
          transition:popIn={{ duration: 200 }}><ArrowUp size={20} /></button>
{/if}

<style>
  /* Above the footer pinned to the bottom of the window, and on phones above the tab bar pinned there (App.svelte). */
  .to-top { position: fixed; right: 1rem; bottom: calc(3.75rem + env(safe-area-inset-bottom)); z-index: 9;
    display: inline-grid; place-items: center; width: 2.9rem; height: 2.9rem; padding: 0; border-radius: 50%;
    color: var(--accent-ink); background: var(--accent); border-color: var(--accent); box-shadow: 0 4px 14px rgb(0 0 0 / .25); }
  .to-top:hover { filter: brightness(1.08); }
  @media (max-width: 40rem) { .to-top { bottom: calc(4.75rem + env(safe-area-inset-bottom)); } }
</style>
