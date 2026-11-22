<!-- Five stars to press, for giving a recipe 1 to 5. Pressing one hands its number to `onpick`; the stars up to the
     one under the pointer light up first, so it is clear what a press will give. -->
<script>
  import Star from '@lucide/svelte/icons/star';

  /** value: the stars chosen now, or null. label: what is being rated, for the buttons' names. */
  let { value = null, label, onpick, size = 28, disabled = false } = $props();

  let hovered = $state(null);
  const lit = $derived(hovered ?? value ?? 0);
</script>

<div class="picker" role="group" aria-label="Rate {label}" onpointerleave={() => (hovered = null)}>
  {#each [1, 2, 3, 4, 5] as place (place)}
    <button type="button" class="star" class:lit={place <= lit} aria-label="{place} star{place === 1 ? '' : 's'}"
            aria-pressed={value === place} {disabled} onpointerenter={() => (hovered = place)} onclick={() => onpick(place)}>
      <Star {size} fill={place <= lit ? 'currentColor' : 'none'} aria-hidden="true" />
    </button>
  {/each}
</div>

<style>
  .picker { display: inline-flex; gap: .1rem; }
  .star { display: inline-flex; padding: .2rem; border: 0; background: none; color: var(--muted); }
  .star.lit { color: var(--warn); }
  .star:disabled { opacity: 1; cursor: progress; }
  @media (prefers-reduced-motion: no-preference) {
    .star { transition: transform .12s; }
    .star:hover { transform: scale(1.15); }
  }
</style>
