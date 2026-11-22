<!-- Stars that show a rating, not ones to press: a recipe's average (`rating`, { average, count }) with how many
     reviews it comes from, or one review's `stars`. Pressable stars are StarPicker.svelte. -->
<script>
  import { ratingText } from '../lib/app.svelte.js';
  import Star from '@lucide/svelte/icons/star';

  let { rating = null, stars = null, size = 14 } = $props();

  const value = $derived(rating ? rating.average : stars ?? 0);
  const label = $derived(rating ? ratingText(rating) : `${stars} out of 5 stars`);
  /** How full the star in each place is, to the nearest half: 1, 0.5 or 0. */
  const fill = (place) => Math.min(1, Math.max(0, Math.round((value - place + 1) * 2) / 2));
</script>

<span class="stars" role="img" aria-label={label} title={label}>
  {#each [1, 2, 3, 4, 5] as place (place)}
    {#if fill(place) === 0.5}
      <span class="half"><Star {size} aria-hidden="true" /><span class="left"><Star {size} fill="currentColor" aria-hidden="true" /></span></span>
    {:else}
      <Star {size} fill={fill(place) === 1 ? 'currentColor' : 'none'} aria-hidden="true" />
    {/if}
  {/each}
  {#if rating}<span class="figure" aria-hidden="true">{rating.average.toFixed(1)} ({rating.count})</span>{/if}
</span>

<style>
  .stars { display: inline-flex; align-items: center; gap: .05rem; color: var(--warn); white-space: nowrap; }
  /* A half star: an empty one with the left half of a full one over it. */
  .half { position: relative; display: inline-flex; }
  .left { position: absolute; inset: 0 50% 0 0; overflow: hidden; display: inline-flex; }
  .figure { margin-left: .3rem; color: var(--muted); font-size: .85em; font-variant-numeric: tabular-nums; }
</style>
