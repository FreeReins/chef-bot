<script>
  import { app, run, isAdmin, openSamples, importFromMealie, loadRecipes } from '../lib/app.svelte.js';
  import { api } from '../lib/api.js';
  import Chip from '../components/Chip.svelte';

  const COURSES = ['Main', 'Breakfast', 'Snack', 'Sweet', 'Other'];

  const groups = $derived(COURSES
    .map((course) => ({ course, recipes: app.recipes.filter((recipe) => recipe.course === course) }))
    .filter((group) => group.recipes.length > 0));

  function add(event) {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    run(async () => {
      await api('POST', '/api/recipes', {
        name: data.get('name'),
        course: data.get('course'),
        isSlowCooker: data.get('isSlowCooker') === 'on',
        servings: data.get('servings') ? Number(data.get('servings')) : null,
        ingredients: String(data.get('ingredients')).split('\n').map((line) => line.trim()).filter(Boolean),
      });
      form.reset();
      await loadRecipes();
    });
  }
</script>

<div class="columns">
  <section>
    <div class="row">
      <h2>Recipes ({app.recipes.length})</h2>
      <div class="row-actions">
        {#if isAdmin()}
          <button class="small" onclick={() => run(openSamples)}>Browse sample recipes</button>
        {/if}
        {#if app.session.mealieConfigured}
          <button class="small" onclick={importFromMealie}>Import from Mealie</button>
        {/if}
      </div>
    </div>
    {#each groups as { course, recipes } (course)}
      <div class="card">
        <h3>{course} ({recipes.length})</h3>
        <ul class="plain">
          {#each recipes as recipe (recipe.id)}
            <li>
              <strong>{recipe.name}</strong>
              {#if recipe.isSlowCooker}<Chip tone="good">Slow cooker</Chip>{/if}
              {#if recipe.origin === 'Mealie'}<Chip>From Mealie</Chip>{/if}
              <p class="muted">{recipe.ingredients.join(', ')}</p>
            </li>
          {/each}
        </ul>
      </div>
    {/each}
  </section>
  <section>
    <h2>Add a recipe</h2>
    <form class="stack card" onsubmit={add}>
      <label>Name <input id="recipe-name" name="name" required></label>
      <label>Course
        <select id="recipe-course" name="course">
          {#each COURSES as course (course)}<option>{course}</option>{/each}
        </select>
      </label>
      <label>Servings <input id="recipe-servings" name="servings" type="number" min="1" step="any" placeholder="4"></label>
      <label style="display:flex;gap:.4rem;align-items:center">
        <input id="recipe-slow" name="isSlowCooker" type="checkbox"> Slow cooker
      </label>
      <label>Ingredients, one per line
        <textarea id="recipe-ingredients" name="ingredients" required
                  placeholder={'1 lb chicken thighs\n0.5 head cabbage\n2 cups brown rice'}></textarea>
      </label>
      <button type="submit" class="primary">Save recipe</button>
    </form>
  </section>
</div>
