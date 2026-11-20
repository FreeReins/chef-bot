<!-- The fields of a recipe, for adding one on the Recipes tab and editing one on its own page. -->
<script>
  const COURSES = ['Main', 'Breakfast', 'Snack', 'Sweet', 'Other'];

  /** recipe: the one being edited, or null for a new one. save(draft) receives a RecipeDraft. */
  let { recipe = null, button, save, cancel = null } = $props();

  const prefix = $derived(recipe ? `edit-${recipe.id}` : 'recipe');

  async function onsubmit(event) {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    const saved = await save({
      name: data.get('name'),
      course: data.get('course'),
      isSlowCooker: data.get('isSlowCooker') === 'on',
      servings: data.get('servings') ? Number(data.get('servings')) : null,
      ingredients: String(data.get('ingredients')).split('\n').map((line) => line.trim()).filter(Boolean),
    });
    if (saved && !recipe) form.reset();
  }
</script>

<form class="stack card" {onsubmit}>
  <label>Name <input id="{prefix}-name" name="name" required value={recipe?.name ?? ''}></label>
  <label>Course
    <select id="{prefix}-course" name="course" value={recipe?.course ?? 'Main'}>
      {#each COURSES as course (course)}<option>{course}</option>{/each}
    </select>
  </label>
  <label>Servings
    <input id="{prefix}-servings" name="servings" type="number" min="1" step="any" placeholder="4" value={recipe?.servings ?? ''}>
  </label>
  <label style="display:flex;gap:.4rem;align-items:center">
    <input id="{prefix}-slow" name="isSlowCooker" type="checkbox" checked={recipe?.isSlowCooker ?? false}> Slow cooker
  </label>
  <label>Ingredients, one per line
    <textarea id="{prefix}-ingredients" name="ingredients" required
              placeholder={'1 lb chicken thighs\n0.5 head cabbage\n2 cups brown rice'}>{recipe?.ingredients.join('\n') ?? ''}</textarea>
  </label>
  <div class="row-actions">
    <button type="submit" class="primary">{button}</button>
    {#if cancel}<button type="button" onclick={cancel}>Cancel</button>{/if}
  </div>
</form>
