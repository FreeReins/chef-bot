<!-- Improve with AI, /recipes/{id}/improve, in a dialog over a recipe's page: what the AI model set up for completing
     recipes suggests the recipe is missing, one change per row with a tick box, so each can be taken or left. Asking
     again replaces the suggestions; nothing in the recipe changes until Apply. Suggestions made by the library sweep
     (Settings, AI assistants) open here too. -->
<script>
  import { app, run, toast, loadRecipes } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import { pending, pendingClick } from '../lib/pending.js';
  import Modal from '../components/Modal.svelte';
  import Sparkles from '@lucide/svelte/icons/sparkles';

  /** recipe: the RecipeResponse. onsaved(recipe): it came back changed. onclose(): asked to close. */
  let { recipe, onsaved, onclose } = $props();

  const FIELDS = {
    Description: 'Description',
    YieldText: 'Makes',
    PrepMinutes: 'Prep time',
    CookMinutes: 'Cook time',
    TotalMinutes: 'Total time',
    Tags: 'Tags',
    Instructions: 'Steps',
    IngredientAmount: 'Ingredient amount',
    NewIngredient: 'Missing ingredient',
    Notes: 'Tips for the notes',
  };
  const MINUTES = new Set(['PrepMinutes', 'CookMinutes', 'TotalMinutes']);

  /** RecipeSuggestionResponse, or null when there are none (or none to show) yet. */
  let suggestion = $state(null);
  let loaded = $state(false);
  let ticked = $state({});

  const usable = $derived(suggestion?.status === 'Waiting' && !suggestion.stale);
  const chosen = $derived(Object.entries(ticked).filter(([, on]) => on).map(([id]) => Number(id)));

  function show(found) {
    suggestion = found;
    ticked = Object.fromEntries((found?.changes ?? []).map((change) => [change.id, true]));
  }

  run(async () => {
    try {
      show(await api('GET', `/api/recipes/${recipe.id}/suggestion`));
    } catch (error) {
      if (error.status !== 404) throw error;
    } finally {
      loaded = true;
    }
  });

  const value = (change, list) => (MINUTES.has(change.field) && list.length ? `${list[0]} min` : list.join(change.field === 'Tags' ? ', ' : '\n'));

  const askAgain = () => run(async () => {
    show(await api('POST', `/api/recipes/${recipe.id}/suggestion`));
    if (suggestion.status === 'NothingToAdd') toast(`${suggestion.modelName} had nothing to add to ${recipe.name}.`);
  });

  function apply(event) {
    event.preventDefault();
    return pending(event, () => run(async () => {
      const saved = await api('POST', `/api/recipes/${recipe.id}/suggestion/apply`, { changeIds: chosen });
      onsaved(saved);
      await loadRecipes();
      toast(`${recipe.name} was updated with ${chosen.length === 1 ? 'one suggestion' : `${chosen.length} suggestions`}.`);
      await onclose();
    }));
  }

  const dismiss = () => run(async () => {
    await api('DELETE', `/api/recipes/${recipe.id}/suggestion`);
    onsaved({ ...recipe, hasSuggestion: false });
    toast(`${recipe.name} was kept as it is.`);
    await onclose();
  });
</script>

<Modal wide title="Improve {recipe.name}" {onclose}>
  {#if !loaded}
    <p class="muted" role="status">Looking for suggestions…</p>
  {:else if !app.aiFeatures.recipeDetails && !usable}
    <p>Suggestions need an AI model. An admin can set one up under Settings, AI assistants.</p>
    <div class="row-actions"><button type="button" onclick={onclose}>Close</button></div>
  {:else if usable}
    <form class="stack" onsubmit={apply}>
      <p class="muted">
        {suggestion.modelName} suggests these. Untick any you don't want; nothing already in the recipe is replaced,
        except short steps with clearer ones.
      </p>
      <ul class="changes">
        {#each suggestion.changes as change (change.id)}
          <li class="card change">
            <label class="pick">
              <input type="checkbox" bind:checked={ticked[change.id]}>
              <strong>{FIELDS[change.field] ?? change.field}{change.line != null ? `, line ${change.line + 1}` : ''}</strong>
            </label>
            <div class="compare">
              {#if change.field === 'Notes'}
                <div><span class="muted label">Added to the end</span><p class="text">{value(change, change.suggested)}</p></div>
              {:else if change.field === 'Instructions'}
                <div><span class="muted label">Now</span>
                  {#if change.current.length}<ol>{#each change.current as step, index (index)}<li>{step}</li>{/each}</ol>{:else}<p class="muted">No steps</p>{/if}
                </div>
                <div><span class="muted label">Suggested</span><ol>{#each change.suggested as step, index (index)}<li>{step}</li>{/each}</ol></div>
              {:else}
                <div><span class="muted label">Now</span><p class="text" class:muted={!change.current.length}>{change.current.length ? value(change, change.current) : 'Empty'}</p></div>
                <div><span class="muted label">Suggested</span><p class="text">{value(change, change.suggested)}</p></div>
              {/if}
            </div>
          </li>
        {/each}
      </ul>
      <div class="row-actions">
        <button type="submit" class="primary" disabled={chosen.length === 0}>Apply {chosen.length === 1 ? 'one change' : `${chosen.length} changes`}</button>
        <button type="button" onclick={pendingClick(dismiss)}>Keep the recipe as it is</button>
        <button type="button" class="with-icon" onclick={pendingClick(askAgain)}><Sparkles size={16} />Ask again</button>
      </div>
    </form>
  {:else}
    <div class="stack">
      {#if suggestion?.stale}
        <p>The recipe changed after the last suggestions were made, so they no longer fit. Ask for new ones.</p>
      {:else if suggestion?.status === 'NothingToAdd'}
        <p>{suggestion.modelName} had nothing to add last time it looked.</p>
      {:else}
        <p>An AI model can suggest what this recipe is missing: a description, times, tags, ingredient amounts, clearer
          steps and tips. You choose what to keep; nothing changes until you apply it.</p>
      {/if}
      <p class="muted">The recipe is sent to the AI model set up for completing recipes. It can take up to a minute.</p>
      <div class="row-actions">
        <button type="button" class="primary with-icon" onclick={pendingClick(askAgain)}><Sparkles size={16} />Ask for suggestions</button>
        <button type="button" onclick={onclose}>Close</button>
      </div>
    </div>
  {/if}
</Modal>

<style>
  .changes { list-style: none; margin: 0; padding: 0; display: grid; gap: .5rem; max-height: 60vh; overflow: auto; }
  .change { display: grid; gap: .4rem; }
  .pick { display: flex; gap: .5rem; align-items: center; }
  .compare { display: grid; gap: .5rem 1rem; grid-template-columns: repeat(auto-fit, minmax(min(14rem, 100%), 1fr)); }
  .compare p, .compare ol { margin: .15rem 0 0; }
  .compare ol { padding-left: 1.25rem; }
  .label { font-size: .8rem; text-transform: uppercase; letter-spacing: .04em; }
  .text { white-space: pre-line; }
</style>
