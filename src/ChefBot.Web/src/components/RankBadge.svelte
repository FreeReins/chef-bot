<!-- The award in the header: the household's chef rank and points, linking to the Achievements page. It loads the
     achievements as the app opens, so anything reached since this viewer last looked is congratulated straight away.
     On phones only the award and the points show; the rank is still read out. lib/achievements.js has the rank names. -->
<script>
  import { onMount } from 'svelte';
  import { app, loadAchievements } from '../lib/app.svelte.js';
  import { RANKS, points } from '../lib/achievements.js';
  import Award from '@lucide/svelte/icons/award';

  const progress = $derived(app.achievements);
  const here = $derived(app.tab === 'achievements');

  onMount(() => { loadAchievements(); });
</script>

<a class="rank" href="/achievements" aria-current={here ? 'page' : undefined} title="Achievements">
  <Award size={18} aria-hidden="true" />
  <span class="sr-only">Achievements{progress ? ':' : ''}</span>
  {#if progress}
    <span class="name">{RANKS[progress.rank].name}</span>
    <span class="xp">{points(progress.points)} XP</span>
  {/if}
</a>

<style>
  .rank { display: inline-flex; align-items: center; gap: .4rem; padding: .3rem .75rem .3rem .55rem; border-radius: 1rem;
    font-size: .85rem; text-decoration: none; color: var(--ink); white-space: nowrap;
    background: color-mix(in srgb, var(--gold) 14%, var(--panel)); border: 1px solid color-mix(in srgb, var(--gold) 45%, var(--line)); }
  .rank :global(svg) { flex: none; color: var(--accent-2); }
  .rank:hover, .rank[aria-current="page"] { border-color: var(--accent-2); }
  .rank[aria-current="page"] { font-weight: 600; }
  .rank:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }
  .name { font-weight: 600; }
  .xp { color: var(--muted); }
  @media (max-width: 40rem) {
    .rank { gap: .25rem; padding: .25rem .55rem .25rem .4rem; }
    /* Read out, but not shown: phones have room for the award and the points only. */
    .name { position: absolute; width: 1px; height: 1px; overflow: hidden; clip-path: inset(50%); white-space: nowrap; }
  }
  @media (prefers-reduced-motion: no-preference) {
    .rank:hover :global(svg) { animation: wobble .5s ease-in-out; }
  }
  @keyframes wobble { 30% { transform: rotate(-14deg); } 65% { transform: rotate(10deg); } }
</style>
