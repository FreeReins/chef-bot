<!-- Recipe health: how complete the library is. Checks with recipes to fix open to list them; passing checks share one line. -->
<script>
  import { onMount } from 'svelte';
  import { app, run, isAdmin, onLive, loadRecipeHealth } from '../lib/app.svelte.js';
  import { RECIPE_ISSUES, listWords, plural } from '../lib/format.js';
  import ChevronRight from '@lucide/svelte/icons/chevron-right';
  import CircleCheck from '@lucide/svelte/icons/circle-check';
  import Sparkles from '@lucide/svelte/icons/sparkles';

  /** How many recipes a check lists before "Show all". */
  const FIRST = 20;
  /** Issues an AI model can suggest a fix for. */
  const AI_FIXES = ['NoDescription', 'NoTimes', 'NoTags', 'ThinSteps', 'MissingAmounts'];

  let health = $state(null);
  let showAll = $state({});

  const load = () => run(async () => { health = await loadRecipeHealth(); });
  onMount(() => {
    load();
    // A recipe was changed here or on another device: the report follows.
    return onLive('recipes', async () => { try { health = await loadRecipeHealth(); } catch { /* keep what is shown */ } });
  });

  const known = $derived(health?.checks.filter((check) => RECIPE_ISSUES[check.issue]) ?? []);
  const failing = $derived(known.filter((check) => check.recipes.length));
  const passing = $derived(known.filter((check) => !check.recipes.length));
  const share = $derived(health?.recipes ? health.complete / health.recipes : 0);
  const aiCanHelp = $derived(app.aiFeatures?.recipeDetails && failing.some((check) => AI_FIXES.includes(check.issue)));
</script>

<h2>Recipe health</h2>

{#if !health}
  <p class="muted" aria-busy="true">Checking the recipes…</p>
{:else if health.recipes === 0}
  <p class="muted">No recipes yet. Once there are some, this shows what each one is missing.</p>
{:else}
  <div class="card summary">
    <p><strong>{health.complete} of {plural(health.recipes, 'recipe')}</strong> {health.complete === 1 ? 'has' : 'have'} everything.</p>
    <div class="meter" role="progressbar" aria-label="Complete recipes" aria-valuemin="0" aria-valuemax={health.recipes}
         aria-valuenow={health.complete} aria-valuetext="{health.complete} of {health.recipes}">
      <span style:width="{share * 100}%"></span>
    </div>
  </div>

  {#if failing.length}
    <ul class="plain checks">
      {#each failing as check (check.issue)}
        {@const about = RECIPE_ISSUES[check.issue]}
        {@const shown = showAll[check.issue] ? check.recipes : check.recipes.slice(0, FIRST)}
        <li>
          <details>
            <summary><ChevronRight size={16} class="chevron" aria-hidden="true" /><span class="title">{about.title}</span><span class="count">{check.recipes.length}</span></summary>
            <div class="found">
              <p class="muted">{about.fix}</p>
              <ul class="plain names">
                {#each shown as recipe (recipe.id)}<li><a href="/recipes/{recipe.id}">{recipe.name}</a></li>{/each}
              </ul>
              {#if shown.length < check.recipes.length}
                <button type="button" class="small" onclick={() => (showAll[check.issue] = true)}>Show all {check.recipes.length}</button>
              {/if}
            </div>
          </details>
        </li>
      {/each}
    </ul>
  {:else}
    <p class="all-good"><CircleCheck size={18} aria-hidden="true" />Every recipe has a photo, description, times, tags, steps and ingredient amounts.</p>
  {/if}

  {#if failing.length && passing.length}
    <p class="muted passing"><CircleCheck size={16} aria-hidden="true" />No recipe is missing {listWords(passing.map((check) => RECIPE_ISSUES[check.issue].words))}.</p>
  {/if}

  {#if aiCanHelp}
    <p class="muted with-icon ai"><Sparkles size={16} aria-hidden="true" /><span>An AI model can suggest what's missing: open a recipe and choose Improve with AI.
      {#if isAdmin()}Or ask for the whole library in <a href="/settings/assistants">AI assistants</a>.{/if}</span></p>
  {/if}

  {#if health.duplicates.length || health.rareTags.length}
    <h3>Tidy up</h3>
    {#if health.duplicates.length}
      <h4>Recipes with the same name</h4>
      <ul class="plain tidy">
        {#each health.duplicates as group (group.recipes[0].id)}
          <li>{#each group.recipes as recipe, index (recipe.id)}{index ? ', ' : ''}<a href="/recipes/{recipe.id}">{recipe.name}</a>{/each}</li>
        {/each}
      </ul>
    {/if}
    {#if health.rareTags.length}
      <h4>Tags only one recipe uses</h4>
      <p class="muted">Often a near copy of another tag, such as "veggie" and "vegetarian".</p>
      <ul class="plain tidy">
        {#each health.rareTags as rare (rare.tag)}
          <li><span class="chip">#{rare.tag}</span> on <a href="/recipes/{rare.recipe.id}">{rare.recipe.name}</a></li>
        {/each}
      </ul>
    {/if}
  {/if}
{/if}

<style>
  h2 { margin-top: 0; }
  h3 { margin: 1.5rem 0 .5rem; font-size: 1.05rem; }
  h4 { margin: .9rem 0 .35rem; font-size: .95rem; }
  .summary { display: grid; gap: .5rem; padding: .9rem 1rem; }
  .summary p { margin: 0; }
  .meter { display: block; height: .5rem; border-radius: 1rem; background: var(--line); overflow: hidden; }
  .meter span { display: block; height: 100%; border-radius: inherit; background: var(--good); }
  .checks { margin: 1rem 0 0; border-top: 1px solid var(--line); }
  .checks > li { border-bottom: 1px solid var(--line); }
  summary { display: flex; align-items: center; gap: .5rem; padding: .65rem .25rem; cursor: pointer; list-style: none; }
  summary::-webkit-details-marker { display: none; }
  summary:focus-visible { outline: 2px solid var(--accent); outline-offset: -2px; }
  summary :global(.chevron) { flex: none; color: var(--muted); }
  details[open] summary :global(.chevron) { transform: rotate(90deg); }
  .title { flex: 1; }
  .count { min-width: 1.75rem; padding: .05rem .5rem; border-radius: 1rem; text-align: center; font-size: .85rem; font-weight: 600;
    background: var(--warn-bg); color: var(--ink); }
  .found { padding: 0 .25rem .8rem 1.75rem; display: grid; gap: .5rem; justify-items: start; }
  .found p { margin: 0; }
  .names { display: grid; gap: .25rem; }
  .all-good, .passing { display: flex; align-items: center; gap: .5rem; }
  .all-good { margin: 1rem 0 0; }
  .all-good :global(svg) { flex: none; color: var(--good); }
  .passing { margin: .75rem 0 0; font-size: .9rem; }
  .passing :global(svg) { flex: none; }
  .ai { margin: .75rem 0 0; font-size: .9rem; align-items: flex-start; }
  .tidy { display: grid; gap: .35rem; }
</style>
