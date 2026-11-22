<!-- The way to plan a week: choose a plan style, then plan it as a draft to review. Shown when nothing is planned,
     and under a confirmed week until the week after it has a plan. -->
<script>
  import { app, isAdmin, planWeek, nextWeekStart } from '../lib/app.svelte.js';
  import { addDays, shortDate } from '../lib/format.js';
  import { choosableStyles, defaultStyle, styleSummary } from '../lib/styles.js';
  import { pendingClick } from '../lib/pending.js';
  import WorkingLine from '../components/WorkingLine.svelte';
  import Sparkles from '@lucide/svelte/icons/sparkles';

  /** title: the card's heading. intro: a sentence under it saying what planning does. */
  let { title, intro } = $props();

  const choices = $derived(choosableStyles(app.styles));
  let chosenName = $state(null);
  const chosen = $derived(choices.find((style) => style.name === chosenName) ?? defaultStyle(app.styles));
  const weekStart = $derived(nextWeekStart());
  const noRecipes = $derived(app.recipeCount === 0);

  /** What planning looks like while it takes a moment. */
  const PLANNING = ['Looking through your recipes…', 'Checking your pantry…', 'Balancing the week…', 'Writing the shopping list…'];
  let planning = $state(false);
  const plan = pendingClick(async () => {
    planning = true;
    try { await planWeek(chosen, weekStart); } finally { planning = false; }
  });
</script>

<section class="card plan-next" aria-labelledby="plan-next-title">
  <h2 id="plan-next-title">{title}</h2>
  <p class="muted">{intro}</p>
  {#if noRecipes}
    <p>There are no recipes to plan with yet. Add some first.</p>
    <div class="actions">
      {#if isAdmin()}<a class="button primary-link" href="/recipes/samples">Add sample recipes</a>{/if}
      <a class="button" href="/recipes">Go to Recipes</a>
    </div>
  {:else}
    <div class="choose">
      {#if choices.length > 1}
        <label>Plan style
          <select value={chosen?.name} disabled={planning} onchange={(event) => (chosenName = event.currentTarget.value)}>
            {#each choices as style (style.name)}<option value={style.name}>{style.name}</option>{/each}
          </select>
        </label>
      {/if}
      <button class="primary with-icon" onclick={plan}>
        {#if !planning}<Sparkles size={16} aria-hidden="true" />{/if}Plan {shortDate(weekStart)} to {shortDate(addDays(weekStart, 6))}
      </button>
      <WorkingLine active={planning} lines={PLANNING} />
    </div>
    {#if chosen}
      <p class="muted style-line"><strong>{chosen.name}:</strong> {chosen.description || styleSummary(chosen)}</p>
    {/if}
    <p class="muted">
      You get a draft to review: move meals between days, swap any you don't want, and check the shopping list before
      you confirm. <a href="/settings/styles">Change plan styles</a>
    </p>
  {/if}
</section>

<style>
  .plan-next { display: grid; gap: .6rem; padding: 1.25rem; }
  .plan-next h2 { margin: 0; }
  .plan-next p { margin: 0; }
  .choose { display: flex; flex-wrap: wrap; gap: .75rem; align-items: end; }
  .style-line { font-size: .9rem; }
  .with-icon { display: inline-flex; align-items: center; gap: .4rem; }
  .with-icon :global(svg) { flex: none; }
  @media (prefers-reduced-motion: no-preference) {
    .with-icon:hover :global(svg) { animation: twinkle .6s ease-in-out; }
  }
  @keyframes twinkle { 50% { transform: rotate(15deg) scale(1.15); } }
  a.primary-link { background: var(--accent); color: var(--accent-ink); border-color: var(--accent); }
</style>
