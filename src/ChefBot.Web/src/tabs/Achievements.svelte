<!-- The Achievements page, opened from the award in the header: the household's chef rank with its XP bar, the first
     steps in the kitchen (KitchenQuest), then a medal per badge in bronze, silver or gold, each saying how far it has
     come and what the next tier asks for. Badges not reached yet are muted and say how to start them. Nothing here
     depends on how many people are in the household. The server says what was reached (AchievementsResponse);
     lib/achievements.js has the wording. -->
<script>
  import { onMount } from 'svelte';
  import { app, loadAchievements } from '../lib/app.svelte.js';
  import KitchenQuest from '../components/KitchenQuest.svelte';
  import { BADGES, MEDALS, RANKS, badgeName, badgeCount, badgeQuip, badgeNext, points } from '../lib/achievements.js';
  import { popIn } from '../lib/motion.js';
  import ChefHat from '@lucide/svelte/icons/chef-hat';
  import Award from '@lucide/svelte/icons/award';

  const progress = $derived(app.achievements);
  const rank = $derived(progress ? RANKS[progress.rank] : null);
  const nextRank = $derived(progress && progress.nextRankPoints != null ? RANKS[progress.rank + 1] : null);
  const rankShare = $derived(progress && nextRank
    ? (progress.points - progress.rankPoints) / (progress.nextRankPoints - progress.rankPoints)
    : 1);
  const MEDAL_NAMES = ['Bronze', 'Silver', 'Gold'];
  const earned = $derived(progress?.badges.filter((badge) => badge.tier > 0).length ?? 0);

  /** How far a badge is from its last tier to its next, for its bar. */
  function share(badge) {
    if (badge.goal == null) return 1;
    return Math.min(1, badge.progress / badge.goal);
  }

  onMount(() => { loadAchievements(); });
</script>

