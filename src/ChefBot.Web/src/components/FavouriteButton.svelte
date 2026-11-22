<!-- The heart on a recipe: filled when it is one of the signed-in person's favourites. Pressing it adds or takes it off
     their favourites, and hands everyone's favourites back through `onchange`. -->
<script>
  import { app, run } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { favouriteOf } from '../lib/format.js';
  import Heart from '@lucide/svelte/icons/heart';

  /** recipe: { id, name, favouredBy }; size: the heart's size in pixels; label: text beside the heart, if any. */
  let { recipe, onchange, size = 18, label = '', class: className = '' } = $props();

  let saving = $state(false);
  let popped = $state(false); // just made a favourite here, so the heart pops
  const favouredBy = $derived(recipe.favouredBy ?? []);
  const mine = $derived(favouredBy.some((favourite) => favourite.userId === app.session?.user?.id));
  const others = $derived(favouredBy.filter((favourite) => favourite.userId !== app.session?.user?.id).map((favourite) => favourite.name));
  const title = $derived(favouredBy.length ? favouriteOf(favouredBy.map((favourite) => favourite.name)) : 'Not anyone\'s favourite yet');

  function toggle() {
    if (saving) return;
    saving = true;
    const adding = !mine;
    popped = false;
    run(async () => {
      const summary = await api(adding ? 'PUT' : 'DELETE', `/api/recipes/${recipe.id}/favourite`);
      onchange?.(summary.favouredBy);
      popped = adding;
    }).finally(() => (saving = false));
  }
</script>

<button type="button" class="favourite {className}" class:mine class:popped aria-pressed={mine} aria-label="Favourite {recipe.name}"
        title={others.length && !mine ? `${title}. Add it to yours too.` : title} disabled={saving} onclick={toggle}>
  <Heart {size} fill={mine ? 'currentColor' : 'none'} aria-hidden="true" />{#if label}<span>{label}</span>{/if}
</button>

<style>
  .favourite { display: inline-flex; align-items: center; gap: .35rem; color: var(--muted); }
  .favourite:hover { color: var(--bad); }
  .favourite.mine { color: var(--bad); }
  .favourite:disabled { opacity: 1; cursor: progress; }
  /* A heart beat when a recipe becomes a favourite. */
  @media (prefers-reduced-motion: no-preference) {
    .favourite.popped :global(svg) { animation: beat .45s cubic-bezier(.3, 1.6, .5, 1); }
    .favourite:disabled :global(svg) { animation: pulse .7s ease-in-out infinite; }
  }
  @keyframes beat { 0% { transform: scale(.6); } 50% { transform: scale(1.3); } 100% { transform: scale(1); } }
  @keyframes pulse { 50% { transform: scale(.85); opacity: .6; } }
</style>
