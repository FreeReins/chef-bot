<!-- The fields of a recipe, for adding one in the Add a recipe dialog (blank, or started from a draft such as one read
     from a photo) and editing one from its own page. -->
<script>
  import { pending } from '../lib/pending.js';

  const COURSES = ['Main', 'Breakfast', 'Snack', 'Sweet', 'Drink', 'Other'];

  /** recipe: the one being edited, or null for a new one. draft: a RecipeDraft to start a new one from, such as one
      read from a photo. save(draft) receives a RecipeDraft. plain: no card around it, for inside a dialog. */
  let { recipe = null, draft = null, button, save, cancel = null, plain = false } = $props();

  const prefix = $derived(recipe ? `edit-${recipe.id}` : 'recipe');
  // What the fields start with: the recipe being edited, or the draft for a new one.
  const start = $derived(recipe ?? draft);

  // Opened when the recipe already has any of the extra details, so editing shows them.
  const hasDetails = $derived(Boolean(start && (start.description || start.instructions?.length || start.prepMinutes
    || start.cookMinutes || start.totalMinutes || start.sourceUrl || start.author || start.yieldText
    || start.notes || start.tags?.length)));

  const text = (data, name) => String(data.get(name) ?? '').trim() || null;
  const minutes = (data, name) => (data.get(name) ? Number(data.get(name)) : null);
  const lines = (value) => String(value ?? '').split('\n').map((line) => line.trim()).filter(Boolean);

  async function onsubmit(event) {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    const saved = await pending(event, () => save({
      name: data.get('name'),
      course: data.get('course'),
      isSlowCooker: data.get('isSlowCooker') === 'on',
      servings: data.get('servings') ? Number(data.get('servings')) : null,
      ingredients: lines(data.get('ingredients')),
      description: text(data, 'description'),
      instructions: lines(data.get('instructions')),
      prepMinutes: minutes(data, 'prepMinutes'),
      cookMinutes: minutes(data, 'cookMinutes'),
      totalMinutes: minutes(data, 'totalMinutes'),
      sourceUrl: text(data, 'sourceUrl'),
      author: text(data, 'author'),
      yieldText: text(data, 'yieldText'),
      notes: text(data, 'notes'),
      tags: String(data.get('tags') ?? '').split(',').map((tag) => tag.trim()).filter(Boolean),
    }));
    if (saved && !recipe) form.reset();
  }
</script>

<form class="stack" class:card={!plain} {onsubmit}>
  <label>Name <input id="{prefix}-name" name="name" required value={start?.name ?? ''}></label>
  <label>Course
    <select id="{prefix}-course" name="course" value={start?.course ?? 'Main'}>
      {#each COURSES as course (course)}<option>{course}</option>{/each}
    </select>
  </label>
  <label>Servings
    <input id="{prefix}-servings" name="servings" type="number" min="1" step="any" placeholder="4" value={start?.servings ?? ''}>
  </label>
  <label style="display:flex;gap:.4rem;align-items:center">
    <input id="{prefix}-slow" name="isSlowCooker" type="checkbox" checked={start?.isSlowCooker ?? false}> Slow cooker
  </label>
  <label>Ingredients, one per line
    <textarea id="{prefix}-ingredients" name="ingredients" required
              placeholder={'1 lb chicken thighs\n0.5 head cabbage\n2 cups brown rice'}>{start?.ingredients.join('\n') ?? ''}</textarea>
  </label>
  <label>Steps, one per line
    <textarea id="{prefix}-instructions" name="instructions"
              placeholder={'Brown the chicken.\nAdd the cabbage and cook until soft.'}>{start?.instructions?.join('\n') ?? ''}</textarea>
  </label>

  <details open={hasDetails}>
    <summary>More details</summary>
    <div class="stack">
      <label>Description
        <textarea id="{prefix}-description" name="description" class="short">{start?.description ?? ''}</textarea>
      </label>
      <div class="times">
        <label>Prep (min)
          <input id="{prefix}-prep" name="prepMinutes" type="number" min="0" max="10080" step="1" value={start?.prepMinutes ?? ''}>
        </label>
        <label>Cook (min)
          <input id="{prefix}-cook" name="cookMinutes" type="number" min="0" max="10080" step="1" value={start?.cookMinutes ?? ''}>
        </label>
        <label>Total (min)
          <input id="{prefix}-total" name="totalMinutes" type="number" min="0" max="10080" step="1" value={start?.totalMinutes ?? ''}>
        </label>
      </div>
      <label>Makes <input id="{prefix}-yield" name="yieldText" placeholder="one 9-inch pie" value={start?.yieldText ?? ''}></label>
      <label>Tags, separated by commas
        <input id="{prefix}-tags" name="tags" placeholder="quick, vegetarian" value={start?.tags?.join(', ') ?? ''}>
      </label>
      <label>Source link
        <input id="{prefix}-source" name="sourceUrl" type="url" placeholder="https://" value={start?.sourceUrl ?? ''}>
      </label>
      <label>Author <input id="{prefix}-author" name="author" value={start?.author ?? ''}></label>
      <label>Notes
        <textarea id="{prefix}-notes" name="notes" class="short">{start?.notes ?? ''}</textarea>
      </label>
    </div>
  </details>

  <div class="row-actions">
    <button type="submit" class="primary">{button}</button>
    {#if cancel}<button type="button" onclick={cancel}>Cancel</button>{/if}
  </div>
</form>

<style>
  summary { cursor: pointer; font-weight: 600; margin-bottom: .5rem; }
  .times { display: grid; gap: .5rem; grid-template-columns: repeat(auto-fit, minmax(7rem, 1fr)); }
  textarea.short { min-height: 4rem; }
</style>
