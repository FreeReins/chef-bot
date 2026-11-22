<!-- A line saying what ChefBot is busy with while something takes a moment, such as planning a week. It waits a
     little before showing, so a quick answer doesn't flash it, then steps through `lines` until `active` is false.
     The lines describe the kind of work in general: they never claim a step that may not have happened. -->
<script>
  import { rise } from '../lib/motion.js';

  /** active: whether the work is under way. lines: what to say, in turn. */
  let { active = false, lines } = $props();

  let shown = $state(false);
  let index = $state(0);

  $effect(() => {
    if (!active) { shown = false; index = 0; return undefined; }
    const show = setTimeout(() => (shown = true), 400);
    const step = setInterval(() => (index = Math.min(index + 1, lines.length - 1)), 900);
    return () => { clearTimeout(show); clearInterval(step); };
  });
</script>

<p class="working-line" role="status">
  {#if shown}
    {#key index}<span class="text" in:rise={{ duration: 200 }}><span class="dots" aria-hidden="true"><i></i><i></i><i></i></span>{lines[index]}</span>{/key}
  {/if}
</p>

<style>
  .working-line { margin: 0; align-self: center; font-size: .9rem; color: var(--muted); }
  .text { display: inline-flex; align-items: center; gap: .5rem; }
  .dots { display: inline-flex; gap: .2rem; }
  .dots i { width: .35rem; height: .35rem; border-radius: 50%; background: var(--accent); }
  @media (prefers-reduced-motion: no-preference) {
    .dots i { animation: bounce 1s ease-in-out infinite; }
    .dots i:nth-child(2) { animation-delay: .15s; }
    .dots i:nth-child(3) { animation-delay: .3s; }
  }
  @keyframes bounce { 0%, 60%, 100% { transform: translateY(0); opacity: .5; } 30% { transform: translateY(-4px); opacity: 1; } }
</style>
