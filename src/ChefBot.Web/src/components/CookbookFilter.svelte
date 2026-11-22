<!-- The Recipes tab's Cookbook filter. It shows only once a cookbook exists (or while one is chosen), so a library
     without cookbooks looks as it always did. It also takes the cookbook named by /recipes?cookbook={id}, the
     address a cookbook's name links to on a recipe's page. -->
<script>
  import { app } from '../lib/app.svelte.js';

  /** value: the chosen cookbook's id, '' for any. cookbooks: CookbookResponse[]. onchange(): the choice changed. */
  let { value = $bindable(''), cookbooks = [], onchange = () => {} } = $props();

  $effect(() => {
    if (!app.recipeCookbook) return;
    value = app.recipeCookbook;
    app.recipeCookbook = null;
    onchange();
  });
</script>

{#if cookbooks.length || value}
  <label>Cookbook
    <select id="recipe-cookbook" bind:value {onchange}>
      <option value="">Any cookbook</option>
      {#each cookbooks as cookbook (cookbook.id)}<option value={cookbook.id}>{cookbook.name} ({cookbook.recipes})</option>{/each}
    </select>
  </label>
{/if}