<section class="achievements">
  <div class="intro">
    <h2>Achievements</h2>
    <p class="muted">Ranks and badges for everything your kitchen gets up to. Nothing here ever goes down.</p>
  </div>

  {#if !progress}
    <div class="card empty">
      <span class="empty-icon" style="--area: var(--area-achievements)" aria-hidden="true"><Award size={26} /></span>
      <p class="muted">Polishing the trophies…</p>
    </div>
  {:else}
    <div class="card rank">
      <span class="hat" aria-hidden="true"><ChefHat size={26} /></span>
      <div class="rank-text">
        <h3>{rank.name}</h3>
        <p class="muted">{rank.quip}</p>
        <div class="meter" role="progressbar" aria-label="Points towards the next rank" aria-valuemin={progress.rankPoints}
             aria-valuemax={progress.nextRankPoints ?? progress.points} aria-valuenow={progress.points}
             aria-valuetext={nextRank ? `${points(progress.points)} of ${points(progress.nextRankPoints)} XP` : `${points(progress.points)} XP`}>
          <span style:width="{rankShare * 100}%"></span>
        </div>
        <p class="xp">
          {#if nextRank}{points(progress.points)} / {points(progress.nextRankPoints)} XP to {nextRank.name}{:else}{points(progress.points)} XP. The top of the brigade.{/if}
        </p>
      </div>
      <p class="count muted">{earned} of {progress.badges.length} badges</p>
    </div>

    <KitchenQuest />

    <h3 class="badges-heading">Badges</h3>
    <ul class="badges">
      {#each progress.badges as badge (badge.kind)}
        {@const next = badgeNext(badge)}
        <li class="badge {badge.tier > 0 ? MEDALS[badge.tier - 1] : 'unstarted'}">
          {#key badge.tier}
            <span class="medal" aria-hidden="true" in:popIn>{BADGES[badge.kind].emoji}</span>
          {/key}
          <div class="about">
            <strong>{badgeName(badge)}</strong>
            <span class="tier">{badge.tier > 0 ? `${MEDAL_NAMES[badge.tier - 1]}, ${badgeCount(badge)}` : badge.progress > 0 ? `On the way, ${badgeCount(badge)}` : 'Not started yet'}</span>
            <span class="muted quip">{badgeQuip(badge)}</span>
            <span class="bar" aria-hidden="true"><span style:width="{share(badge) * 100}%"></span></span>
            <span class="muted next">{next ? `${next}.` : 'Gold! Nothing left to prove.'}</span>
          </div>
        </li>
      {/each}
    </ul>
  {/if}
</section>

<style>
  .achievements { display: grid; gap: .75rem; }
  .intro h2 { margin-bottom: .25rem; }
  .intro p { margin: 0; }
  .badges-heading { margin-top: .75rem; }
  .rank { margin: 0; display: flex; flex-wrap: wrap; gap: .75rem 1rem; align-items: center; padding: 1rem 1.1rem;
    background: linear-gradient(135deg, color-mix(in srgb, var(--gold) 14%, var(--panel)), var(--panel) 65%); }
  .hat { flex: none; display: inline-grid; place-items: center; width: 3.2rem; height: 3.2rem; border-radius: 50%;
    color: var(--accent-2); background: color-mix(in srgb, var(--accent-2) 14%, var(--panel)); }
  .rank-text { flex: 1 1 14rem; min-width: 0; display: grid; gap: .25rem; }
  .rank-text h3 { font-size: 1.15rem; }
  .count { flex: none; }
  .meter, .bar { display: block; height: .5rem; border-radius: 1rem; background: var(--line); overflow: hidden; }
  .meter span, .bar span { display: block; height: 100%; border-radius: inherit; }
  .meter span { background: linear-gradient(90deg, var(--accent-2), var(--gold)); }
  .xp { margin: 0; font-size: .85rem; font-weight: 600; }
  .badges { list-style: none; margin: 0; padding: 0; display: grid; gap: .6rem;
    grid-template-columns: repeat(auto-fill, minmax(min(15rem, 100%), 1fr)); }
  .badge { --medal: var(--line); display: flex; gap: .7rem; align-items: flex-start; padding: .75rem .8rem; border-radius: .5rem;
    background: var(--panel); border: 1px solid var(--line); }
  .badge.bronze { --medal: var(--bronze); } .badge.silver { --medal: var(--silver); } .badge.gold { --medal: var(--gold); }
  .medal { flex: none; display: inline-grid; place-items: center; width: 2.8rem; height: 2.8rem; border-radius: 50%; font-size: 1.35rem;
    background: color-mix(in srgb, var(--medal) 22%, var(--panel)); box-shadow: inset 0 0 0 3px var(--medal); }
  .badge.unstarted { background: var(--bg); }
  .badge.unstarted .medal { filter: grayscale(1); opacity: .55; box-shadow: inset 0 0 0 2px var(--line); }
  .about { display: grid; gap: .15rem; min-width: 0; flex: 1; }
  .tier { font-size: .8rem; color: var(--muted); }
  .badge:not(.unstarted) .tier { color: var(--ink); font-weight: 600; }
  .quip, .next { font-size: .82rem; }
  .bar { height: .35rem; margin: .2rem 0 .05rem; }
  .bar span { background: var(--medal); }
  .badge.unstarted .bar span { background: var(--muted); }
  .badge.gold .medal { background: radial-gradient(circle at 35% 30%, color-mix(in srgb, var(--gold) 45%, var(--panel)), color-mix(in srgb, var(--gold) 18%, var(--panel))); }
  @media (prefers-reduced-motion: no-preference) {
    .meter span, .bar span { transition: width .6s cubic-bezier(.3, .9, .3, 1); }
    .badge.gold .medal { animation: shine 4s ease-in-out infinite; }
  }
  @keyframes shine { 0%, 85%, 100% { transform: none; } 90% { transform: rotate(-8deg) scale(1.08); } 95% { transform: rotate(6deg) scale(1.08); } }
</style>
